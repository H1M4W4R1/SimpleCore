using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Automation.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AutoCreatedObjectAttribute : Attribute
    {
        /// <summary>
        ///     Path to create object at (prepended with Assets/Generated/)
        /// </summary>
        [NotNull] public string Path { get; }
        
        /// <summary>
        ///     Label of addressable asset
        /// </summary>
        [CanBeNull] public string Label { get; }
        
        public AutoCreatedObjectAttribute([NotNull] string path, [CanBeNull] string label)
        {
            Path = path;
            Label = label;
        }
    }
}