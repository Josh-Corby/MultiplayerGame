using System;
using UnityEngine;

namespace Project
{
    public class AICharacterMotor : CharacterMotor
    {
        [Header("AI Motor")]
        [SerializeField, Range(0f, 1f)] private float _desiredVelocityWeighting = 0.5f;
        [SerializeField] private float _maxAngleToPermitMovement = 30f;
        [SerializeField] private AnimationCurve _speedScaleVsRotationPriority;
        [SerializeField] private float _maxAngleToTreatAsLookingAt = 5f;

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

            // allow surface to effect sensitivity
            float hSensitivity = _config.Camera_HorizontalSensitivity;

            if (CurrentSurfaceSource != null)
            {
                hSensitivity = CurrentSurfaceSource.Effect(hSensitivity, EEffectableParameter.CameraSensitivity);
            }

            // calculate our camera inputs
            float cameraYawDelta = _input_Look.x * hSensitivity * Time.fixedDeltaTime;

            // rotate character
            transform.localRotation = Quaternion.Slerp(transform.localRotation,
                                                       transform.localRotation * Quaternion.Euler(0f, cameraYawDelta, 0f),
                                                       20f * Time.fixedDeltaTime);
        }

        public void SteerTowards(Vector3 target, float rotationSpeed, float stoppingDistance, float speed)
        {
            // get vector to target
            Vector3 vectorToTarget = target - transform.position;
            vectorToTarget.y = 0f;

            // determine our velocities
            Vector3 desiredVelocity = vectorToTarget.normalized * speed;
            Vector3 targetVelocity = Vector3.Lerp(_linkedRB.velocity, desiredVelocity, _desiredVelocityWeighting);

            // get the angle between our current facing and the target facing
            float angleDelta = Mathf.Acos(Vector3.Dot(targetVelocity.normalized, transform.forward) * Mathf.Rad2Deg);

            // are we needing to turn too far?
            float movementScale = 0f;
            float rotationPriority = 1f;
            if(angleDelta < _maxAngleToPermitMovement)
            {
                rotationPriority = Mathf.Abs(angleDelta) / _maxAngleToPermitMovement;
                movementScale = _speedScaleVsRotationPriority.Evaluate(rotationPriority);
            }

            // determine and apply the rotation required
            float rotationRequired = Mathf.Sign(Vector3.Dot(targetVelocity, transform.right)) * rotationSpeed * Time.deltaTime;
            rotationRequired *= rotationPriority;
            transform.localRotation = transform.localRotation * Quaternion.AngleAxis(rotationRequired, transform.up);

            // calculate the movement input
            _input_Move.y = movementScale * Mathf.Clamp(Vector3.Dot(targetVelocity, transform.forward) / CurrentMaxSpeed, -1f, 1f);
            _input_Move.x = movementScale * Mathf.Clamp(Vector3.Dot(targetVelocity, transform.right) / CurrentMaxSpeed, -1f, 1f);
        }

        public void Stop()
        {
            _input_Move = Vector2.zero;
        }

        public bool LookTowards(Transform target, float rotationSpeed)
        {
            // get 2D vector to target
            Vector3 vectorToTarget = target.position - transform.position;
            vectorToTarget.y = 0;
            vectorToTarget.Normalize();

            // are we already looking at the target
            float angleToTarget = Mathf.Acos(Vector3.Dot(vectorToTarget, transform.forward)) * Mathf.Rad2Deg;
            if(angleToTarget <= _maxAngleToTreatAsLookingAt)
                return true;

            // look towards target
            float rotationRequired = Mathf.Sign(Vector3.Dot(vectorToTarget, transform.right)) * rotationSpeed * Time.deltaTime;
            transform.localRotation = transform.localRotation * Quaternion.AngleAxis(rotationRequired, transform.up);

            return false;
        }
    }
}
