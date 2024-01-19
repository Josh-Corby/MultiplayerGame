using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class EditPlayerName : SingletonBase<EditPlayerName>
    {
        public event EventHandler OnNameChanged;
        [SerializeField] private TextMeshProUGUI _playerNameText;
        private string _playerName = "Player";

        protected override void Awake()
        {
            base.Awake();

            GetComponent<Button>().onClick.AddListener(() => {
                UI_InputWindow.Show_Static("Player Name", _playerName, "abcdefghijklmnopqrstuvxywzABCDEFGHIJKLMNOPQRSTUVXYWZ .,-", 20,
                () => {
                    // Cancel
                },
                (string newName) => {
                    _playerName = newName;

                    _playerNameText.text = _playerName;

                    OnNameChanged?.Invoke(this, EventArgs.Empty);
                });
            });

            _playerName = "Player-" + UnityEngine.Random.Range(0, 1000);
            _playerNameText.text = _playerName;
        }

        private void Start()
        {
            OnNameChanged += EditPlayerName_OnNameChanged;
        }

        private void EditPlayerName_OnNameChanged(object sender, EventArgs e)
        {
            LobbyManager.Instance.UpdatePlayerName(GetPlayerName());
        }

        public string GetPlayerName()
        {
            return _playerName;
        }
    }
}
