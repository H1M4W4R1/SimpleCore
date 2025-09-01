using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

namespace Systems.SimpleCore.Utility.Input
{
    /// <summary>
    ///     Brute force to display names in English... usually...
    /// </summary>
    public static class InputNames
    {
        private static List<InputInfo> PathToKeyData { get; } = new();
        private static bool _loaded;

        /// <summary>
        ///     Get the display name of an input path
        /// </summary>
        /// <param name="path">Path to analyze</param>
        /// <param name="preferShortName">Prefer short name if available</param>
        /// <returns>String containing the display name or null if no name available</returns>
        /// <remarks>
        ///     If returning null default to Unity's implementation
        /// </remarks>
        [CanBeNull] public static string GetDisplayName(string path, bool preferShortName = true)
        {
            InputInfo? inputInfo = GetInputInfo(path);

            if (inputInfo == null) return null;
            return preferShortName ? inputInfo.Value.ShortName : inputInfo.Value.DisplayName;
        }

        /// <summary>
        ///     Get the input info for a path
        /// </summary>
        /// <param name="path">Path to get info for</param>
        /// <returns>InputInfo or null if no info available (if that happens then Unity API is broken)</returns>
        [CanBeNull] public static InputInfo? GetInputInfo(string path)
        {
            EnsureLoaded();

            for (int i = 0; i < PathToKeyData.Count; i++)
            {
                InputInfo info = PathToKeyData[i];

                // Skip if device type is invalid
                if (!path.Contains(info.deviceTypeName)) continue;

                // Analyze alias
                if (path.EndsWith(info.pathPart, StringComparison.InvariantCulture)) return info;
            }

            return null;
        }


        /// <summary>
        ///     Ensure all button names are correctly loaded
        /// </summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            LoadNames();
            _loaded = true;
        }

        private static void LoadNames()
        {
            PathToKeyData.Clear();

            // Get all input systems
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int n = 0; n < assemblies.Length; n++)
            {
                Type[] types = assemblies[n].GetTypes();
                for (int i = 0; i < types.Length; i++)
                {
                    Type type = types[i];
                    if (!type.IsSubclassOf(typeof(InputDevice)) &&
                        !typeof(IInputStateTypeInfo).IsAssignableFrom(type))
                        continue;

                    // Find any existing InputControl attributes
                    for (int j = 0; j < type.GetFields().Length; j++)
                    {
                        FieldInfo field = type.GetFields()[j];

                        foreach (InputControlAttribute attribute in field
                                     .GetCustomAttributes<InputControlAttribute>())
                            HandleAttribute(type, attribute);
                    }

                    // Get attributes from properties
                    for (int j = 0; j < type.GetProperties().Length; j++)
                    {
                        PropertyInfo property = type.GetProperties()[j];
                        foreach (InputControlAttribute attribute in property
                                     .GetCustomAttributes<InputControlAttribute>())
                            HandleAttribute(type, attribute);
                    }
                }
            }

            Debug.Log($"Found {PathToKeyData.Count} input keys");
        }

        private static void HandleAttribute(
            [NotNull] Type type,
            [NotNull] InputControlAttribute attribute)
        {
            // Register main key
            if (!string.IsNullOrEmpty(attribute.name))
            {
                PathToKeyData.Add(new InputInfo(
                    type,
                    attribute.name,
                    attribute.displayName,
                    attribute.shortDisplayName
                ));
            }

            // Handle main alias because Unity Technologies Architects are
            // a piece of shit
            if (!string.IsNullOrEmpty(attribute.alias))
            {
                PathToKeyData.Add(new InputInfo(
                    type,
                    attribute.alias,
                    attribute.displayName,
                    attribute.shortDisplayName
                ));
            }

            // Handle aliases
            if (attribute.aliases is null) return;
            for (int index = 0; index < attribute.aliases.Length; index++)
            {
                string alias = attribute.aliases[index];
                if (string.IsNullOrEmpty(alias)) continue;
                PathToKeyData.Add(new InputInfo(
                    type,
                    alias,
                    attribute.displayName,
                    attribute.shortDisplayName
                ));
            }
        }
    }
}