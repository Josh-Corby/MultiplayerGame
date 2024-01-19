using System.Collections.Generic;
using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Project
{
    public class LobbyListUI : SingletonBase<LobbyListUI>
    {
        [SerializeField] private Transform _lobbySingleTemplate;
        [SerializeField] private Transform _container;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _createLobbyButton;

        protected override void Awake()
        {
            base.Awake();

            _lobbySingleTemplate.gameObject.SetActive(false);

            _refreshButton.onClick.AddListener(RefreshButtonClick);
            _createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
        }

        private void Start()
        {
            LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
            LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
            LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
            LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnKickedFromLobby;
        }

        private void LobbyManager_OnKickedFromLobby(object sender, LobbyManager.LobbyEventArgs e)
        {
            Show();
        }

        private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
        {
            Show();
        }

        private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
        {
            Hide();
        }

        private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
        {
            UpdateLobbyList(e.lobbyList);
        }

        private void UpdateLobbyList(List<Lobby> lobbyList)
        {
            foreach (Transform child in _container)
            {
                if (child == _lobbySingleTemplate) continue;

                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in lobbyList)
            {
                Transform lobbySingleTransform = Instantiate(_lobbySingleTemplate, _container);
                lobbySingleTransform.gameObject.SetActive(true);
                LobbyListSingleUI lobbyListSingleUI = lobbySingleTransform.GetComponent<LobbyListSingleUI>();
                lobbyListSingleUI.UpdateLobby(lobby);
            }
        }

        private async void RefreshButtonClick()
        {
            await LobbyManager.Instance.RefreshLobbyList();
        }

        private void CreateLobbyButtonClick()
        {
            LobbyCreateUI.Instance.Show();
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Show()
        {
            gameObject.SetActive(true);
        }
    }
}
