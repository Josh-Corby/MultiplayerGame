using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public class MotorState
    {
        public Rigidbody LinkedRB;
        public CapsuleCollider LinkedCollider;
        public GravityTracker LocalGravity;
        public CharacterMotorConfig LinkedConfig;

        public Vector2 Input_Move;
        public Vector2 Input_Look;
        public bool Input_Jump;
        public bool Input_Run;
        public bool Input_Crouch;
        public bool Input_PrimaryAction;

        public bool IsGrounded;
        public bool IsRunning;
        public bool IsCrouched;
        public bool IsMovementLocked;
        public bool IsLookingLocked;

        public Transform CurrentParent;
        public SurfaceEffectSource CurrentSurfaceSource;

        public bool InCrouchTransition = false;
        public bool TargetCrouchState = false;
        public float CrouchTransitionProgress = 1f;

        public float TimeInAir = 0f;

        public Vector3 LastRequestedVelocity = Vector3.zero;

        private Dictionary<EParameter, List<IParameterEffector>> _activeEffects = new Dictionary<EParameter, List<IParameterEffector>>();
        private Dictionary<EParameter, float> _cachedMultipliers = new Dictionary<EParameter, float>();

        public Vector3 UpVector => LocalGravity != null ? LocalGravity.Up : Vector3.up;
        public Vector3 DownVector => LocalGravity != null ? LocalGravity.Down : Vector3.down;

        public float CurrentHeight
        {
            get
            {
                float heightMultiplier = _cachedMultipliers[EParameter.Height];

                if (InCrouchTransition)
                {
                    return heightMultiplier * Mathf.Lerp(LinkedConfig.CrouchHeight, LinkedConfig.Height, CrouchTransitionProgress);
                }

                return heightMultiplier * (IsCrouched ? LinkedConfig.CrouchHeight : LinkedConfig.Height);
            }
            set
            {
                throw new NotImplementedException($"CurrentHeight cannot be set directly. Update the motor config to change height");
            }
        }

        public float JumpVelocity => _cachedMultipliers[EParameter.JumpHeight] * LinkedConfig.JumpVelocity;

        public void AddParameterEffector(IParameterEffector newEffector)
        {
            if (!_activeEffects.ContainsKey(newEffector.GetEffectedParameter()))
                _activeEffects[newEffector.GetEffectedParameter()] = new List<IParameterEffector>();

            _activeEffects[newEffector.GetEffectedParameter()].Add(newEffector);

            CacheEffectMultipliers();
        }

        private void CacheEffectMultipliers()
        {
            foreach (var rawEnumValue in Enum.GetValues(typeof(EParameter)))
            {
                EParameter parameter = (EParameter)rawEnumValue;
                _cachedMultipliers[parameter] = 1f;

                if (!_activeEffects.ContainsKey(parameter))
                    continue;

                foreach (var effector in _activeEffects[parameter])
                {
                    _cachedMultipliers[parameter] = effector.Effect(_cachedMultipliers[parameter]);
                }
            }
        }

        public void Tick(float deltaTime)
        {
            List<IParameterEffector> toCleanup = new List<IParameterEffector>();
            // kvp = key value pair
            foreach (var kvp in _activeEffects)
            {
                var effectList = kvp.Value;

                // tick the effects and store if any need to be cleaned up
                foreach (var effect in effectList)
                {
                    if (effect.Tick(deltaTime))
                        toCleanup.Add(effect);
                }
            }

            // perform cleanup
            foreach (var effect in toCleanup)
            {
                _activeEffects[effect.GetEffectedParameter()].Remove(effect);
            }

            if (toCleanup.Count > 0)
                CacheEffectMultipliers();
        }

        public MotorState()
        {
            CacheEffectMultipliers();
        }
    }
}
