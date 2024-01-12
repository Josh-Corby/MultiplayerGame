using UnityEngine;
using Cinemachine;
using KBCore.Refs;

namespace Project
{
    public class FOVKick : ValidatedMonoBehaviour
    {
        [SerializeField, Self] CinemachineVirtualCamera _linkedCamera;

        [SerializeField] private float _walkingFOV = 40f;
        [SerializeField] private float _runningFOV = 50f;
        [SerializeField] private float _fovSlewRate = 50;

        private float _targetFOV;

        private void Start()
        {
            _targetFOV = _walkingFOV;
        }

        private void Update()
        {
            if(_targetFOV != _linkedCamera.m_Lens.FieldOfView)
            {
                _linkedCamera.m_Lens.FieldOfView = Mathf.MoveTowards(_linkedCamera.m_Lens.FieldOfView,
                                                                     _targetFOV, 
                                                                     _fovSlewRate * Time.deltaTime);
            }
        }

        public void OnRunStateChanged(bool isRunning)
        {
            _targetFOV = isRunning ? _runningFOV : _walkingFOV;
        }
    }
}
