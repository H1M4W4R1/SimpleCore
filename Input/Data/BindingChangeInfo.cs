using JetBrains.Annotations;
using Systems.SimpleCore.Input.Enums;
using UnityEngine.InputSystem;

namespace Systems.SimpleCore.Input.Data
{
    /// <summary>
    ///     Information about binding change.
    /// </summary>
    public readonly ref struct BindingChangeInfo
    {
        /// <summary>
        ///     Action that has binding changed.
        /// </summary>
        [NotNull] public readonly InputAction action;
        
        /// <summary>
        ///     Index of binding that has changed.
        /// </summary>
        public readonly int bindingIndex;
        
        /// <summary>
        ///     Pass-through device limits
        /// </summary>
        public readonly InputDeviceType allowedDevices;
        
        /// <summary>
        ///     Old effective path of binding, can be <see cref="string.Empty"/> if not known.
        /// </summary>
        [NotNull] public readonly string oldEffectivePath;
        
        /// <summary>
        ///     New effective path of binding.
        /// </summary>
        [NotNull] public readonly string newEffectivePath;

        public BindingChangeInfo(
            [NotNull] InputAction action,
            int bindingIndex,
            InputDeviceType allowedDevices,
            [NotNull] string oldEffectivePath,
            [NotNull] string newEffectivePath)
        {
            this.action = action;
            this.bindingIndex = bindingIndex;
            this.allowedDevices = allowedDevices;
            this.oldEffectivePath = oldEffectivePath;
            this.newEffectivePath = newEffectivePath;
        }
    }
}