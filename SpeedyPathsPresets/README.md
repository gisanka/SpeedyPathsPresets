# SpeedyPaths Preset Switcher

Create multiple SpeedyPaths configurations and switch between them with a hotkey.

This mod is a companion mod for [SpeedyPaths](https://thunderstore.io/c/valheim/p/Nextek/SpeedyPaths/). It reads the SpeedyPaths config entries, creates matching preset sections, and applies the selected preset back to SpeedyPaths while you play.

## Features

- Switch between named SpeedyPaths presets with one configurable key.
- Ability to use global values for all speed and stamina modifiers.
- Presets are prefilled with SpeedyPaths' default values
- On each spawn Default preset is activated

## Installation

### Mod manager

Install with r2modman or Thunderstore Mod Manager. The required dependencies are installed automatically:

- BepInExPack Valheim
- Jotunn
- SpeedyPaths

### Manual

1. Install BepInExPack Valheim, Jotunn, and SpeedyPaths.
2. Copy `SpeedyPathsPresets.dll` into `BepInEx/plugins/SpeedyPathsPresets/`.
3. Start Valheim once so the config file is generated.

## Configuration

The config file is created at:

```text
BepInEx/config/maxime.SpeedyPathsPresets.cfg
```

Important options:

- `General > ToggleKey`: Key used to switch to the next preset. Default: `F4`.
- `Presets > PresetNames`: Comma-separated list of presets. Default: `Default,FastTravel`.
- `Presets > GeneratePresetConfigs`: Set to `true` after changing the preset list to generate config entries for the new presets. The option resets itself to `false`.

Each preset gets its own section named `Preset: <Name>`.

The `Default` preset is using `SetAllValuesToSameLevel = false`. All values are initially set to SpeedyPaths default values. Adapt to your liking.

The `FastTravel` preset is using `SetAllValuesToSameLevel = true` and sets `SpeedModifier_Global = 3` and `StaminaModifier_Global = 0.1` to enable a way of fast travelling even in noportal style runs. Adapt to your liking.

## Notes

- This mod requires SpeedyPaths and does not do anything useful by itself.
- Presets are local config changes. If you use SpeedyPaths' server-sync features, this mod might cause problems.
- After editing the preset list in the mod manager configuration manager, launch the game once to see the new presets in the configuration.
- After editing the preset list in the ingame configuration manager, enable the `GeneratePresetConfigs` option (it will reset itself immediately) and reopen the configuration manager to see the new entries.

## Changelog

See `CHANGELOG.md`.

## Source

https://github.com/gisanka/SpeedyPathsPresets
