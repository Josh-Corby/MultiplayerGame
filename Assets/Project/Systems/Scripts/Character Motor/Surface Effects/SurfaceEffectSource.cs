using UnityEngine;

namespace Project
{
    public class SurfaceEffectSource : MonoBehaviour
    {
        [SerializeField] private EffectSet _linkedEffect;
        public float PersistenceTime => _linkedEffect.PersistanceTime;

        public float Effect(float currentValue, EEffectableParameter parameter)
        {
            return _linkedEffect.Effect(currentValue, parameter);
        }
    }
}
