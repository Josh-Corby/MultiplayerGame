using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Project.Input;

namespace Project
{
    public class PlayerCharacterMotor : CharacterMotor
    {
        [Header("Player Motor")]
        [SerializeField] private InputReader _input;
        [SerializeField] private Transform _linkedCamera;
        [SerializeField] private Animator _animController;

        private const string k_AnimForwardsSpeed = "ForwardsSpeed";
        private const string k_AnimSidewaysSpeed = "SidewaysSpeed";

        private float _currentCameraPitch = 0f;
        private float _headbobProgress = 0f;
        private float Camera_CurrentTime = 0f;

        private Transform CameraTransform => _linkedCamera.transform;
        public bool SendUIInteractions { get; protected set; } = true;

        #region Input
        private void OnLook(Vector2 cameraMovement)
        {
            _state.Input_Look = cameraMovement;
        }

        private void OnJump(bool isJumping)
        {
            _state.Input_Jump = isJumping;
        }

        private void OnRun(bool isRunning)
        {
            _state.Input_Run = isRunning;
        }

        private void OnCrouch(bool isCrouching)
        {
            _state.Input_Crouch = isCrouching;
        }

        public void ReceivePrimaryActionInput(bool value)
        {
            _state.Input_PrimaryAction = value;

            // need to inject pointer event
            if (_state.Input_PrimaryAction && SendUIInteractions)
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

            if (_state.Input_PrimaryAction)
                OnPrimaryAction?.Invoke();
        }
        #endregion

        private void OnEnable()
        {
            _input.Look += OnLook;
            _input.Jump += OnJump;
            _input.Run += OnRun;
            _input.Crouch += OnCrouch;
        }

        private void OnDisable()
        {
            _input.Look -= OnLook;
            _input.Jump -= OnJump;
            _input.Run -= OnRun;
            _input.Crouch -= OnCrouch;
        }

        protected override void Start()
        {
            base.Start();
            SetCursorLock(true);
            SendUIInteractions = _config.SendUIInteractions;
            _linkedCamera.transform.localPosition = Vector3.up * (_state.CurrentHeight + _config.Camera_VerticalOffset);
        }

        protected override void Update()
        {
            _state.Input_Move = new Vector2(_input.Direction.x, _input.Direction.y);

            base.Update();

            float forwardsSpeed = Vector3.Dot(_state.LinkedRB.velocity, transform.forward) / _config.RunSpeed;
            float sidewaysSpeed = Vector3.Dot(_state.LinkedRB.velocity, transform.right) / _config.RunSpeed;

            _animController.SetFloat(k_AnimForwardsSpeed, forwardsSpeed);
            _animController.SetFloat(k_AnimSidewaysSpeed, sidewaysSpeed);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            UpdateCamera();
        }

        protected void UpdateCamera()
        {
            if (!IsOwner) return;

            // not around to look around?
            if (_state.IsLookingLocked)
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

            if (_state.CurrentSurfaceSource != null)
            {
                hSensitivity = _state.CurrentSurfaceSource.Effect(hSensitivity, EEffectableParameter.CameraSensitivity);
                vSensitivity = _state.CurrentSurfaceSource.Effect(vSensitivity, EEffectableParameter.CameraSensitivity);
            }

            // calculate our camera inputs
            float cameraYawDelta = _state.Input_Look.x * hSensitivity * Time.fixedDeltaTime;
            float cameraPitchDelta = _state.Input_Look.y * vSensitivity * Time.fixedDeltaTime * (_config.Camera_InvertY ? 1f : -1f);

            // rotate character
            transform.localRotation = Quaternion.Slerp(transform.localRotation, 
                                                       transform.localRotation * Quaternion.Euler(0f, cameraYawDelta, 0f), 
                                                       20f * Time.fixedDeltaTime);

            CameraTransform.localPosition = Vector3.up * (_state.CurrentHeight + _config.Camera_VerticalOffset);

            // head bob enabled and on the ground?
            if (_config.Headbob_Enable && _state.IsGrounded)
            {
                float currentSpeed = _state.LinkedRB.velocity.magnitude;
                {
                    // moving fast enough to bob?
                    Vector3 defaultCameraOffset = Vector3.up * (_state.CurrentHeight + _config.Camera_VerticalOffset);
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

                    CameraTransform.localPosition = Vector3.MoveTowards(CameraTransform.localPosition,
                                                                                defaultCameraOffset,
                                                                                _config.Headbob_TranslationBlendSpeed * Time.deltaTime);
                }
            }

            // tilt the camera
            _currentCameraPitch = Mathf.Clamp(_currentCameraPitch + cameraPitchDelta,
                                              _config.Camera_MinPitch,
                                              _config.Camera_MaxPitch);

            CameraTransform.localRotation = Quaternion.Euler(_currentCameraPitch, 0f, 0f);
            //_linkedCamera.transform.localRotation = Quaternion.Slerp(_linkedCamera.transform.localRotation, 
                                                                     //Quaternion.Euler(_currentCameraPitch, 0f, 0f), 
                                                                     //2f * Time.fixedDeltaTime);
        }

        public void SetCursorLock(bool locked)
        {
            Cursor.visible = !locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        }

        public void SetLookLock(bool locked)
        {
            _state.IsLookingLocked = locked;
        }
    }
}
