param(
    [string]$BaseBranch = "main",
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")

$worktrees = @(
    @{ Branch = "worktree/unity-620";      Path = "worktrees/unity-620" },
    @{ Branch = "worktree/unity-630-lts";  Path = "worktrees/unity-630-lts" },
    @{ Branch = "worktree/unity-6500";     Path = "worktrees/unity-6500" }
)

Push-Location $repoRoot
try {
    foreach ($wt in $worktrees) {
        $targetPath = Join-Path $repoRoot $wt.Path
        if (Test-Path $targetPath) {
            if ($Force) {
                Write-Host "Removing existing worktree at $($wt.Path)..."
                git worktree remove --force $targetPath 2>$null
                if (Test-Path $targetPath) {
                    Remove-Item -Recurse -Force $targetPath
                }
            }
            else {
                Write-Host "Skipping $($wt.Path) (already exists). Use -Force to recreate."
                continue
            }
        }

        Write-Host "Creating worktree $($wt.Path) on branch $($wt.Branch) from $BaseBranch..."
        git worktree add -B $wt.Branch $targetPath $BaseBranch
    }

    Write-Host ""
    Write-Host "Worktrees ready. Open each folder in Unity Hub with the matching editor:"
    Write-Host "  worktrees/unity-620      -> Unity 6.2"
    Write-Host "  worktrees/unity-630-lts  -> Unity 6.3 LTS"
    Write-Host "  worktrees/unity-6500     -> Unity 6.5"
}
finally {
    Pop-Location
}
