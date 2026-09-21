using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ProductionTimeChange
{
    /// <summary>
    /// Config setup helpers for Keybinding and Color selection in Mod Options.
    /// </summary>
    public static class CustomMenuAPI
    {
        /// <summary>
        /// Creates a keybind setting button in Mod Options for a mod config entry.
        /// </summary>
        public static ConfigEntry<string> SetupKeybindConfig(
            ConfigFile config,
            string name,
            string displayName,
            string tooltip,
            string defaultKey = "None",
            Action<Key>? onKeyChanged = null)
        {
            var entry = config.GetEntry<string>(name, defaultKey);
            entry.UI.Name = displayName;
            entry.UI.Hidden = true;
            entry.UI.Tooltip = tooltip;
            entry.UI.OnUI = (ConfigEntryBase entryBase) =>
            {
                if (PrefabManager.instance == null || ModOptionsScreen.instance == null) return;

                var btn = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab, ModOptionsScreen.instance.ButtonsParent);
                btn.transform.localScale = Vector3.one;
                btn.transform.localPosition = Vector3.zero;
                btn.transform.localRotation = Quaternion.identity;

                var keybindUI = btn.gameObject.AddComponent<KeybindSettingUI>();
                Key current = KeybindSettingUI.ParseKey(entry, Key.None);
                keybindUI.Initialize(current, displayName, (newKey) =>
                {
                    entry.Value = newKey.ToString();
                    onKeyChanged?.Invoke(newKey);
                });
            };
            return entry;
        }

        public static ConfigEntry<string> SetupKeybindConfig(
            Mod mod,
            string name,
            string displayName,
            string tooltip,
            string defaultKey = "None",
            Action<Key>? onKeyChanged = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            return SetupKeybindConfig(mod.Config, name, displayName, tooltip, defaultKey, onKeyChanged);
        }

        public static ConfigEntry<string> SetupKeybindConfig(
            ConfigFile config,
            string name,
            string displayName,
            string tooltip,
            Key defaultKey,
            Action<Key>? onKeyChanged = null)
        {
            return SetupKeybindConfig(config, name, displayName, tooltip, defaultKey.ToString(), onKeyChanged);
        }

        public static ConfigEntry<string> SetupKeybindConfig(
            Mod mod,
            string name,
            string displayName,
            string tooltip,
            Key defaultKey,
            Action<Key>? onKeyChanged = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            return SetupKeybindConfig(mod.Config, name, displayName, tooltip, defaultKey.ToString(), onKeyChanged);
        }

        /// <summary>
        /// Creates a color palette picker button in Mod Options for a mod config entry.
        /// </summary>
        public static ConfigEntry<string> SetupColorConfig(
            ConfigFile config,
            string name,
            string displayName,
            string tooltip,
            string defaultColorHex = "#FFFFFF",
            Action<Color>? onColorChanged = null)
        {
            var entry = config.GetEntry<string>(name, defaultColorHex);
            entry.UI.Name = displayName;
            entry.UI.Hidden = true;
            entry.UI.Tooltip = tooltip;
            entry.UI.OnUI = (ConfigEntryBase entryBase) =>
            {
                if (PrefabManager.instance == null || ModOptionsScreen.instance == null) return;

                var btn = UnityEngine.Object.Instantiate(PrefabManager.instance.ButtonPrefab, ModOptionsScreen.instance.ButtonsParent);
                btn.transform.localScale = Vector3.one;
                btn.transform.localPosition = Vector3.zero;
                btn.transform.localRotation = Quaternion.identity;

                var colorUI = btn.gameObject.AddComponent<ColorSettingUI>();
                Color current = ColorSettingUI.ParseColor(entry, Color.white);
                colorUI.Initialize(current, displayName, (newColor) =>
                {
                    entry.Value = ColorSettingUI.ColorToHex(newColor);
                    onColorChanged?.Invoke(newColor);
                });
            };
            return entry;
        }

        public static ConfigEntry<string> SetupColorConfig(
            Mod mod,
            string name,
            string displayName,
            string tooltip,
            string defaultColorHex = "#FFFFFF",
            Action<Color>? onColorChanged = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            return SetupColorConfig(mod.Config, name, displayName, tooltip, defaultColorHex, onColorChanged);
        }

        public static ConfigEntry<string> SetupColorConfig(
            ConfigFile config,
            string name,
            string displayName,
            string tooltip,
            Color defaultColor,
            Action<Color>? onColorChanged = null)
        {
            return SetupColorConfig(config, name, displayName, tooltip, ColorSettingUI.ColorToHex(defaultColor), onColorChanged);
        }

        public static ConfigEntry<string> SetupColorConfig(
            Mod mod,
            string name,
            string displayName,
            string tooltip,
            Color defaultColor,
            Action<Color>? onColorChanged = null)
        {
            if (mod == null) throw new ArgumentNullException(nameof(mod));
            return SetupColorConfig(mod.Config, name, displayName, tooltip, ColorSettingUI.ColorToHex(defaultColor), onColorChanged);
        }

        /// <summary>
        /// Parses a Keybind string config into a Key enum value.
        /// </summary>
        public static Key ParseKey(ConfigEntry<string>? config, Key defaultKey = Key.None)
            => KeybindSettingUI.ParseKey(config, defaultKey);

        /// <summary>
        /// Parses a Hex color string config into a Unity Color.
        /// </summary>
        public static Color ParseColor(ConfigEntry<string>? config, Color defaultColor = default)
            => ColorSettingUI.ParseColor(config, defaultColor);

        /// <summary>
        /// Converts a Unity Color to Hex string format (#RRGGBBAA).
        /// </summary>
        public static string ColorToHex(Color color)
            => ColorSettingUI.ColorToHex(color);
    }
}
