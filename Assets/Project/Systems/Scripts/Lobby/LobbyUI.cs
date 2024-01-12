using Eflatun.SceneReference;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class LobbyUI : MonoBehaviour
    {
        [SerializeField] private Button _createLobbyButton;
        [SerializeField] private Button _joinLobbyButton;
        [SerializeField] private SceneReference _gameScene;

        private void Awake()
        {
            _createLobbyButton.onClick.AddListener(CreateGame);
            _joinLobbyButton.onClick.AddListener(JoinGame);
        }

        private async void CreateGame()
        {
            await Multiplayer.Instance.CreateLobby();
            Loader.LoadNetwork(_gameScene);
        }

        private async void JoinGame()
        {
            await Multiplayer.Instance.QuickJoinLobby();
        }
    }
}
