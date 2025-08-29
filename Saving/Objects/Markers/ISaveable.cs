using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Systems.SimpleCore.Saving.Data;

namespace Systems.SimpleCore.Saving.Objects.Markers
{
    /// <summary>
    ///     Represents that object can be saved and handles saving methodology
    /// </summary>
    public interface ISaveable<[UsedImplicitly] TSaveFile> : ISaveable
        where TSaveFile : SaveFileBase
    {
        // TODO: Automate this using attributes with a separate file type?
        
        /// <summary>
        ///     Saves the current state of the object
        /// </summary>
        /// <returns>Data of saved object</returns>
        [NotNull] protected internal TSaveFile Save();

        /// <summary>
        ///     Loads the saved state of the object
        /// </summary>
        /// <param name="saveFile">Data of saved object</param>
        protected internal void Load([NotNull] TSaveFile saveFile);
    }

    /// <summary>
    ///     Represents that object can be saved and handles saving methodology
    /// </summary>
    public interface ISaveable
    {
        /// <summary>
        ///     Checks if this object can be saved as the given type
        /// </summary>
        /// <typeparam name="TSaveFile">Type of save file</typeparam>
        /// <returns>True if this object can be saved as the given type</returns>
        public bool CanBeSavedExactlyAs<TSaveFile>()
            where TSaveFile : SaveFileBase => this is ISaveable<TSaveFile>;
        
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
                if (genericType == typeof(ISaveable<>))
                    results.Add(interfaceType.GetGenericArguments()[0]);
            }
            
            return results;
        }
    }
}