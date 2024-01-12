using KBCore.Refs;
using UnityEngine;

namespace Project
{
    public class SceneLight : ValidatedMonoBehaviour
    {
        [SerializeField] private bool _startOn;
        [SerializeField, Self] private Light _linkedLight;
        [SerializeField, Child] private LightSwitch _linkedSwitch;

        public LightSwitch LinkedSwitch { get => _linkedSwitch; }
        public bool IsOn { get; private set; }

        private void Awake()
        {
            IsOn = _startOn;
        }
        private void Start()
        {
            SceneLightManager.Instance.RegisterLight(this);
            if(IsOn)
            {
                SceneLightManager.Instance.RegisterActiveLight(this);
            }
        }
       
        public void ToggleLight()
        {
            IsOn = !IsOn;
            SetLightEnabled(IsOn);
        }

        public void SetLightEnabled(bool enabled)
        {
            if(enabled)
            {
                EnableLight();
                return;
            }

            if(!enabled)
            {
                DisableLight();
                return;
            }
        }

        public void EnableLight()
        {
            IsOn = true;
            SceneLightManager.Instance.RegisterActiveLight(this);
            Debug.Log($"Light {gameObject.name} turned on");
            _linkedLight.enabled = true;
        }

        public void DisableLight()
        {
            IsOn = false;
            SceneLightManager.Instance.DeregisterActiveLight(this);
            Debug.Log($"Light {gameObject.name} turned off");
            _linkedLight.enabled = false;
        }


        private void OnDestroy()
        {
            if(SceneLightManager.Instance != null)
                    SceneLightManager.Instance.DeregisterLight(this);
        }
    }
}
