param(
    [string]$GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Timberborn",
    [string]$HarmonyDirectory = "C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3284904751",
    [Parameter(Mandatory = $true)]
    [string]$MonoCecilPath
)

$ErrorActionPreference = "Stop"

$projectDirectory = $PSScriptRoot
$modDirectory = Join-Path $projectDirectory "mod\GuidelineNumbers"
$managedDirectory = Join-Path $GameDirectory "Timberborn_Data\Managed"
$manifestPath = Join-Path $modDirectory "manifest.json"
$assemblyPath = Join-Path $modDirectory "Scripts\Local.GuidelineNumbers.dll"
$harmonyAssembly = Join-Path $HarmonyDirectory "0Harmony.dll"
$guidelinesAssemblyPath = Join-Path $managedDirectory "Timberborn.ConstructionGuidelines.dll"

foreach ($requiredPath in @($MonoCecilPath, $manifestPath, $assemblyPath, $harmonyAssembly, $guidelinesAssemblyPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Missing required file: $requiredPath"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.Id -ne "gem0303.GuidelineNumbers") {
    throw "Unexpected mod id: $($manifest.Id)"
}
if ($manifest.Version -ne "0.1.0") {
    throw "Unexpected mod version: $($manifest.Version)"
}
if ($manifest.MinimumGameVersion -ne "1.1.2.7") {
    throw "Unexpected minimum game version: $($manifest.MinimumGameVersion)"
}

$harmonyDependency = $manifest.RequiredMods |
    Where-Object { $_.Id -eq "Harmony" } |
    Select-Object -First 1
if ($null -eq $harmonyDependency -or $harmonyDependency.MinimumVersion -ne "2.4.1") {
    throw "Harmony 2.4.1 is not declared as the minimum required dependency."
}

Add-Type -Path $MonoCecilPath
$resolver = [Mono.Cecil.DefaultAssemblyResolver]::new()
$resolver.AddSearchDirectory($managedDirectory)
$resolver.AddSearchDirectory($HarmonyDirectory)
$parameters = [Mono.Cecil.ReaderParameters]::new()
$parameters.AssemblyResolver = $resolver
$assembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($assemblyPath, $parameters)
$guidelinesAssembly = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($guidelinesAssemblyPath)

try {
    $requiredTypes = @(
        "Local.GuidelineNumbers.GuidelineNumbersModStarter",
        "Local.GuidelineNumbers.GuidelineNumbersConfigurator",
        "Local.GuidelineNumbers.GuidelineNumbersService",
        "Local.GuidelineNumbers.CursorGuidelinesUpdatedPatch",
        "Local.GuidelineNumbers.GuidelinesClearedPatch"
    )
    foreach ($typeName in $requiredTypes) {
        if ($null -eq $assembly.MainModule.GetType($typeName)) {
            throw "Compiled assembly is missing required type: $typeName"
        }
    }

    $configurator = $assembly.MainModule.GetType("Local.GuidelineNumbers.GuidelineNumbersConfigurator")
    $context = $configurator.CustomAttributes |
        Where-Object { $_.AttributeType.FullName -eq "Bindito.Core.ContextAttribute" }
    if ($context.ConstructorArguments[0].Value -ne "Game") {
        throw "Configurator is not registered for the Game context."
    }

    $renderer = $guidelinesAssembly.MainModule.GetType(
        "Timberborn.ConstructionGuidelines.ConstructionGuidelinesRenderingService")
    $crossParameters = $guidelinesAssembly.MainModule.GetType(
        "Timberborn.ConstructionGuidelines.CrossParameters")
    if ($null -eq $renderer -or $null -eq $crossParameters) {
        throw "Game assembly is missing the expected construction-guideline types."
    }
    if (@($renderer.Methods | Where-Object { $_.Name -eq "GetGuidelinesFromMousePosition" }).Count -ne 1) {
        throw "Expected exactly one cursor-guidelines target method."
    }

    foreach ($fieldName in @("_tilesAtSameLevel", "_tilesBelow", "_tilesAbove", "_lastCrossParameters")) {
        if ($fieldName -notin $renderer.Fields.Name) {
            throw "Game assembly is missing required field: $fieldName"
        }
    }
    if ($null -eq ($crossParameters.Methods | Where-Object { $_.Name -eq "Reset" }) -or
        $null -eq ($crossParameters.Properties | Where-Object { $_.Name -eq "Center" })) {
        throw "Game assembly is missing the expected cross-parameter members."
    }

    $missingReferences = @()
    foreach ($reference in $assembly.MainModule.AssemblyReferences) {
        if ($reference.Name -in @("mscorlib", "netstandard")) {
            continue
        }

        $gameReference = Join-Path $managedDirectory ($reference.Name + ".dll")
        $harmonyReference = Join-Path $HarmonyDirectory ($reference.Name + ".dll")
        if (-not (Test-Path -LiteralPath $gameReference) -and
            -not (Test-Path -LiteralPath $harmonyReference)) {
            $missingReferences += $reference.FullName
        }
    }

    if ($missingReferences.Count -gt 0) {
        throw "Missing assembly references: $($missingReferences -join '; ')"
    }

    [pscustomobject]@{
        Mod = $manifest.Name
        Version = $manifest.Version
        Assembly = $assembly.Name.Name
        GameReferencesValidated = $assembly.MainModule.AssemblyReferences.Count
        Result = "PASS"
    }
}
finally {
    $assembly.Dispose()
    $guidelinesAssembly.Dispose()
    $resolver.Dispose()
}
