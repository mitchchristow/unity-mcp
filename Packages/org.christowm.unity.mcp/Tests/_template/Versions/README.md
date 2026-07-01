# Unity MCP — test scaffold template

Copy this folder to `Tests/Versions/{line}/` when adding support for a new Unity editor line.

## Steps

1. Run `scripts/test/scaffold-unity-version.ps1 -UnityLine 6000.6`
2. Narrow the previous line's asmdef with `!UNITY_6000_6_OR_NEWER`
3. Add `InternalsVisibleTo` for the new assembly in `Editor/AssemblyInfo.cs`
4. Add CI matrix row in `.github/workflows/test-unity-matrix.yml`
5. Add `Tests/Fixtures/versions/{line}.manifest.json`
6. Fill in version-specific tests (wire format, API deltas)

## Folder layout

```
Versions/XXXX.Y/
├── README.md
└── Editor/
    ├── UnityMcp.Tests.Editor.VXXXX_Y.asmdef
    └── VersionLineSmokeTests.cs
```
