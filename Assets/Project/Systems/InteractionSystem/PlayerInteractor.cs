using Project.Input;

using UnityEngine;

namespace Project
{
    public class PlayerInteractor : Interactor
    {
        [SerializeField] private InputReader _input;

        private void OnEnable()
        {
            _input.PrimaryAction += Interact;
        }

        private void OnDisable()
        {
            _input.PrimaryAction -= Interact;
        }
    }
}
