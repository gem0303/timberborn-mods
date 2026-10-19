param(
    [string]$GameDirectory = 'C:\Program Files (x86)\Steam\steamapps\common\Timberborn'
)

$ErrorActionPreference = 'Stop'

$projectDirectory = $PSScriptRoot
$managedDirectory = Join-Path $GameDirectory 'Timberborn_Data\Managed'
$compiler = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$source = Join-Path $projectDirectory 'src\NaturalRemovalButtonConfigurator.cs'
$outputDirectory = Join-Path $projectDirectory 'mod\NaturalRemovalButton\Scripts'
$output = Join-Path $outputDirectory 'Local.NaturalRemovalButton.dll'

if (-not (Test-Path -LiteralPath $compiler)) {
    throw "C# compiler not found: $compiler"
}

if (-not (Test-Path -LiteralPath $managedDirectory -PathType Container)) {
    throw "Timberborn managed assemblies not found: $managedDirectory"
}

if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
    throw "Natural Removal Button source file not found: $source"
}

New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null

$references = @(
    'mscorlib.dll',
    'netstandard.dll',
    'Bindito.Core.dll',
    'Timberborn.BaseComponentSystem.dll',
    'Timberborn.BlueprintSystem.dll',
    'Timberborn.Common.dll',
    'Timberborn.ModManagerScene.dll',
    'Timberborn.NaturalResources.dll',
    'Timberborn.TemplateInstantiation.dll',
    'Timberborn.Yielding.dll',
    'UnityEngine.CoreModule.dll'
) | ForEach-Object { '/reference:' + (Join-Path $managedDirectory $_) }

$arguments = @(
    '/nologo',
    '/nostdlib+',
    '/target:library',
    '/optimize+',
    '/debug:pdbonly',
    '/langversion:5',
    ('/out:' + $output),
    $source
) + $references

& $compiler $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE."
}

Get-Item -LiteralPath $output
