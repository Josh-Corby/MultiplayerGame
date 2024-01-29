using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class RenderCamera : NetworkBehaviour
    {
        [SerializeField] private Shader _shader;
        private Camera _camera;
        [field: SerializeField] public RenderTexture RenderTexture { get; private set; }

        private void Awake()
        {
            if (!IsOwner)
                enabled = false;

            _camera = GetComponent<Camera>();

            if (RenderTexture == null)
                RenderTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            RenderTexture.Create();
            _camera.targetTexture = RenderTexture;

            RenderTexture.Release();
        }
    }
}
