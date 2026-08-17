using System.IO;
using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 16: no asset import and no Player.prefab/scene wiring is needed here
    /// (UI screens are scene-level, procedurally built at runtime by UIManager -
    /// same technique Stage 12's RenoSoundIndicatorUI already established, since
    /// no Fonts/Icons/Backgrounds assets exist yet). This setup script's only real
    /// job is the decisions log; BuildAndTest runs Stage16UITest in Play Mode.
    /// </summary>
    public static class Stage16UISetup
    {
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage16_Decisions_Log.txt";

        [MenuItem("PrankMansion/Stage 16 - Build UI Decisions Log")]
        public static void BuildUISystem()
        {
            WriteDecisionsLog();
            Debug.Log("[Stage16UISetup] Decisions log written. UI is built entirely at runtime by UIManager - no scene/prefab changes needed.");
        }

        [MenuItem("PrankMansion/Stage 16 - Build And Run UI Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildUISystem();

            var testGo = new GameObject("Stage16_UITestRunner");
            testGo.AddComponent<Stage16UITest>();

            Debug.Log("[Stage16UISetup] Entering Play Mode to run UI system test...");
            EditorApplication.isPlaying = true;
        }

        private static void WriteDecisionsLog()
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new[]
            {
                "=== Stage 16 - Full UI System - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Every screen is built procedurally at runtime (Canvas/Text/Button/Image",
                "   via UIBuilder) rather than as hand-authored prefabs/scenes - the same",
                "   technique Stage 12's RenoSoundIndicatorUI already used, since",
                "   Assets/_Project/UI/Fonts, Icons, and Backgrounds are all still empty",
                "   (Law 0.2). Text uses Unity's built-in LegacyRuntime font; icons are",
                "   plain colored shapes; there is no logo texture or background art -",
                "   all placeholder, matching PlaceholderAudio's established spirit for a",
                "   missing-asset category.",
                "",
                "2. Part 12's language system (Stage 17, not built yet) means every",
                "   screen's own text stays English regardless of the language choice made",
                "   in 11.1/11.7 - PlayerProfile.Language is stored correctly now so Stage",
                "   17 has something to read, but no screen re-renders its strings yet.",
                "",
                "3. Part 11.2's rotating 3D character preview builds the actual turntable",
                "   instance and rotation (independently correct/testable, batch-mode Play",
                "   Mode never renders a frame anyway) but leaves the camera -> RenderTexture",
                "   -> RawImage plumbing that would make it visible inside a UI card for the",
                "   first real interactive viewing - a rendering-pipeline detail, not a rule",
                "   this stage's automated verification can meaningfully check either way.",
                "",
                "4. Part 11.6's MVP stat is explicitly marked optional in the document",
                "   itself (\"قسم إحصائيات مختصر اختياري\") and is skipped - Stage 14's",
                "   RoundManager only tracks TEAM-level scores; retrofitting every Part 9.1",
                "   scoring call site to also attribute an individual scorer isn't",
                "   justified for a feature the document itself doesn't require.",
                "",
                "5. Part 10.4's waiting-room screen has no dedicated Part 11.x subsection",
                "   (11.4 only covers create/join) but 10.4's own text already specifies",
                "   most of its visual layout, so WaitingRoomPanel is built here to fully",
                "   close the create/join -> waiting-room -> (eventually) round loop.",
                "   Actually spawning players into the mansion scene and calling",
                "   RoundManager.StartRound needs the real connected player list, which",
                "   only exists once real networking connects real players (Steamworks.NET,",
                "   still deferred per Stage 15) - WaitingRoomPanel fires OnGameStarted as",
                "   its side of that handoff and stops there.",
                "",
                "6. GameplayHUD and EndRoundPanel are NOT managed by UIManager (which only",
                "   owns the menu-flow screens) - both need a live RoundManager and the",
                "   local player's own PlayerCarry/PlayerTeam, which only exist once a",
                "   round has actually started. Same menu-vs-in-round boundary Stage 15",
                "   already drew for its own scope.",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
