param(
    [string]$GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Timberborn",
    [string]$ModSettingsDirectory = "C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3283831040\version-1.1\Scripts"
)

$ErrorActionPreference = "Stop"

$projectDirectory = Split-Path -Parent $MyInvocation.MyCommand.Path
$sourceDirectory = Join-Path $projectDirectory "src"
$outputDirectory = Join-Path $projectDirectory "mod\IdleWorkerAlert\Scripts"
$managedDirectory = Join-Path $GameDirectory "Timberborn_Data\Managed"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputAssembly = Join-Path $outputDirectory "Local.IdleWorkerAlert.dll"

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "C# compiler not found: $compiler"
}

if (-not (Test-Path -LiteralPath $managedDirectory)) {
    throw "Timberborn managed assemblies not found: $managedDirectory"
}

$modSettingsCore = Join-Path $ModSettingsDirectory "ModSettings.Core.dll"
if (-not (Test-Path -LiteralPath $modSettingsCore -PathType Leaf)) {
    throw "Mod Settings assembly not found: $modSettingsCore"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$references = @(
    "mscorlib.dll",
    "netstandard.dll",
    "System.dll",
    "System.Core.dll",
    "Bindito.Core.dll",
    "Timberborn.BaseComponentSystem.dll",
    "Timberborn.BehaviorSystem.dll",
    "Timberborn.BlockSystem.dll",
    "Timberborn.BlueprintSystem.dll",
    "Timberborn.BuilderHubSystem.dll",
    "Timberborn.Buildings.dll",
    "Timberborn.Common.dll",
    "Timberborn.CoreUI.dll",
    "Timberborn.EntityPanelSystem.dll",
    "Timberborn.EntitySystem.dll",
    "Timberborn.EnterableSystem.dll",
    "Timberborn.Hauling.dll",
    "Timberborn.Localization.dll",
    "Timberborn.Modding.dll",
    "Timberborn.SettingsSystem.dll",
    "Timberborn.SingletonSystem.dll",
    "Timberborn.StatusSystem.dll",
    "Timberborn.DuplicationSystem.dll",
    "Timberborn.RelationSystem.dll",
    "Timberborn.TemplateInstantiation.dll",
    "Timberborn.TickSystem.dll",
    "Timberborn.TimeSystem.dll",
    "Timberborn.WorkSystem.dll",
    "Timberborn.WorldPersistence.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.UIElementsModule.dll"
) | ForEach-Object { "/reference:$managedDirectory\$_" }

$references += "/reference:$modSettingsCore"

$sources = Get-ChildItem -LiteralPath $sourceDirectory -File -Filter "*.cs" |
    ForEach-Object { $_.FullName }

& $compiler /nologo /noconfig /target:library /optimize+ /nostdlib+ "/out:$outputAssembly" $references $sources
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Built $outputAssembly"
