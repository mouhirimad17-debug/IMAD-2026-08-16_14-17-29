using System;
using PrankMansion.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// Part 11.4's create-room screen, wired to Stage 15's LobbyManager (full
    /// functional rules live there per Part 10.2). Create stays disabled while
    /// the room name is invalid, matching Part 10.2's "زر الإنشاء يبقى معطّلاً".
    public class CreateRoomPanel : MonoBehaviour
    {
        public event Action OnRoomCreated;

        private LobbyManager lobbyManager;
        private InputField nameField;
        private int selectedMaxPlayers = LobbySettings.AllowedMaxPlayers[0];
        private float selectedDuration = LobbySettings.AllowedRoundDurations[0];
        private Button createButton;

        public int SelectedMaxPlayers => selectedMaxPlayers;
        public float SelectedDuration => selectedDuration;

        public void BuildUI(LobbyManager manager)
        {
            lobbyManager = manager;
            var canvas = UIBuilder.CreateScreenCanvas("CreateRoomCanvas", transform);

            UIBuilder.CreateText(canvas.transform, "NameLabel", "Room Name", 18, Color.white);
            nameField = UIBuilder.CreateInputField(canvas.transform, "RoomNameInput", 20);
            nameField.onValueChanged.AddListener(_ => RefreshCreateButton());

            UIBuilder.CreateText(canvas.transform, "MaxPlayersLabel", "Max Players", 18, Color.white);
            foreach (int count in LobbySettings.AllowedMaxPlayers)
            {
                int c = count;
                var btn = UIBuilder.CreateButton(canvas.transform, $"MaxPlayers_{c}", c.ToString(), new Color(0.3f, 0.3f, 0.3f), Color.white);
                btn.onClick.AddListener(() => selectedMaxPlayers = c);
            }

            UIBuilder.CreateText(canvas.transform, "DurationLabel", "Round Duration", 18, Color.white);
            foreach (float duration in LobbySettings.AllowedRoundDurations)
            {
                float d = duration;
                string label = d < 400f ? "5 min" : "10 min";
                var btn = UIBuilder.CreateButton(canvas.transform, $"Duration_{d}", label, new Color(0.3f, 0.3f, 0.3f), Color.white);
                btn.onClick.AddListener(() => selectedDuration = d);
            }

            createButton = UIBuilder.CreateButton(canvas.transform, "CreateButton", "Create", new Color(0.2f, 0.7f, 0.3f), Color.white);
            createButton.onClick.AddListener(Create);
            RefreshCreateButton();
        }

        private void RefreshCreateButton() => createButton.interactable = lobbyManager.CanCreateRoom(nameField.text);

        public void Create()
        {
            bool created = lobbyManager.TryCreateRoom(nameField.text, selectedMaxPlayers, selectedDuration, PlayerProfile.LocalPlayerId, PlayerProfile.PlayerName);
            if (!created) return;
            OnRoomCreated?.Invoke();
        }

        // Test-only direct-set paths.
        public void DebugSetRoomName(string name) { nameField.text = name; RefreshCreateButton(); }
        public void DebugSetMaxPlayers(int count) => selectedMaxPlayers = count;
        public void DebugSetDuration(float duration) => selectedDuration = duration;
    }
}
