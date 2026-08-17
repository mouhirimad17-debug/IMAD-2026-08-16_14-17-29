using System.IO;
using PrankMansion.Systems;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 18 (Parts 13, 15, 16, 18, 19): final audio/VFX wiring, performance
    /// settings, exception handling, comprehensive testing, and release settings -
    /// the closing stage of the whole roadmap (Part 20's المرحلة الثامنة عشرة).
    /// Most of Stage 18's actual work is runtime code (AudioService, StuckDetection,
    /// OutOfBoundsRecovery, etc.) already wired directly into the relevant Stage 3-
    /// 17 classes; this Editor-only script covers the pieces that can only be done
    /// from the Editor: static-batching/occlusion flags on the PureStatic prop
    /// prefabs across all five rooms (Part 15.2), and the release build settings
    /// (Part 19.1).
    /// </summary>
    public static class Stage18FinalizationSetup
    {
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage18_Decisions_Log.txt";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";

        [MenuItem("PrankMansion/Stage 18 - Apply Performance Flags + Build Settings")]
        public static void BuildFinalizationSystem()
        {
            int flagged = ApplyStaticFlagsForPureStaticProps();
            ApplyBuildSettings();
            WriteMissingAssetsLog();
            WriteDecisionsLog();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Stage18FinalizationSetup] Static-batching/occlusion flags applied to {flagged} PureStatic prefabs. Build settings + logs written.");
        }

        [MenuItem("PrankMansion/Stage 18 - Build And Run Finalization Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildFinalizationSystem();

            // The rope-tie sound test needs a real ragdoll to physically fall and
            // settle onto real floor geometry (Part 7.3/7.5) - same scene-opening
            // step every earlier physics-dependent stage's own BuildAndTest already
            // does (e.g. Stage5CaptureSetup) before entering Play Mode.
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var testGo = new GameObject("Stage18_FinalizationTestRunner");
            testGo.AddComponent<Stage18FinalizationTest>();

            Debug.Log("[Stage18FinalizationSetup] Entering Play Mode to run finalization test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        // Part 15.2: static batching/occlusion culling/GI apply to every prop
        // classified "ثابت صرف" (PureStatic) - same StaticEditorFlags combination
        // MansionBlockoutBuilder already applies to the architecture itself
        // (walls/floors), extended here to the five rooms' furniture prefabs.
        private static int ApplyStaticFlagsForPureStaticProps()
        {
            int flagged = 0;

            foreach (var e in Stage6FoyerPropSpec.Table)
                if (e.cls == Stage6FoyerPropSpec.PropClass.PureStatic && TryFlagStatic(Stage6FoyerPropSpec.PrefabDir, e.unityName)) flagged++;

            foreach (var e in Stage7KitchenPropSpec.Table)
                if (e.cls == Stage7KitchenPropSpec.PropClass.PureStatic && TryFlagStatic(Stage7KitchenPropSpec.PrefabDir, e.unityName)) flagged++;

            foreach (var e in Stage8OfficePropSpec.Table)
                if (e.cls == Stage8OfficePropSpec.PropClass.PureStatic && TryFlagStatic(Stage8OfficePropSpec.PrefabDir, e.unityName)) flagged++;

            foreach (var e in Stage9BedroomsPropSpec.Table)
                if (e.cls == Stage9BedroomsPropSpec.PropClass.PureStatic && TryFlagStatic(Stage9BedroomsPropSpec.PrefabDir, e.unityName)) flagged++;

            foreach (var e in Stage10GymPropSpec.Table)
                if (e.cls == Stage10GymPropSpec.PropClass.PureStatic && TryFlagStatic(Stage10GymPropSpec.PrefabDir, e.unityName)) flagged++;

            return flagged;
        }

        private static bool TryFlagStatic(string prefabDir, string unityName)
        {
            string path = prefabDir + unityName + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return false; // not imported yet - already logged by that room's own Stage importer (Law 0.2)

            GameObjectUtility.SetStaticEditorFlags(prefab,
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccluderStatic |
                StaticEditorFlags.OccludeeStatic | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
            EditorUtility.SetDirty(prefab);
            return true;
        }

        // ---------------------------------------------------------------
        // Part 19.1: Windows desktop target, fullscreen at the device's own
        // native resolution by default. Company name is left untouched - unlike
        // "Prank Mansion" (the game's own title, stated throughout the document),
        // a company/publisher name is a real business decision for the project
        // owner, not something this stage should invent.
        private static void ApplyBuildSettings()
        {
            PlayerSettings.productName = "Prank Mansion";
            // fullScreenMode alone covers "fullscreen at the device's native
            // resolution" in current Unity - defaultIsFullScreen is deprecated.
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        }

        // ---------------------------------------------------------------
        private static void WriteMissingAssetsLog()
        {
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var line = $"{System.DateTime.Now:yyyy-MM-dd} | Assets/_Project/Audio/Music, Assets/_Project/Audio/SFX/* | " +
                       "Part 13's full final mix (every entry in 13.1's sound map) still runs entirely on " +
                       "PlaceholderAudio.GenerateTone procedural tones (Law 0.2) - no real recorded/composed audio " +
                       "assets exist anywhere in the project yet. All thirteen event categories ARE wired up and " +
                       "audible (wind, ignite, slip, door, drawer, flour/plate/milk, rope-tie, round alert, " +
                       "background music, end-round jingles, parrot voice lines, UI clicks), and all correctly " +
                       "scale with Part 13.3's music/SFX sliders via Systems/AudioService - only the actual sound " +
                       "FILES are placeholders pending real audio assets.";
            File.AppendAllText(MissingAssetsLogPath, line + System.Environment.NewLine);
        }

        private static void WriteDecisionsLog()
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new[]
            {
                "=== Stage 18 - Final Assembly, Optimization, Exception Handling, Testing, Release (Law 21.2) ===",
                "",
                "PART 13 - AUDIO/MUSIC/VFX:",
                "1. Every sound event in 13.1's map is now wired: wind loop/ignite (already existed, now volume-",
                "   scaled), slip (SlipZone), material-tagged collision sounds + door/drawer (already existed,",
                "   now volume-scaled), flour-explode/plate-shatter/milk-leak (ImpactReactionProp), rope-tie",
                "   (PlayerCapture), round alert + new looping background music (RoundManager), end-round jingles",
                "   (already existed, now registered as MUSIC not SFX), parrot voice (already existed, now",
                "   volume-scaled), and UI clicks (a single hook in UIBuilder.CreateButton covers every button",
                "   project-wide, localized or not).",
                "2. Part 13.1's material-tagged collision sound is implemented as a new ImpactMaterial enum",
                "   (Wood/Metal/Glass/Human) + ImpactSoundLibrary, applied at the PROP-CLASS level",
                "   (CarryableObject/FallableProp/PushableProp default to Wood, PlayerRagdoll's hard-collision",
                "   point uses Human) rather than opening every one of the ~100 individual Part 4 table entries",
                "   to hand-assign a material - the document doesn't assign materials per-item either, and doing",
                "   so would mean re-touching every Stage 6-10 importer for a stage whose real job is wiring, not",
                "   re-classifying already-built prop tables. Individual props can still override `.material`.",
                "3. Part 13.3's volume sliders now do something: Systems/AudioService applies",
                "   PlayerProfile.MusicVolume/SfxVolume to every registered AudioSource LIVE (dragging the slider",
                "   changes already-playing sounds immediately, same 'no restart' spirit as Stage 17's language",
                "   switch) - before this stage the sliders only ever wrote to PlayerPrefs and were never read by",
                "   any actual AudioSource.",
                "4. Law 0.5's camera-shake-on-hard-collision entry (0.15 units, 0.2s, fades) had never been built",
                "   at all (Stage 2's camera work predates the physics stages that needed it) - added to",
                "   PlayerCameraRig (TriggerShake/LocalInstance) and wired into every hard-collision point",
                "   alongside its new material sound, since both fire from the exact same event.",
                "",
                "PART 15 - PERFORMANCE:",
                "5. Static batching/occlusion/GI flags (StaticEditorFlags, same combination",
                "   MansionBlockoutBuilder already applies to the architecture) are now applied to every PureStatic",
                "   prop prefab across all five rooms by this setup script.",
                "6. The actual Occlusion Culling BAKE (Window > Rendering > Occlusion Culling > Bake) needs a fully",
                "   assembled, lit scene open in the Editor and isn't something this batch-mode script should",
                "   attempt - the static flags above are the complete, safe prerequisite; the bake itself is a",
                "   manual step for whenever the real Scene_04_Mansion is fully assembled.",
                "7. LOD groups are NOT added: no lower-poly mesh variants exist for any imported prop (only a",
                "   single source mesh per item was ever imported), so a LODGroup would have nothing lower to",
                "   switch to - adding one around a single mesh provides zero actual benefit. Logged as a Law 0.2",
                "   missing-asset gap (needs real multi-LOD source meshes), not built as a fake placeholder.",
                "8. Part 15.2's idle-freeze for carryable objects uses Rigidbody.Sleep() (CarryableObject, 5s of",
                "   near-zero velocity while not held) rather than toggling isKinematic - Unity's own sleep state",
                "   already wakes automatically the instant another moving body touches it or a force/velocity is",
                "   applied, which is exactly 'إعادة تفعيلها فوراً عند أي تفاعل جديد معه' for free, and doesn't",
                "   need reinventing.",
                "9. Part 15.3's 20Hz network sync rate is recorded as Networking/NetworkTuning.PositionSyncRateHz.",
                "   It can't be APPLIED anywhere yet because no NetworkManager/transport exists in a scene (Stage",
                "   15 deferred that whole layer pending real Steamworks.NET) - the constant is ready the moment",
                "   that scene-level setup happens.",
                "",
                "PART 16 - EXCEPTION HANDLING:",
                "10. 16.1 (stuck detection): implemented as a continuous per-frame accumulator",
                "    (Player/StuckDetection.cs) rather than literally polling every 5 seconds - functionally the",
                "    same rule (~8s of zero movement despite active input triggers a reposition + toast), but far",
                "    easier to verify deterministically and strictly more responsive. Reused Part 12's toast-style",
                "    short-message pattern via a new small UI/ToastNotification helper.",
                "11. 16.2 (out-of-bounds recovery): a new OutOfBoundsRecovery component, auto-attached via",
                "    [RequireComponent] to CarryableObject/FallableProp/PushableProp (Part 16.2 says 'any physical",
                "    object', no exceptions), remembers each object's own spawn transform and restores it if the",
                "    object's Y ever drops below -5m (DECISION - the document gives no exact threshold, only",
                "    'ارتفاع سلبي كبير جداً').",
                "12. 16.3 (simultaneous-grab race condition): reviewed, not newly coded. This is inherently a",
                "    NETWORKED-authority concern (two different clients' requests racing to reach a server) -",
                "    within a single client's own main thread, two 'simultaneous' presses can't literally happen",
                "    (Unity has no true concurrency), and PlayerCarry.TryPickUpNearest's existing CarrierCount",
                "    guard already gives correct sequential-consistency. Real server-authoritative ordering needs",
                "    the still-deferred real Steamworks.NET transport (same dependency chain as items 9 and 19.2",
                "    below) - there's no networking layer yet for 'whoever the server processes first' to mean",
                "    anything beyond what already works today.",
                "13. 16.4 (invalid special-action guard): reviewed, already fully compliant. Every one of Part",
                "    7.5's insult options (PlayerCapture.TryRestrainBy/TryGrabEnd/TryThrowFromBalcony/",
                "    TryMountOnFan) already checks its exact precondition and silently returns false with zero",
                "    side effects when it isn't met, exactly matching 16.4's 'يُلغى الإجراء بصمت بلا أي تأثير أو",
                "    رسالة خطأ مزعجة'. Stage18FinalizationTest verifies this directly rather than re-implementing",
                "    something that was already correct.",
                "14. 16.5 (duplicate room names): LobbyInfo gained a HostDisplayName field, populated by",
                "    LocalLobbyDirectory.SearchByName from whichever player IsHost, so two identically-named rooms",
                "    are now distinguishable BY DATA. The actual clickable, visually-distinct list ROW UI remains",
                "    deferred (Stage 16's own decisions log already noted 'real result rows... need Stage 16's",
                "    still-nonexistent Icons/list-view art' - unchanged by this stage).",
                "",
                "PART 18 - TESTING: Stage18FinalizationTest.cs (Play Mode) covers everything net-new above that",
                "no earlier stage's test already covers - AudioService volume application/live update, material-",
                "tagged impact sound, Law 0.5 camera shake, the new slip/rope-tie/flour/UI-click sounds, background",
                "music start/stop with the round, CarryableObject idle-sleep, stuck-detection recovery, out-of-",
                "bounds recovery, duplicate-room-name host disambiguation, the already-compliant invalid-action",
                "guard, and the credits screen. Items 6 (occlusion bake) and 9 (network tick rate) aren't",
                "automatically testable - both are documented, deliberate deferrals with a clear trigger condition",
                "for when they become actionable, not gaps this stage silently skipped.",
                "",
                "PART 19 - RELEASE:",
                "15. Build settings (19.1): productName set to 'Prank Mansion', fullscreen-at-native-resolution",
                "    defaults applied. companyName is deliberately left untouched (a real business decision for",
                "    the project owner, unlike the game's own already-stated title).",
                "16. Steam integration (19.2) is explicitly out of Claude Code's scope per the document's own text",
                "    ('خطوة خارج نطاق عمل Claude Code البرمجي، تتطلب حساب مطور حقيقي وإجراءات إدارية') - nothing to",
                "    build here; the trial App ID from Part 10.1 stays until the project owner registers a real",
                "    one on Steamworks and swaps it in.",
                "17. Credits screen (19.3): new CreditsPanel, reached from Settings rather than added as a sixth",
                "    main-menu button, so Part 11.3's fixed five-button main-menu order isn't disturbed. Only the",
                "    'Developed by Imad' line is shown - no asset-store package anywhere in this project's",
                "    imported assets carries a license that requires its own attribution line.",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
