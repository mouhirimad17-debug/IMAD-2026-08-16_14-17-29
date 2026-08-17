using System;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// Part 11.3's main menu: player name/character corner, logo, and the five
    /// buttons in the document's fixed order (create room, join room, switch
    /// character, settings, quit).
    public class MainMenuPanel : MonoBehaviour
    {
        public event Action OnCreateRoom;
        public event Action OnJoinRoom;
        public event Action OnSwitchCharacter;
        public event Action OnSettings;
        public event Action OnQuit;

        private Text playerCornerText;

        public void BuildUI()
        {
            var canvas = UIBuilder.CreateScreenCanvas("MainMenuCanvas", transform);

            playerCornerText = UIBuilder.CreateText(canvas.transform, "PlayerCorner", PlayerProfile.PlayerName, 20, Color.white);
            UIBuilder.CreateLocalizedText(canvas.transform, "Logo", "menu.logo", 72, new Color(1f, 0.75f, 0.2f));

            var createRoom = UIBuilder.CreateLocalizedButton(canvas.transform, "CreateRoomButton", "menu.createroom", new Color(0.2f, 0.6f, 0.9f), Color.white);
            createRoom.onClick.AddListener(() => OnCreateRoom?.Invoke());

            var joinRoom = UIBuilder.CreateLocalizedButton(canvas.transform, "JoinRoomButton", "menu.joinroom", new Color(0.2f, 0.6f, 0.9f), Color.white);
            joinRoom.onClick.AddListener(() => OnJoinRoom?.Invoke());

            var switchChar = UIBuilder.CreateLocalizedButton(canvas.transform, "SwitchCharacterButton", "menu.switchcharacter", new Color(0.5f, 0.4f, 0.8f), Color.white);
            switchChar.onClick.AddListener(() => OnSwitchCharacter?.Invoke());

            var settings = UIBuilder.CreateLocalizedButton(canvas.transform, "SettingsButton", "menu.settings", new Color(0.4f, 0.4f, 0.4f), Color.white);
            settings.onClick.AddListener(() => OnSettings?.Invoke());

            var quit = UIBuilder.CreateLocalizedButton(canvas.transform, "QuitButton", "menu.quit", new Color(0.7f, 0.2f, 0.2f), Color.white);
            quit.onClick.AddListener(() => OnQuit?.Invoke());
        }

        // Called whenever the player's name/character might have just changed
        // (e.g. returning from Settings' switch-character shortcut).
        public void RefreshPlayerCorner() => playerCornerText.text = PlayerProfile.PlayerName;
    }
}
