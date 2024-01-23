using Project.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public class EquipmentManager : MonoBehaviour
    {
        [SerializeField] private InputReader _input;

        [Header("Debug")]
        [SerializeField] private EquipmentBase DEBUG_EquipmentToAdd;
        [SerializeField] private bool DEBUG_AddEquipment;

        protected EquipmentUI _linkedUI;

        private List<EquipmentBase> _allEquipment = new List<EquipmentBase>();
        private int _currentEquipmentIndex = -1;

        public EquipmentBase CurrentEquipment
        {
            get
            {
                if (_currentEquipmentIndex < 0 || _currentEquipmentIndex >= _allEquipment.Count)
                    return null;

                return _allEquipment[_currentEquipmentIndex];
            }
        }

        private void Start()
        {
            _linkedUI = FindObjectOfType<EquipmentUI>();

            if (_allEquipment.Count > 0)
                SelectEquipment(_currentEquipmentIndex);
        }

        private void OnEnable()
        {
            _input.PreviousEquipment += PreviousEquipment;
            _input.NextEquipment += NextEquipment;
            _input.UseEquipment += UseEquipment;
        }

        private void OnDisable()
        {
            _input.PreviousEquipment -= PreviousEquipment;
            _input.NextEquipment -= NextEquipment;
            _input.UseEquipment -= UseEquipment;
        }

        private void Update()
        {
            if (DEBUG_AddEquipment && DEBUG_EquipmentToAdd != null)
            {
                DEBUG_AddEquipment = false;

                AddEquipment(DEBUG_EquipmentToAdd);
            }

            if (CurrentEquipment != null)
            {
                if (CurrentEquipment.Tick())
                    RemoveEquipment(CurrentEquipment);

                if (CurrentEquipment != null)
                {
                    _linkedUI.SetEquipmentInUse(CurrentEquipment.IsActive);
                    _linkedUI.SetEquipmentTimeRemaining(CurrentEquipment.HasCharge, CurrentEquipment.GetChargesRemaining());
                }
            }
        }

        public void AddEquipment(EquipmentBase equipment)
        {
            var newEquipment = ScriptableObject.Instantiate(equipment);

            newEquipment.LinkTo(this);

            newEquipment.OnPickedUp();

            if (newEquipment.AddToInventory)
            {
                _allEquipment.Add(newEquipment);

                if (_currentEquipmentIndex == -1)
                    SelectEquipment(0);
            }      
        }

        protected void RemoveEquipment(EquipmentBase equipment)
        {
            var wasActive = _currentEquipmentIndex == _allEquipment.IndexOf(equipment);

            _allEquipment.Remove(equipment);

            if (wasActive)
            {
                if (_allEquipment.Count == 0)
                    SelectEquipment(-1);
                else
                    SelectEquipment(_currentEquipmentIndex % _allEquipment.Count);
            }
        }

        protected void SelectEquipment(int newIndex)
        {
            _currentEquipmentIndex = newIndex;

            if (_currentEquipmentIndex > 0 || _currentEquipmentIndex >= _allEquipment.Count)
                _linkedUI.gameObject.SetActive(false);
            else
            {
                _linkedUI.gameObject.SetActive(true);
                _linkedUI.SetEquipment(CurrentEquipment);
            }
        }

        protected void PreviousEquipment()
        {
            SelectEquipment((_currentEquipmentIndex - 1 + _allEquipment.Count) % _allEquipment.Count);
        }

        protected void NextEquipment()
        {
            SelectEquipment((_currentEquipmentIndex + 1) % _allEquipment.Count);
        }

        protected void UseEquipment()
        {
            if (_currentEquipmentIndex < 0 || _currentEquipmentIndex >= _allEquipment.Count)
                return;

            var equipmentToUse = _allEquipment[_currentEquipmentIndex];
            if (equipmentToUse.ToggleUse())
            {
                RemoveEquipment(equipmentToUse);
            }
        }


    }
}
