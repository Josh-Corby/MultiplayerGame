using UnityEngine;

namespace Project
{
    public class GravityTracker : MonoBehaviour
    {
        public enum EUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        [SerializeField] EUpdateMode _updateMode = EUpdateMode.FixedUpdate;
        public bool ApplyGravity = true;
        private Rigidbody _linkedRB;

        public Vector3 GravityVector { get; private set; } = Vector3.zero;
        public Vector3 Up { get; private set; } = Vector3.zero;
        public Vector3 Down { get; private set; } = Vector3.zero;

        private void Awake()
        {
            _linkedRB = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            if (_updateMode == EUpdateMode.Update)
                UpdateGravity();
        }

        void LateUpdate()
        {
            if (_updateMode == EUpdateMode.LateUpdate)
                UpdateGravity();
        }

        void FixedUpdate()
        {
            if (_updateMode == EUpdateMode.FixedUpdate)
                UpdateGravity();

            // apply gravity
            if (ApplyGravity)
                _linkedRB.AddForce(GravityVector, ForceMode.Acceleration);
        }

        void UpdateGravity()
        {
            GravityVector = Vector3.zero;

            foreach (var source in GravityManager.Instance.AllSources)
            {
                GravityVector += source.GetGravityFor(transform.position);
            }

            Down = GravityVector.normalized;
            Up = -Down;
        }
    }
}