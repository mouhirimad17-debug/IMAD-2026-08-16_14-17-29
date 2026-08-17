using System;
using System.Collections.Generic;
using PrankMansion.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 10.4's waiting-room screen (its own text already specifies most of the
    /// visual layout, so it's built here rather than waiting for a dedicated Part
    /// 11.x subsection that doesn't exist): a vertical player list (readiness
    /// indicator, host tag), fixed room-settings header, and a start-game button
    /// visible ONLY to the host, enabled only once LobbyManager.CanStartGame is
    /// true. Actually spawning players into the mansion scene and calling
    /// RoundManager.StartRound needs the real connected NetworkPlayerState/
    /// PlayerTeam list, which only exists once real networking connects real
    /// players (Steamworks.NET, still deferred per Stage 15) - this screen fires
    /// OnGameStarted as its side of the handoff and stops there.
    /// </summary>
    public class WaitingRoomPanel : MonoBehaviour
    {
        public event Action OnGameStarted;
        public event Action OnLeft;

        private LobbyManager lobbyManager;
        private Transform playerListRoot;
        public Button StartButton { get; private set; }
        private Button readyButton;
        private Text settingsHeader;
        private readonly List<GameObject> rowPool = new List<GameObject>();

        public void BuildUI(LobbyManager manager)
        {
            lobbyManager = manager;
            var canvas = UIBuilder.CreateScreenCanvas("WaitingRoomCanvas", transform);

            settingsHeader = UIBuilder.CreateText(canvas.transform, "SettingsHeader", "", 18, Color.white);

            var listGo = new GameObject("PlayerList");
            listGo.transform.SetParent(canvas.transform, false);
            playerListRoot = listGo.transform;

            readyButton = UIBuilder.CreateButton(canvas.transform, "ReadyToggleButton", "Ready", new Color(0.3f, 0.3f, 0.3f), Color.white);
            readyButton.onClick.AddListener(ToggleLocalReady);

            StartButton = UIBuilder.CreateButton(canvas.transform, "StartGameButton", "Start Game", new Color(0.2f, 0.7f, 0.3f), Color.white);
            StartButton.onClick.AddListener(StartGame);

            var leaveButton = UIBuilder.CreateButton(canvas.transform, "LeaveButton", "Leave", new Color(0.6f, 0.2f, 0.2f), Color.white);
            leaveButton.onClick.AddListener(Leave);

            Refresh();
        }

        public void Refresh()
        {
            if (lobbyManager.CurrentLobbyId == null) return;

            var settings = lobbyManager.Directory.GetSettings(lobbyManager.CurrentLobbyId);
            settingsHeader.text = $"{settings.RoomName}  |  {settings.MaxPlayers} players  |  {(settings.RoundDurationSeconds < 400f ? "5 min" : "10 min")}";

            RebuildPlayerRows(lobbyManager.GetCurrentPlayers());

            bool isHost = lobbyManager.IsLocalHost();
            StartButton.gameObject.SetActive(isHost); // Part 10.4: "يظهر حصرياً للاعب المضيف"
            StartButton.interactable = isHost && lobbyManager.CanStartGame();
        }

        private void RebuildPlayerRows(List<PlayerLobbyEntry> players)
        {
            while (rowPool.Count < players.Count)
            {
                var row = new GameObject($"PlayerRow_{rowPool.Count}");
                row.transform.SetParent(playerListRoot, false);
                var text = row.AddComponent<Text>();
                text.font = UIBuilder.BuiltinFont;
                text.fontSize = 18;
                rowPool.Add(row);
            }

            for (int i = 0; i < rowPool.Count; i++)
            {
                bool active = i < players.Count;
                rowPool[i].SetActive(active);
                if (!active) continue;

                var p = players[i];
                var text = rowPool[i].GetComponent<Text>();
                // Ready indicator: grey (not ready) / green (ready) - Part 10.4's
                // "رمادي يعني غير جاهز، أخضر يعني جاهز".
                text.text = $"{p.DisplayName}{(p.IsHost ? " (Host)" : "")}  [{(p.IsReady ? "Ready" : "Not Ready")}]";
                text.color = p.IsReady ? new Color(0.4f, 0.9f, 0.4f) : new Color(0.6f, 0.6f, 0.6f);
            }
        }

        public void ToggleLocalReady()
        {
            var local = lobbyManager.GetCurrentPlayers().Find(p => p.PlayerId == lobbyManager.LocalPlayerId);
            bool newReady = local == null || !local.IsReady;
            lobbyManager.SetLocalReady(newReady);
            Refresh();
        }

        public void StartGame()
        {
            if (!lobbyManager.IsLocalHost() || !lobbyManager.CanStartGame()) return;
            OnGameStarted?.Invoke();
        }

        public void Leave()
        {
            lobbyManager.Leave();
            OnLeft?.Invoke();
        }
    }
}
