using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Saving.Abstract
{
    /// <summary>
    ///     Optional interface an <see cref="ISaveData"/> implementation may expose to indicate
    ///     its default save-file type.
    /// </summary>
    public interface IHasDefaultSaveFile
    {
        /// <summary>
        ///     Returns the default save file Type (derived from SaveFileBase) to be used when no explicit target is provided.
        /// </summary>
        [CanBeNull] public Type DefaultSaveFileType { get; }
    }
}