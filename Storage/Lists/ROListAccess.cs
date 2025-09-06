using System.Collections.Generic;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Storage.Lists
{
    /// <summary>
    ///     Read-only list access structure for lists that are managed by Unity Pooling system.
    ///     TODO: When Unity finally updates C# add IDisposable here
    /// </summary>
    public ref struct ROListAccess<TListType>
    {
        /// <summary>
        ///     RW access to the list
        /// </summary>
        private RWListAccess<TListType> _access;

        /// <summary>
        ///     Access to the list
        /// </summary>
        [NotNull] public IReadOnlyList<TListType> List => _access.List;

        internal ROListAccess(RWListAccess<TListType> access)
        {
            _access = access;
        }

        public static ROListAccess<TListType> Create()
        {
            // Create new RW access
            RWListAccess<TListType> access = RWListAccess<TListType>.Create();
            return new ROListAccess<TListType>(access);
        }

        public void Release()
        {
            _access.Release();
        }
    }
}