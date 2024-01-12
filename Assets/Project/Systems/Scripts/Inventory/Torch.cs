using UnityEngine;

namespace Project
{
    public class Torch : MonoBehaviour
    {
        [SerializeField] private Light _torchLight;
        [SerializeField] private bool _enabled;
        // Start is called before the first frame update

        private void Start()
        {
            _enabled = _torchLight.enabled;
        }


        public void Use()
        {
            _enabled = !_enabled;
            _torchLight.enabled = _enabled;
        }
    }
}
