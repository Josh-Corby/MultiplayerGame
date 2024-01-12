using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public class DamageZone : MonoBehaviour
    {
        [SerializeField] private float _damageRate = 5f;
        protected List<IDamageable> _objectsInZone = new List<IDamageable>();

        private void Update()
        {
            foreach(var damageable in _objectsInZone)
            {
                damageable.OnTakeDamage(gameObject, _damageRate * Time.deltaTime);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            IDamageable damageable;
            if (other.TryGetComponent<IDamageable>(out damageable))
            {
                _objectsInZone.Add(damageable);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            IDamageable damageable;
            if (other.TryGetComponent<IDamageable>(out damageable))
            {
                _objectsInZone.Remove(damageable);
            }
        }
    }
}
