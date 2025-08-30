using JetBrains.Annotations;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Systems.SimpleCore.Editor.Utility
{
    /// <summary>
    /// Provides helper methods to manage Addressable assets (ScriptableObjects) in the Unity Editor, 
    /// and safe runtime-loading by key. Editor-only functionality is compiled out in player builds.
    /// </summary>
    public static class AddressableExtensions
    {
        /// <summary>
        /// Ensures the given asset is marked Addressable in the specified group and label.
        /// In the Editor, this will create the group if missing, move or create the entry, and assign the label.
        /// In a player build, this method does nothing.
        /// </summary>
        /// <param name="asset">The ScriptableObject asset to mark as Addressable.</param>
        /// <param name="groupName">The Addressables group name to use (will be created if needed).</param>
        /// <param name="address">Optional custom address for the asset (defaults to asset name if empty).</param>
        /// <param name="label">Optional label to assign to the asset.</param>
        /// <returns>True if any changes were detected, false otherwise.</returns>
        public static bool MarkAssetAddressable(
            [NotNull] string asset,
            [NotNull] string groupName,
            [CanBeNull] string address = null,
            [CanBeNull] string label = null)
        {
#if UNITY_EDITOR
            if (string.IsNullOrEmpty(asset) || string.IsNullOrEmpty(groupName)) return false;

            // Get default Addressables settings
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            bool changesDetected = false;
            
            // Find or create the group
            AddressableAssetGroup group = settings.FindGroup(groupName);
            if (group == null)
            {
                // Create a new non-default, modifiable group with default schema
                group = settings.CreateGroup(groupName, false, false, false, null, null);
                changesDetected = true;
            }

            // Get asset GUID and move/create entry
            string assetGuid = AssetDatabase.AssetPathToGUID(asset);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(assetGuid, group, false, false);

            // Skip if entry is not found
            if (entry == null) return changesDetected;

            // Optionally set custom address
            if (!string.IsNullOrEmpty(address))
            {
                if (entry.address != address)
                {
                    Debug.Log($"Address of {asset} changed from {entry.address} to {address}");
                    changesDetected = true;
                }

                entry.SetAddress(address, false);
            }

            // Assign label if provided
            if (!string.IsNullOrEmpty(label))
            {
                settings.AddLabel(label);
                if (!entry.labels.Contains(label))
                {
                    Debug.Log($"Label {label} was added to {asset}");
                    changesDetected = true;
                }

                entry.SetLabel(label, true, true);
            }

            // Mark settings dirty so changes are saved
            if (changesDetected)
            {
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
            }

            return changesDetected;
#endif
        }
    }
}