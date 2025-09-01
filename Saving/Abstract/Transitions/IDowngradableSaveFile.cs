using JetBrains.Annotations;

namespace Systems.SimpleCore.Saving.Abstract.Transitions
{
    /// <summary>
    ///     Indicates a save file type can be downgraded (converted) from a newer version.
    /// </summary>
    /// <typeparam name="TToFile">Target (older) save file type</typeparam>
    /// <typeparam name="TFromFile">Source (newer) save file type</typeparam>
    public interface IDowngradableSaveFile<out TToFile, in TFromFile>
        where TToFile : SaveFileBase
        where TFromFile : SaveFileBase
    {
        /// <summary>
        ///     Gets the downgraded (older) version of <paramref name="originalFile"/>.
        /// </summary>
        /// <param name="originalFile">Original (newer) save file</param>
        /// <returns>Downgraded save file instance</returns>
        TToFile GetDowngradedVersion([NotNull] TFromFile originalFile);
    }
}