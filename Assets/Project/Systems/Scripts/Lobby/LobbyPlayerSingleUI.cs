using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class LobbyPlayerSingleUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image characterImage;
        [SerializeField] private Button kickPlayerButton;

        private Player _player;

        private void Awake()
        {
            kickPlayerButton.onClick.AddListener(KickPlayer);
        }

        public void SetKickPlayerButtonVisible(bool visible)
        {
            kickPlayerButton.gameObject.SetActive(visible);
        }

        public void UpdatePlayer(Player player)
        {
            _player = player;
            playerNameText.text = player.Data[LobbyManager.k_playerName].Value;
        }

        private void KickPlayer()
        {
            if (_player != null)
            {
                LobbyManager.Instance.KickPlayer(_player.Id);
            }
        }
    }
}
