using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using Systems.SimpleCore.Input.Data;
using Systems.SimpleCore.Input.Enums;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Composites;

namespace Systems.SimpleCore.Input
{
    /// <summary>
    ///     Helper class used as proxy layer between stupid UnityEngine.InputSystem and
    ///     any reasonable input handling methodology. 
    /// </summary>
    public static class InputAPI
    {
        private const string ESCAPE = "<Keyboard>/escape";
        private const string ANY_KEY = "<Keyboard>/anyKey";

#region Events

        public delegate void OnBindingChangeStartedEventHandler(BindingChangeInfo bindingChangeInfo);

        public delegate void OnBindingChangeCancelledEventHandler(BindingChangeInfo bindingChangeInfo);

        public delegate void OnBindingChangeDuplicateFoundEventHandler(BindingChangeInfo bindingChangeInfo);

        public delegate void OnBindingChangeCompletedEventHandler(BindingChangeInfo bindingChangeInfo);

        public delegate void OnBindingResetEventHandler(BindingChangeInfo bindingChangeInfo);

        /// <summary>
        ///     Event that is raised when binding change has started.
        /// </summary>
        public static event OnBindingChangeStartedEventHandler OnBindingChangeStartedGlobalEvent;

        /// <summary>
        ///     Event that is raised when binding change is cancelled.
        /// </summary>
        public static event OnBindingChangeCancelledEventHandler OnBindingChangeCancelledGlobalEvent;

        /// <summary>
        ///     Event that is raised when binding change duplicate is found.
        /// </summary>
        public static event OnBindingChangeDuplicateFoundEventHandler OnBindingDuplicateFoundGlobalEvent;

        /// <summary>
        ///     Event that is raised when binding change is completed.
        /// </summary>
        public static event OnBindingChangeCompletedEventHandler OnBindingChangeCompletedGlobalEvent;

        /// <summary>
        ///     Event that is raised when binding is reset to default.
        /// </summary>
        public static event OnBindingResetEventHandler OnBindingResetGlobalEvent;

#endregion

        /// <summary>
        ///     If set to true, the rebind operation will be cancelled if the device is not allowed.
        ///     Otherwise, the rebind operation will continue and the device will be ignored.
        /// </summary>
        public static bool CancelIfDeviceIsNotAllowed { get; set; } = true;

        /// <summary>
        ///     Rebinding operation that is already in progress.
        /// </summary>
        private static InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

        /// <summary>
        ///     Initializes the input API. Call this method at the start of the application
        ///     to ensure that input system is properly initialized.
        /// </summary>
        public static void Initialize()
        {
            // Attach to input system action change event
            InputSystem.onActionChange += OnInputActionChanged;

            // Ensure that rebinding operation is disposed when application quits
            Application.quitting += () =>
            {
                _rebindingOperation?.Cancel();
                _rebindingOperation?.Dispose();
                _rebindingOperation = null;

                // Unsubscribe from input system action change event
                InputSystem.onActionChange -= OnInputActionChanged;
            };
        }

        /// <summary>
        ///     Checks if device is valid for specified binding
        /// </summary>
        /// <param name="actionReference">Action to check device for</param>
        /// <param name="bindingIndex">Index of binding to check device for</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <param name="ignoreOverrides">Will check default path (not effective)</param>
        /// <returns>True if device is valid for specified binding</returns>
        public static bool IsValidDevice(
            [NotNull] this InputActionReference actionReference,
            int bindingIndex,
            InputDeviceType allowedDevices,
            bool ignoreOverrides = false)
        {
            return IsValidDevice(actionReference.action, bindingIndex, allowedDevices, ignoreOverrides);
        }

        /// <summary>
        ///     Checks if device is valid for specified binding
        /// </summary>
        /// <param name="action">Action to check device for</param>
        /// <param name="bindingIndex">Index of binding to check device for</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <param name="ignoreOverrides">Will check default path (not effective)</param>
        /// <returns>True if device is valid for specified binding</returns>
        private static bool IsValidDevice(
            [NotNull] this InputAction action,
            int bindingIndex,
            InputDeviceType allowedDevices,
            bool ignoreOverrides = false)
        {
            // Handle binding
            Assert.IsTrue(bindingIndex >= 0 && bindingIndex < action.bindings.Count,
                "Binding index is out of range");

            InputBinding binding = action.bindings[bindingIndex];

            // Handle composites - when we reach first binding of composite that is registered for this device
            // we should return true to ensure that everything is fine...
            //
            // Usually this will be first found binding.
            if (binding.isComposite)
            {
                int traversingIndex = bindingIndex + 1;
                while (traversingIndex < action.bindings.Count &&
                       action.bindings[traversingIndex].isPartOfComposite)
                {
                    if (IsValidDevice(action, traversingIndex, allowedDevices, ignoreOverrides)) return true;
                    traversingIndex++;
                }

                return false;
            }


            // Simple binding, check device data
            Regex regex = new(@"(\<[^\<\>]*\>)|(\/[^\/]*\/)");
            MatchCollection matches = regex.Matches(ignoreOverrides
                ? binding.path
                : binding.effectivePath);

            // Handle check if device for this binding is correct
            string deviceName = matches.Count > 0 ? matches[0].Value : string.Empty;
            deviceName = deviceName.Trim('/', '<', '>');

            if (string.IsNullOrEmpty(deviceName)) return false;

            // Check if is one of non-allowed input devices
            for (byte i = 0; i < 31; i++)
            {
                // Get enum value
                InputDeviceType deviceType = (InputDeviceType) (1 << i);

                // Get device name from enum value
                string localDeviceName = deviceType.ToString();

                // Check if device name should be skipped as enum value is not defined
                if (string.IsNullOrEmpty(localDeviceName)) continue;

                // Check if device is not allowed
                if ((allowedDevices & deviceType) == 0) continue;
                if (!string.Equals(localDeviceName, deviceName, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                return true;
            }

            return false;
        }

        /// <summary>
        ///     Gets display name of the binding.
        /// </summary>
        /// <param name="actionReference">Reference to action to get binding for</param>
        /// <param name="preferShortNames">Will use short names when possible</param>
        /// <param name="ignoreOverrides">Will return default binding</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <returns>Display name of the binding or empty string if binding is not valid</returns>
        [NotNull] public static string GetBindingDisplayName(
            [NotNull] this InputActionReference actionReference,
            bool preferShortNames = true,
            bool ignoreOverrides = false,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            if (GetBindingFromAction(actionReference.action, allowedDevices, ignoreOverrides,
                    out int bindingIndex))
                return GetBindingDisplayName(actionReference.action, bindingIndex, preferShortNames,
                    ignoreOverrides,
                    allowedDevices);

            return string.Empty;
        }

        /// <summary>
        ///     Gets display name of the binding.
        /// </summary>
        /// <param name="actionReference">Reference to action to get binding for</param>
        /// <param name="bindingIndex">Index of binding to get display name for</param>
        /// <param name="preferShortNames">Will use short names when possible</param>
        /// <param name="ignoreOverrides">Will return default binding</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <returns>Display name of the binding or empty string if binding is not valid</returns>
        [NotNull] public static string GetBindingDisplayName(
            [NotNull] this InputActionReference actionReference,
            int bindingIndex,
            bool preferShortNames = true,
            bool ignoreOverrides = false,
            InputDeviceType allowedDevices = InputDeviceType.All) =>
            GetBindingDisplayName(actionReference.action, bindingIndex, preferShortNames, ignoreOverrides,
                allowedDevices);

        /// <summary>
        ///     Gets display name of the binding.
        /// </summary>
        /// <param name="action">Action to get binding for</param>
        /// <param name="preferShortNames">Will use short names when possible</param>
        /// <param name="ignoreOverrides">Will return default binding</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <returns>Display name of the binding or empty string if binding is not valid</returns>
        /// <remarks>
        ///     Won't work for overcomplicated modifier bindings such as modifier + axis or modifier + Vector2...
        ///     Will print all bindings related to device, so e.g. Arrows and WASD should be two separate composites.
        ///     <br/><br/>
        ///     This method also can't handle */Cancel and similar bindings as those are weird... Just TF assign separate
        ///     bindings for each controller and use generic as backup.
        ///     <br/><br/>
        ///     Pass-through should work correctly, but is considered undefined behavior.
        /// </remarks>
        [NotNull] private static string GetBindingDisplayName(
            [NotNull] this InputAction action,
            bool preferShortNames = true,
            bool ignoreOverrides = false,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            if (GetBindingFromAction(action, allowedDevices, ignoreOverrides, out int bindingIndex))
                return GetBindingDisplayName(action, bindingIndex, preferShortNames, ignoreOverrides,
                    allowedDevices);

            return string.Empty;
        }

        /// <summary>
        ///     Gets display name of the binding.
        /// </summary>
        /// <param name="action">Action to get binding for</param>
        /// <param name="bindingIndex">Index of binding to get display name for</param>
        /// <param name="preferShortNames">Will use short names when possible</param>
        /// <param name="ignoreOverrides">Will return default binding</param>
        /// <param name="allowedDevices">Allowed devices</param>
        /// <returns>Display name of the binding or empty string if binding is not valid</returns>
        [NotNull] private static string GetBindingDisplayName(
            [NotNull] this InputAction action,
            int bindingIndex,
            bool preferShortNames = true,
            bool ignoreOverrides = false,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Check if binding index is valid (within action bindings range)
            Assert.IsTrue(bindingIndex >= 0 && bindingIndex < action.bindings.Count,
                $"Invalid binding index '{bindingIndex}' for '{action.name}'");

            // Check if device is allowed, if not skip
            if (!IsValidDevice(action, bindingIndex, allowedDevices)) return string.Empty;

            // We found composite binding
            if (action.bindings[bindingIndex].isComposite)
            {
                // Convert name into type and decide what shit happens next
                StringBuilder result = new();
                string splitString = " / ";

                // Shitty way, but should work correctly
                InputActionType type = action.type;

                // Don't question my sanity, Unity Input System is so crappy package that anything they did
                // was probably better than this useless piece of feature-lacking shit
                splitString = type is InputActionType.Value ? " / " : " + ";

                int traversingIndex = bindingIndex + 1;

                // Handle binding with modifiers
                while (traversingIndex < action.bindings.Count &&
                       action.bindings[traversingIndex].isPartOfComposite)
                {
                    // Get binding name directly
                    string bindingDisplayName = GetBindingDisplayNameInternal(action, traversingIndex,
                        preferShortNames, ignoreOverrides);

                    // We shouldn't append empty bindings, it would look like shit
                    if (!string.IsNullOrEmpty(bindingDisplayName))
                    {
                        if (result.Length == 0)
                            result.Append(bindingDisplayName);
                        else
                            result.Append(splitString).Append(bindingDisplayName);
                    }

                    traversingIndex++;
                }

                return result.ToString();
            }

            // Default case
            return GetBindingDisplayNameInternal(action, bindingIndex, preferShortNames, ignoreOverrides);
        }

        /// <summary>
        ///     Tries to get binding display name
        /// </summary>
        /// <param name="action">Action to get binding display name from</param>
        /// <param name="bindingIndex">Index of binding to get display name from</param>
        /// <param name="preferShortNames">Prefer short name if available</param>
        /// <param name="ignoreOverrides">Ignore overrides</param>
        /// <returns>Binding display name</returns>
        [NotNull] private static string GetBindingDisplayNameInternal(
            [NotNull] this InputAction action,
            int bindingIndex,
            bool preferShortNames = true,
            bool ignoreOverrides = false)
        {
            // Create options data from parameters
            InputBinding.DisplayStringOptions options =
                preferShortNames ? default : InputBinding.DisplayStringOptions.DontUseShortDisplayNames;
            if (ignoreOverrides) options |= InputBinding.DisplayStringOptions.IgnoreBindingOverrides;

            Assert.IsTrue(bindingIndex >= 0 && bindingIndex < action.bindings.Count,
                $"Binding {bindingIndex} is invalid for action {action.name}");

            // Get binding to make it easier to read
            InputBinding binding = action.bindings[bindingIndex];

            // We don't support composite types here
            Assert.IsFalse(binding.isComposite);

            string effectivePath = ignoreOverrides
                ? binding.path
                : binding.effectivePath;

            string displayName = InputNames.GetDisplayName(effectivePath, preferShortNames);
            if (string.IsNullOrEmpty(displayName))
                displayName = action.GetBindingDisplayString(bindingIndex, options);
            if (string.IsNullOrEmpty(displayName)) displayName = binding.name;
            return displayName;
        }

        /// <summary>
        ///     Used to rebind the provided action with default binding (at index 0).
        /// </summary>
        public static bool Rebind(
            [NotNull] this InputActionReference reference,
            InputDeviceType allowedDevices = InputDeviceType.All,
            [CanBeNull] OnBindingChangeStartedEventHandler onBindingChangeStarted = null,
            [CanBeNull] OnBindingChangeCancelledEventHandler onBindingChangeCancelled = null,
            [CanBeNull] OnBindingChangeCompletedEventHandler onBindingChangeCompleted = null,
            [CanBeNull] OnBindingChangeDuplicateFoundEventHandler onBindingDuplicateFound = null)
        {
            InputAction action = reference.action;
            if (action == null) return false;

            // Register events
            OnBindingChangeStartedGlobalEvent += onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent += onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent += onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent += onBindingDuplicateFound;

            bool result = action.Rebind(allowedDevices);

            // Detach events
            OnBindingChangeStartedGlobalEvent -= onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent -= onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent -= onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent -= onBindingDuplicateFound;

            return result;
        }

        /// <summary>
        ///     Used to rebind the provided action with default binding (at index 0).
        /// </summary>
        private static bool Rebind(
            [NotNull] this InputAction action,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Get action bindings count
            Assert.IsTrue(action.bindings.Count == 1,
                $"Cannot rebind action '{action.name}' with multiple bindings. " +
                $"You need to specify binding name or index.");

            // Rebind action
            return action.Rebind(0, allowedDevices);
        }

        /// <summary>
        ///     Used to rebind the provided action with the provided binding name.
        ///     Requires map to be disabled before rebind.
        /// </summary>
        public static bool Rebind(
            [NotNull] this InputActionReference reference,
            [NotNull] string bindingName,
            InputDeviceType allowedDevices = InputDeviceType.All,
            [CanBeNull] OnBindingChangeStartedEventHandler onBindingChangeStarted = null,
            [CanBeNull] OnBindingChangeCancelledEventHandler onBindingChangeCancelled = null,
            [CanBeNull] OnBindingChangeCompletedEventHandler onBindingChangeCompleted = null,
            [CanBeNull] OnBindingChangeDuplicateFoundEventHandler onBindingDuplicateFound = null)
        {
            // Get action from reference
            InputAction action = reference.action;
            if (action == null) return false;

            // Register events
            OnBindingChangeStartedGlobalEvent += onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent += onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent += onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent += onBindingDuplicateFound;

            bool result = action.Rebind(bindingName, allowedDevices);

            // Detach events
            OnBindingChangeStartedGlobalEvent -= onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent -= onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent -= onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent -= onBindingDuplicateFound;

            return result;
        }

        /// <summary>
        ///     Starts to rebind the provided action with the provided binding name.
        ///     Requires map to be disabled before rebind.
        /// </summary>
        private static bool Rebind(
            [NotNull] this InputAction action,
            [NotNull] string bindingName,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Get binding index from action and binding name
            return GetBindingFromAction(action, bindingName, out int bindingIndex) &&
                   Rebind(action, bindingIndex, allowedDevices);
        }

        /// <summary>
        ///     Starts to rebind the provided action with the provided binding name.
        ///     Requires map to be disabled before rebind.
        /// </summary>
        public static bool Rebind(
            [NotNull] this InputActionReference reference,
            int bindingIndex,
            InputDeviceType allowedDevices = InputDeviceType.All,
            [CanBeNull] OnBindingChangeStartedEventHandler onBindingChangeStarted = null,
            [CanBeNull] OnBindingChangeCancelledEventHandler onBindingChangeCancelled = null,
            [CanBeNull] OnBindingChangeCompletedEventHandler onBindingChangeCompleted = null,
            [CanBeNull] OnBindingChangeDuplicateFoundEventHandler onBindingDuplicateFound = null)
        {
            // Get action from reference
            InputAction action = reference.action;
            if (action == null) return false;

            // Register events
            OnBindingChangeStartedGlobalEvent += onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent += onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent += onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent += onBindingDuplicateFound;

            bool result = action.Rebind(bindingIndex, allowedDevices);

            // Detach events
            OnBindingChangeStartedGlobalEvent -= onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent -= onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent -= onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent -= onBindingDuplicateFound;

            return result;
        }

        /// <summary>
        ///     Starts to rebind the provided action with the provided binding index.
        ///     Requires map to be disabled before rebind.
        /// </summary>
        private static bool Rebind(
            [NotNull] this InputAction action,
            int bindingIndex,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Check if device type is unknown, this is not allowed for rebind
            Assert.IsFalse((allowedDevices & InputDeviceType.Unknown) != 0,
                "Cannot rebind with unknown allowed devices.");

            // Check if binding index is valid (within action bindings range)
            Assert.IsTrue(bindingIndex >= 0 && bindingIndex < action.bindings.Count,
                $"Invalid binding index '{bindingIndex}' for '{action.name}'");

            if (action.bindings[bindingIndex].isComposite)
            {
                int firstPartIndex = bindingIndex + 1;
                if (firstPartIndex < action.bindings.Count && action.bindings[firstPartIndex].isPartOfComposite)
                    _Rebind(action, firstPartIndex, allowedDevices, allCompositeParts: true);
            }
            else
            {
                _Rebind(action, bindingIndex, allowedDevices);
            }

            return true;
        }

        /// <summary>
        ///     Internal rebind process for input action.
        /// </summary>
        private static void _Rebind(
            [NotNull] InputAction action,
            int bindingIndex,
            InputDeviceType allowedDevices,
            bool allCompositeParts = false)
        {
            // Cancel current rebind operation to prevent conflicts
            _rebindingOperation?.Cancel();
            _rebindingOperation?.Dispose();
            _rebindingOperation = null;

            // Cache current binding override path
            // to be able to reset it if binding has failed.
            string oldEffectivePath = action.bindings[bindingIndex].effectivePath;
            string oldBindingOverride = action.bindings[bindingIndex].overridePath;

            // Create new rebinding operation
            _rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
                .OnCancel(OnOperationCancelled)
                .OnComplete(OnOperationCompleted)
                .OnPotentialMatch(OnPotentialMatch)
                .WithControlsExcluding(ANY_KEY) // Fix Unity Input System being a shit
                .WithControlsExcluding(ESCAPE) // also same as above to allow thing below to work properly
                .WithCancelingThrough(ESCAPE); // Always use ESC as cancel button

            // Trigger global event for binding change started
            OnBindingChangeStartedGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingIndex,
                allowedDevices, oldEffectivePath, action.bindings[bindingIndex].effectivePath));

            // Start rebind operation
            _rebindingOperation.Start();
            return;

#region REBIND_EVENTS

            void OnPotentialMatch([NotNull] InputActionRebindingExtensions.RebindingOperation rebindingOperation)
            {
                // Skip logic if we don't need to cancel the operation
                // to save on computation time
                if (!CancelIfDeviceIsNotAllowed) return;

                // Check if device is allowed
                if (IsValidDevice(action, bindingIndex, allowedDevices)) return;

                // If device is not allowed cancel rebind operation and return
                _rebindingOperation?.Cancel();
                _rebindingOperation?.Dispose();
                _rebindingOperation = null;
            }

            // Handle operation cancelled event
            void OnOperationCancelled(
                [NotNull] InputActionRebindingExtensions.RebindingOperation rebindingOperation)
            {
                OnBindingChangeCancelledGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingIndex,
                    allowedDevices, oldEffectivePath, action.bindings[bindingIndex].effectivePath));
                _rebindingOperation?.Dispose();
                _rebindingOperation = null;
            }

            // Handle operation completed event
            void OnOperationCompleted(
                [NotNull] InputActionRebindingExtensions.RebindingOperation rebindingOperation)
            {
                // Check for duplicates in the action map and handle them accordingly.
                // We don't need to handle composites here as they are already handled
                // by the method that is recursive.
                if (SearchForDuplicate(action, bindingIndex, allCompositeParts))
                {
                    string newEffectivePath = action.bindings[bindingIndex].effectivePath;

                    // Reset binding to default if duplicate is found and old binding was set.
                    if (oldBindingOverride != null)
                        action.ApplyBindingOverride(bindingIndex, oldBindingOverride);
                    else
                        action.RemoveBindingOverride(bindingIndex);

                    // Notify for duplicate found
                    OnBindingDuplicateFoundGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingIndex,
                        allowedDevices, oldEffectivePath, newEffectivePath));

                    // Dispose rebinding operation and return.
                    _rebindingOperation?.Dispose();
                    _rebindingOperation = null;
                    return;
                }

                // Trigger global event for binding change completed
                OnBindingChangeCompletedGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingIndex,
                    allowedDevices, oldEffectivePath, action.bindings[bindingIndex].effectivePath));
                _rebindingOperation?.Dispose();
                _rebindingOperation = null;

                // If there's more composite parts we should bind, initiate a rebind
                // for the next part.
                if (!allCompositeParts) return;

                // Get next binding index and perform rebind
                int nextBindingIndex = bindingIndex + 1;
                if (nextBindingIndex < action.bindings.Count &&
                    action.bindings[nextBindingIndex].isPartOfComposite)
                    _Rebind(action, nextBindingIndex, allowedDevices, true);
            }

#endregion
        }

        /// <summary>
        ///     Searches for duplicate bindings in the action map.
        /// </summary>
        public static bool SearchForDuplicate(
            [NotNull] this InputActionReference reference,
            [NotNull] string bindingName,
            bool allCompositeParts = false)
        {
            // Get action from reference
            InputAction action = reference.action;

            // Get binding from action
            if (!GetBindingFromAction(action, bindingName, out int bindingIndex)) return false;
            return action != null && action.SearchForDuplicate(bindingIndex, allCompositeParts);
        }

        /// <summary>
        ///     Searches for duplicate bindings in the action map.
        /// </summary>
        private static bool SearchForDuplicate(
            [NotNull] this InputAction action,
            int bindingIndex,
            bool allCompositeParts = false)
        {
            InputBinding newBinding = action.bindings[bindingIndex];
            int currentIndex = -1;

            // Search all bindings in the action map for duplicates
            foreach (InputBinding binding in action.actionMap.bindings)
            {
                currentIndex++;

                // For current action binding we need to handle composite bindings
                // with different indexes.
                if (binding.action == newBinding.action)
                {
                    if (binding.isPartOfComposite && currentIndex != bindingIndex)
                    {
                        if (binding.effectivePath == newBinding.effectivePath) return true;
                    }
                    else
                        continue;
                }

                // Otherwise we can check for duplicates by just comparing effective path
                if (binding.effectivePath == newBinding.effectivePath) return true;
            }

            // If we don't need to check all composite parts for duplicates
            // we can just return false.
            if (!allCompositeParts) return false;

            // If we need to check all composite parts for duplicates
            // we shall loop through all bindings in the action.
            for (int i = 1; i < bindingIndex; i++)
            {
                if (action.bindings[i].effectivePath == newBinding.overridePath) return true;
            }

            // If we don't find any duplicates we can return false.
            return false;
        }

        /// <summary>
        ///     Used to reset input action binding to default value.
        /// </summary>
        public static bool ResetToDefault(
            [NotNull] this InputActionReference reference,
            [NotNull] string bindingName,
            InputDeviceType allowedDevices = InputDeviceType.All,
            [CanBeNull] OnBindingChangeStartedEventHandler onBindingChangeStarted = null,
            [CanBeNull] OnBindingChangeCancelledEventHandler onBindingChangeCancelled = null,
            [CanBeNull] OnBindingChangeCompletedEventHandler onBindingChangeCompleted = null,
            [CanBeNull] OnBindingChangeDuplicateFoundEventHandler onBindingDuplicateFound = null)
        {
            // Get action from reference
            InputAction action = reference.action;
            if (action == null) return false;

            // Register events
            OnBindingChangeStartedGlobalEvent += onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent += onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent += onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent += onBindingDuplicateFound;

            bool result = action.ResetToDefault(bindingName, allowedDevices);

            // Detach events
            OnBindingChangeStartedGlobalEvent -= onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent -= onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent -= onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent -= onBindingDuplicateFound;

            return result;
        }

        /// <summary>
        ///     Used to reset input action binding to default value.
        /// </summary>
        public static bool ResetToDefault(
            [NotNull] this InputActionReference reference,
            int bindingIndex,
            InputDeviceType allowedDevices = InputDeviceType.All,
            [CanBeNull] OnBindingChangeStartedEventHandler onBindingChangeStarted = null,
            [CanBeNull] OnBindingChangeCancelledEventHandler onBindingChangeCancelled = null,
            [CanBeNull] OnBindingChangeCompletedEventHandler onBindingChangeCompleted = null,
            [CanBeNull] OnBindingChangeDuplicateFoundEventHandler onBindingDuplicateFound = null)
        {
            // Get action from reference
            InputAction action = reference.action;
            if (action == null) return false;

            // Register events
            OnBindingChangeStartedGlobalEvent += onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent += onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent += onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent += onBindingDuplicateFound;

            bool result = action.ResetToDefault(bindingIndex, allowedDevices);

            // Detach events
            OnBindingChangeStartedGlobalEvent -= onBindingChangeStarted;
            OnBindingChangeCancelledGlobalEvent -= onBindingChangeCancelled;
            OnBindingChangeCompletedGlobalEvent -= onBindingChangeCompleted;
            OnBindingDuplicateFoundGlobalEvent -= onBindingDuplicateFound;

            return result;
        }

        /// <summary>
        ///     Used to reset input action binding to default value.
        /// </summary>
        private static bool ResetToDefault(
            [NotNull] this InputAction action,
            [NotNull] string bindingName,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Get binding from action
            return GetBindingFromAction(action, bindingName, out int bindingIndex) &&
                   action.ResetToDefault(bindingIndex, allowedDevices);
        }

        /// <summary>
        ///     Used to reset input action binding to default value.
        /// </summary>
        private static bool ResetToDefault(
            [NotNull] this InputAction action,
            int bindingIndex,
            InputDeviceType allowedDevices = InputDeviceType.All)
        {
            // Create binding overrides dictionary to cache all bindings that will be reset
            // to be able to revert them if duplicate is found.
            //
            // Key is always a binding index.
            Dictionary<int, string> oldBindingOverrides = new();
            Dictionary<int, string> oldBindingEffectivePaths = new();

            // Reset binding to default, for composite bindings remove all parts
            if (action.bindings[bindingIndex].isComposite)
            {
                for (int i = bindingIndex + 1;
                     i < action.bindings.Count && action.bindings[i].isPartOfComposite;
                     i++)
                {
                    oldBindingOverrides.Add(i, action.bindings[i].overridePath);
                    oldBindingEffectivePaths.Add(i, action.bindings[i].effectivePath);
                    action.RemoveBindingOverride(i);
                }
            }
            else
            {
                oldBindingOverrides.Add(bindingIndex, action.bindings[bindingIndex].overridePath);
                oldBindingEffectivePaths.Add(bindingIndex, action.bindings[bindingIndex].effectivePath);
                action.RemoveBindingOverride(bindingIndex);
            }

            // Check if any duplicates were created by resetting the binding
            // if any duplicates were found, reset all bindings to previous state
            // and notify for duplicate found.
            if (SearchForDuplicate(action, bindingIndex))
            {
                // If duplicate was found, reset binding to override and notify for duplicate found
                // perform for all bindings that were reset.
                foreach (KeyValuePair<int, string> bindingOverride in oldBindingOverrides)
                {
                    // Get effective path by index
                    string oldEffectivePath = oldBindingEffectivePaths[bindingOverride.Key];
                    string newEffectivePath = action.bindings[bindingOverride.Key].effectivePath;

                    // Revert binding override if it was set
                    if (bindingOverride.Value != null)
                        action.ApplyBindingOverride(bindingOverride.Key, bindingOverride.Value);
                    else
                        action.RemoveBindingOverride(bindingOverride.Key);

                    // Notify for duplicate found
                    OnBindingDuplicateFoundGlobalEvent?.Invoke(new BindingChangeInfo(action,
                        bindingOverride.Key,
                        allowedDevices, oldEffectivePath, newEffectivePath));
                }

                return false;
            }

            // IF NO DUPLICATES WERE FOUND
            // Raise events for all bindings that were reset
            foreach (KeyValuePair<int, string> bindingOverride in oldBindingOverrides)
            {
                // Get effective path by index
                string oldEffectivePath = oldBindingEffectivePaths[bindingOverride.Key];
                string newEffectivePath = action.bindings[bindingOverride.Key].effectivePath;

                // Notify for binding change completed
                OnBindingChangeCompletedGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingOverride.Key,
                    allowedDevices, oldEffectivePath, newEffectivePath));
                OnBindingResetGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingOverride.Key,
                    allowedDevices, oldEffectivePath, newEffectivePath));
            }

            return true;
        }

        /// <summary>
        ///     Uses provided <see cref="InputActionAsset"/> to find action and binding index of the provided
        ///     action and binding name.
        /// </summary>
        public static bool GetActionAndBinding(
            [NotNull] this InputActionAsset asset,
            [NotNull] string actionName,
            [NotNull] string bindingName,
            [CanBeNull] out InputAction action,
            out int bindingIndex)
        {
            // Get action from asset
            action = asset.FindAction(actionName);

            // Get binding from action
            return GetBindingFromAction(action, bindingName, out bindingIndex);
        }

        /// <summary>
        ///     Uses provided <see cref="InputActionReference"/> to find binding index of the provided binding name.
        /// </summary>
        public static bool GetActionAndBinding(
            [NotNull] this InputActionReference reference,
            [NotNull] string bindingName,
            [CanBeNull] out InputAction action,
            out int bindingIndex)
        {
            // Get action from reference
            action = reference.action;

            // Get binding from action
            return GetBindingFromAction(action, bindingName, out bindingIndex);
        }

        /// <summary>
        ///     Uses provided <see cref="InputAction"/> to find binding index of the provided binding name.
        /// </summary>
        public static bool GetBindingFromAction(
            [CanBeNull] InputAction action,
            [NotNull] string bindingName,
            out int bindingIndex)
        {
            bindingIndex = -1;

            // Ensure action is not null or binding name is not provided
            if (action == null || string.IsNullOrEmpty(bindingName))
            {
                return false;
            }

            // Get binding from action
            Guid bindingId = new(bindingName);
            bindingIndex = action.bindings.IndexOf(x => x.id == bindingId);

            // Check if binding was found
            Assert.IsFalse(bindingIndex != -1, $"Cannot find binding with ID '{bindingId}' on '{action.name}'");
            return bindingIndex != -1;
        }

        /// <summary>
        ///     Uses provided <see cref="InputActionReference"/> to find FIRST! binding index for provided device. 
        /// </summary>
        public static bool GetBindingFromAction(
            [NotNull] InputActionReference reference,
            InputDeviceType allowedDevices,
            bool ignoreOverrides,
            out int bindingIndex)
        {
            return GetBindingFromAction(reference.action, allowedDevices, ignoreOverrides, out bindingIndex);
        }

        /// <summary>
        ///     Uses provided <see cref="InputAction"/> to find FIRST! binding index for provided device.
        /// </summary>
        public static bool GetBindingFromAction(
            [NotNull] InputAction action,
            InputDeviceType allowedDevices,
            bool ignoreOverrides,
            out int bindingIndex)
        {
            bindingIndex = -1;

            // Loop through action binding
            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (!IsValidDevice(action, i, allowedDevices, ignoreOverrides)) continue;
                bindingIndex = i;
                return true;
            }

            return false;
        }

        /// <summary>
        ///     This is event attached to <see cref="InputSystem.onActionChange"/> to handle changes
        ///     in input actions.
        /// </summary>
        private static void OnInputActionChanged(object obj, InputActionChange change)
        {
            if (change != InputActionChange.BoundControlsChanged) return;

            // Notify for update of all bindings in the action map
            switch (obj)
            {
                case InputAction action: NotifyActionBindingsChanged(action); break;
                case InputActionMap actionMap: NotifyActionMapBindingsChanged(actionMap); break;
            }
        }

        private static void NotifyActionMapBindingsChanged([NotNull] InputActionMap actionMap)
        {
            // Notify for update of all bindings in the action map
            foreach (InputAction action in actionMap.actions) NotifyActionBindingsChanged(action);
        }

        private static void NotifyActionBindingsChanged([NotNull] InputAction action)
        {
            // Notify for update of all bindings in the action asset
            // TODO: Check if this event is not raised twice during rebind via InputAPI and remove it if so.
            for (int bindingIndex = 0; bindingIndex < action.bindings.Count; bindingIndex++)
            {
                string newEffectivePath = action.bindings[bindingIndex].effectivePath;

                OnBindingChangeCompletedGlobalEvent?.Invoke(new BindingChangeInfo(action, bindingIndex,
                    InputDeviceType.Unknown, string.Empty, newEffectivePath));
            }
        }
    }
}