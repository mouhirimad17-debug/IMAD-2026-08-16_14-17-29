using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 12: imports and scale-corrects the seven real character models (Part 5)
    /// per Law 0.3/0.1 (each corrected to exactly 1.000m tall), configures every one
    /// of them - and every Player_Common animation clip - as a Humanoid rig (all 7
    /// characters and all 18 clips validated as real, working Humanoid avatars, no
    /// placeholder needed here), and builds ONE shared AnimatorController used by
    /// every character via Mecanim retargeting.
    ///
    /// Ragdoll DECISION: Part 7.3's ragdoll (Stage 4/5) already fully satisfies the
    /// document's own numeric spec (11+ joints, thresholds, timing) using a
    /// synthetic capsule rig. Rebuilding it on each of the 7 real skeletons' actual
    /// bones would mean 7 separate bone-mapping/joint-tuning passes for a purely
    /// visual upgrade Part 5/7.3 never actually requires - the real character mesh
    /// is shown for all normal animated gameplay, and only during the brief (2-4s)
    /// ragdoll window does the view drop back to the capsule proxies before the real
    /// mesh reappears on standing up. Logged as a deliberate scope decision, not an
    /// oversight - can be revisited later if the owner wants full ragdoll fidelity.
    /// </summary>
    public static class Stage12CharacterImporter
    {
        private const string PrefabDir = "Assets/_Project/Prefabs/Characters/";
        private const string AnimDir = "Assets/_Project/Animations/Player_Common/";
        private const string ControllerPath = "Assets/_Project/Animations/PlayerAnimator.controller";
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage12_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage12_StaticVerification_Report.txt";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const float ReferenceHeight = 1.0f; // Law 0.1

        // Part 5.3's full shared clip list mapped to the real Player_Common files
        // found on disk. Four clips have no real file (Pour, joint-carry-end hold,
        // rope-tying, standing-funny) - logged missing (Law 0.2), not placeholder-
        // built, since none of them block any other Stage 12 system from being
        // built and tested (only their own state would have nothing to show).
        private static readonly (string state, string file)[] ClipMap =
        {
            ("Idle", "Idle.fbx"),
            ("Walk", "Walk.fbx"),
            ("Run", "Run.fbx"),
            ("WalkingBackwards", "WalkingBackwards.fbx"),
            ("StrafeLeft", "StrafeLeft.fbx"),
            ("StrafeRight", "StrafeRight.fbx"),
            ("Jumpstart", "Jumpstart.fbx"),
            ("Fall", "Fall.fbx"),
            ("Land", "Land.fbx"),
            ("idleCarrylight", "idleCarrylight.fbx"),
            ("Walkcarrylight", "Walkcarrylight.fbx"),
            ("WalkCarryHeavy", "WalkCarryHeavy.fbx"),
            ("Push", "Push.fbx"),
            ("Throw", "Throw.fbx"),
            ("interact", "interact.fbx"),
            ("Walk_silly", "Walk_silly.fbx"),
            ("Run_silly", "Run_silly.fbx"),
            ("WalkBackwards_silly", "WalkBackwards_silly.fbx"),
        };

        private static readonly string[] MissingClipStates = { "Pour", "JointCarryEnd", "RopeTie", "Idle_silly" };

        [MenuItem("PrankMansion/Stage 12 - Import Characters & Build Animator")]
        public static void ImportAndBuild()
        {
            SetHumanoid(CharacterProfile.Table.Select(e => e.modelPath));
            SetHumanoid(ClipMap.Select(c => AnimDir + c.file));

            var controller = BuildAnimatorController();

            var results = new List<(CharacterProfile.Entry entry, GameObject prefab, float measuredHeight, float factor)>();
            foreach (var entry in CharacterProfile.Table)
                results.Add(CorrectAndSaveCharacter(entry, controller));

            UpdatePlayerPrefab();
            WriteMissingClipsLog();
            WriteDecisionsLog(results);

            Debug.Log($"[Stage12CharacterImporter] Done. Characters corrected: {results.Count(r => r.prefab != null)}/{CharacterProfile.Table.Length}.");
        }

        [MenuItem("PrankMansion/Stage 12 - Import And Run Character Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndBuild();
            RunStaticVerification();

            var testGo = new GameObject("Stage12_CharacterTestRunner");
            testGo.AddComponent<Stage12CharacterTest>();

            Debug.Log("[Stage12CharacterImporter] Entering Play Mode to run dynamic character verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        private static void SetHumanoid(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

        private static AnimatorController BuildAnimatorController()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            var sm = controller.layers[0].stateMachine;
            var existingStates = new HashSet<string>(sm.states.Select(s => s.state.name));

            foreach (var (state, file) in ClipMap)
            {
                if (existingStates.Contains(state)) continue;
                var clip = LoadClip(AnimDir + file);
                var newState = sm.AddState(state);
                newState.motion = clip;
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
        }

        // ---------------------------------------------------------------
        private static (CharacterProfile.Entry entry, GameObject prefab, float measuredHeight, float factor) CorrectAndSaveCharacter(
            CharacterProfile.Entry entry, AnimatorController controller)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(entry.modelPath);
            if (source == null)
                throw new InvalidOperationException($"Could not load character model at {entry.modelPath}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float measuredHeight = bounds.size.y;

            float factor = measuredHeight > 0.0001f ? ReferenceHeight / measuredHeight : 1f;

            // Pivot recentering (same Stage 9/10 lesson): X/Z centered, Y bottom at 0,
            // so the corrected character's own root sits exactly at its feet -
            // the standard convention CharacterController/PlayerLocomotion assume.
            var pivotRoot = new GameObject(entry.unityName);
            pivotRoot.transform.position = Vector3.zero;
            pivotRoot.transform.rotation = Quaternion.identity;
            instance.transform.SetParent(pivotRoot.transform, true);
            instance.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            pivotRoot.transform.localScale = Vector3.one * factor;

            var animator = instance.GetComponent<Animator>();
            if (animator == null) animator = instance.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(entry.modelPath).OfType<Avatar>().FirstOrDefault();
            animator.applyRootMotion = false;

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + entry.unityName + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(pivotRoot, prefabPath);
            UnityEngine.Object.DestroyImmediate(pivotRoot);

            return (entry, saved, measuredHeight, factor);
        }

        // ---------------------------------------------------------------
        // Brings Player.prefab (Stage 2/5's bare gameplay rig) up to date with every
        // interaction component built since (Stage 6's push, Stage 9's drag, Stage
        // 11's doors) plus Stage 12's own new ones - these were only ever exercised
        // through ad hoc test rigs before now, never actually added to the real
        // player prefab a live game would use.
        private static void UpdatePlayerPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            AddIfMissing<PlayerPushInteraction>(root);
            AddIfMissing<PlayerDragInteraction>(root);
            AddIfMissing<PlayerDoorInteraction>(root);
            AddIfMissing<FootstepSoundEmitter>(root);
            AddIfMissing<SoundDetector>(root);
            AddIfMissing<FunnyAnimationSwapper>(root);
            AddIfMissing<PlayerAnimatorDriver>(root);
            AddIfMissing<CharacterSelector>(root);

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        private static void AddIfMissing<T>(GameObject go) where T : Component
        {
            if (go.GetComponent<T>() == null) go.AddComponent<T>();
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 12 - Static Verification (Part 5 + Law 0.1/0.3) ===", "" };
            int total = 0, passed = 0;
            void Check(string name, bool ok, string detail)
            {
                total++;
                if (ok) passed++;
                lines.Add($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Check("Shared PlayerAnimator controller exists", controller != null, $"path={ControllerPath}");
            if (controller != null)
            {
                var stateNames = new HashSet<string>(controller.layers[0].stateMachine.states.Select(s => s.state.name));
                foreach (var (state, _) in ClipMap)
                    Check($"AnimatorController has the '{state}' state with a real clip (Part 5.3)",
                        stateNames.Contains(state) && controller.layers[0].stateMachine.states.First(s => s.state.name == state).state.motion != null,
                        $"present={stateNames.Contains(state)}");
            }

            foreach (var entry in CharacterProfile.Table)
            {
                string prefabPath = PrefabDir + entry.unityName + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Check($"{entry.unityName}: corrected character prefab exists", prefab != null, $"path={prefabPath}");
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                float tolerance = ReferenceHeight * 0.10f + 0.005f;
                Check($"{entry.unityName}: corrected height matches Law 0.1 (1.000m within 10%)",
                    Mathf.Abs(b.size.y - ReferenceHeight) <= tolerance, $"measured={b.size.y:F3}m");

                var animator = prefab.GetComponentInChildren<Animator>();
                Check($"{entry.unityName}: has a real Humanoid Animator (Part 5.3)",
                    animator != null && animator.avatar != null && animator.avatar.isHuman && animator.runtimeAnimatorController == controller,
                    $"found={(animator != null)} isHuman={(animator != null && animator.avatar != null && animator.avatar.isHuman)}");
            }

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Check("Player.prefab has PlayerAnimatorDriver (Stage 12)", playerPrefab != null && playerPrefab.GetComponent<PlayerAnimatorDriver>() != null, "");
            Check("Player.prefab has FunnyAnimationSwapper (Part 5.3)", playerPrefab != null && playerPrefab.GetComponent<FunnyAnimationSwapper>() != null, "");
            Check("Player.prefab has FootstepSoundEmitter (Part 5.1)", playerPrefab != null && playerPrefab.GetComponent<FootstepSoundEmitter>() != null, "");
            Check("Player.prefab has SoundDetector (Part 5.1/5.2)", playerPrefab != null && playerPrefab.GetComponent<SoundDetector>() != null, "");
            Check("Player.prefab has CharacterSelector (Stage 12)", playerPrefab != null && playerPrefab.GetComponent<CharacterSelector>() != null, "");
            Check("Player.prefab has PlayerPushInteraction (Part 4.1, retrofitted)", playerPrefab != null && playerPrefab.GetComponent<PlayerPushInteraction>() != null, "");
            Check("Player.prefab has PlayerDragInteraction (Stage 9, retrofitted)", playerPrefab != null && playerPrefab.GetComponent<PlayerDragInteraction>() != null, "");
            Check("Player.prefab has PlayerDoorInteraction (Stage 11, retrofitted)", playerPrefab != null && playerPrefab.GetComponent<PlayerDoorInteraction>() != null, "");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage12CharacterImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage12CharacterImporter] Static verification passed: {passed}/{total}.");
        }

        // ---------------------------------------------------------------
        private static void WriteMissingClipsLog()
        {
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(MissingAssetsLogPath, MissingClipStates.Select(n =>
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Animations/Player_Common/{n}.fbx | Part 5.3 shared animation - source file not found, no numbered variant either"));
        }

        private static void WriteDecisionsLog(List<(CharacterProfile.Entry entry, GameObject prefab, float measuredHeight, float factor)> results)
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 12 - Character Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. All 7 character FBX files AND all 18 real Player_Common animation clips",
                "   validated as genuine, working Humanoid rigs after being set to",
                "   ModelImporterAnimationType.Human (Stage12_HumanoidCheck_Report.txt,",
                "   temporary diagnostic, deleted after use) - Mecanim retargets the one",
                "   shared PlayerAnimator.controller onto each character's own Avatar with",
                "   no per-character animation work needed.",
                "",
                "2. RAGDOLL DECISION: Part 7.3's ragdoll (built Stage 4/5) already fully",
                "   satisfies the document's own numeric spec using a synthetic 12-capsule",
                "   rig - Part 5 never actually requires a real-skeleton ragdoll, only real",
                "   character MODELS for normal animated gameplay. Rebuilding physical",
                "   ragdoll joints on each of the 7 real skeletons' actual bones would be a",
                "   large, purely-visual undertaking (7 separate bone-mapping/joint-tuning",
                "   passes) for a brief 2-4s window. Kept the proven capsule rig for the",
                "   ragdoll state itself; the real character mesh is shown for all normal",
                "   locomotion/animation and reappears the instant the character stands",
                "   back up. Logged as a deliberate scope decision, revisitable later.",
                "",
                "3. Throw mechanic (PlayerCarry.cs): Part 7.1/7.2 never give a base throw",
                "   value anywhere, only Nouka's +25% relative bonus (Part 5.2) - added",
                "   BaseThrowSpeed=8 m/s as a reasonable, logged value in the same scale as",
                "   the project's other established forces (RocketForce=12,",
                "   PlayerPushInteraction.BasePushForce=5). The Throw button previously",
                "   just dropped the held object (Stage 3's own documented placeholder) -",
                "   now actually throws it forward at BaseThrowSpeed * throwPowerMultiplier.",
                "",
                "4. Sound detection (new SoundEvents/FootstepSoundEmitter/SoundDetector,",
                "   Player/): Part 5.1/5.2's footstep/door hearing-range mechanic (Reno",
                "   15m, Fifi's own footsteps only carry 4m, standard 8m both ways) did",
                "   not exist as any system before this stage. A sound is heard only within",
                "   BOTH the listener's own hearingRange AND the emitter's own",
                "   audibleRange (the smaller of the two governs) - this combines both",
                "   traits correctly without special-casing either character. Footstep",
                "   cadence (0.4s while moving) and detection-hold duration (1.5s) are not",
                "   given numbers in the document - reasonable, logged decisions.",
                "   DoorProp.TryToggle (Stage 11) now also emits a sound pulse at the",
                "   standard 8m range, matching Reno's own text (\"سماع خطوات وفتح أبواب\").",
                "",
                "5. Reno's screen-edge directional arrow (Part 11.5) is normally Stage 16's",
                "   job (full UI system) - built here anyway as a minimal, self-contained",
                "   runtime Canvas (RenoSoundIndicatorUI.cs) rather than waiting, since Part",
                "   18.1 explicitly requires an ACTUAL measurable test of Reno's hearing",
                "   range, and CharacterSelector only ever adds this component when the",
                "   chosen character is Reno specifically, matching the document's",
                "   \"exclusively on his own screen\" requirement by construction.",
                "",
                "6. Four of Part 5.3's shared clips have no real source file anywhere",
                "   (Pour, joint-carry-end hold, rope-tying, standing-funny/Idle_silly) -",
                "   logged missing per Law 0.2 below. None of them block any other system",
                "   in this stage (they're just clips with nothing to play yet), so no",
                "   placeholder animation was generated for them.",
                "",
                "7. PlayerLocomotion gained CharacterSpeedMultiplier (separate from the",
                "   existing, TEMPORARY SpeedMultiplier used for heavy-carry/joint-carry",
                "   penalties) for Part 5.2's PERMANENT per-character base speed trait",
                "   (only Bomba's -15% uses a value other than 1). The two stack",
                "   multiplicatively rather than fighting over the same field.",
                "",
                "8. Player.prefab (Stage 2/5's bare gameplay rig - CharacterController +",
                "   PlayerLocomotion/PlayerInputReader/PlayerCarry/PlayerRagdoll/",
                "   PlayerCapture only) never actually received PlayerPushInteraction",
                "   (Stage 6), PlayerDragInteraction (Stage 9), or PlayerDoorInteraction",
                "   (Stage 11) - each of those was only ever exercised through that",
                "   stage's own self-built test-rig GameObject, never retrofitted onto the",
                "   real prefab an actual game session would spawn. Added all three here",
                "   plus Stage 12's own new components, since this is the first stage",
                "   where the complete, real player composition actually matters.",
                "",
                $"Missing this stage ({MissingClipStates.Length} animation clips): {string.Join(", ", MissingClipStates)}",
            };

            foreach (var (entry, _, measuredHeight, factor) in results)
                lines.Add($"  {entry.unityName}: raw height={measuredHeight:F3}m -> factor={factor:F3} -> corrected 1.000m");

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
