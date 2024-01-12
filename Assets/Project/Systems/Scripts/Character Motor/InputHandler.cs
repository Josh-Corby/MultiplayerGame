using KBCore.Refs;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Input
{
    public class InputHandler : ValidatedMonoBehaviour
    {
        [SerializeField, Self] private PlayerCharacterMotor _playerMotor;
        [SerializeField, Self] private InventoryController _inventoryController;

        private void OnMove(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen)
                _playerMotor.ReceiveMoveInput(value.Get<Vector2>());
        }

        private void OnLook(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen) 
                _playerMotor.ReceiveLookInput(value.Get<Vector2>());
        }

        private void OnJump(InputValue value)
        {
            if (!_inventoryController.InventoryIsOpen)
                _playerMotor.ReceiveJumpInput(value.isPressed);
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
    }
}
