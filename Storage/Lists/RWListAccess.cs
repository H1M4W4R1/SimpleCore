using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine.Assertions;
using UnityEngine.Pool;

namespace Systems.SimpleCore.Storage.Lists
{
    /// <summary>
    ///     Read-write list access structure for lists that are managed by Unity Pooling system.
    ///     TODO: When Unity finally updates C# add IDisposable here
    /// </summary>
    public ref struct RWListAccess<TListType>
    {
        /// <summary>
        ///     Access to the list
        /// </summary>
        [NotNull] private List<TListType> _list;

        /// <summary>
        ///     Checks if the access is valid
        /// </summary>
        public bool IsValid => _list != null;

        /// <summary>
        ///     List access
        /// </summary>
        [NotNull] public List<TListType> List
        {
            get
            {
                Assert.IsTrue(IsValid, "Access is not valid");
                return _list;
            }
        }

        public ROListAccess<TListType> ToReadOnly() => new(this);
        
        internal RWListAccess([NotNull] List<TListType> list)
        {
            _list = list;
        }

        public static RWListAccess<TListType> Create() => new(ListPool<TListType>.Get());

        public void Release()
        {
            ListPool<TListType>.Release(_list);
            _list = null!;
        }
    }
}