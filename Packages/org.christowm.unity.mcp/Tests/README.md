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
│   ├── rpc-manifest.readonly.json   # ~38 read-only RPC smoke tests
│   └── versions/        # Per-line manifest overlays
├── Playmode/Shared/     # Cross-version PlayMode (future)
└── _template/           # Scaffold for new Unity lines
```

## Running locally

From repo root (PowerShell):

```powershell
# All assemblies for the current project's Unity version
.\scripts\test\run-unity.ps1

# Specific worktree / line
.\scripts\test\run-unity.ps1 -UnityLine 6000.5 -ProjectPath worktrees\unity-6500

# Gateway unit tests (no Unity required)
.\scripts\test\run-gateway.ps1
```

## Adding a Unity line

```powershell
.\scripts\test\scaffold-unity-version.ps1 -UnityLine 6000.6
```

## Dropping a Unity line

Delete `Tests/Versions/{line}/`, its fixture overlay, CI matrix row, and worktree docs. Shared tests are unchanged.
