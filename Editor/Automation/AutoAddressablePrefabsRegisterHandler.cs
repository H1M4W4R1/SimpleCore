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
                if (assetType == null) continue;
                
                // Check if asset has proper attribute
                AutoAddressableObjectAttribute attribute = assetType.GetCustomAttribute<AutoAddressableObjectAttribute>(true);
                if (attribute == null) continue;
                
                // Get asset from path
                Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                
                // Register asset in Addressables system
                AddressableExtensions.MarkAssetAddressable(path, attribute.Path, label: attribute.Label);
            }

            // Return unmodified paths
            return paths;
        }
    }
}
#endif