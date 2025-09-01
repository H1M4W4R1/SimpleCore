using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Systems.SimpleCore.Saving.Utility;

namespace Systems.SimpleCore.Saving.Abstract
{
    /// <summary>
    ///     Represents save data that can be used to create a file.
    /// </summary>
    /// <remarks>
    ///     Those markers should be used on structures that already contain all data to be saved.
    ///     For example: by saving inventory and equipment you can create custom structure
    ///     InventoryAndEquipmentData which afterward can be saved as JSON or XML using
    ///     custom save file types.
    /// </remarks>
    public interface ISaveData<[UsedImplicitly] TSaveFile> : ISaveData
        where TSaveFile : SaveFileBase
    {
        /// <summary>
        ///     Collects all data for this object, should set all fields before
        ///     <see cref="BuildSaveFile"/> can convert them into save file.
        /// </summary>
        public void CollectData();

        /// <summary>
        ///     Distributes data loaded from save file into objects it was
        ///     collected from. By default, does nothing.
        /// </summary>
        public void DistributeData()
        {
            
        }

        /// <summary>
        ///     Builds save file from previously collected data
        /// </summary>
        /// <returns>Save file</returns>
        [NotNull] public TSaveFile BuildSaveFile();

        /// <summary>
        ///     Saves the current state of the object
        /// </summary>
        /// <returns>Data of saved object</returns>
        [NotNull] public TSaveFile SaveAs()
        {
            CollectData();
            return BuildSaveFile();
        }

        /// <summary>
        ///     Parses save file into this object
        /// </summary>
        /// <param name="saveFile">File to parse</param>
        public void ParseSaveFile([NotNull] TSaveFile saveFile);
        
        /// <summary>
        ///     Loads the saved state of the object
        /// </summary>
        /// <param name="saveFile">Data of saved object</param>
        public void LoadAs([NotNull] TSaveFile saveFile)
        {
            ParseSaveFile(saveFile);
            DistributeData();
        }
    }

    /// <summary>
    ///     Represents that object can be saved and handles saving methodology
    /// </summary>
    public interface ISaveData
    {
        /// <summary>
        ///     Saves the current state of the object
        /// </summary>
        /// <returns>Save file</returns>
        [NotNull] public SaveFileBase Save() => SaveAPI.Save(this);
        
        /// <summary>
        ///     Loads the saved state of the object
        /// </summary>
        /// <param name="saveFile">File to load from</param>
        public void Load([NotNull] SaveFileBase saveFile) => SaveAPI.Load(this, saveFile);
        
        /// <summary>
        ///     Gets all supported file types for this saveable object
        /// </summary>
        /// <returns>List of supported file types</returns>
        [NotNull] internal IReadOnlyList<Type> GetAllSupportedFileTypes()
        {
            // Create list to store results
            List<Type> results = new();

            // Access type using object header, supports polymorphism
            Type thisType = GetType();

            // Get all implementations of ISaveable<TX>
            Type[] interfaces = thisType.GetInterfaces();
            for (int nInterface = 0; nInterface < interfaces.Length; nInterface++)
            {
                // Check if interface is of generic type
                Type interfaceType = interfaces[nInterface];
                if (!interfaceType.IsGenericType) continue;

                // Validate if generic type is ISaveable<T>
                Type genericType = interfaceType.GetGenericTypeDefinition();
                if (genericType == typeof(ISaveData<>))
                    results.Add(interfaceType.GetGenericArguments()[0]);
            }
            
            return results;
        }
    }
}