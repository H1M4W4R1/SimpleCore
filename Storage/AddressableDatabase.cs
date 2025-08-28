using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Assertions;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Systems.SimpleCore.Storage
{
    /// <summary>
    ///     Database of all items in game
    /// </summary>
    public abstract class AddressableDatabase<TSelf, TScriptableObject>
        where TSelf : AddressableDatabase<TSelf, TScriptableObject>, new()
        where TScriptableObject : ScriptableObject
    {
        /// <summary>
        ///     Label of addressable assets
        /// </summary>
        protected abstract string AddressableLabel { get; }
        
        /// <summary>
        ///     Internal data storage
        /// </summary>
        protected static readonly List<TScriptableObject> internalDataStorage = new();

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
        
        private AsyncOperationHandle<IList<TScriptableObject>> _loadRequest;

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
            _loadRequest = Addressables.LoadAssetsAsync<TScriptableObject>(
                    new[] {AddressableLabel}, OnItemLoaded,
                    Addressables.MergeMode.Union);
            
            // Check if request is complete
            if(_loadRequest.IsDone) OnItemsLoadComplete(_loadRequest);
            else _loadRequest.Completed += OnItemsLoadComplete;
        }
        
        /// <summary>
        ///     Loads all items from Resources folder
        /// </summary>
        private void LoadSynchronously()
        {
            StartLoading();
            _loadRequest.WaitForCompletion();
            
            // Mark load request as complete if it is not already
            if(!_isLoadingComplete)
                OnItemsLoadComplete(_loadRequest);
        }

        private void OnItemsLoadComplete(AsyncOperationHandle<IList<TScriptableObject>> _)
        {
            // Sort after loading to ensure binary search works correctly
            internalDataStorage.Sort();
            _isLoaded = true;
            _isLoading = false;
            _isLoadingComplete = true;
        }

        private void OnItemLoaded<TObject>(TObject obj)
        {
            if (obj is not TScriptableObject item) return;
            internalDataStorage.Add(item);
        }

        /// <summary>
        ///     Gets first item of specified type
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public static TItemType Get<TItemType>()
            where TItemType : TScriptableObject =>
            _instance._GetItem<TItemType>();

        /// <summary>
        ///     Gets first item of specified type
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public TItemType _GetItem<TItemType>()
            where TItemType : TScriptableObject
        {
            EnsureLoaded();

            // Loop through all items
            for (int i = 0; i < internalDataStorage.Count; i++)
            {
                if (internalDataStorage[i] is TItemType item) return item;
            }

            Assert.IsNotNull(null, "Item not found in database");
            return null;
        }
        
        /// <summary>
        ///     Gets all items of specified type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Read-only list of items of specified type</returns>
        [NotNull] public static IReadOnlyList<TItemType> GetAll<TItemType>()
            where TItemType : TScriptableObject =>
            _instance._GetAllItems<TItemType>();

        /// <summary>
        ///     Gets all items of specified type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Read-only list of items of specified type</returns>
        [NotNull] public IReadOnlyList<TItemType> _GetAllItems<TItemType>()
            where TItemType : TScriptableObject
        {
            EnsureLoaded();

            List<TItemType> items = new();

            // Loop through all items
            for (int i = 0; i < internalDataStorage.Count; i++)
            {
                if (internalDataStorage[i] is TItemType item) items.Add(item);
            }

            return items;
        }
    }
}