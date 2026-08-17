using PrankMansion.Networking;
using UnityEngine;

namespace PrankMansion.UI
{
    public enum UIScreenKind
    {
        LanguageSelect, NameEntry, CharacterSelect, MainMenu,
        CreateRoom, JoinRoom, WaitingRoom, Settings, Credits, None
    }

    /// <summary>
    /// Part 11.1's flow orchestration: first-ever launch (no saved profile) walks
    /// language -> name -> forced character select -> main menu; every later
    /// launch skips straight to the main menu ("يُتخطى هذا التسلسل بالكامل
    /// تلقائياً"). Also wires the main menu's five buttons, Settings' switch-
    /// character shortcut, and the create/join/waiting-room chain through
    /// Stage 15's LobbyManager. Gameplay-only screens (GameplayHUD, EndRoundPanel)
    /// aren't managed here - they need a live RoundManager/local player that only
    /// exists once a round has actually started, same "menu vs in-round" boundary
    /// Stage 15 already drew. Every screen's own text now re-renders live in the
    /// chosen language via Stage 17's LocalizationManager/LocalizedText.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public UIScreenKind CurrentScreen { get; private set; } = UIScreenKind.None;

        public LanguageSelectPanel LanguagePanel { get; private set; }
        public NameEntryPanel NamePanel { get; private set; }
        public CharacterSelectPanel CharacterPanel { get; private set; }
        public MainMenuPanel MainMenuPanel { get; private set; }
        public CreateRoomPanel CreateRoomPanel { get; private set; }
        public JoinRoomPanel JoinRoomPanel { get; private set; }
        public WaitingRoomPanel WaitingRoomPanel { get; private set; }
        public SettingsPanel SettingsPanel { get; private set; }
        public CreditsPanel CreditsPanel { get; private set; }

        public LobbyManager LobbyManager { get; private set; }

        private bool pendingForcedCharacterSelect;
        private UIScreenKind screenBeforeCharacterSelect;

        public void Initialize(LobbyManager lobbyManager)
        {
            LobbyManager = lobbyManager;

            LanguagePanel = BuildChild<LanguageSelectPanel>("LanguageSelectPanel");
            LanguagePanel.BuildUI();
            LanguagePanel.OnLanguageChosen += _ => AdvanceFirstLaunch();

            NamePanel = BuildChild<NameEntryPanel>("NameEntryPanel");
            NamePanel.BuildUI();
            NamePanel.OnNameConfirmed += _ => AdvanceFirstLaunch();

            CharacterPanel = BuildChild<CharacterSelectPanel>("CharacterSelectPanel");

            MainMenuPanel = BuildChild<MainMenuPanel>("MainMenuPanel");
            MainMenuPanel.BuildUI();
            MainMenuPanel.OnCreateRoom += () => ShowCreateRoom();
            MainMenuPanel.OnJoinRoom += () => ShowJoinRoom();
            MainMenuPanel.OnSwitchCharacter += () => ShowCharacterSelect(forced: false, returnTo: UIScreenKind.MainMenu);
            MainMenuPanel.OnSettings += () => Show(UIScreenKind.Settings);
            MainMenuPanel.OnQuit += Application.Quit;

            CreateRoomPanel = BuildChild<CreateRoomPanel>("CreateRoomPanel");
            CreateRoomPanel.OnRoomCreated += () => Show(UIScreenKind.WaitingRoom);

            JoinRoomPanel = BuildChild<JoinRoomPanel>("JoinRoomPanel");
            JoinRoomPanel.OnRoomJoined += () => Show(UIScreenKind.WaitingRoom);

            WaitingRoomPanel = BuildChild<WaitingRoomPanel>("WaitingRoomPanel");
            WaitingRoomPanel.OnLeft += () => Show(UIScreenKind.MainMenu);

            SettingsPanel = BuildChild<SettingsPanel>("SettingsPanel");
            SettingsPanel.BuildUI();
            SettingsPanel.OnSwitchCharacterRequested += () => ShowCharacterSelect(forced: false, returnTo: UIScreenKind.Settings);
            SettingsPanel.OnBack += () => Show(UIScreenKind.MainMenu);
            SettingsPanel.OnCreditsRequested += () => Show(UIScreenKind.Credits);

            CreditsPanel = BuildChild<CreditsPanel>("CreditsPanel");
            CreditsPanel.BuildUI();
            CreditsPanel.OnBack += () => Show(UIScreenKind.Settings);

            SetAllInactive();

            if (PlayerProfile.HasSavedProfile)
                Show(UIScreenKind.MainMenu);
            else
                Show(UIScreenKind.LanguageSelect);
        }

        private T BuildChild<T>(string name) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            return go.AddComponent<T>();
        }

        private void AdvanceFirstLaunch()
        {
            if (CurrentScreen == UIScreenKind.LanguageSelect) { Show(UIScreenKind.NameEntry); return; }
            if (CurrentScreen == UIScreenKind.NameEntry) { ShowCharacterSelect(forced: true, returnTo: UIScreenKind.None); return; }
        }

        public void ShowCreateRoom()
        {
            CreateRoomPanel.BuildUI(LobbyManager);
            SetActiveScreen(UIScreenKind.CreateRoom);
        }

        public void ShowJoinRoom()
        {
            JoinRoomPanel.BuildUI(LobbyManager);
            SetActiveScreen(UIScreenKind.JoinRoom);
        }

        public void ShowWaitingRoom()
        {
            WaitingRoomPanel.BuildUI(LobbyManager);
            SetActiveScreen(UIScreenKind.WaitingRoom);
        }

        public void ShowCharacterSelect(bool forced, UIScreenKind returnTo)
        {
            pendingForcedCharacterSelect = forced;
            screenBeforeCharacterSelect = returnTo;
            CharacterPanel.OnCharacterConfirmed -= HandleCharacterConfirmed;
            CharacterPanel.OnCharacterConfirmed += HandleCharacterConfirmed;
            CharacterPanel.BuildUI(forced);
            SetActiveScreen(UIScreenKind.CharacterSelect);
        }

        private void HandleCharacterConfirmed(int index)
        {
            if (pendingForcedCharacterSelect)
            {
                pendingForcedCharacterSelect = false;
                PlayerProfile.MarkFirstLaunchComplete();
                Show(UIScreenKind.MainMenu);
                return;
            }

            MainMenuPanel.RefreshPlayerCorner();
            Show(screenBeforeCharacterSelect == UIScreenKind.None ? UIScreenKind.MainMenu : screenBeforeCharacterSelect);
        }

        public void Show(UIScreenKind screen)
        {
            if (screen == UIScreenKind.WaitingRoom) { ShowWaitingRoom(); return; }
            SetActiveScreen(screen);
        }

        private void SetActiveScreen(UIScreenKind screen)
        {
            CurrentScreen = screen;
            SetAllInactive();

            switch (screen)
            {
                case UIScreenKind.LanguageSelect: LanguagePanel.gameObject.SetActive(true); break;
                case UIScreenKind.NameEntry: NamePanel.gameObject.SetActive(true); break;
                case UIScreenKind.CharacterSelect: CharacterPanel.gameObject.SetActive(true); break;
                case UIScreenKind.MainMenu: MainMenuPanel.gameObject.SetActive(true); break;
                case UIScreenKind.CreateRoom: CreateRoomPanel.gameObject.SetActive(true); break;
                case UIScreenKind.JoinRoom: JoinRoomPanel.gameObject.SetActive(true); break;
                case UIScreenKind.WaitingRoom: WaitingRoomPanel.gameObject.SetActive(true); break;
                case UIScreenKind.Settings: SettingsPanel.gameObject.SetActive(true); break;
                case UIScreenKind.Credits: CreditsPanel.gameObject.SetActive(true); break;
            }
        }

        private void SetAllInactive()
        {
            LanguagePanel.gameObject.SetActive(false);
            NamePanel.gameObject.SetActive(false);
            CharacterPanel.gameObject.SetActive(false);
            MainMenuPanel.gameObject.SetActive(false);
            CreateRoomPanel.gameObject.SetActive(false);
            JoinRoomPanel.gameObject.SetActive(false);
            WaitingRoomPanel.gameObject.SetActive(false);
            SettingsPanel.gameObject.SetActive(false);
            CreditsPanel.gameObject.SetActive(false);
        }
    }
}
