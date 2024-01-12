using UnityEngine;

namespace Project
{
    public interface IDamageable
    {
        public void OnTakeDamage(GameObject source, float amount);

        public void OnPerformHeal(GameObject source, float amount);
    }
}
