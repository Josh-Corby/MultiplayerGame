using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project
{
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private Torch _torch;
        public void OnUseItem(InputValue inputValue)
        {
            Debug.Log("Using item");
            _torch.Use();
        }
    }
}
