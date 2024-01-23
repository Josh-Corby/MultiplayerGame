using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project
{
    public sealed class SceneLightManager : SingletonBase<SceneLightManager>
    {
        [field: SerializeField] public List<SceneLight> AllLights { get; private set; } = new();

        public void RegisterLight(SceneLight light) => AllLights.Add(light);
        public void DeregisterLight(SceneLight light) => AllLights.Remove(light);
    }
}
