#if UNITY_EDITOR
using System;
using System.Reflection;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Systems.SimpleCore.Automation.Attributes;
using Systems.SimpleCore.Editor.Utility;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Systems.SimpleCore.Editor.Automation
{
    /// <summary>
    ///     Script used to automatically register prefabs in Addressables system
    /// </summary>
    public sealed class AutoAddressablePrefabsRegisterHandler : AssetModificationProcessor
    {
        [NotNull] private static string[] OnWillSaveAssets([NotNull] string[] paths)
        {
            // Handle all assets
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                string path = paths[pathIndex];

                // Get asset type
                Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (assetType != typeof(GameObject)) continue;
                
                // Get all components from GameObject
                GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Component[] components = gameObject.GetComponents<Component>();

                // Handle all components
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    // Get component type
                    Type componentType = components[componentIndex].GetType();
                    
                    // Check if asset has proper attribute
                    AutoAddressableObjectAttribute attribute =
                        componentType.GetCustomAttribute<AutoAddressableObjectAttribute>(true);
                    if (attribute == null) continue;

                    // Register asset in Addressables system
                    AddressableExtensions.MarkAssetAddressable(path, attribute.Path, label: attribute.Label);
                }
            }

            // Return unmodified paths
            return paths;
        }
    }
}
#endif