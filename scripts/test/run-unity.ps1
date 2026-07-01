param(
    [ValidateSet("6000.2", "6000.3", "6000.5")]
    [string]$UnityLine = "",
    [string]$ProjectPath = "",
    [string]$ResultsDir = "TestResults"
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = $repoRoot.Path
}

$lineToAssembly = @{
    "6000.2" = "UnityMcp.Tests.Editor.V6000_2"
    "6000.3" = "UnityMcp.Tests.Editor.V6000_3"
    "6000.5" = "UnityMcp.Tests.Editor.V6000_5"
}

$assemblies = @("UnityMcp.Tests.Editor.Shared")
if (-not [string]::IsNullOrWhiteSpace($UnityLine)) {
    $assemblies += $lineToAssembly[$UnityLine]
} else {
    $version = (Get-Content (Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt") -Raw)
    if ($version -match "6000\.5") { $assemblies += $lineToAssembly["6000.5"] }
    elseif ($version -match "6000\.3") { $assemblies += $lineToAssembly["6000.3"] }
    elseif ($version -match "6000\.2") { $assemblies += $lineToAssembly["6000.2"] }
}

$assemblyList = ($assemblies -join ";")
$resultsPath = Join-Path $repoRoot $ResultsDir
New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null

$suffix = if ([string]::IsNullOrWhiteSpace($UnityLine)) { "auto" } else { $UnityLine }
$resultsFile = Join-Path $resultsPath "unity-editmode-$suffix.xml"
$logFile = Join-Path $resultsPath "unity-editmode-$suffix.log"

$unityExe = $env:UNITY_EDITOR_PATH
if ([string]::IsNullOrWhiteSpace($unityExe)) {
    $hubEditors = "${env:ProgramFiles}\Unity\Hub\Editor"
    if (Test-Path $hubEditors) {
        $candidates = Get-ChildItem $hubEditors -Directory | Sort-Object Name -Descending
        if ($candidates.Count -gt 0) {
            $unityExe = Join-Path $candidates[0].FullName "Editor\Unity.exe"
        }
    }
}

if ([string]::IsNullOrWhiteSpace($unityExe) -or -not (Test-Path $unityExe)) {
    throw "Unity editor not found. Set UNITY_EDITOR_PATH to Unity.exe or install via Unity Hub."
}

Write-Host "Project:    $ProjectPath"
Write-Host "Unity:      $unityExe"
Write-Host "Assemblies: $assemblyList"
Write-Host "Results:    $resultsFile"

& $unityExe `
    -batchmode -nographics -quit `
    -projectPath $ProjectPath `
    -runTests -testPlatform editmode `
    -assemblyNames $assemblyList `
    -testResults $resultsFile `
    -logFile $logFile

if ($LASTEXITCODE -ne 0) {
  throw "Unity EditMode tests failed (exit $LASTEXITCODE). See $logFile"
}

Write-Host "Unity EditMode tests passed."
