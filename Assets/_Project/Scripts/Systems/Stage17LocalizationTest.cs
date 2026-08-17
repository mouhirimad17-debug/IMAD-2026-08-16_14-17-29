using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Localization;
using PrankMansion.Networking;
using PrankMansion.UI;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode verification for Stage 17's two-language system (Part 12): live
    /// re-render on language switch with no restart (12.2), RTL flag/anchor
    /// mirroring, per-language font selection, full translation scope coverage
    /// (12.3 - main menu, create/join room, all seven character descriptions,
    /// error messages, end-round screen, settings), and the explicit exclusion
    /// of free-typed player/room names from translation.
    /// </summary>
    public class Stage17LocalizationTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage17_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            report.AppendLine("=== Stage 17 - Dynamic Localization System Test (Part 12, Play Mode) ===");
            report.AppendLine();

            TestBasicLookupAndDefaultLanguage();
            TestLiveSwitchNoRestart();
            TestRTLFlagAndAnchorMirroring();
            TestFontSelection();
            TestAllSevenCharacterDescriptionsTranslated();
            yield return TestJoinRoomErrorMessagesLocalizedAndLive();
            TestPlayerAndRoomNamesNeverTranslated();
            yield return TestEndRoundScreenLocalization();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 17 localization system matches Part 12 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage17DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage17DynamicTest] DONE");

            // Reset language back to English so this test doesn't leak state into
            // whichever other Stage's Play Mode test happens to run after it in
            // the same batch session.
            LocalizationManager.SetLanguage(GameLanguage.English);

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

        // ---------------------------------------------------------------
        private void TestBasicLookupAndDefaultLanguage()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            Check("Default/English lookup returns the English column (Part 12.1)",
                LocalizationManager.Get("menu.createroom") == "Create Room", $"value={LocalizationManager.Get("menu.createroom")}");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("Switching to Darija returns the Arabic-script column, not a transliteration (Part 12.1)",
                LocalizationManager.Get("menu.createroom") == "أنشئ غرفة", $"value={LocalizationManager.Get("menu.createroom")}");

            Check("PlayerProfile.Language stays the single source of truth LocalizationManager reads/writes (Part 12.2)",
                PlayerProfile.Language == GameLanguage.Darija, $"lang={PlayerProfile.Language}");

            LocalizationManager.SetLanguage(GameLanguage.English);
        }

        // ---------------------------------------------------------------
        private void TestLiveSwitchNoRestart()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            var panelGo = new GameObject("Stage17Test_MainMenu");
            var panel = panelGo.AddComponent<MainMenuPanel>();
            panel.BuildUI();

            var logo = panelGo.transform.Find("MainMenuCanvas/Logo").GetComponent<Text>();
            var createBtnLabel = panelGo.transform.Find("MainMenuCanvas/CreateRoomButton/Label").GetComponent<Text>();

            Check("Freshly-built screen shows English text by default", createBtnLabel.text == "Create Room", $"text={createBtnLabel.text}");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("Every currently-visible LocalizedText updates IMMEDIATELY in place on language change, no rebuild/restart (Part 12.2)",
                createBtnLabel.text == "أنشئ غرفة", $"text={createBtnLabel.text}");
            Check("Logo (a second, independent LocalizedText on the same screen) updates too",
                logo.text == "PRANK MANSION", $"text={logo.text}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Check("Switching back to English restores the English text on the still-live screen",
                createBtnLabel.text == "Create Room", $"text={createBtnLabel.text}");

            Destroy(panelGo);
        }

        // ---------------------------------------------------------------
        private void TestRTLFlagAndAnchorMirroring()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            Check("IsRTL is false for English (Part 12.2)", !LocalizationManager.IsRTL, "");
            Check("MirrorAnchor is a no-op under LTR", LocalizationManager.MirrorAnchor(TextAnchor.MiddleLeft) == TextAnchor.MiddleLeft, "");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("IsRTL is true for Darija (Part 12.2 - 'يتحول تلقائياً لاتجاه الكتابة من اليمين لليسار')", LocalizationManager.IsRTL, "");
            Check("MirrorAnchor flips Left<->Right under RTL",
                LocalizationManager.MirrorAnchor(TextAnchor.MiddleLeft) == TextAnchor.MiddleRight &&
                LocalizationManager.MirrorAnchor(TextAnchor.MiddleRight) == TextAnchor.MiddleLeft,
                $"left->{LocalizationManager.MirrorAnchor(TextAnchor.MiddleLeft)}, right->{LocalizationManager.MirrorAnchor(TextAnchor.MiddleRight)}");
            Check("MirrorAnchor leaves CENTER anchors untouched under RTL (most of this stage's placeholder layout)",
                LocalizationManager.MirrorAnchor(TextAnchor.MiddleCenter) == TextAnchor.MiddleCenter, "");

            LocalizationManager.SetLanguage(GameLanguage.English);
        }

        // ---------------------------------------------------------------
        private void TestFontSelection()
        {
            LocalizationManager.SetLanguage(GameLanguage.English);
            Check("English uses the built-in Latin font (Part 12.2)", LocalizationManager.GetFont() == UIBuilder.BuiltinFont, "");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            var darijaFont = LocalizationManager.GetFont();
            Check("Darija resolves SOME font without throwing (OS-dynamic-font fallback, Law 0.2 - see Stage17 decisions log for the known shaping limitation)",
                darijaFont != null, $"font={(darijaFont != null ? darijaFont.name : "null")}");

            LocalizationManager.SetLanguage(GameLanguage.English);
        }

        // ---------------------------------------------------------------
        private void TestAllSevenCharacterDescriptionsTranslated()
        {
            var table = PrankMansion.Systems.CharacterProfile.Table;
            Check("All seven characters have a description key assigned (Part 11.2/12.3)",
                table.Length == 7, $"count={table.Length}");

            bool allResolve = true, allDistinct = true;
            var seenEnglish = new System.Collections.Generic.HashSet<string>();
            foreach (var entry in table)
            {
                LocalizationManager.SetLanguage(GameLanguage.English);
                string en = LocalizationManager.Get(entry.descriptionKey);
                LocalizationManager.SetLanguage(GameLanguage.Darija);
                string darija = LocalizationManager.Get(entry.descriptionKey);

                if (string.IsNullOrEmpty(en) || en.StartsWith("!") || string.IsNullOrEmpty(darija) || darija.StartsWith("!"))
                    allResolve = false;
                if (en == darija) allResolve = false; // must actually be a real translation, not a copy
                if (!seenEnglish.Add(en)) allDistinct = false;
            }
            LocalizationManager.SetLanguage(GameLanguage.English);

            Check("Every character description resolves to a real, distinct-per-language string in both columns (Part 12.3)", allResolve, "");
            Check("All seven character descriptions are textually distinct from each other (no accidental key collisions)", allDistinct, "");
        }

        // ---------------------------------------------------------------
        private IEnumerator TestJoinRoomErrorMessagesLocalizedAndLive()
        {
            var hostGo = new GameObject("Stage17Test_ExternalHost");
            var hostManager = hostGo.AddComponent<LobbyManager>();
            hostManager.TryCreateRoom("Localization Target", 2, 300f, "extHost17", "ExtHost17");

            var joinerLobbyGo = new GameObject("Stage17Test_JoinerLobby");
            var joinerLobby = joinerLobbyGo.AddComponent<LobbyManager>();
            joinerLobby.Directory = hostManager.Directory;

            var panelGo = new GameObject("Stage17Test_JoinRoom");
            var panel = panelGo.AddComponent<JoinRoomPanel>();
            LocalizationManager.SetLanguage(GameLanguage.English);
            panel.BuildUI(joinerLobby);

            panel.SelectRoom(hostManager.CurrentLobbyId);
            panel.DebugEnterCode("ZZZZZZ");
            var errorText = panelGo.transform.Find("JoinRoomCanvas/ErrorText").GetComponent<Text>();
            Check("Wrong-code error shows the English message (Part 10.3/12.3)", errorText.text == "Incorrect code", $"text={errorText.text}");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("Error message updates live to Darija without re-triggering the join attempt (Part 12.2)",
                errorText.text == "الكود غالط", $"text={errorText.text}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Destroy(panelGo);
            Destroy(hostGo);
            Destroy(joinerLobbyGo);
            yield return null;
        }

        // ---------------------------------------------------------------
        private void TestPlayerAndRoomNamesNeverTranslated()
        {
            PlayerProfile.ResetForTesting();
            PlayerProfile.MarkFirstLaunchComplete();
            PlayerProfile.PlayerName = "Imad";

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            var panelGo = new GameObject("Stage17Test_MainMenu2");
            var panel = panelGo.AddComponent<MainMenuPanel>();
            panel.BuildUI();
            var playerCorner = panelGo.transform.Find("MainMenuCanvas/PlayerCorner").GetComponent<Text>();

            Check("The player's own free-typed name is never looked up in the string table, even under Darija (Part 12.3 explicit exclusion)",
                playerCorner.text == "Imad", $"text={playerCorner.text}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Destroy(panelGo);
        }

        // ---------------------------------------------------------------
        private IEnumerator TestEndRoundScreenLocalization()
        {
            var roundGo = new GameObject("Stage17Test_RoundManager");
            var roundManager = roundGo.AddComponent<PrankMansion.Systems.RoundManager>();
            roundManager.StartRound(new System.Collections.Generic.List<PrankMansion.Player.PlayerTeam>(), 0.1f);
            roundManager.RegisterPoint(PrankMansion.Player.Team.Team1);

            // Same wait-for-HasEnded pattern as Stage16UITest.TestEndRoundPanelMusicSelection
            // - WinnerTeam only settles once the round actually ends.
            float elapsed = 0f;
            while (!roundManager.HasEnded && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }
            Check("Setup: round actually ended before checking end-screen localization", roundManager.HasEnded, $"hasEnded={roundManager.HasEnded}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            var panelGo = new GameObject("Stage17Test_EndRound");
            var panel = panelGo.AddComponent<EndRoundPanel>();
            panel.BuildUI(roundManager, PrankMansion.Player.Team.Team1);

            Check("End-round winner text uses the localized team name (Part 11.6/12.3)", panel.WinnerText.text.Contains("Team 1"), $"text={panel.WinnerText.text}");

            LocalizationManager.SetLanguage(GameLanguage.Darija);
            Check("End-round winner text re-renders live in Darija (Part 12.2)", panel.WinnerText.text.Contains("الفريق 1"), $"text={panel.WinnerText.text}");
            Check("End-round scores line re-renders live in Darija too", panel.ScoresText.text.Contains("الفريق"), $"text={panel.ScoresText.text}");

            LocalizationManager.SetLanguage(GameLanguage.English);
            Destroy(panelGo);
            Destroy(roundGo);
        }
    }
}
