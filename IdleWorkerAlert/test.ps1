param(
    [string]$GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Timberborn",
    [string]$ModSettingsDirectory = "C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3283831040\version-1.1\Scripts",
    [Parameter(Mandatory = $true)]
    [string]$MonoCecilPath
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$modDirectory = Join-Path $projectDirectory "mod\IdleWorkerAlert"
$managedDirectory = Join-Path $GameDirectory "Timberborn_Data\Managed"
$manifestPath = Join-Path $modDirectory "manifest.json"
$assemblyPath = Join-Path $modDirectory "Scripts\Local.IdleWorkerAlert.dll"
$localizationPath = Join-Path $modDirectory "Localizations\enUS.csv"

if (-not (Test-Path -LiteralPath $MonoCecilPath -PathType Leaf)) {
    throw "Mono.Cecil assembly not found: $MonoCecilPath"
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Id -ne "gem0303.IdleWorkerAlert") {
    throw "Unexpected mod id: $($manifest.Id)"
}
if ($manifest.Version -ne "0.2.0") {
    throw "Unexpected mod version: $($manifest.Version)"
}
if ($manifest.RequiredMods.Id -notcontains "eMka.ModSettings") {
    throw "Mod Settings is not declared as a required dependency."
}
$modSettingsDependency = $manifest.RequiredMods |
    Where-Object { $_.Id -eq "eMka.ModSettings" } |
    Select-Object -First 1
if ($modSettingsDependency.MinimumVersion -ne "1.1.1.0") {
    throw "Unexpected minimum Mod Settings version: $($modSettingsDependency.MinimumVersion)"
}

foreach ($requiredPath in @($assemblyPath, $localizationPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Missing required file: $requiredPath"
    }
}

$localization = Import-Csv -LiteralPath $localizationPath
$expectedKeys = @(
    "Local.IdleWorkerAlert.Panel",
    "Local.IdleWorkerAlert.Status",
    "Local.IdleWorkerAlert.Alert",
    "Local.IdleWorkerAlert.Settings.Header",
    "Local.IdleWorkerAlert.Settings.GracePeriod",
    "Local.IdleWorkerAlert.Settings.GracePeriodTooltip"
)
foreach ($key in $expectedKeys) {
    if ($key -notin $localization.ID) {
        throw "Missing localization key: $key"
    }
}

Add-Type -Path $MonoCecilPath
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($managedDirectory)
$resolver.AddSearchDirectory($ModSettingsDirectory)
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath, $parameters)

try {
    $configurator = $assembly.MainModule.GetType("Local.IdleWorkerAlert.IdleWorkerAlertConfigurator")
    $monitor = $assembly.MainModule.GetType("Local.IdleWorkerAlert.IdleWorkerMonitor")
    $fragment = $assembly.MainModule.GetType("Local.IdleWorkerAlert.IdleWorkerFragment")
    $settings = $assembly.MainModule.GetType("Local.IdleWorkerAlert.IdleWorkerSettings")
    $mainMenuConfigurator = $assembly.MainModule.GetType("Local.IdleWorkerAlert.IdleWorkerAlertMainMenuConfigurator")

    if ($null -eq $configurator -or $null -eq $monitor -or $null -eq $fragment -or
        $null -eq $settings -or $null -eq $mainMenuConfigurator) {
        throw "Compiled assembly is missing a required mod type."
    }

    $defaultGracePeriod = $settings.Fields |
        Where-Object { $_.Name -eq "DefaultGracePeriodInHours" }
    if ($null -eq $defaultGracePeriod -or [float]$defaultGracePeriod.Constant -ne 1.0) {
        throw "Compiled default idle grace period is not 1 in-game hour."
    }

    $context = $configurator.CustomAttributes |
        Where-Object { $_.AttributeType.FullName -eq "Bindito.Core.ContextAttribute" }
    if ($context.ConstructorArguments[0].Value -ne "Game") {
        throw "Configurator is not registered for the Game context."
    }

    $mainMenuContext = $mainMenuConfigurator.CustomAttributes |
        Where-Object { $_.AttributeType.FullName -eq "Bindito.Core.ContextAttribute" }
    if ($mainMenuContext.ConstructorArguments[0].Value -ne "MainMenu") {
        throw "Settings configurator is not registered for the MainMenu context."
    }

    $missingReferences = @()
    foreach ($reference in $assembly.MainModule.AssemblyReferences) {
        if ($reference.Name -in @("mscorlib", "netstandard")) {
            continue
        }

        $gameReference = Join-Path $managedDirectory ($reference.Name + ".dll")
        $modSettingsReference = Join-Path $ModSettingsDirectory ($reference.Name + ".dll")
        if (-not (Test-Path -LiteralPath $gameReference) -and
            -not (Test-Path -LiteralPath $modSettingsReference)) {
            $missingReferences += $reference.FullName
        }
    }

    if ($missingReferences.Count -gt 0) {
        throw "Missing game assembly references: $($missingReferences -join '; ')"
    }

    [pscustomobject]@{
        Mod = $manifest.Name
        Version = $manifest.Version
        Assembly = $assembly.Name.Name
        LocalizationKeys = $localization.Count
        GameReferencesValidated = $assembly.MainModule.AssemblyReferences.Count
        Result = "PASS"
    }
}
finally {
    $assembly.Dispose()
    $resolver.Dispose()
}
