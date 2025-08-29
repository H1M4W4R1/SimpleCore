using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Saving.Data.Markers
{
    /// <summary>
    ///     Optional interface an ISaveable implementation may expose to indicate its default save-file type.
    /// </summary>
    public interface IHasDefaultSaveFile
    {
        /// <summary>
        ///     Returns the default save file Type (derived from SaveFileBase) to be used when no explicit target is provided.
        /// </summary>
        [CanBeNull] Type DefaultSaveFileType { get; }
    }
}