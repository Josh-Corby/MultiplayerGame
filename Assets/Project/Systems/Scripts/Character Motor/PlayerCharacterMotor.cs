using Cinemachine;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Project.Input
{

    public class PlayerCharacterMotor : CharacterMotor
    {
        [SerializeField] private Transform _linkedCamera;
        [SerializeField] private CinemachineVirtualCamera _playerCamera;
        [SerializeField] private AudioListener _playerAudioListener;

        protected float _currentCameraPitch = 0f;
        protected float _headbobProgress = 0f;

        public bool SendUIInteractions { get; protected set; } = true;
        public float Camera_CurrentTime { get; protected set; }

        #region Input System Handling
        public void ReceiveMoveInput(Vector2 value)
        {
            _input_Move = value;
        }

        public void ReceiveLookInput(Vector2 value)
        {
            _input_Look = value;
        }

        public void ReceiveJumpInput(bool value)
        {
            _input_Jump = value;
        }

        public void ReceiveRunInput(bool value)
        {
            _input_Run = value;
        }

        public void ReceiveCrouchInput(bool value)
        {
            _input_Crouch = value;
        }

        public void ReceivePrimaryActionInput(bool value)
        {
            _input_PrimaryAction = value;

            // need to inject pointer event
            if (_input_PrimaryAction && SendUIInteractions)
            {
                PointerEventData pointerData = new PointerEventData(EventSystem.current);
                pointerData.position = Mouse.current.position.ReadValue();

                // raycast against the UI
                List<RaycastResult> results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerData, results);

                foreach (RaycastResult result in results)
                {
                    if (result.distance < _config.MaxInteractionDistance)
                    {
                        ExecuteEvents.Execute(result.gameObject, pointerData, ExecuteEvents.pointerClickHandler);
                    }
                }
            }
        }
        #endregion

        public override void OnNetworkSpawn()
        {
            if(!IsOwner)
            {
                _playerAudioListener.enabled = false;
                _playerCamera.Priority = 0;
                return;
            }

            _playerAudioListener.enabled = true;
            _playerCamera.Priority = 100;

            SetCursorLock(true);
            SendUIInteractions = _config.SendUIInteractions;
            _linkedCamera.transform.localPosition = Vector3.up * (CurrentHeight + _config.Camera_VerticalOffset);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            UpdateCamera();
        }

        protected void UpdateCamera()
        {
            // not around to look around?
            if (IsLookingLocked)
                return;

            // ignore any campera input for a brief time (mostly helps editor side when hitting play buttin)
            if (Camera_CurrentTime < _config.Camera_InitialDiscardTime)
            {
                Camera_CurrentTime += Time.deltaTime;
                return;
            }

            // allow surface to effect sensitivity
            float hSensitivity = _config.Camera_HorizontalSensitivity;
            float vSensitivity = _config.Camera_VerticalSensitivity;

            if (CurrentSurfaceSource != null)
            {
                hSensitivity = CurrentSurfaceSource.Effect(hSensitivity, EEffectableParameter.CameraSensitivity);
                vSensitivity = CurrentSurfaceSource.Effect(vSensitivity, EEffectableParameter.CameraSensitivity);
            }

            // calculate our camera inputs
            float cameraYawDelta = _input_Look.x * hSensitivity * Time.fixedDeltaTime;
            float cameraPitchDelta = _input_Look.y * vSensitivity * Time.fixedDeltaTime * (_config.Camera_InvertY ? 1f : -1f);

            // rotate character
            //transform.localRotation = transform.localRotation * Quaternion.Euler(0f, cameraYawDelta, 0f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, 
                                                       transform.localRotation * Quaternion.Euler(0f, cameraYawDelta, 0f), 
                                                       20f * Time.fixedDeltaTime);

            _linkedCamera.transform.localPosition = Vector3.up * (CurrentHeight + _config.Camera_VerticalOffset);

            // head bob enabled and on the ground?
            if (_config.Headbob_Enable && IsGrounded)
            {
                float currentSpeed = _linkedRB.velocity.magnitude;
                {
                    // moving fast enough to bob?
                    Vector3 defaultCameraOffset = Vector3.up * (CurrentHeight + _config.Camera_VerticalOffset);
                    if (currentSpeed >= _config.Headbob_MinSpeedToBob)
                    {
                        float speedFactor = currentSpeed / (_config.CanRun ? _config.RunSpeed : _config.WalkSpeed);

                        // update our progress
                        _headbobProgress += Time.deltaTime / _config.Headbob_PeriodVsSpeedFactor.Evaluate(speedFactor);
                        _headbobProgress %= 1f;

                        // determine the maximum translations
                        float maxVTranslation = _config.Headbob_VTranslationVsSpeedFactor.Evaluate(speedFactor);
                        float maxHTranslation = _config.Headbob_HTranslationVsSpeedFactor.Evaluate(speedFactor);

                        float sinProgress = Mathf.Sin(_headbobProgress * Mathf.PI * 2f);

                        // update the camera location
                        defaultCameraOffset += Vector3.up * sinProgress * maxVTranslation;
                        defaultCameraOffset += Vector3.right * sinProgress * maxHTranslation;
                    }
                    else
                        _headbobProgress = 0f;

                    _linkedCamera.transform.localPosition = Vector3.MoveTowards(_linkedCamera.transform.localPosition,
                                                                                defaultCameraOffset,
                                                                                _config.Headbob_TranslationBlendSpeed * Time.deltaTime);
                }
            }

            // tilt the camera
            _currentCameraPitch = Mathf.Clamp(_currentCameraPitch + cameraPitchDelta,
                                              _config.Camera_MinPitch,
                                              _config.Camera_MaxPitch);

            //_linkedCamera.transform.localRotation = Quaternion.Euler(_currentCameraPitch, 0f, 0f);
            _linkedCamera.transform.localRotation = Quaternion.Slerp(_linkedCamera.transform.localRotation, 
                                                                     Quaternion.Euler(_currentCameraPitch, 0f, 0f), 
                                                                     2f * Time.fixedDeltaTime);
        }

        public void SetCursorLock(bool locked)
        {
            Cursor.visible = !locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public void SetLookLock(bool locked)
        {
            IsLookingLocked = locked;
        }
    }
}
