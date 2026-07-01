# Unity 6000.2.x test line

**Supported editor:** Unity 6.2 (`6000.2.x`)

## Pruning

When dropping 6.2 support, delete this entire `6000.2/` folder and:

- Remove the `6000.2` row from `.github/workflows/test-unity-matrix.yml`
- Remove `6000.2` from `scripts/test/run-unity.ps1`
- Delete `Tests/Fixtures/versions/6000.2.manifest.json`
- Remove `UnityMcp.Tests.Editor.V6000_2` from `Editor/AssemblyInfo.cs`

**Worktree:** `worktrees/unity-620`
