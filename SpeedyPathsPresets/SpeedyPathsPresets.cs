using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Extensions;
using UnityEngine;
using HarmonyLib;

namespace SpeedyPathsPresets
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency(SpeedyPathsGuid, BepInDependency.DependencyFlags.HardDependency)]
    internal class SpeedyPathsPresets : BaseUnityPlugin
    {
        public const string PluginGUID = "maxime.SpeedyPathsPresets";
        public const string PluginName = "SpeedyPaths Preset Switcher";
        public const string PluginVersion = "0.1.0";

        private const string SpeedyPathsGuid = "nex.SpeedyPaths";
        private ConfigFile _speedyPathsConfig;
        private static readonly string[] SpeedyPathsValueSections =
        {
            "SpeedModifiers",
            "StaminaModifiers",
            "SpeedModifiers_Biomes",
            "StaminaModifiers_Biomes"
        };
        private ConfigEntry<KeyboardShortcut> _toggleKey;
        private ConfigEntry<string> _presetNames;
        private ConfigEntry<bool> _generatePresetConfigs;
        private string[] _presets;
        private int _currentPreset = 0;

        private Harmony _harmony;
        internal static SpeedyPathsPresets Instance;

        private class PresetConfig
        {
            public string Name;
            public ConfigEntry<bool> UseGlobalValues;
            public ConfigEntry<float> SpeedModifierGlobal;
            public ConfigEntry<float> StaminaModifierGlobal;
        }

        private readonly Dictionary<string, PresetConfig> _presetConfigs = new();

        private void Awake()
        {
            Instance = this;

            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            _toggleKey = Config.BindConfigInOrder(
                "General",
                "ToggleKey",
                new KeyboardShortcut(KeyCode.F4),
                "Key used to switch to the next SpeedyPaths preset.",
                synced: false
            );

            _presetNames = Config.BindConfigInOrder(
                "Presets",
                "PresetNames",
                "Default,FastTravel",
                "Comma-separated list of preset names.",
                synced: false
            );

            _generatePresetConfigs = Config.BindConfigInOrder(
                "Presets",
                "GeneratePresetConfigs",
                false,
                "I changed the preset list, please generate configuration options (button will reset automatically when ingame, please reopen configuration manager window or run game once)",
                synced: false
            );

            _generatePresetConfigs.SettingChanged += (_, _) =>
            {
                if (!_generatePresetConfigs.Value)
                {
                    return;
                }

                CreatePresetConfigs();

                _generatePresetConfigs.Value = false;

                Jotunn.Logger.LogInfo("Preset configuration options regenerated.");
            };

            BaseUnityPlugin speedyPaths = GetSpeedyPathsPlugin();

            if (speedyPaths != null)
            {
                _speedyPathsConfig = speedyPaths.Config;
                Jotunn.Logger.LogInfo($"Found {speedyPaths.Info.Metadata.Name}");
            }

            CreatePresetConfigs();

            Jotunn.Logger.LogInfo("SpeedyPaths Configuration Switcher has loaded");
        }

        private void ApplyPresetByIndex(int presetIndex)
        {
            if (_presets == null || _presets.Length == 0)
            {
                return;
            }

            if (presetIndex < 0 || presetIndex >= _presets.Length)
            {
                Jotunn.Logger.LogWarning($"Invalid preset index: {presetIndex}");
                return;
            }

            string preset = _presets[presetIndex].Trim();

            if (!_presetConfigs.TryGetValue(preset, out PresetConfig presetConfig))
            {
                Jotunn.Logger.LogWarning($"No config found for preset: {preset}");
                return;
            }

            if (presetConfig.UseGlobalValues.Value)
            {
                ApplyGlobalPreset(
                    presetConfig.SpeedModifierGlobal.Value,
                    presetConfig.StaminaModifierGlobal.Value
                );
            }
            else
            {
                ApplyDetailedPreset(presetConfig);
            }

            MessageHud.instance?.ShowMessage(
                MessageHud.MessageType.TopLeft,
                $"SpeedyPaths Preset: {preset}"
            );

            Jotunn.Logger.LogInfo($"Applied preset: {preset}");
        }

        private void ApplyGlobalPreset(float speedModifier, float staminaModifier)
        {
            if (!IsSpeedyPathsConfigAvailable())
            {
                return;
            }

            SetAllSpeedyPathsValuesInSection(
                _speedyPathsConfig,
                "SpeedModifiers",
                speedModifier
            );

            SetAllSpeedyPathsValuesInSection(
                _speedyPathsConfig,
                "StaminaModifiers",
                staminaModifier
            );

            SetAllSpeedyPathsValuesInSection(
                _speedyPathsConfig,
                "SpeedModifiers_Biomes",
                speedModifier
            );

            SetAllSpeedyPathsValuesInSection(
                _speedyPathsConfig,
                "StaminaModifiers_Biomes",
                staminaModifier
            );

            Jotunn.Logger.LogInfo(
                $"Applied global preset to SpeedyPaths: Speed={speedModifier}, Stamina={staminaModifier}"
            );
        }

        private void SetAllSpeedyPathsValuesInSection(
            ConfigFile config,
            string section,
            float value
        )
        {
            foreach (ConfigDefinition definition in config.Keys)
            {
                if (definition.Section != section)
                {
                    continue;
                }

                SetSpeedyPathsValue(
                    config,
                    definition.Section,
                    definition.Key,
                    value
                );
            }
        }

        private void ApplyDetailedPreset(PresetConfig presetConfig)
        {
            if (!IsSpeedyPathsConfigAvailable())
            {
                return;
            }

            // PresetConfig does not store the section name, so use one of its entries to reach it
            string presetSection = presetConfig.UseGlobalValues.Definition.Section;

            foreach (string targetSection in SpeedyPathsValueSections)
            {
                ApplyDetailedPresetSection(
                    presetSection,
                    _speedyPathsConfig,
                    targetSection
                );
            }

            Jotunn.Logger.LogInfo($"Applied detailed preset to SpeedyPaths: {presetConfig.Name}");
        }

        private void ApplyDetailedPresetSection(
            string presetSection,
            ConfigFile speedyPathsConfig,
            string sourceSection
        )
        {
            foreach (ConfigDefinition definition in speedyPathsConfig.Keys)
            {
                if (definition.Section != sourceSection)
                {
                    continue;
                }

                ConfigDefinition presetDefinition = new ConfigDefinition(
                    presetSection,
                    definition.Key
                );

                if (!Config.TryGetEntry<float>(presetDefinition, out ConfigEntry<float> presetEntry))
                {
                    Jotunn.Logger.LogWarning(
                        $"Could not find preset config value: [{presetSection}] {definition.Key}"
                    );

                    continue;
                }

                SetSpeedyPathsValue(
                    speedyPathsConfig,
                    definition.Section,
                    definition.Key,
                    presetEntry.Value
                );
            }
        }

        private void SetSpeedyPathsValue(ConfigFile config, string section, string key, float value)
        {
            ConfigDefinition definition = new ConfigDefinition(section, key);

            if (!config.TryGetEntry<float>(definition, out ConfigEntry<float> entry))
            {
                Jotunn.Logger.LogWarning($"Could not find SpeedyPaths config value: [{section}] {key}");
                return;
            }

            entry.Value = value;
        }

        private void CreatePresetConfigs()
        {
            _presets = _presetNames.Value.Split(',');

            foreach (string rawPresetName in _presets)
            {
                string presetName = rawPresetName.Trim();

                if (string.IsNullOrWhiteSpace(presetName))
                {
                    continue;
                }
                string section = $"Preset: {presetName}";

                // use different Default values per preset, only preconfigured preset FastTravel will get global values enabled and set to != 1
                bool useGlobalValuesDefault = presetName.Equals("FastTravel", StringComparison.OrdinalIgnoreCase);
                float speedModifierGlobalDefault = useGlobalValuesDefault ? 3f : 1f;
                float staminaModifierGlobalDefault = useGlobalValuesDefault ? 0.1f : 1f;

                if (!_presetConfigs.TryGetValue(presetName, out PresetConfig presetConfig))
                {
                    presetConfig = new PresetConfig
                    {
                        Name = presetName,

                        UseGlobalValues = Config.BindConfigInOrder(
                            section,
                            "SetAllValuesToSameLevel",
                            useGlobalValuesDefault,
                            "Set all SpeedyPaths speed and stamina values in this preset to the same level. (ignore detailed settings)",
                            synced: false
                        ),

                        SpeedModifierGlobal = Config.BindConfigInOrder(
                            section,
                            "SpeedModifier_Global",
                            speedModifierGlobalDefault,
                            "Global speed modifier for this preset.",
                            synced: false
                        ),

                        StaminaModifierGlobal = Config.BindConfigInOrder(
                            section,
                            "StaminaModifier_Global",
                            staminaModifierGlobalDefault,
                            "Global stamina modifier for this preset.",
                            synced: false
                        )
                    };

                    _presetConfigs[presetName] = presetConfig;
                }

                if (!IsSpeedyPathsConfigAvailable())
                {
                    continue;
                }

                foreach (string sourceSection in SpeedyPathsValueSections)
                {
                    CreatePresetValuesFromSpeedyPaths(
                        presetConfig,
                        _speedyPathsConfig,
                        sourceSection,
                        section
                    );
                }
            }
        }

        private void CreatePresetValuesFromSpeedyPaths(
            PresetConfig presetConfig,
            ConfigFile speedyPathsConfig,
            string sourceSection,
            string section
            )
        {
            foreach (ConfigDefinition definition in speedyPathsConfig.Keys)
            {
                if (definition.Section != sourceSection)
                {
                    continue;
                }

                if (!speedyPathsConfig.TryGetEntry<float>(definition, out ConfigEntry<float> sourceEntry))
                {
                    continue;
                }

                // Use SpeedyPaths defaults so newly generated presets start from sensible values.
                Config.BindConfigInOrder(
                    section,
                    definition.Key,
                    (float)sourceEntry.DefaultValue,
                    sourceEntry.Description.Description,
                    synced: false
                );
            }
        }

        private BaseUnityPlugin GetSpeedyPathsPlugin()
        {
            if (!Chainloader.PluginInfos.TryGetValue(SpeedyPathsGuid, out var pluginInfo))
            {
                Jotunn.Logger.LogWarning("SpeedyPaths plugin not found.");
                return null;
            }

            return pluginInfo.Instance;
        }

        private bool IsSpeedyPathsConfigAvailable()
        {
            if (_speedyPathsConfig != null)
            {
                return true;
            }

            Jotunn.Logger.LogWarning("SpeedyPaths config is not available.");
            return false;
        }

        private void Update()
        {
            if (_toggleKey.Value.IsDown())
            {
                _currentPreset++;

                if (_currentPreset >= _presets.Length)
                {
                    _currentPreset = 0;
                }

                ApplyPresetByIndex(_currentPreset);
            }
        }

        internal void ApplyDefaultPreset()
        {
            _currentPreset = 0;
            ApplyPresetByIndex(_currentPreset);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    internal static class Player_OnSpawned_Patch
    {
        private static void Postfix()
        {
            SpeedyPathsPresets.Instance?.ApplyDefaultPreset();
        }
    }

}
