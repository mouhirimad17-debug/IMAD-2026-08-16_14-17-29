using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 13: imports and scale-corrects Part 6's cage and parrot models (Law 0.3,
    /// against Stage13ParrotSpec's logged DECISION dimensions since Part 4 has no
    /// table row for either), assembles them into one composite Parrot_Cage_01
    /// prefab with ParrotController wired up and its audio clips assigned, and
    /// places it near the foyer center per Part 6.0. Same PropSpec/Importer/Test
    /// split every prop-import stage has used since Stage 6.
    /// </summary>
    public static class Stage13ParrotImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage13_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage13_StaticVerification_Report.txt";
        private const string ParrotRootName = Stage13ParrotSpec.SceneRootName;

        [MenuItem("PrankMansion/Stage 13 - Import Parrot & Cage")]
        public static void ImportAndCorrect()
        {
            var warnings = new List<string>();

            var (cagePrefabPart, cageFactor) = CorrectModel(Stage13ParrotSpec.CageModelPath, "Parrot_Cage_Visual", Stage13ParrotSpec.ExpectedCageMaxDim);
            var (parrotPrefabPart, parrotFactor) = CorrectModel(Stage13ParrotSpec.ParrotModelPath, "Parrot_Visual", Stage13ParrotSpec.ExpectedParrotMaxDim);

            if (cageFactor > 5f || cageFactor < 0.2f)
                warnings.Add($"Parrot_Cage_01: factor={cageFactor:F3} (expected {Stage13ParrotSpec.ExpectedCageMaxDim}m)");
            if (parrotFactor > 5f || parrotFactor < 0.2f)
                warnings.Add($"Parrot_Model_01: factor={parrotFactor:F3} (expected {Stage13ParrotSpec.ExpectedParrotMaxDim}m)");

            var (laughClips, englishClips, arabicClips) = LoadAudioClips(out var missingArabic);

            var prefab = BuildCompositePrefab(cagePrefabPart, parrotPrefabPart, laughClips, englishClips, arabicClips);
            PlaceInFoyer(prefab);

            WriteWarningsLog(warnings);
            WriteMissingAudioLog(missingArabic);
            WriteDecisionsLog(warnings, missingArabic, laughClips.Length, englishClips.Length, arabicClips.Length);

            Debug.Log($"[Stage13ParrotImporter] Done. Cage factor={cageFactor:F3}, Parrot factor={parrotFactor:F3}, " +
                      $"laugh clips={laughClips.Length}, EN clips={englishClips.Length}, AR clips={arabicClips.Length}.");
        }

        [MenuItem("PrankMansion/Stage 13 - Import And Run Parrot Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage13_ParrotTestRunner");
            testGo.AddComponent<Stage13ParrotTest>();

            Debug.Log("[Stage13ParrotImporter] Entering Play Mode to run dynamic parrot verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 13 - Static Verification (Part 6 + Law 0.3 + Law 0.2) ===", "" };
            int total = 0, passed = 0;

            void Check(string name, bool ok, string detail)
            {
                total++;
                if (ok) passed++;
                lines.Add($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
            }

            var root = GameObject.Find(ParrotRootName);
            Check("Parrot_Cage_01 composite is placed in the mansion scene", root != null, $"found={(root != null)}");

            var controller = root != null ? root.GetComponentInChildren<ParrotController>() : null;
            Check("ParrotController component present", controller != null, $"found={(controller != null)}");

            if (controller != null)
            {
                Check("Cage carries no Rigidbody (Part 6.4: zero physical reaction to pushes)",
                    controller.GetComponent<Rigidbody>() == null && controller.GetComponentInChildren<Rigidbody>() == null, "");
                Check("Cage has a solid collider (players can still collide with it, just can't move it)",
                    controller.GetComponentInChildren<Collider>() != null, "");
                Check("visualRoot assigned for Look_Around / jump bounce", controller.visualRoot != null, "");
                Check("At least one laugh clip loaded", controller.laughClips != null && controller.laughClips.Length > 0,
                    $"count={(controller.laughClips?.Length ?? 0)}");
                Check("At least one English mockery clip loaded", controller.mockClipsEnglish != null && controller.mockClipsEnglish.Length > 0,
                    $"count={(controller.mockClipsEnglish?.Length ?? 0)}");
            }

            string missingLogText = File.Exists(MissingAssetsLogPath) ? File.ReadAllText(MissingAssetsLogPath) : "";
            if (controller != null && (controller.mockClipsArabic == null || controller.mockClipsArabic.Length == 0))
                Check("Missing Arabic mockery set logged (Law 0.2)", missingLogText.Contains("parrotmock_AR"), "checked Missing_Assets_Log.txt");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage13ParrotImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage13ParrotImporter] Static verification passed: {passed}/{total}.");
        }

        // ---------------------------------------------------------------
        private static (GameObject instance, float factor) CorrectModel(string sourcePath, string name, float expectedMaxDim)
        {
            EnsureTestBedScene();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (source == null)
                throw new InvalidOperationException($"Could not load model at {sourcePath}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.name = name;
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(instance.transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float measuredDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            float factor = measuredDim > 0.0001f ? expectedMaxDim / measuredDim : 1f;
            instance.transform.localScale = Vector3.one * factor; // Law 0.3 step 6: root only

            return (instance, factor);
        }

        private static void EnsureTestBedScene()
        {
            if (SceneManager.GetActiveScene().path == TestBedScenePath) return;

            if (File.Exists(TestBedScenePath))
            {
                EditorSceneManager.OpenScene(TestBedScenePath, OpenSceneMode.Single);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var dir = Path.GetDirectoryName(TestBedScenePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                EditorSceneManager.SaveScene(scene, TestBedScenePath);
            }
        }

        // ---------------------------------------------------------------
        private static (AudioClip[] laugh, AudioClip[] english, AudioClip[] arabic) LoadAudioClips(out bool missingArabic)
        {
            var laugh = new List<AudioClip>();
            var english = new List<AudioClip>();
            var arabic = new List<AudioClip>();

            if (Directory.Exists(Stage13ParrotSpec.LaughAudioDir))
            {
                foreach (var path in Directory.GetFiles(Stage13ParrotSpec.LaughAudioDir, "*.mp3").OrderBy(p => p))
                {
                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip == null) continue;

                    string fileUpper = Path.GetFileName(path).ToUpperInvariant();
                    if (fileUpper.Contains("LAUGH")) laugh.Add(clip);
                    else if (fileUpper.Contains("_AR_") || fileUpper.Contains("_AR.")) arabic.Add(clip);
                    else if (fileUpper.Contains("_EN_") || fileUpper.Contains("_EN.")) english.Add(clip);
                }
            }

            missingArabic = arabic.Count == 0;
            return (laugh.ToArray(), english.ToArray(), arabic.ToArray());
        }

        // ---------------------------------------------------------------
        private static GameObject BuildCompositePrefab(GameObject cageInstance, GameObject parrotInstance,
            AudioClip[] laughClips, AudioClip[] englishClips, AudioClip[] arabicClips)
        {
            var root = new GameObject(Stage13ParrotSpec.PrefabName);

            cageInstance.transform.SetParent(root.transform, false);
            parrotInstance.transform.SetParent(root.transform, false);
            parrotInstance.transform.localPosition = Vector3.zero; // DECISION: parrot centered inside its cage

            AddCollidersIfMissing(cageInstance);

            var controller = root.AddComponent<ParrotController>();
            controller.visualRoot = parrotInstance.transform;
            controller.laughClips = laughClips;
            controller.mockClipsEnglish = englishClips;
            controller.mockClipsArabic = arabicClips;

            if (!Directory.Exists(Stage13ParrotSpec.PrefabDir)) Directory.CreateDirectory(Stage13ParrotSpec.PrefabDir);
            string prefabPath = Stage13ParrotSpec.PrefabDir + Stage13ParrotSpec.PrefabName + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            UnityEngine.Object.DestroyImmediate(root); // Law 0.3 step 7: delete temp copy from the test bed

            return saved;
        }

        private static void AddCollidersIfMissing(GameObject root)
        {
            // PureStatic classification (Stage 6 convention): non-convex MeshColliders,
            // no Rigidbody anywhere - fully immovable, blocks movement, zero reaction.
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = false;
            }
        }

        // ---------------------------------------------------------------
        private static void PlaceInFoyer(GameObject prefab)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find(ParrotRootName);
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

            var foyer = MansionSpec.Foyer;
            Vector3 pos = new Vector3(
                foyer.centerX + Stage13ParrotSpec.CageCenterOffsetX,
                Stage13ParrotSpec.CageMountHeight,
                foyer.centerZ + Stage13ParrotSpec.CageCenterOffsetZ);

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = ParrotRootName;
            go.transform.position = pos;

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MansionScenePath);
        }

        // ---------------------------------------------------------------
        private static void WriteWarningsLog(List<string> warnings)
        {
            if (warnings.Count == 0) return;
            var dir = Path.GetDirectoryName(ScaleWarningsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(ScaleWarningsLogPath, warnings.Select(w => $"{DateTime.UtcNow:yyyy-MM-dd} | {w}"));
        }

        private static void WriteMissingAudioLog(bool missingArabic)
        {
            if (!missingArabic) return;
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(MissingAssetsLogPath, new[]
            {
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Audio/Voice/Parrot/SFX_parrotmock_AR_*.mp3 | " +
                "Part 6.3 Arabic mockery sentence set - no file found (only *_EN_* and *LAUGH* exist); " +
                "ParrotController falls back to the English set whenever the 50/50 Arabic roll is picked."
            });
        }

        private static void WriteDecisionsLog(List<string> warnings, bool missingArabic, int laughCount, int englishCount, int arabicCount)
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 13 - Parrot/Cage Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Part 4's prop table has no row for Parrot_Cage_01 or Parrot_Model_01, so",
                "   Law 0.3's scale correction has no expected-dimension number to target.",
                $"   Chose ExpectedParrotMaxDim={Stage13ParrotSpec.ExpectedParrotMaxDim}m and",
                $"   ExpectedCageMaxDim={Stage13ParrotSpec.ExpectedCageMaxDim}m against Law 0.1's 1.000m",
                "   character reference - bird-sized next to a character, cage roomy enough to",
                "   read clearly without dwarfing it. Purely a visual/scale call, not a balance",
                "   number, so decided and logged here rather than blocking on the owner per 21.2.",
                "",
                "2. Placement: Part 6.0 asks for a mount height between 1.5-2m near the foyer",
                "   center; Part 3.3 reserves the exact foyer center for the future seating",
                "   cluster, and the relocated staircase footprint (MansionSpec.StaircaseFootprint,",
                "   x:[36,54] z:[24,36]) also covers part of that same central area. Used the",
                $"   midpoint height ({Stage13ParrotSpec.CageMountHeight}m) offset",
                $"   ({Stage13ParrotSpec.CageCenterOffsetX}m, {Stage13ParrotSpec.CageCenterOffsetZ}m) into the foyer's open",
                "   front-left quadrant, clearing both the reserved seating spot and the",
                "   staircase footprint while staying close enough to center for the 10m",
                "   detection radius to cover most of the room.",
                "",
                "3. Cage and parrot are assembled into ONE composite prefab (Parrot_Cage_01)",
                "   with ParrotController on the root - Part 6 always treats them as a single",
                "   fixed entity (the parrot never leaves the cage), so one prefab/one scene",
                "   object matches the document's own framing better than two separate props.",
                "",
                "4. Only the cage gets a collider (non-convex MeshCollider, no Rigidbody -",
                "   Stage 6's PureStatic pattern, so pushes/collisions produce zero reaction",
                "   by construction, matching 6.4 exactly). The parrot mesh itself is left",
                "   collider-free since it's already enclosed by the cage's own solid volume.",
                "",
                "5. Part 6.1's Look_Around and the Law 0.5 jump bounce both have no real",
                "   animation clip (Animations/Parrot is empty) and no numeric turn rate is",
                "   given anywhere. Built procedurally in ParrotController instead of blocking:",
                $"   a {Stage13ParrotSpec.LookAroundMaxYawDeg}deg yaw sway over {Stage13ParrotSpec.LookAroundPeriodSeconds}s while idle, and a",
                $"   {Stage13ParrotSpec.JumpHeight}m/{Stage13ParrotSpec.JumpCycleSeconds}s bounce (Law 0.5's own numbers) while any mockery audio plays.",
                "",
                $"6. Audio folder scan found {laughCount} laugh clip(s) and {englishCount} English",
                $"   mockery clip(s), but {arabicCount} Arabic mockery clip(s) - Part 6.3 needs a",
                "   50/50 Arabic/English split. Logged to Missing_Assets_Log.txt per Law 0.2;",
                "   ParrotController falls back to the English set whenever the Arabic roll is",
                "   picked, rather than staying silent, until the Arabic set is supplied.",
                "",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
                $"Missing this stage: {(missingArabic ? "Arabic mockery audio set (SFX_parrotmock_AR_*.mp3)" : "none")}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
