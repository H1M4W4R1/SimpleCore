using NUnit.Framework;
using Systems.SimpleCore.Input;
using Systems.SimpleCore.Input.Data;
using Systems.SimpleCore.Input.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Systems.SimpleCore.Tests
{
    public sealed class InputAPITests
    {
        [Test]
        public void InputInfo_NormalizesPathAndFallsBackDisplayNames()
        {
            InputInfo info = new InputInfo(typeof(Keyboard), "space", "Space Bar", "Space");
            InputInfo displayOnly = new InputInfo(typeof(Keyboard), "/enter", "Enter", null);
            InputInfo shortOnly = new InputInfo(typeof(Keyboard), "/tab", null, "Tab");

            Assert.AreEqual("Keyboard", info.deviceTypeName);
            Assert.AreEqual("/space", info.pathPart);
            Assert.AreEqual("Space Bar", info.DisplayName);
            Assert.AreEqual("Space", info.ShortName);
            Assert.AreEqual("Enter", displayOnly.ShortName);
            Assert.AreEqual("Tab", shortOnly.DisplayName);
        }

        [Test]
        public void GetBindingFromAction_FindsFirstBindingForAllowedDevice()
        {
            InputAction action = new InputAction("Jump", InputActionType.Button);
            try
            {
                action.AddBinding("<Gamepad>/buttonSouth");
                action.AddBinding("<Keyboard>/space");

                bool foundKeyboard = InputAPI.GetBindingFromAction(
                    action,
                    InputDeviceType.Keyboard,
                    false,
                    out int keyboardBindingIndex);

                bool foundMouse = InputAPI.GetBindingFromAction(
                    action,
                    InputDeviceType.Mouse,
                    false,
                    out int mouseBindingIndex);

                Assert.IsTrue(foundKeyboard);
                Assert.AreEqual(1, keyboardBindingIndex);
                Assert.IsFalse(foundMouse);
                Assert.AreEqual(-1, mouseBindingIndex);
            }
            finally
            {
                action.Dispose();
            }
        }

        [Test]
        public void GetActionAndBinding_FindsBindingByGuidString()
        {
            InputAction action = new InputAction("Fire", InputActionType.Button);
            try
            {
                action.AddBinding("<Keyboard>/f");
                string bindingId = action.bindings[0].id.ToString();

                bool found = InputAPI.GetBindingFromAction(action, bindingId, out int bindingIndex);

                Assert.IsTrue(found);
                Assert.AreEqual(0, bindingIndex);
            }
            finally
            {
                action.Dispose();
            }
        }

        [Test]
        public void GetBindingDisplayName_ReturnsNonEmptyNameForKnownBinding()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionReference reference = null;
            try
            {
                InputActionMap actionMap = new InputActionMap("Gameplay");
                InputAction action = actionMap.AddAction("Jump", InputActionType.Button);
                action.AddBinding("<Keyboard>/space");
                asset.AddActionMap(actionMap);
                reference = InputActionReference.Create(action);

                string displayName = reference.GetBindingDisplayName();

                Assert.IsFalse(string.IsNullOrEmpty(displayName));
            }
            finally
            {
                if (!ReferenceEquals(reference, null)) Object.DestroyImmediate(reference);
                if (!ReferenceEquals(asset, null)) Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void SearchForDuplicate_FindsDuplicateEffectivePathInActionMap()
        {
            InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
            InputActionReference reference = null;
            try
            {
                InputActionMap actionMap = new InputActionMap("Gameplay");
                InputAction jump = actionMap.AddAction("Jump", InputActionType.Button);
                InputAction fire = actionMap.AddAction("Fire", InputActionType.Button);
                jump.AddBinding("<Keyboard>/space");
                fire.AddBinding("<Keyboard>/space");
                asset.AddActionMap(actionMap);
                reference = InputActionReference.Create(jump);

                string bindingId = jump.bindings[0].id.ToString();
                bool duplicateFound = reference.SearchForDuplicate(bindingId);

                Assert.IsTrue(duplicateFound);
            }
            finally
            {
                if (!ReferenceEquals(reference, null)) Object.DestroyImmediate(reference);
                if (!ReferenceEquals(asset, null)) Object.DestroyImmediate(asset);
            }
        }
    }
}
