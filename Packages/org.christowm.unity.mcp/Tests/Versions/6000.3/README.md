# Unity 6000.3.x LTS test line

**Supported editor:** Unity 6.3 LTS (`6000.3.x`)

## Pruning

When dropping 6.3 support, delete this entire `6000.3/` folder and:

- Remove the `6000.3` row from `.github/workflows/test-unity-matrix.yml`
- Remove `6000.3` from `scripts/test/run-unity.ps1`
- Delete `Tests/Fixtures/versions/6000.3.manifest.json`
- Remove `UnityMcp.Tests.Editor.V6000_3` from `Editor/AssemblyInfo.cs`

**Worktree:** `worktrees/unity-630-lts`
