using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class LobbyCreateUI : SingletonBase<LobbyCreateUI>
    {
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _lobbyNameButton;
        [SerializeField] private Button _publicPrivateButton;
        [SerializeField] private Button _maxPlayersButton;
        [SerializeField] private TextMeshProUGUI _lobbyNameText;
        [SerializeField] private TextMeshProUGUI _publicPrivateText;
        [SerializeField] private TextMeshProUGUI _maxPlayersText;

        private string _lobbyName;
        private bool _isPrivate;
        private int _maxPlayers;

        protected override void Awake()
        {
            base.Awake();

            _createButton.onClick.AddListener(() =>
            {
                LobbyManager.Instance.CreateLobby(_lobbyName, _maxPlayers, _isPrivate);
                Hide();
            });

            _lobbyNameButton.onClick.AddListener(() =>
            {
                UI_InputWindow.Show_Static("Lobby Name", _lobbyName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
                    () =>
                    {
                        // Cancel
                    },
                    (string lobbyName) =>
                    {
                        _lobbyName = lobbyName;
                        UpdateText();
                    });
            });

            _publicPrivateButton.onClick.AddListener(() =>
            {
                _isPrivate = !_isPrivate;
                UpdateText();
            });

            _maxPlayersButton.onClick.AddListener(() =>
            {
                UI_InputWindow.Show_Static("Max Players", _maxPlayers,
                    () =>
                    {
                        // Cancel
                    },
                    (int maxPlayers) =>
                    {
                        _maxPlayers = maxPlayers;
                        UpdateText();
                    });
            });    
            
            Hide();
        }

        private void UpdateText()
        {
            _lobbyNameText.text = _lobbyName;
            _publicPrivateText.text = _isPrivate ? "Private" : "Public";
            _maxPlayersText.text = _maxPlayers.ToString();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            _lobbyName = "MyLobby";
            _isPrivate = false;
            _maxPlayers = 4;

            UpdateText();
        }
    }
}
