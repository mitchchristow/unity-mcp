# Unity MCP Test Layout

Automated tests are organized for **pruning by Unity editor line**.

## Structure

```
Tests/
├── Shared/              # Runs on every supported Unity version
├── Versions/
│   ├── 6000.2/          # Unity 6.2 only — delete folder when dropping 6.2
│   ├── 6000.3/          # Unity 6.3 LTS only
│   └── 6000.5/          # Unity 6.5 only (EntityId wire format)
├── Fixtures/
│   ├── rpc-manifest.shared.json
│   ├── rpc-manifest.readonly.json   # ~55 read-only RPC smoke tests
│   ├── rpc-manifest.mutating.json   # ordered core mutating scenarios with $entryId refs
│   ├── mutating/                    # per-controller mutating manifests (prunable by tag)
│   └── versions/        # Per-line manifest overlays
├── Playmode/Shared/     # Cross-version PlayMode smoke tests
└── _template/           # Scaffold for new Unity lines
```

## Running locally

From repo root (PowerShell):

```powershell
# Gateway unit tests (no Unity required)
.\scripts\test\run-gateway.ps1

# EditMode tests — auto-detects editor from ProjectVersion.txt
.\scripts\test\run-unity.ps1

# Pin a specific Unity line (uses Hub install or UNITY_EDITOR_6000_5 env var)
.\scripts\test\run-unity.ps1 -UnityLine 6000.5

# EditMode + PlayMode
.\scripts\test\run-unity.ps1 -UnityLine 6000.5 -TestPlatform both

# Version worktree (optional — keeps per-version Library/ separate)
.\scripts\test\run-unity.ps1 -UnityLine 6000.5 -ProjectPath worktrees\unity-6500

# HTTP E2E smoke (Unity Editor open, MCP server on port 17890)
.\scripts\test\run-e2e.ps1
```

### Unity editor resolution

`run-unity.ps1` picks the editor in this order:

1. `-UnityLine` → `UNITY_EDITOR_6000_2` / `UNITY_EDITOR_6000_3` / `UNITY_EDITOR_6000_5` environment variable
2. `-UnityLine` → newest matching folder under `%ProgramFiles%\Unity\Hub\Editor\`
3. `UNITY_EDITOR_PATH` (when `-UnityLine` omitted)
4. Hub folder matching `ProjectSettings/ProjectVersion.txt`

### Local worktrees

```powershell
.\scripts\test\setup-worktrees.ps1          # create worktrees/unity-620, etc.
.\scripts\test\setup-worktrees.ps1 -Force   # recreate if they already exist
```

## GitHub Actions (optional)

Workflows are **manual dispatch only** (`workflow_dispatch`). To run them on this laptop, [add a self-hosted Windows runner](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/adding-self-hosted-runners) and set repository secrets:

| Secret | Example |
|--------|---------|
| `UNITY_EDITOR_6000_2` | `C:\Program Files\Unity\Hub\Editor\6000.2.13f1\Editor\Unity.exe` |
| `UNITY_EDITOR_6000_3` | `C:\Program Files\Unity\Hub\Editor\6000.3.x\Editor\Unity.exe` |
| `UNITY_EDITOR_6000_5` | `C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe` |

- **Unity EditMode Matrix** — runs `run-unity.ps1` for each line
- **Unity E2E Smoke** — requires Unity already open; runs `run-e2e.ps1`

## Adding a Unity line

```powershell
.\scripts\test\scaffold-unity-version.ps1 -UnityLine 6000.6
```

## Dropping a Unity line

Delete `Tests/Versions/{line}/`, its fixture overlay, CI matrix row, and worktree docs. Shared tests are unchanged.
