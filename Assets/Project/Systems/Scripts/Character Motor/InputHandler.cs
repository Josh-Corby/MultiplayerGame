using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Input
{
    public class InputHandler : NetworkBehaviour
    {
        [SerializeField] private PlayerCharacterMotor _playerMotor;
        [SerializeField] private InventoryController _inventoryController;

        [SerializeField] private PlayerInput _playerInput;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner) return;

            _playerInput.enabled = true;
        }
        private void OnMove(InputValue value)
        {
            Vector2 moveVector = _inventoryController.InventoryIsOpen ? Vector2.zero : value.Get<Vector2>();
            _playerMotor.ReceiveMoveInput(moveVector);
        }

        private void OnLook(InputValue value)
        {
            Vector2 lookVector = _inventoryController.InventoryIsOpen ? Vector2.zero : value.Get<Vector2>();
            _playerMotor.ReceiveLookInput(lookVector);
        }

        private void OnJump(InputValue value)
        {
            bool jump = _inventoryController.InventoryIsOpen ? false : value.isPressed;
            _playerMotor.ReceiveJumpInput(jump);
        }

        private void OnRun(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen)
                _playerMotor.ReceiveRunInput(value.isPressed);
        }

        private void OnCrouch(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen)
                _playerMotor.ReceiveCrouchInput(value.isPressed);
        }

        private void OnPrimaryAction(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen)
                _playerMotor.ReceivePrimaryActionInput(value.isPressed);
        }

        private void OnToggleInventory(InputValue value)
        {
            _inventoryController.ToggleInventory();
        }
        private void OnEnable()
        {
            _inventoryController.OnInventoryToggled += ToggleControls;
        }

        private void ToggleControls(bool enabled)
        {
            Cursor.visible = enabled;
            Cursor.lockState = enabled ? CursorLockMode.Confined : CursorLockMode.Locked;
        }
    }
}
