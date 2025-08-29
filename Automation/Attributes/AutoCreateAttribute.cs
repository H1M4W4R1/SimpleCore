using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Automation.Attributes
{
    /// <summary>
    ///     Attribute to mark ScriptableObject to be created automatically in Assets/Generated folder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class AutoCreateAttribute : AutoAddressableObjectAttribute
    {
        public AutoCreateAttribute([NotNull] string path, [CanBeNull] string label) : base(path, label)
        {
       
        }
    }
}