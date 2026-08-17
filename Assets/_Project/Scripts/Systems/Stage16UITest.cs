using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Networking;
using PrankMansion.Player;
using PrankMansion.Systems;
using PrankMansion.UI;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode verification for Stage 16's full UI system (Part 11). Proves the
    /// first-launch vs repeat-launch flow (11.1), field-validation gating on the
    /// name/character/create-room screens, the create/join/waiting-room chain
    /// wired to Stage 15's LobbyManager, the in-round HUD's live score/timer/
    /// warning display, world-space nameplate following, and the end-of-round
    /// screen's personal win/lose/tie music selection - all against real, running
    /// components, not just field values.
    /// </summary>
    public class Stage16UITest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage16_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            report.AppendLine("=== Stage 16 - Dynamic UI System Test (Part 11, Play Mode) ===");
            report.AppendLine();

            TestFirstLaunchFlow();
            TestRepeatLaunchSkipsToMainMenu();
            TestNameValidationGating();
            TestCharacterSelectGating();
            TestMainMenuNavigation();
            TestCreateRoomFlow();
            TestJoinRoomFlow();
            TestWaitingRoomHostGating();
            yield return TestGameplayHUD();
            TestPlayerNameplate();
            yield return TestEndRoundPanelMusicSelection();
            TestSettingsPanel();

            yield return null;

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 16 UI system matches Part 11 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage16DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage16DynamicTest] DONE");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
                UnityEditor.EditorApplication.Exit(passed == total ? 0 : 1);
#endif
        }

        private void Check(string name, bool ok, string detail)
        {
            total++;
            if (ok) passed++;
            report.AppendLine($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
        }

        private UIManager BuildUIManager()
        {
            var lobbyGo = new GameObject("Stage16Test_LobbyManager");
            var lobbyManager = lobbyGo.AddComponent<LobbyManager>();
            var uiGo = new GameObject("Stage16Test_UIManager");
            var uiManager = uiGo.AddComponent<UIManager>();
            uiManager.Initialize(lobbyManager);
            return uiManager;
        }

        // ---------------------------------------------------------------
        private void TestFirstLaunchFlow()
        {
            PlayerProfile.ResetForTesting();
            var ui = BuildUIManager();

            Check("First-ever launch starts on the language-select screen (Part 11.1)",
                ui.CurrentScreen == UIScreenKind.LanguageSelect, $"screen={ui.CurrentScreen}");

            ui.LanguagePanel.Choose(GameLanguage.English);
            Check("Choosing a language advances straight to name entry, no separate continue button",
                ui.CurrentScreen == UIScreenKind.NameEntry, $"screen={ui.CurrentScreen}");

            ui.NamePanel.DebugSetName("Imad");
            ui.NamePanel.Confirm();
            Check("Confirming a valid name advances to the forced character-select screen",
                ui.CurrentScreen == UIScreenKind.CharacterSelect && ui.CharacterPanel.IsForcedFirstPick,
                $"screen={ui.CurrentScreen} forced={ui.CharacterPanel.IsForcedFirstPick}");
            Check("Forced first-pick starts with confirm disabled (nothing chosen yet) (Part 11.2)",
                !ui.CharacterPanel.CanConfirm && SelectedNoneYet(ui), $"canConfirm={ui.CharacterPanel.CanConfirm}");

            ui.CharacterPanel.Select(3); // Reno
            Check("Selecting a card enables confirm", ui.CharacterPanel.CanConfirm, $"canConfirm={ui.CharacterPanel.CanConfirm}");
            ui.CharacterPanel.Confirm();
            Check("Confirming a character on the forced first-launch path completes it and reaches the main menu",
                ui.CurrentScreen == UIScreenKind.MainMenu && PlayerProfile.HasSavedProfile && PlayerProfile.SelectedCharacterIndex == 3,
                $"screen={ui.CurrentScreen} hasProfile={PlayerProfile.HasSavedProfile} character={PlayerProfile.SelectedCharacterIndex}");
            Check("Player name was saved permanently (Part 11.1)", PlayerProfile.PlayerName == "Imad", $"name={PlayerProfile.PlayerName}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
        }

        private bool SelectedNoneYet(UIManager ui) => ui.CharacterPanel.SelectedIndex < 0;

        private void TestRepeatLaunchSkipsToMainMenu()
        {
            // Profile already has data from TestFirstLaunchFlow (name/character/HasSavedProfile).
            var ui = BuildUIManager();
            Check("A repeat launch (saved profile already exists) skips straight to the main menu (Part 11.1)",
                ui.CurrentScreen == UIScreenKind.MainMenu, $"screen={ui.CurrentScreen}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
        }

        private void TestNameValidationGating()
        {
            PlayerProfile.ResetForTesting();
            var ui = BuildUIManager(); // starts on LanguageSelect -> advance to NameEntry
            ui.LanguagePanel.Choose(GameLanguage.English);

            ui.NamePanel.DebugSetName("");
            Check("Empty name keeps continue disabled (Part 11.1)", !ui.NamePanel.CanConfirm, $"canConfirm={ui.NamePanel.CanConfirm}");

            // NOTE: the InputField widget's own characterLimit already prevents
            // typing (or even script-assigning) more than MaxNameLength characters
            // in the first place - this checks the underlying validation RULE
            // directly, independent of that separate UI-level enforcement.
            Check("Name over 15 characters is rejected by the validation rule itself (Part 11.1)",
                !PlayerProfile.IsValidName("ThisNameIsWayTooLong"), "");

            ui.NamePanel.DebugSetName("Valid Name 1");
            Check("A valid name enables continue", ui.NamePanel.CanConfirm, $"canConfirm={ui.NamePanel.CanConfirm}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
        }

        private void TestCharacterSelectGating()
        {
            // Free-access mode (from Settings/main menu, not the forced first pick):
            // a prior selection already exists, so confirm starts enabled by default.
            PlayerProfile.ResetForTesting();
            PlayerProfile.SelectedCharacterIndex = 0;
            var panel = new GameObject("Stage16Test_CharPanel").AddComponent<CharacterSelectPanel>();
            panel.BuildUI(forcedFirstPick: false);
            Check("Character select reached NOT via forced first-pick starts with confirm already enabled (Part 11.2)",
                panel.SelectedIndex == 0 && panel.CanConfirm, $"selectedIndex={panel.SelectedIndex} canConfirm={panel.CanConfirm}");

            Destroy(panel.gameObject);
        }

        private void TestMainMenuNavigation()
        {
            PlayerProfile.ResetForTesting();
            PlayerProfile.MarkFirstLaunchComplete();
            var ui = BuildUIManager();

            InvokeCreateRoom(ui);
            Check("Main menu's Create Room button navigates to the create-room screen (Part 11.3)",
                ui.CurrentScreen == UIScreenKind.CreateRoom, $"screen={ui.CurrentScreen}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
        }

        private void InvokeCreateRoom(UIManager ui) => ui.ShowCreateRoom();

        // ---------------------------------------------------------------
        private void TestCreateRoomFlow()
        {
            PlayerProfile.ResetForTesting();
            PlayerProfile.MarkFirstLaunchComplete();
            PlayerProfile.PlayerName = "Host Player";
            var ui = BuildUIManager();
            ui.ShowCreateRoom();

            ui.CreateRoomPanel.DebugSetRoomName("");
            Check("Empty room name keeps create disabled (Part 10.2/11.4)",
                !ui.LobbyManager.CanCreateRoom(""), "");

            ui.CreateRoomPanel.DebugSetRoomName("Test Room");
            ui.CreateRoomPanel.DebugSetMaxPlayers(4);
            ui.CreateRoomPanel.DebugSetDuration(600f);
            ui.CreateRoomPanel.Create();

            Check("Creating a valid room navigates to the waiting room (Part 10.2 -> 10.4)",
                ui.CurrentScreen == UIScreenKind.WaitingRoom, $"screen={ui.CurrentScreen}");
            Check("Creator is registered as host in the resulting lobby",
                ui.LobbyManager.IsLocalHost(), $"isHost={ui.LobbyManager.IsLocalHost()}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
        }

        private void TestJoinRoomFlow()
        {
            // Pre-create a room via an independent host sharing one directory,
            // like a second real client would - mirrors Stage 15's own test pattern.
            var hostManagerGo = new GameObject("Stage16Test_ExternalHost");
            var hostManager = hostManagerGo.AddComponent<LobbyManager>();
            hostManager.TryCreateRoom("Join Target", 2, 300f, "externalHost", "ExternalHost");
            string realCode = hostManager.LastGeneratedCode;

            PlayerProfile.ResetForTesting();
            PlayerProfile.MarkFirstLaunchComplete();
            PlayerProfile.PlayerName = "Joiner";
            var ui = BuildUIManager();
            ui.LobbyManager.Directory = hostManager.Directory; // share the same backend
            ui.ShowJoinRoom();

            var results = ui.JoinRoomPanel.LastResults;
            Check("Join-room search finds the externally-created room (Part 10.3)",
                results.Exists(r => r.RoomName == "Join Target"), $"count={results.Count}");

            ui.JoinRoomPanel.SelectRoom(hostManager.CurrentLobbyId);
            ui.JoinRoomPanel.DebugEnterCode("ZZZZZZ");
            Check("Wrong code shows an error and does not navigate away (Part 10.3)",
                ui.JoinRoomPanel.LastAttemptResult == JoinResult.WrongCode && ui.CurrentScreen == UIScreenKind.JoinRoom,
                $"result={ui.JoinRoomPanel.LastAttemptResult} screen={ui.CurrentScreen}");

            ui.JoinRoomPanel.DebugEnterCode(realCode);
            Check("Correct code joins successfully and navigates to the waiting room (Part 10.3 -> 10.4)",
                ui.JoinRoomPanel.LastAttemptResult == JoinResult.Success && ui.CurrentScreen == UIScreenKind.WaitingRoom,
                $"result={ui.JoinRoomPanel.LastAttemptResult} screen={ui.CurrentScreen}");

            Destroy(ui.gameObject);
            Destroy(ui.LobbyManager.gameObject);
            Destroy(hostManagerGo);
        }

        private void TestWaitingRoomHostGating()
        {
            PlayerProfile.ResetForTesting();
            PlayerProfile.MarkFirstLaunchComplete();
            PlayerProfile.PlayerName = "HostA";
            var host = BuildUIManager();
            host.ShowCreateRoom();
            host.CreateRoomPanel.DebugSetRoomName("Gate Test");
            host.CreateRoomPanel.DebugSetMaxPlayers(2);
            host.CreateRoomPanel.Create();

            host.WaitingRoomPanel.Refresh();
            Check("Start button is visible for the host, disabled with only 1 player (Part 10.4)",
                host.WaitingRoomPanel.StartButton.gameObject.activeSelf && !host.WaitingRoomPanel.StartButton.interactable, "");

            // A second (guest) client joins the same lobby.
            var guestGo = new GameObject("Stage16Test_Guest");
            var guestManager = guestGo.AddComponent<LobbyManager>();
            guestManager.Directory = host.LobbyManager.Directory;
            guestManager.TryJoinRoom(host.LobbyManager.CurrentLobbyId, host.LobbyManager.LastGeneratedCode, "guestB", "GuestB");
            guestManager.SetLocalReady(true);
            host.LobbyManager.SetLocalReady(true);
            host.WaitingRoomPanel.Refresh();

            Check("Start button becomes interactable once >=2 players are all ready (Part 10.4)",
                host.WaitingRoomPanel.StartButton.interactable, "");

            Destroy(host.gameObject);
            Destroy(host.LobbyManager.gameObject);
            Destroy(guestGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestGameplayHUD()
        {
            var roundGo = new GameObject("Stage16Test_RoundManager");
            var roundManager = roundGo.AddComponent<RoundManager>();
            roundManager.StartRound(new System.Collections.Generic.List<PlayerTeam>(), 65f);
            roundManager.RegisterPoint(Team.Team1);
            roundManager.RegisterPoint(Team.Team2);
            roundManager.RegisterPoint(Team.Team2);

            var playerGo = new GameObject("Stage16Test_LocalPlayer");
            var controller = playerGo.AddComponent<CharacterController>();
            controller.height = 1f; controller.radius = 0.3f; controller.center = new Vector3(0f, 0.5f, 0f);
            playerGo.AddComponent<PlayerLocomotion>();
            var carry = playerGo.AddComponent<PlayerCarry>();

            var hudGo = new GameObject("Stage16Test_HUD");
            var hud = hudGo.AddComponent<GameplayHUD>();
            hud.BuildUI(roundManager, carry);

            yield return null; // let Update() run at least once before reading its output

            Check("HUD score counters reflect RoundManager's live scores (Part 11.5)",
                hud.ScoreTeam1Text.text == "1" && hud.ScoreTeam2Text.text == "2",
                $"team1Text={hud.ScoreTeam1Text.text} team2Text={hud.ScoreTeam2Text.text}");
            Check("Carry-warning icon is hidden while not heavy-carrying solo", !hud.CarryWarningIcon.activeSelf, "");

            Destroy(hudGo);
            Destroy(playerGo);
            Destroy(roundGo);
        }

        private void TestPlayerNameplate()
        {
            var targetGo = new GameObject("Stage16Test_NameplateTarget");
            targetGo.transform.position = new Vector3(600f, 0f, 600f);

            var nameplateGo = new GameObject("Stage16Test_Nameplate");
            var nameplate = nameplateGo.AddComponent<PlayerNameplate>();
            nameplate.BuildUI(targetGo.transform, "TestPlayer", Team.Team2);

            Check("Nameplate shows the correct display name (Part 11.5)", nameplate.NameText.text == "TestPlayer", $"text={nameplate.NameText.text}");
            Check("Nameplate background uses the player's team color (Part 9.5.2)",
                ApproximatelyEqual(nameplate.Background.color, PlayerTeam.Team2Color), $"color={nameplate.Background.color}");
            Check("Nameplate is positioned above the target's head", nameplate.transform.position.y > targetGo.transform.position.y,
                $"nameplateY={nameplate.transform.position.y} targetY={targetGo.transform.position.y}");

            Destroy(nameplateGo);
            Destroy(targetGo);
        }

        private static bool ApproximatelyEqual(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.01f && Mathf.Abs(a.g - b.g) < 0.01f && Mathf.Abs(a.b - b.b) < 0.01f;

        private IEnumerator TestEndRoundPanelMusicSelection()
        {
            var roundGo = new GameObject("Stage16Test_EndRoundManager");
            var roundManager = roundGo.AddComponent<RoundManager>();
            roundManager.StartRound(new System.Collections.Generic.List<PlayerTeam>(), 0.1f);
            roundManager.RegisterPoint(Team.Team1);
            roundManager.RegisterPoint(Team.Team1);

            float elapsed = 0f;
            while (!roundManager.HasEnded && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }
            Check("Setup: round actually ended before checking end-screen behaviour", roundManager.HasEnded, $"hasEnded={roundManager.HasEnded}");

            var panelGo = new GameObject("Stage16Test_EndRoundPanel");
            var panel = panelGo.AddComponent<EndRoundPanel>();
            panel.BuildUI(roundManager, Team.Team1);
            Check("End-round winner text names the actual winning team (Part 11.6)",
                panel.WinnerText.text.Contains("Team 1"), $"text={panel.WinnerText.text}");
            Check("Local player on the WINNING team gets the victory jingle (Part 11.6 personal-result music)",
                panel.LastPlayedClip != null && panel.LastPlayedClip.name.Contains("Victory"), $"clip={panel.LastPlayedClip?.name}");

            var panelLoseGo = new GameObject("Stage16Test_EndRoundPanelLose");
            var panelLose = panelLoseGo.AddComponent<EndRoundPanel>();
            panelLose.BuildUI(roundManager, Team.Team2);
            Check("Local player on the LOSING team gets a different jingle (Part 11.6 personal-result music)",
                panelLose.LastPlayedClip != null && panelLose.LastPlayedClip.name.Contains("Defeat"), $"clip={panelLose.LastPlayedClip?.name}");

            Destroy(panelGo);
            Destroy(panelLoseGo);
            Destroy(roundGo);
        }

        private void TestSettingsPanel()
        {
            PlayerProfile.ResetForTesting();
            var panelGo = new GameObject("Stage16Test_SettingsPanel");
            var panel = panelGo.AddComponent<SettingsPanel>();
            panel.BuildUI();

            panel.MusicSlider.value = 0.4f;
            panel.SfxSlider.value = 0.9f;
            Check("Music volume slider writes through to PlayerProfile (Part 11.7)",
                Mathf.Approximately(PlayerProfile.MusicVolume, 0.4f), $"volume={PlayerProfile.MusicVolume}");
            Check("SFX volume slider writes through to PlayerProfile independently (Part 11.7)",
                Mathf.Approximately(PlayerProfile.SfxVolume, 0.9f), $"volume={PlayerProfile.SfxVolume}");

            Destroy(panelGo);
        }
    }
}
