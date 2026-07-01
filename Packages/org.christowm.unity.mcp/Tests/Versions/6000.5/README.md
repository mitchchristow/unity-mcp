# Unity 6000.5.x test line

**Supported editor:** Unity 6.5 (`6000.5.x`)

## Pruning / succession

When Unity 6.6 ships:

1. Add `defineConstraints` upper bound here (`!UNITY_6000_6_OR_NEWER`)
2. Scaffold `Tests/Versions/6000.6/` from `Tests/_template/`

When dropping 6.5 support, delete this entire `6000.5/` folder and remove CI matrix row, fixture overlay, and `AssemblyInfo.cs` entry.

**Worktree:** `worktrees/unity-6500`
