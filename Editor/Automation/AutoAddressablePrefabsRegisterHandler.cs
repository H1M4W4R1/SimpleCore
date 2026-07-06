#if UNITY_EDITOR
using System;
using System.Reflection;
using JetBrains.Annotations;
using Systems.SimpleCore.Automation.Attributes;
using Systems.SimpleCore.Editor.Utility;
using UnityEditor;
using UnityEngine;

namespace Systems.SimpleCore.Editor.Automation
{
    /// <summary>
    ///     Script used to automatically register prefabs in Addressables system
    /// </summary>
    [InitializeOnLoad]
    public sealed class AutoAddressablePrefabsRegisterHandler : AssetPostprocessor
    {
        static AutoAddressablePrefabsRegisterHandler()
        {
            EditorApplication.delayCall += RegisterAllPrefabs;
        }

        private static void OnPostprocessAllAssets(
            [NotNull] string[] importedAssets,
            [NotNull] string[] deletedAssets,
            [NotNull] string[] movedAssets,
            [NotNull] string[] movedFromAssetPaths)
        {
            for (int assetIndex = 0; assetIndex < importedAssets.Length; assetIndex++)
            {
                RegisterPrefabIfNeeded(importedAssets[assetIndex]);
            }

            for (int assetIndex = 0; assetIndex < movedAssets.Length; assetIndex++)
            {
                RegisterPrefabIfNeeded(movedAssets[assetIndex]);
            }
        }

        private static void RegisterAllPrefabs()
        {
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
            for (int prefabIndex = 0; prefabIndex < prefabGuids.Length; prefabIndex++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[prefabIndex]);
                RegisterPrefabIfNeeded(path);
            }
        }

        internal static void RegisterPrefabIfNeeded([CanBeNull] string path)
        {
            if (string.IsNullOrEmpty(path)) return;

            Type assetType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (assetType != typeof(GameObject)) return;

            GameObject gameObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!gameObject) return;

            Component[] components = gameObject.GetComponents<Component>();
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                Component component = components[componentIndex];
                if (!component) continue;

                Type componentType = component.GetType();
                AutoAddressableObjectAttribute attribute =
                    componentType.GetCustomAttribute<AutoAddressableObjectAttribute>(true);
                if (ReferenceEquals(attribute, null)) continue;

                AddressableExtensions.MarkAssetAddressable(path, attribute.Path, label: attribute.Label);
            }
        }
    }

    public sealed class AutoAddressablePrefabsSaveHandler : AssetModificationProcessor
    {
        [NotNull]
        private static string[] OnWillSaveAssets([NotNull] string[] paths)
        {
            for (int pathIndex = 0; pathIndex < paths.Length; pathIndex++)
            {
                AutoAddressablePrefabsRegisterHandler.RegisterPrefabIfNeeded(paths[pathIndex]);
            }

            return paths;
        }
    }
}
#endif
