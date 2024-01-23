using KBCore.Refs;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public class FootstepAudio_BuiltIn : ValidatedMonoBehaviour
    {
        [SerializeField, Anywhere] private AudioSource _linkedSource;
        [SerializeField] private List<AudioClip> _beginJumpSounds;
        [SerializeField] private List<AudioClip> _hitGroundSounds;
        [SerializeField] private List<AudioClip> _footstepSounds;

        public void OnBeginJump(Vector3 location)
        {
            _linkedSource.PlayOneShot(_beginJumpSounds[Random.Range(0, _beginJumpSounds.Count)]);
        }

        public void OnHitGround(Vector3 location)
        {
            _linkedSource.PlayOneShot(_hitGroundSounds[Random.Range(0, _hitGroundSounds.Count)]);
        }

        public void OnFootstep(Vector3 location, float currentVelocity)
        {
            _linkedSource.PlayOneShot(_footstepSounds[Random.Range(0, _footstepSounds.Count)]);
        }
    }
}
