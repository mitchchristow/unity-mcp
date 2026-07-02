param(
    [ValidateSet("6000.2", "6000.3", "6000.5")]
    [string]$UnityLine = "",
    [string]$ProjectPath = "",
    [string]$ResultsDir = "TestResults",
    [ValidateSet("editmode", "playmode", "both")]
    [string]$TestPlatform = "editmode"
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

function Get-EditModeAssemblies {
    param([string]$Line, [string]$Project)

    $assemblies = @("UnityMcp.Tests.Editor.Shared")
    if (-not [string]::IsNullOrWhiteSpace($Line)) {
        $assemblies += $lineToAssembly[$Line]
        return $assemblies
    }

    $version = (Get-Content (Join-Path $Project "ProjectSettings\ProjectVersion.txt") -Raw)
    if ($version -match "6000\.5") { $assemblies += $lineToAssembly["6000.5"] }
    elseif ($version -match "6000\.3") { $assemblies += $lineToAssembly["6000.3"] }
    elseif ($version -match "6000\.2") { $assemblies += $lineToAssembly["6000.2"] }
    return $assemblies
}

function Resolve-UnityEditorPath {
    param([string]$Line)

    if (-not [string]::IsNullOrWhiteSpace($Line)) {
        $lineKey = $Line.Replace(".", "_")
        $lineEnvVar = "UNITY_EDITOR_$lineKey"
        $fromLineEnv = [Environment]::GetEnvironmentVariable($lineEnvVar)
        if (-not [string]::IsNullOrWhiteSpace($fromLineEnv) -and (Test-Path $fromLineEnv)) {
            return (Resolve-Path $fromLineEnv).Path
        }

        $hubEditors = "${env:ProgramFiles}\Unity\Hub\Editor"
        if (Test-Path $hubEditors) {
            $match = Get-ChildItem $hubEditors -Directory |
                Where-Object { $_.Name -like "$Line*" } |
                Sort-Object Name -Descending |
                Select-Object -First 1
            if ($null -ne $match) {
                return (Join-Path $match.FullName "Editor\Unity.exe")
            }
        }

        throw "Unity $Line editor not found. Set $lineEnvVar or install via Unity Hub."
    }

    if (-not [string]::IsNullOrWhiteSpace($env:UNITY_EDITOR_PATH) -and (Test-Path $env:UNITY_EDITOR_PATH)) {
        return (Resolve-Path $env:UNITY_EDITOR_PATH).Path
    }

    $hubEditors = "${env:ProgramFiles}\Unity\Hub\Editor"
    if (Test-Path $hubEditors) {
        $projectVersionFile = Join-Path $ProjectPath "ProjectSettings\ProjectVersion.txt"
        if (Test-Path $projectVersionFile) {
            $version = Get-Content $projectVersionFile -Raw
            if ($version -match "6000\.(\d+)") {
                $detectedLine = "6000.$($Matches[1])"
                try {
                    return Resolve-UnityEditorPath -Line $detectedLine
                }
                catch {
                    Write-Warning $_.Exception.Message
                }
            }
        }

        $candidates = Get-ChildItem $hubEditors -Directory | Sort-Object Name -Descending
        if ($candidates.Count -gt 0) {
            return (Join-Path $candidates[0].FullName "Editor\Unity.exe")
        }
    }

    throw "Unity editor not found. Set UNITY_EDITOR_PATH, use -UnityLine, or install via Unity Hub."
}

function Invoke-UnityTestRun {
    param(
        [string]$UnityExe,
        [string]$Project,
        [string]$Platform,
        [string[]]$Assemblies,
        [string]$ResultsFile,
        [string]$LogFile
    )

    $assemblyList = ($Assemblies -join ";")
    Write-Host "Platform:   $Platform"
    Write-Host "Assemblies: $assemblyList"
    Write-Host "Results:    $ResultsFile"

    & $UnityExe `
        -batchmode -nographics -quit `
        -projectPath $Project `
        -runTests -testPlatform $Platform `
        -assemblyNames $assemblyList `
        -testResults $ResultsFile `
        -logFile $logFile

    if ($LASTEXITCODE -ne 0) {
        throw "Unity $Platform tests failed (exit $LASTEXITCODE). See $LogFile"
    }

    Write-Host "Unity $Platform tests passed."
}

$unityExe = Resolve-UnityEditorPath -Line $UnityLine
$resultsPath = Join-Path $repoRoot $ResultsDir
New-Item -ItemType Directory -Force -Path $resultsPath | Out-Null

$suffix = if ([string]::IsNullOrWhiteSpace($UnityLine)) { "auto" } else { $UnityLine }

Write-Host "Project: $ProjectPath"
Write-Host "Unity:   $unityExe"

if ($TestPlatform -eq "editmode" -or $TestPlatform -eq "both") {
    $editAssemblies = Get-EditModeAssemblies -Line $UnityLine -Project $ProjectPath
    Invoke-UnityTestRun `
        -UnityExe $unityExe `
        -Project $ProjectPath `
        -Platform "editmode" `
        -Assemblies $editAssemblies `
        -ResultsFile (Join-Path $resultsPath "unity-editmode-$suffix.xml") `
        -LogFile (Join-Path $resultsPath "unity-editmode-$suffix.log")
}

if ($TestPlatform -eq "playmode" -or $TestPlatform -eq "both") {
    Invoke-UnityTestRun `
        -UnityExe $unityExe `
        -Project $ProjectPath `
        -Platform "playmode" `
        -Assemblies @("UnityMcp.Tests.Playmode.Shared") `
        -ResultsFile (Join-Path $resultsPath "unity-playmode-$suffix.xml") `
        -LogFile (Join-Path $resultsPath "unity-playmode-$suffix.log")
}
