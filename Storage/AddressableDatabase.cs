using System.Collections.Generic;
using JetBrains.Annotations;
using Systems.SimpleCore.Identifiers;
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
            if (_loadRequest.IsDone)
                OnItemsLoadComplete(_loadRequest);
            else
                _loadRequest.Completed += OnItemsLoadComplete;
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

        private void OnItemsLoadComplete(AsyncOperationHandle<IList<TScriptableObject>> _)
        {
            // Sort after loading to ensure binary search works correctly
            internalDataStorage.Sort((a, b) => HashIdentifier.New(a).CompareTo(HashIdentifier.New(b)));
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
        /// <remarks>
        ///     Uses fast searching methodology, so it works only for items that are not abstact,
        ///     for abstract items use <see cref="GetAbstract{TItemType}"/>
        /// </remarks>
        [CanBeNull] public static TItemType GetExact<TItemType>()
            where TItemType : TScriptableObject, new() =>
            GetFast<TItemType>();

        /// <summary>
        ///     Gets first item of specified type. To get items by interface
        ///     use <see cref="GetAbstractUnsafe{TItemType}"/>
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public static TItemType GetAbstract<TItemType>()
            where TItemType : TScriptableObject =>
            _instance._GetItem<TItemType>();

        /// <summary>
        ///     Gets first item of specified type
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public static TItemType GetAbstractUnsafe<TItemType>()
            => _instance._GetItem<TItemType>();
        
        /// <summary>
        ///     Gets first item of specified type.
        /// </summary>
        /// <typeparam name="TItemType">Item type to get </typeparam>
        /// <returns>First item of specified type or null if no item of specified type is found</returns>
        [CanBeNull] public TItemType _GetItem<TItemType>()
        {
            EnsureLoaded();

            // Loop through all items
            for (int i = 0; i < internalDataStorage.Count; i++)
            {
                if (internalDataStorage[i] is TItemType item) return item;
            }

            Assert.IsNotNull(null, "Item not found in database");
            return default;
        }

        /// <summary>
        ///     Gets all items of specified type. To get items by interface
        ///     see <see cref="GetAllUnsafe{TItemType}"/>
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
        [NotNull] public static IReadOnlyList<TItemType> GetAllUnsafe<TItemType>()
            => _instance._GetAllItems<TItemType>();

        /// <summary>
        ///     Gets all items of specified type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Read-only list of items of specified type</returns>
        [NotNull] private IReadOnlyList<TItemType> _GetAllItems<TItemType>()
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

        /// <summary>
        ///     Gets item by type
        /// </summary>
        /// <typeparam name="TItemType">Type of item to get</typeparam>
        /// <returns>Item with given identifier or null if not found</returns>
        [CanBeNull] public static TItemType GetFast<TItemType>()
            where TItemType : TScriptableObject
        {
            _instance.EnsureLoaded();
            HashIdentifier hashIdentifier = HashIdentifier.New(typeof(TItemType));

            int low = 0;
            int high = internalDataStorage.Count - 1;
            int foundMid = -1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                TScriptableObject midItem = internalDataStorage[mid];

                // Get object hash
                HashIdentifier midItemHash = HashIdentifier.New(midItem);

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
            if (foundMid == -1) return null;

            // Search for first item of type TItemType
            for (int n = foundMid; n < internalDataStorage.Count; n++)
                if (internalDataStorage[n] is TItemType item)
                    return item;

            // If not found, return null
            return null;
        }

        /// <summary>
        ///     Gets item by identifier
        /// </summary>
        /// <param name="hashIdentifier">Identifier of item to get</param>
        /// <returns>Item with given identifier or null if not found</returns>
        [CanBeNull] public static TScriptableObject GetFast(HashIdentifier hashIdentifier)
        {
            _instance.EnsureLoaded();

            int low = 0;
            int high = internalDataStorage.Count - 1;

            while (low <= high)
            {
                int mid = (low + high) >> 1;
                TScriptableObject midItem = internalDataStorage[mid];

                // Get object hash
                HashIdentifier midItemHash = HashIdentifier.New(midItem);

                int cmp = midItemHash.CompareTo(hashIdentifier);
                if (cmp == 0) return midItem;
                if (cmp < 0)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return null;
        }
    }
}