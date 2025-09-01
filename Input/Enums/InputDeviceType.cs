using System;
using JetBrains.Annotations;

namespace Systems.SimpleCore.Input.Enums
{
    /// <summary>
    ///     Type of input device. Must match controller path in Unity Input System.
    ///     Eg. Keyboard will be mapped to &lt;Keyboard&gt;
    /// </summary>
    [Flags] public enum InputDeviceType : uint
    {
        [UsedImplicitly] None = 0,

        [UsedImplicitly] Keyboard = 1 << 0,
        [UsedImplicitly] Mouse = 1 << 1,
        [UsedImplicitly] Gamepad = 1 << 2,
        [UsedImplicitly] Joystick = 1 << 3,
        [UsedImplicitly] Touchscreen = 1 << 4,
        [UsedImplicitly] Pen = 1 << 5,
        [UsedImplicitly] Pointer = 1 << 6,
        [UsedImplicitly] Sensor = 1 << 7,
        [UsedImplicitly] TrackedDevice = 1 << 8,
        [UsedImplicitly] XRController = 1 << 9,
        [UsedImplicitly] XRHMD = 1 << 10,
        [UsedImplicitly] HID = 1 << 11,

        // Indicate that change was made externally and InputDeviceType is unknown.
        Unknown = (uint) 1 << 31,
        All = 0xFFFFFFFF & ~Unknown
    }
}