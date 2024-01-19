using System;
using System.Collections.Generic;

namespace Project
{
    public sealed class SceneLightManager : SingletonBase<SceneLightManager>
    {
        public List<SceneLight> AllLights { get; private set; } = new();
        public List<SceneLight> ActivatedLights { get; private set; } = new();

        public static event Action<SceneLight> OnLightActivated = null;
        public static event Action<SceneLight> OnLightDeactivated = null;

        public void RegisterLight(SceneLight light) => AllLights.Add(light);
        public void DeregisterLight(SceneLight light) => AllLights.Remove(light);

        public void RegisterActiveLight(SceneLight light)
        {
            ActivatedLights.Add(light);
            OnLightActivated?.Invoke(light);
        }

        public void DeregisterActiveLight(SceneLight light)
        {
            ActivatedLights.Remove(light);
            OnLightDeactivated?.Invoke(light);
        }
    }
}
