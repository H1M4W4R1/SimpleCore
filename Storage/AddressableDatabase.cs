using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.Exceptions;
using Object = UnityEngine.Object;

namespace Systems.SimpleCore.Storage
{
    public abstract class
        AddressableDatabase<TSelf, TUnityObject> : AddressableDatabase<TSelf, TUnityObject, TUnityObject>
        where TSelf : AddressableDatabase<TSelf, TUnityObject, TUnityObject>, new()
        where TUnityObject : Object
    {
    }

    public abstract class AddressableDatabase<TSelf, TUnityObject, TLoadType>
        where TSelf : AddressableDatabase<TSelf, TUnityObject, TLoadType>, new()
        where TUnityObject : Object
        where TLoadType : Object
    {
        /// <summary>
        ///     Quick access to instance
        /// </summary>
        public static TSelf Instance => _instance;

        /// <summary>
        ///     Label of addressable assets
        /// </summary>
        protected abstract string AddressableLabel { get; }

        /// <summary>
        ///     Internal data storage
        /// </summary>
        protected static readonly List<AddressableDatabaseEntry<TUnityObject>> internalDataStorage = new();

        /// <summary>
        ///     Instance of this database
        /// </summary>
        protected static readonly TSelf _instance = new();

        /// <summary>
        ///     If true this means that all items have been loaded
        /// </summary>
        private bool _isLoaded;

        /// <summary>
        ///     If true this means that items are currently being loaded
        /// </summary>
        private bool _isLoading;

        /// <summary>
        ///     True if loading is complete
        /// </summary>
        private bool _isLoadingComplete;

        private AsyncOperationHandle<IList<TLoadType>> _loadRequest;

        /// <summary>
        ///     Gets loading progress
        /// </summary>
        public static float LoadProgress
            => _instance._loadRequest.IsValid() ? _instance._loadRequest.PercentComplete : 0;

        /// <summary>
        ///     Amount of entries in database, way off the real item count
        ///     as base types are registered as well
        /// </summary>
        public static int Count => _instance._Count;

        /// <summary>
        ///     Total number of items in database
        /// </summary>
        protected int _Count
        {
            get
            {
                EnsureLoaded();
                return internalDataStorage.Count;
            }
        }

        /// <summary>
        ///     Ensures that all items are loaded
        /// </summary>
        protected void EnsureLoaded()
        {
            if (!_isLoaded) LoadSynchronously();
        }

        private void StartLoading()
        {
            // Prevent multiple loads
            if (_isLoading) return;
            _isLoading = true;
            _isLoadingComplete = false;

            // Load items
            try
            {
                Assert.IsFalse(typeof(MonoBehaviour).IsAssignableFrom(typeof(TLoadType)),
                    "This won't work properly. Use GameObject as base type and cast it in OnItemLoaded");

                _loadRequest = Addressables.LoadAssetsAsync<TLoadType>(
                    new[] {AddressableLabel}, OnItemLoaded,
                    Addressables.MergeMode.Union);

                // Check if request is complete
                if (_loadRequest.IsDone)
                    OnItemsLoadComplete(_loadRequest);
                else
                    _loadRequest.Completed += OnItemsLoadComplete;
            }
            catch (OperationException)
            {
                _isLoading = false;
                _isLoadingComplete = true;
            }
        }

        /// <summary>
        ///     Loads all items from Resources folder
        /// </summary>
        private void LoadSynchronously()
        {
            StartLoading();
            _loadRequest.WaitForCompletion();

            // Mark load request as complete if it is not already
            if (!_isLoadingComplete) OnItemsLoadComplete(_loadRequest);
        }

        private void OnItemsLoadComplete(AsyncOperationHandle<IList<TLoadType>> _)
        {
            // Sort after loading to ensure binary search works correctly
            internalDataStorage.Sort((a, b) => a.hashIdentifier.CompareTo(b.hashIdentifier));
            _isLoaded = true;
            _isLoading = false;
            _isLoadingComplete = true;
        }

        protected void OnItemLoaded<TObject>(TObject obj)
        {
            // Handle game object
            if (obj is GameObject gameObj)
            {
                TUnityObject item = gameObj.GetComponent<TUnityObject>();
                if (ReferenceEquals(item, null)) return;
                RegisterItem(item);
                return;
            }

            if (obj is not TUnityObject validItem) return;
            RegisterItem(validItem);
        }

        private void RegisterItem([NotNull] TUnityObject item)
        {
            // Register base item
            internalDataStorage.Add(
                new AddressableDatabaseEntry<TUnityObject>(HashIdentifier.New(item.GetType()), item));

            // Handle Unity Textures, Sprites etc.
            if (item.GetType() == typeof(TUnityObject)) return;
            
            // Now handle all base types
            Type baseType = item.GetType().BaseType;

            // Handle all base types until core type is found
            while (baseType != typeof(TUnityObject))
            {
                // Prevent null types (just in case)
                if (baseType == null) break;

                internalDataStorage.Add(
                    new AddressableDatabaseEntry<TUnityObject>(HashIdentifier.New(baseType), item));
                baseType = baseType.BaseType;
            }
        }

        /// <summary>
        ///     Gets first item of specified type
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        /// <remarks>
        ///     Uses fast searching methodology, so it works only for items that are not abstact,
        ///     for abstract items use <see cref="GetAny{TItemType}"/>
        /// </remarks>
        [CanBeNull] public static TItemType GetExact<TItemType>()
            where TItemType : TUnityObject, new() =>
            GetFirstFast<TItemType>();

        /// <summary>
        ///     Gets first item of specified type. 
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public static TItemType GetAny<TItemType>()
            where TItemType : TUnityObject =>
            GetFirstFast<TItemType>();

        /// <summary>
        ///     Gets all items of specified type. 
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Read-only list of items of specified type</returns>
        /// <remarks>
        ///     Using base type aka. <see cref="TUnityObject"/> won't work for this method.
        /// </remarks>
        public static ROListAccess<TItemType> GetAll<TItemType>()
            where TItemType : TUnityObject =>
            _instance.GetAllFast<TItemType>();

        /// <summary>
        ///     Gets all items of specified type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Read-only list of items of specified type</returns>
        private ROListAccess<TItemType> GetAllFast<TItemType>()
        {
            EnsureLoaded();

            RWListAccess<TItemType> list = RWListAccess<TItemType>.Create();
            List<TItemType> refList = list.List;

            // Get first item
            int firstItem = GetFirstIndexFast<TItemType>();
            if (firstItem == -1) return list.ToReadOnly();
            
            while (firstItem < internalDataStorage.Count && internalDataStorage[firstItem].entryObject is TItemType item)
            {
                refList.Add(item);
                firstItem++;
            }

            return list.ToReadOnly();
        }

        /// <summary>
        ///     Gets item by type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Item with given identifier or null if not found</returns>
        [CanBeNull] private static TItemType GetFirstFast<TItemType>()
            where TItemType : TUnityObject
        {
            int foundItem = GetFirstIndexFast<TItemType>();

            // Prevent out of bounds
            if (foundItem == -1) return null;

            // Return item
            return internalDataStorage[foundItem].entryObject as TItemType;
        }

        private static int GetFirstIndexFast<TItemType>()
        {
            _instance.EnsureLoaded();
            HashIdentifier hashIdentifier = HashIdentifier.New(typeof(TItemType));

            int low = 0;
            int high = internalDataStorage.Count - 1;
            int foundMid = -1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                AddressableDatabaseEntry<TUnityObject> midItem = internalDataStorage[mid];

                // Get object hash
                HashIdentifier midItemHash = midItem.hashIdentifier;

                int cmp = midItemHash.CompareTo(hashIdentifier);
                if (cmp == 0)
                {
                    foundMid = mid;
                    break;
                }

                if (cmp < 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            // If not found, return null
            if (foundMid == -1) return -1;

            // Run backward until first item is found
            while (foundMid >= 0 && internalDataStorage[foundMid].entryObject is TItemType) foundMid--;

            foundMid++;

            // If not found, return null
            if (foundMid >= internalDataStorage.Count) return -1;

            return foundMid;
        }
    }
}