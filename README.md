# SpeedyPaths Preset Switcher

A Valheim companion mod for [SpeedyPaths](https://thunderstore.io/c/valheim/p/Nextek/SpeedyPaths/). It lets players define multiple SpeedyPaths configurations and switch between them in game with a configurable hotkey.

The Thunderstore-facing README lives in `SpeedyPathsPresets/README.md`.

## Development Setup

1. Install the .NET SDK.
2. Install Valheim with BepInExPack Valheim, Jotunn, and SpeedyPaths.
3. Configure your local Valheim paths in `Environment.props`:
    -VALHEIM_INSTALL
    -BEPINEX_PATH (chose correct profile if a mod manager is handling the BepInEx installation)
    -MOD_DEPLOYPATH (when using a mod manager set to $(BEPINEX_PATH)\plugins)
4. Build the solution with Visual Studio or `dotnet build SpeedyPathsPresets.sln`.

The project targets `.NET Framework 4.8`, as expected by the Valheim/BepInEx modding stack used here.

## Build Automation

Deployment to local plugin folder will be done in `scripts/publish.ps1` after each build on Windows.

### Debug

The compiled plugin is copied to `<ValheimDir>\BepInEx\plugins` or to the path configured in `MOD_DEPLOYPATH`.

### Release

Release will be built with dotnet build -c Release

The Release-build creates a Thunderstore-ready zip at:

```text
SpeedyPathsPresets/bin/Release/net48/SpeedyPathsPresets.zip
```

The package contains:

- `manifest.json`
- `README.md`
- `CHANGELOG.md`
- `icon.png`
- `plugins/SpeedyPathsPresets.dll`

## Actions after a game update

When Valheim updates it is likely that parts of the assembly files change.
If this is the case, the references to the assembly files must be renewed in Visual Studio and Unity.
