using System.IO;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 14: wires Part 9.5's PlayerTeam onto the real Player.prefab and drops a
    /// single RoundManager into the mansion scene to drive Part 9's scoring/timer.
    /// No new asset import here - this stage is pure gameplay logic, the same shape
    /// as Stage 3/4/5's systems-only setup scripts, not the later asset-import stages.
    /// </summary>
    public static class Stage14ScoringSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        private const string RoundManagerName = "Stage14_RoundManager";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage14_Decisions_Log.txt";

        [MenuItem("PrankMansion/Stage 14 - Build Scoring & Team System")]
        public static void BuildScoringSystem()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = GameObject.Find("Player");
            if (player != null && player.GetComponent<PlayerTeam>() == null)
                player.AddComponent<PlayerTeam>();

            var existing = GameObject.Find(RoundManagerName);
            if (existing != null) Object.DestroyImmediate(existing);
            new GameObject(RoundManagerName).AddComponent<RoundManager>();

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.GetComponent<PlayerTeam>() == null)
                prefabRoot.AddComponent<PlayerTeam>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

            WriteDecisionsLog();
            Debug.Log("[Stage14ScoringSetup] PlayerTeam wired onto Player; RoundManager placed in scene.");
        }

        [MenuItem("PrankMansion/Stage 14 - Build And Run Scoring Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildScoringSystem();

            var testGo = new GameObject("Stage14_ScoringTestRunner");
            testGo.AddComponent<Stage14ScoringTest>();

            Debug.Log("[Stage14ScoringSetup] Entering Play Mode to run scoring/team system test...");
            EditorApplication.isPlaying = true;
        }

        private static void WriteDecisionsLog()
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new[]
            {
                "=== Stage 14 - Scoring & Team System - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Part 9.5.1's team-split trigger (\"host presses start in the Part 10.4",
                "   lobby\") can't exist yet - Steam Multiplayer is Stage 15. RoundManager.",
                "   StartRound(connectedPlayers, duration) stands in for that trigger; a",
                "   future lobby just calls it with its own connected-player list.",
                "",
                "2. Event 3 (\"إطلاق خصم صاروخياً عبر نظام الريح بشكل غير مقصود\") is",
                "   inherently self-inflicted in the implemented mechanic (Part 7.2: the",
                "   wind-active CARRYING player is the one who walks into the fire and",
                "   launches themselves) - there is no second, acting player to credit.",
                "   Per 9.1's own explicit simplification note (\"يمكن تبسيط هذا ... ليشمل",
                "   أي حالة اشتعال تصيب خصماً\"), the point goes to the launched player's",
                "   OPPOSING team as a whole rather than a specific credited player, since",
                "   only 2 teams ever exist.",
                "",
                "3. Part 9.1's throw-hit event only credits the FIRST collision after a",
                "   throw (\"بشكل مباشر\" - direct) - hitting a wall, teammate, or anything",
                "   else first clears the thrown object's scoring eligibility; no bounce",
                "   hits count, even against an opponent afterward.",
                "",
                "4. Part 9.2/9.3's actual on-screen widgets (colored timer text, score",
                "   counters, the round-end/team-reveal transition screens) are Part 11's",
                "   UI, Stage 16 - not built here. RoundManager exposes the backing data",
                "   (ScoreTeam1/2, TimeRemaining, IsFinalCountdown, WinnerTeam, IsTie) for",
                "   that stage to read. The one non-visual behaviour Part 9.2 asks for -",
                "   the pulsing final-30-seconds alert SOUND - doesn't need a screen to",
                "   exist first, so it's built now via PlaceholderAudio (no real alert SFX",
                "   asset exists yet either - Audio/SFX/UI is still empty).",
                "",
                "5. Part 9.3's \"freeze movement/interact input, not physics\" is one static",
                "   PlayerInputReader.RoundInputFrozen flag - every player freezes at the",
                "   same instant, matching the document (round end is a single global",
                "   event, not per-player). Camera look stays free since 9.3 only names",
                "   \"الحركة\" (movement) and \"التفاعل\" (interaction), not the camera.",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
