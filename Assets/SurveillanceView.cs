using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Project
{
    public class SurveillanceView : NetworkBehaviour
    {
        [SerializeField] private List<RenderTexture> _playerRenderTextures = new();
        private MeshRenderer _renderer;
        [SerializeField] private NetworkVariable<int> _currentRenderTextureIndex = new(readPerm: NetworkVariableReadPermission.Everyone, writePerm: NetworkVariableWritePermission.Server);

        [SerializeField] private InteractableButton _nextCameraButton;
        [SerializeField] private InteractableButton _previousCameraButton;

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();

            _nextCameraButton.OnInteracted += ToNextCamera;
            _previousCameraButton.OnInteracted += ToPreviousCamera;
        }

        private void Start()
        {
            foreach (var player in PlayerNetworkManager.Instance.ConnectedPlayers)
            {
                GetPlayerRenderTexture(player);
            }

            PlayerNetworkManager.Instance.OnPlayerConnected += GetPlayerRenderTexture;
        }

        private void GetPlayerRenderTexture(PlayerNetwork player)
        {
            Debug.Log("getting new render texture");
            RenderCamera camera = player.GetComponentInChildren<RenderCamera>();
            RenderTexture texture = camera.RenderTexture;
            AddRenderTexture(texture);
        }

        private void AddRenderTexture(RenderTexture renderTexture)
        {
            _playerRenderTextures.Add(renderTexture);

            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            RenderTexture texture = _playerRenderTextures[_currentRenderTextureIndex.Value];
            _renderer.material.SetTexture("_BaseMap", texture);
        }

        private void ToNextCamera()
        {
            _currentRenderTextureIndex.Value = (_currentRenderTextureIndex.Value + 1) % _playerRenderTextures.Count;
            UpdateDisplay();
        }

        private void ToPreviousCamera()
        {
            _currentRenderTextureIndex.Value = (_currentRenderTextureIndex.Value - 1 + _playerRenderTextures.Count) % _playerRenderTextures.Count;
            UpdateDisplay();
        }
    }
}