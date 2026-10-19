param(
    [string]$GameDirectory = "C:\Program Files (x86)\Steam\steamapps\common\Timberborn",
    [string]$HarmonyDirectory = "C:\Program Files (x86)\Steam\steamapps\workshop\content\1062090\3284904751"
)

$ErrorActionPreference = "Stop"

$projectDirectory = $PSScriptRoot
$sourceDirectory = Join-Path $projectDirectory "src"
$outputDirectory = Join-Path $projectDirectory "mod\GuidelineNumbers\Scripts"
$managedDirectory = Join-Path $GameDirectory "Timberborn_Data\Managed"
$harmonyAssembly = Join-Path $HarmonyDirectory "0Harmony.dll"
$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$outputAssembly = Join-Path $outputDirectory "Local.GuidelineNumbers.dll"

foreach ($requiredPath in @($compiler, $managedDirectory, $harmonyAssembly, $sourceDirectory)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Required build path not found: $requiredPath"
    }
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$references = @(
    "mscorlib.dll",
    "netstandard.dll",
    "System.dll",
    "System.Core.dll",
    "Bindito.Core.dll",
    "Timberborn.CameraSystem.dll",
    "Timberborn.ConstructionGuidelines.dll",
    "Timberborn.InputSystem.dll",
    "Timberborn.ModManagerScene.dll",
    "Timberborn.SingletonSystem.dll",
    "Timberborn.WorldPersistence.dll",
    "UnityEngine.CoreModule.dll",
    "UnityEngine.MathematicsModule.dll"
) | ForEach-Object { "/reference:$managedDirectory\$_" }

$references += "/reference:$harmonyAssembly"
$sources = Get-ChildItem -LiteralPath $sourceDirectory -File -Filter "*.cs" |
    ForEach-Object { $_.FullName }

& $compiler /nologo /noconfig /nostdlib+ /target:library /optimize+ /debug:pdbonly /langversion:5 `
    "/out:$outputAssembly" $references $sources

if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

Write-Host "Built $outputAssembly"
