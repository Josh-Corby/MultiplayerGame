using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static PlayerInputActions;

namespace Project.Input
{
    [CreateAssetMenu(menuName = "InputReader", fileName = "InputReader")]
    public class InputReader : ScriptableObject, IPlayerActions
    {
        public event UnityAction<Vector2> Move = delegate { };
        public event UnityAction<Vector2> Look = delegate { };
        public event UnityAction<bool> Jump = delegate { };
        public event UnityAction<bool> Run = delegate { };
        public event UnityAction<bool> Crouch = delegate { };
        public event UnityAction PrimaryAction = delegate { };
        public event UnityAction PreviousEquipment = delegate { };
        public event UnityAction NextEquipment = delegate { };
        public event UnityAction UseEquipment = delegate { };
        public event UnityAction ToggleInventory = delegate { };
        public event UnityAction RotateItem = delegate { };

        public PlayerInputActions InputActions { get; private set; }

        public Vector3 Direction => InputActions.Player.Move.ReadValue<Vector2>();

        private void OnEnable()
        {
            if (InputActions == null)
            {
                InputActions = new PlayerInputActions();
                InputActions.Player.SetCallbacks(this);
            }
        }

        public void EnablePlayerActions()
        {
            InputActions.Enable();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            Move?.Invoke(context.ReadValue<Vector2>());
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            if(Cursor.lockState == CursorLockMode.Locked)
                    Look?.Invoke(context.ReadValue<Vector2>());
            if (Cursor.lockState != CursorLockMode.Locked)
                Look?.Invoke(Vector2.zero);
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    Jump?.Invoke(true);
                    break;
                case InputActionPhase.Canceled:
                    Jump?.Invoke(false);
                    break;
            }
        }

        public void OnRun(InputAction.CallbackContext context)
        {

            switch (context.phase)
            {
                case InputActionPhase.Started:
                    Run?.Invoke(true);
                    break;
                case InputActionPhase.Canceled:
                    Run?.Invoke(false);
                    break;
            }
        }

        public void OnCrouch(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    Crouch?.Invoke(true);
                    break;
                case InputActionPhase.Canceled:
                    Crouch?.Invoke(false);
                    break;
            }
        }

        public void OnPrimaryAction(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
                PrimaryAction?.Invoke();
        }

        public void OnPreviousEquipment(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
                PreviousEquipment?.Invoke();
        }

        public void OnNextEquipment(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
                NextEquipment?.Invoke();
        }

        public void OnUseItem(InputAction.CallbackContext context)
        {
            if(context.phase == InputActionPhase.Started)
                UseEquipment?.Invoke();
        }

        public void OnToggleInventory(InputAction.CallbackContext context)
        {
            ToggleInventory?.Invoke();
        }

        public void OnRotateItem(InputAction.CallbackContext context)
        {
            if (context.phase == InputActionPhase.Started)
                RotateItem?.Invoke();
        }

        public void SetMouseVisibility(bool enabled)
        {
            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Confined : CursorLockMode.Locked;
        }
    }
}
