using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Automation.Attributes
{
    /// <summary>
    ///     Used to mark object prefab to be automatically registered in Addressables system
    ///     also works for ScriptableObjects
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class AutoAddressableObjectAttribute : Attribute
    {
        /// <summary>
        ///     Path to create object at (prepended with Assets/Generated/)
        /// </summary>
        [NotNull] public string Path { get; }
        
        /// <summary>
        ///     Label of addressable asset
        /// </summary>
        [CanBeNull] public string Label { get; }
        
        public AutoAddressableObjectAttribute([NotNull] string path, [CanBeNull] string label)
        {
            Path = path;
            Label = label;
        }
    }
}