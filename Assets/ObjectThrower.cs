using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class ObjectThrower : NetworkBehaviour
    {
        [SerializeField] private Transform _throwForward;
        [SerializeField] private float _throwStrength;
        private GameObject _objectToThrow;
        public void ThrowObject(GameObject objectToThrow)
        {
            _objectToThrow = objectToThrow;
            if (!IsOwner) return;
            Rigidbody objectRigidbody = objectToThrow.GetComponent<Rigidbody>();
            objectRigidbody.isKinematic = false;
            objectRigidbody.freezeRotation = false;
            objectRigidbody.velocity = Vector3.zero;
            objectRigidbody.angularVelocity = Vector3.zero;
            objectRigidbody.AddForce(_throwForward.forward * _throwStrength, ForceMode.Impulse);
            SpawnThrowObjectServerRpc();
        }

        [ServerRpc]
        private void SpawnThrowObjectServerRpc()
        {
            _objectToThrow.GetComponent<NetworkObject>().Spawn(true);
            _objectToThrow.transform.SetParent(null, true);
        }

    }
}
