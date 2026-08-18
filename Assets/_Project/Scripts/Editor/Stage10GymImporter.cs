using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Entry = PrankMansion.Blockout.Stage10GymPropSpec.Entry;
using PropClass = PrankMansion.Blockout.Stage10GymPropSpec.PropClass;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 10: imports and scale-corrects the Gym's real prop models (Part 4.7)
    /// following Law 0.3's exact protocol, same pipeline as Stages 6-9.
    ///
    /// Applies the pivot-recentering fix discovered during the Stage 9 fix-up from
    /// the start this time (see Stage9_Decisions_Log.txt point 7): a uniform scale
    /// factor alone does not fix a bad source-file pivot, so every corrected mesh is
    /// recentered to its own footprint base (X/Z center, Y minimum) before the
    /// factor is applied - except Gym_PunchingBag_01, whose pivot needs to stay at
    /// its own geometric center (matching CeilingFan's Stage 9 exception) since
    /// PunchingBagProp's ConfigurableJoint anchors relative to the object's own
    /// transform, and its swing motion should hang from above its actual mesh, not
    /// from an artificially-shifted floor-level origin.
    ///
    /// Gym_DumbbellHeavy_01/Gym_Barbell_01 matched to unlabeled numbered variant
    /// sets (Gym_dumbbell_01/02/03.glb, gym_barbell_01/02/03.glb) - Part 4.7 only
    /// asks for one of each, so _01 is used and _02/_03 are logged as unused spares
    /// (lower-confidence inferred match, same caveat as prior stages' synonym
    /// matches).
    ///
    /// Also applies Stage 9's second fix-up lesson from the start: Editor
    /// scripting's AddComponent does not reliably fire Awake() synchronously in
    /// headless batch mode, so PunchingBagProp exposes a public
    /// EnsureInitialized() called explicitly right after AddComponent (building its
    /// ConfigurableJoint before the prefab is saved), in addition to its own
    /// idempotent Awake() for correctness on every later real instantiation.
    /// </summary>
    public static class Stage10GymImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage10_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage10_StaticVerification_Report.txt";
        private const float DimensionTolerancePct = 0.10f;

        private static Entry[] Table => Stage10GymPropSpec.Table;
        private static string PrefabDir => Stage10GymPropSpec.PrefabDir;
        private static string PropsRootName => Stage10GymPropSpec.GymPropsRootName;

        [MenuItem("PrankMansion/Stage 10 - Import & Correct Gym Props")]
        public static void ImportAndCorrect()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            var missing = new List<string>();
            var warnings = new List<string>();

            foreach (var entry in Table)
            {
                if (string.IsNullOrEmpty(entry.sourcePath) || !File.Exists(entry.sourcePath))
                {
                    missing.Add(entry.unityName);
                    continue;
                }

                var (prefab, measuredDim, factor) = CorrectAndSavePrefab(entry);
                results.Add((entry, prefab, measuredDim, factor));
                if (factor > 5f || factor < 0.2f)
                    warnings.Add($"{entry.unityName}: factor={factor:F3} (expected {entry.expectedMaxDim}m, measured {measuredDim:F3}m)");
            }

            PlaceIntoGym(results);
            WriteMissingLog(missing);
            WriteWarningsLog(warnings);
            WriteDecisionsLog(missing, warnings);

            Debug.Log($"[Stage10GymImporter] Done. Corrected {results.Count(r => r.prefab != null)}, missing {missing.Count}.");
        }

        // Fix-up entry point: re-runs ONLY PlaceIntoGym against the prefabs that
        // already exist on disk (no re-import) - see Stage6's counterpart.
        [MenuItem("PrankMansion/Stage 10 - Regenerate Gym Placement Only")]
        public static void RegeneratePlacementOnly()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            foreach (var entry in Table)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + entry.unityName + ".prefab");
                results.Add((entry, prefab, entry.expectedMaxDim, 1f));
            }
            PlaceIntoGym(results);
            Debug.Log($"[Stage10GymImporter] Re-placed {results.Count(r => r.prefab != null)} existing Gym prefabs.");
        }

        [MenuItem("PrankMansion/Stage 10 - Import And Run Gym Props Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage10_GymPropsTestRunner");
            testGo.AddComponent<Stage10GymPropsTest>();

            Debug.Log("[Stage10GymImporter] Entering Play Mode to run dynamic prop verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 10 - Static Verification (Part 4.7 + Law 0.3 + Law 0.2) ===", "" };
            int total = 0, passed = 0;

            void Check(string name, bool ok, string detail)
            {
                total++;
                if (ok) passed++;
                lines.Add($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
            }

            var missingSet = new HashSet<string>(Table.Where(e => string.IsNullOrEmpty(e.sourcePath)).Select(e => e.unityName));
            string missingLogText = File.Exists(MissingAssetsLogPath) ? File.ReadAllText(MissingAssetsLogPath) : "";

            foreach (var entry in Table)
            {
                if (missingSet.Contains(entry.unityName))
                {
                    Check($"{entry.unityName}: logged as missing (Law 0.2)", missingLogText.Contains(entry.unityName),
                        "checked Missing_Assets_Log.txt");
                    continue;
                }

                string prefabPath = PrefabDir + entry.unityName + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Check($"{entry.unityName}: corrected prefab exists", prefab != null, $"path={prefabPath}");
                if (prefab == null) continue;

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float measured = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float tolerance = entry.expectedMaxDim * DimensionTolerancePct + 0.005f;
                Check($"{entry.unityName}: corrected size matches Part 4 ({entry.expectedMaxDim}m within {DimensionTolerancePct:P0})",
                    Mathf.Abs(measured - entry.expectedMaxDim) <= tolerance,
                    $"measured={measured:F3}m expected={entry.expectedMaxDim}m");

                bool classOk = entry.cls switch
                {
                    PropClass.CarryLight => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Light,
                    PropClass.CarryHeavy => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Heavy,
                    PropClass.PureStatic => prefab.GetComponent<Rigidbody>() == null || entry.isPunchingBag,
                    _ => false
                };
                Check($"{entry.unityName}: correct Part 4.1 classification component(s)", classOk, $"class={entry.cls}");

                if (entry.isPunchingBag)
                {
                    var bag = prefab.GetComponent<PunchingBagProp>();
                    Check($"{entry.unityName}: has the real PunchingBagProp component (Part 4.7)", bag != null, $"found={(bag != null)}");
                    Check($"{entry.unityName}: anchored via a ConfigurableJoint (Part 4.7)",
                        prefab.GetComponent<ConfigurableJoint>() != null, $"found={(prefab.GetComponent<ConfigurableJoint>() != null)}");
                }
            }

            var placedRoot = GameObject.Find(PropsRootName);
            int expectedPlaced = Table.Count(e => !missingSet.Contains(e.unityName));
            Check("Gym props are placed in the actual mansion scene", placedRoot != null && placedRoot.transform.childCount == expectedPlaced,
                $"childCount={(placedRoot != null ? placedRoot.transform.childCount : -1)} expected={expectedPlaced}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage10GymImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage10GymImporter] Static verification passed: {passed}/{total}.");
        }

        // ---------------------------------------------------------------
        private static (GameObject prefab, float measuredDim, float factor) CorrectAndSavePrefab(Entry entry)
        {
            EnsureTestBedScene();

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(entry.sourcePath);
            if (source == null)
                throw new InvalidOperationException($"Could not load model at {entry.sourcePath}");

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0
                ? renderers[0].bounds
                : new Bounds(instance.transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            float measuredDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

            float factor = measuredDim > 0.0001f ? entry.expectedMaxDim / measuredDim : 1f;

            // Pivot recentering (Stage 9 fix-up point 7's lesson, applied proactively
            // here): recenter X/Z to the pre-scale bounds center and Y to its
            // minimum (the object's true floor-contact point) before scaling, so
            // PlaceIntoGym's `position = floor slot` lands on the object's actual
            // footprint base rather than an arbitrary source-file pivot. Exception:
            // the punching bag keeps its own geometric center as its pivot (Y not
            // recentered to the bottom) - PunchingBagProp's joint/swing anchors
            // relative to this transform's own origin and bounds, and it hangs from
            // above rather than resting on the floor.
            var pivotRoot = new GameObject(entry.unityName);
            pivotRoot.transform.position = Vector3.zero;
            pivotRoot.transform.rotation = Quaternion.identity;
            instance.transform.SetParent(pivotRoot.transform, true);
            float pivotY = entry.isPunchingBag ? bounds.center.y : bounds.min.y;
            instance.transform.position -= new Vector3(bounds.center.x, pivotY, bounds.center.z);

            pivotRoot.transform.localScale = Vector3.one * factor;

            bool needsConvex = entry.cls != PropClass.PureStatic || entry.isPunchingBag;
            AddCollidersIfMissing(pivotRoot, needsConvex);
            AddClassificationComponents(pivotRoot, entry);

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + entry.unityName + ".prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(pivotRoot, prefabPath);
            UnityEngine.Object.DestroyImmediate(pivotRoot);

            return (savedPrefab, measuredDim, factor);
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

        private static void AddCollidersIfMissing(GameObject root, bool convex)
        {
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Collider>() != null) continue;
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = convex;
            }
        }

        private static void AddClassificationComponents(GameObject root, Entry entry)
        {
            switch (entry.cls)
            {
                case PropClass.CarryLight:
                    var lightCarry = root.AddComponent<CarryableObject>();
                    lightCarry.weightClass = CarryableObject.WeightClass.Light;
                    break;
                case PropClass.CarryHeavy:
                    var heavyCarry = root.AddComponent<CarryableObject>();
                    heavyCarry.weightClass = CarryableObject.WeightClass.Heavy;
                    break;
                case PropClass.PureStatic:
                    break;
            }

            // EnsureInitialized() called explicitly - same Stage 9 lesson (Editor
            // scripting's AddComponent does not reliably fire Awake synchronously in
            // headless batch mode), and the joint must exist before
            // PrefabUtility.SaveAsPrefabAsset runs below.
            if (entry.isPunchingBag) root.AddComponent<PunchingBagProp>().EnsureInitialized();
        }

        // ---------------------------------------------------------------
        private static void PlaceIntoGym(List<(Entry entry, GameObject prefab, float measuredDim, float factor)> results)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existingRoot = GameObject.Find(PropsRootName);
            if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot);
            var root = new GameObject(PropsRootName).transform;

            var gym = MansionSpec.Gym;
            const float margin = 1.5f;
            const float spacing = 2f;

            float xMin = gym.x + margin, xMax = gym.xMax - margin;
            float zMin = gym.z + margin, zMax = gym.zMax - margin;

            int placeCount = results.Count(r => r.prefab != null);
            var slots = FurnitureGridPlacement.BuildSlots(xMin, xMax, zMin, zMax, placeCount, spacing);

            int i = 0;
            foreach (var (entry, prefab, _, _) in results)
            {
                if (prefab == null || i >= slots.Count) continue;
                Vector2 slot = slots[i++];

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root, false);

                // The punching bag hangs from the ceiling, not the floor - placed at
                // ceiling height like Stage 6/9's chandelier/fan exceptions.
                float y = entry.isPunchingBag ? MansionSpec.Floor1CeilingY - 0.1f : 0.1f;
                go.transform.position = new Vector3(slot.x, y, slot.y);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MansionScenePath);
        }

        // ---------------------------------------------------------------
        private static void WriteMissingLog(List<string> missing)
        {
            if (missing.Count == 0) return;
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(MissingAssetsLogPath, missing.Select(n =>
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Gym/(Static|Throwable)/{n}.* | Part 4.7 Gym prop - source file not found, no numbered variant or clear synonym either"));
        }

        private static void WriteWarningsLog(List<string> warnings)
        {
            if (warnings.Count == 0) return;
            var dir = Path.GetDirectoryName(ScaleWarningsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(ScaleWarningsLogPath, warnings.Select(w => $"{DateTime.UtcNow:yyyy-MM-dd} | {w}"));
        }

        private static void WriteDecisionsLog(List<string> missing, List<string> warnings)
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 10 - Gym Prop Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Pivot recentering (Entities root cause found during the Stage 9 fix-up,",
                "   see Stage9_Decisions_Log.txt point 7) is applied from the start here -",
                "   every corrected mesh is recentered to its own X/Z center and Y minimum",
                "   before Law 0.3's uniform scale is applied, so it actually stands on its",
                "   floor slot instead of wherever the source file's arbitrary pivot was.",
                "   Exception: Gym_PunchingBag_01 keeps its own geometric center as pivot",
                "   (not recentered to its base) since it hangs from a ceiling anchor rather",
                "   than resting on the floor - see point 4 below.",
                "",
                "2. Gym_DumbbellHeavy_01 matched to Gym_dumbbell_01.glb - an unlabeled",
                "   numbered variant set (Gym_dumbbell_01/02/03.glb) sitting alongside the",
                "   clearly-labeled gym_dumbbelllight_01.glb. Part 4.7 only lists one Heavy",
                "   dumbbell entry, so _01 is used and _02/_03 are left as unused spare",
                "   variants. Flagged as a lower-confidence inferred match, same caveat as",
                "   prior stages' synonym matches.",
                "",
                "3. Gym_Barbell_01 matched the same way to gym_barbell_01.glb out of its own",
                "   01/02/03 variant set (gym_barbell_02.glb in particular is a much larger",
                "   file than the other two - not investigated further since _01 already",
                "   satisfies the single required entry).",
                "",
                "4. Gym_PunchingBag_01's \"يتأرجح فيزيائياً عند الاصطدام من نقطة تعليق علوية\"",
                "   (swings physically on impact from an upper suspension point) is fully",
                "   described (unlike Stage 9's two document gaps) so no owner clarification",
                "   was needed - implemented as a new PunchingBagProp component",
                "   (Entities/PunchingBagProp.cs): a ConfigurableJoint anchored to a fixed",
                "   world point at the top of its own bounds, linear motion Locked, angular",
                "   X/Z Free (Y lightly Limited so it doesn't spin freely around its own",
                "   hanging axis) - ordinary collision physics then makes it swing with no",
                "   extra \"on hit\" code required. Placed at ceiling height like Stage 6/9's",
                "   chandelier/ceiling-fan exceptions rather than the floor grid.",
                "",
                "5. Four items have no source file and no numbered variant or clear synonym",
                "   (Mirror, MedicineBall, Door, Window) - logged per Law 0.2 step 2b. None",
                "   qualify as \"essential to a whole system\" (step 3's own threshold), so -",
                "   unlike Stage 9's owner-directed fix-up - no placeholder was built for",
                "   any of them here; that override was scoped to that specific fix-up task.",
                "",
                "6. Gym_Door_01's hinge/open-close behavior is Stage 11's job, same as every",
                "   other room's door in Stages 6-9 - moot here anyway since the source file",
                "   itself is missing (point 5).",
                "",
                "7. Scene placement uses the shared FurnitureGridPlacement grid-scatter",
                "   helper (same as Stages 6-9; no interior-design coordinates given",
                "   anywhere in the document), 2m target spacing, sized to actually span",
                "   the room's full floor area on both axes instead of only its first row",
                "   (fix-up: the old per-stage row-major slot lists filled front-to-back,",
                "   so a small prop count always bunched into a single line along the",
                "   room's near edge).",
                "",
                $"Missing this stage ({missing.Count}): {string.Join(", ", missing)}",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
