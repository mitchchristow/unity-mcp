param(
    [Parameter(Mandatory = $true)]
    [string]$UnityLine
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$templateRoot = Join-Path $repoRoot "Packages\org.christowm.unity.mcp\Tests\_template\Versions"
$targetRoot = Join-Path $repoRoot "Packages\org.christowm.unity.mcp\Tests\Versions\$UnityLine"

if (Test-Path $targetRoot) {
    throw "Version folder already exists: $targetRoot"
}

$asmSuffix = "V" + ($UnityLine -replace "\.", "")
$namespaceSuffix = $UnityLine -replace "\.", "_"

New-Item -ItemType Directory -Force -Path (Join-Path $targetRoot "Editor") | Out-Null

$readme = @"
# Unity $UnityLine test line

**Supported editor:** Unity $UnityLine

## Pruning

Delete this entire ``$UnityLine/`` folder when dropping support, plus CI matrix row, fixture overlay, and ``AssemblyInfo.cs`` entry.

"@
Set-Content -Path (Join-Path $targetRoot "README.md") -Value $readme -Encoding UTF8

$asmTemplate = Get-Content (Join-Path $templateRoot "Editor\UnityMcp.Tests.Editor.VXXXX_Y.asmdef.template") -Raw
$asmTemplate = $asmTemplate -replace "VXXXX_Y", $asmSuffix
$asmTemplate = $asmTemplate -replace "UNITY_6000_6_OR_NEWER", "UNITY_${namespaceSuffix}_OR_NEWER"
$asmTemplate = $asmTemplate -replace "UNITY_6000_7_OR_NEWER", "UNITY_${namespaceSuffix}_NEXT_OR_NEWER"
Set-Content -Path (Join-Path $targetRoot "Editor\UnityMcp.Tests.Editor.$asmSuffix.asmdef") -Value $asmTemplate -Encoding UTF8

$csTemplate = Get-Content (Join-Path $templateRoot "Editor\VersionLineSmokeTests.cs.template") -Raw
$csTemplate = $csTemplate -replace "VXXXX_Y", $asmSuffix
Set-Content -Path (Join-Path $targetRoot "Editor\VersionLineSmokeTests.cs") -Value $csTemplate -Encoding UTF8

$fixturePath = Join-Path $repoRoot "Packages\org.christowm.unity.mcp\Tests\Fixtures\versions\$UnityLine.manifest.json"
if (-not (Test-Path $fixturePath)) {
    $fixture = @"
{
  "description": "Optional RPC tests specific to Unity $UnityLine.",
  "entries": []
}
"@
    Set-Content -Path $fixturePath -Value $fixture -Encoding UTF8
}

Write-Host "Scaffolded $targetRoot"
Write-Host "Next: add InternalsVisibleTo(""UnityMcp.Tests.Editor.$asmSuffix"") to Editor/AssemblyInfo.cs and CI matrix row."
