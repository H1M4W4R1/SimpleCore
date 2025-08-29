using JetBrains.Annotations;

namespace Systems.SimpleCore.Saving.Data.Markers
{
    /// <summary>
    ///     Added to <see cref="SaveFileBase"/> to indicate it can be upgraded to a new version
    /// </summary>
    /// <typeparam name="TToFile">Target upgraded version of save file</typeparam>
    /// <typeparam name="TFromFile">Older version of save file</typeparam>
    public interface IUpgradeableSaveFile<out TToFile, in TFromFile>
        where TToFile : SaveFileBase
        where TFromFile : SaveFileBase
    {
        /// <summary>
        ///     Gets the upgraded version of the save file
        /// </summary>
        /// <param name="originalFile">Original save file</param>
        /// <returns>Upgraded version of the save file</returns>
        public TToFile GetUpgradedVersion([NotNull] TFromFile originalFile);
    }
}