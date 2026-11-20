using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace benjohnson
{
    /// <summary>
    /// Single place that reads the pointer (mouse, touch or pen) and re-broadcasts it as events, so gameplay
    /// scripts never talk to the Input System directly. Lives in the persistent utilities scene.
    /// Positions are in screen pixels. Actions are defined in code, so no input asset needs wiring.
    /// Subclasses that override Awake MUST call base.Awake().
    /// </summary>
    public class InputManager : Singleton<InputManager>
    {
        private InputAction pressAction;
        private InputAction pointAction;

        /// <summary>Pointer went down at this screen position.</summary>
        public event Action<Vector2> PointerPressed;

        /// <summary>Pointer came up at this screen position.</summary>
        public event Action<Vector2> PointerReleased;

        /// <summary>Pointer moved to this screen position.</summary>
        public event Action<Vector2> PointerMoved;

        /// <summary>Current pointer position in screen pixels.</summary>
        public Vector2 PointerPosition => pointAction != null ? pointAction.ReadValue<Vector2>() : Vector2.zero;

        protected override void Awake()
        {
            base.Awake();
            if (instance != this) return; // duplicate: base.Awake has already queued this object for destruction

            pressAction = new InputAction("Press", InputActionType.Button, "<Pointer>/press");
            pointAction = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
        }

        private void OnEnable()
        {
            if (pressAction == null) return;

            pressAction.performed += OnPress;
            pressAction.canceled += OnRelease;
            pointAction.performed += OnPoint;
            pressAction.Enable();
            pointAction.Enable();
        }

        private void OnDisable()
        {
            if (pressAction == null) return;

            pressAction.performed -= OnPress;
            pressAction.canceled -= OnRelease;
            pointAction.performed -= OnPoint;
            pressAction.Disable();
            pointAction.Disable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            pressAction?.Dispose();
            pointAction?.Dispose();
        }

        private void OnPress(InputAction.CallbackContext context)
        {
            PointerPressed?.Invoke(PointerPosition);
        }

        private void OnRelease(InputAction.CallbackContext context)
        {
            PointerReleased?.Invoke(PointerPosition);
        }

        private void OnPoint(InputAction.CallbackContext context)
        {
            PointerMoved?.Invoke(context.ReadValue<Vector2>());
        }
    }
}
