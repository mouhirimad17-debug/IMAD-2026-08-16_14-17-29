using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Entry = PrankMansion.Blockout.Stage6FoyerPropSpec.Entry;
using PropClass = PrankMansion.Blockout.Stage6FoyerPropSpec.PropClass;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 6: imports and scale-corrects the Foyer's real prop models (Part 4.2)
    /// following Law 0.3's exact protocol (measure against a persistent ScaleTestBed
    /// scene, compute a correction factor against Part 4's expected size, apply to
    /// the root only, save as a canonically-named Prefab, log any >5x/<0.2x
    /// correction), then tags each with its Part 4.1 physical classification and
    /// places it in the actual Foyer. Table/constants live in the runtime-accessible
    /// Stage6FoyerPropSpec (Systems/) so Play Mode's Stage6FoyerPropsTest can also
    /// read them - this Editor-only file can't be referenced from there.
    ///
    /// SCOPE NOTE: Part 4.2 also lists Foyer_StaircaseDouble_01 and Foyer_Railing_01.
    /// Both are deliberately excluded from this pass - swapping them in for Stage 1's
    /// tested procedural blockout means matching a real mesh's own pivot/orientation
    /// to the exact Law-0.4-verified footprint, which cannot be safely confirmed
    /// without visually inspecting the asset (not possible in headless batch mode).
    /// Doing that blind risks silently breaking the already-verified wall-closure and
    /// traversal path. Deferred - see Stage6_Decisions_Log.txt.
    /// </summary>
    public static class Stage6FoyerImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage6_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage6_StaticVerification_Report.txt";
        private const float DimensionTolerancePct = 0.10f;

        private static Entry[] Table => Stage6FoyerPropSpec.Table;
        private static string PrefabDir => Stage6FoyerPropSpec.PrefabDir;
        private static string FoyerPropsRootName => Stage6FoyerPropSpec.FoyerPropsRootName;

        [MenuItem("PrankMansion/Stage 6 - Import & Correct Foyer Props")]
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

            PlaceIntoFoyer(results);
            WriteMissingLog(missing);
            WriteWarningsLog(warnings);
            WriteDecisionsLog(missing, warnings);

            Debug.Log($"[Stage6FoyerImporter] Done. Corrected {results.Count(r => r.prefab != null)}, missing {missing.Count}.");
        }

        // Fix-up entry point: re-runs ONLY PlaceIntoFoyer against the prefabs that
        // already exist on disk (no re-import, no CorrectAndSavePrefab, no TestBed
        // scene round-trip) - used to pick up a placement-only algorithm change
        // (e.g. FurnitureGridPlacement) without touching already-correct prefabs.
        [MenuItem("PrankMansion/Stage 6 - Regenerate Foyer Placement Only")]
        public static void RegeneratePlacementOnly()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            foreach (var entry in Table)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + entry.unityName + ".prefab");
                results.Add((entry, prefab, entry.expectedMaxDim, 1f));
            }
            PlaceIntoFoyer(results);
            Debug.Log($"[Stage6FoyerImporter] Re-placed {results.Count(r => r.prefab != null)} existing Foyer prefabs.");
        }

        [MenuItem("PrankMansion/Stage 6 - Import And Run Foyer Props Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage6_FoyerPropsTestRunner");
            testGo.AddComponent<Stage6FoyerPropsTest>();

            Debug.Log("[Stage6FoyerImporter] Entering Play Mode to run dynamic prop verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        // Static (Editor-only, no Play Mode needed) verification: prefab existence,
        // Law 0.3's scale-correction accuracy, Part 4.1 classification components,
        // Law 0.2's missing-asset logging, and scene placement. Separate report from
        // the Play Mode physics test (Stage6FoyerPropsTest) since neither needs the
        // other's context - same "each report stands on its own" pattern as Stages
        // 1/2's separate reports.
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 6 - Static Verification (Part 4.2 + Law 0.3 + Law 0.2) ===", "" };
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
                // Prefab-asset renderer bounds already reflect the corrected root scale
                // (world-space bounds of a prefab asset use its own stored transforms).
                float measured = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float tolerance = entry.expectedMaxDim * DimensionTolerancePct + 0.005f;
                Check($"{entry.unityName}: corrected size matches Part 4 ({entry.expectedMaxDim}m within {DimensionTolerancePct:P0})",
                    Mathf.Abs(measured - entry.expectedMaxDim) <= tolerance,
                    $"measured={measured:F3}m expected={entry.expectedMaxDim}m");

                bool classOk = entry.cls switch
                {
                    PropClass.Fallable => prefab.GetComponent<FallableProp>() != null && prefab.GetComponent<Rigidbody>() != null,
                    PropClass.FireSourceFallable => prefab.GetComponent<FallableProp>() != null && prefab.GetComponent<FireSource>() != null,
                    PropClass.CarryLight => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Light,
                    PropClass.Pushable => prefab.GetComponent<PushableProp>() != null,
                    PropClass.PureStatic => prefab.GetComponent<Rigidbody>() == null,
                    _ => false
                };
                Check($"{entry.unityName}: correct Part 4.1 classification component(s)", classOk, $"class={entry.cls}");
            }

            var placedRoot = GameObject.Find(FoyerPropsRootName);
            int expectedPlaced = Table.Count(e => !missingSet.Contains(e.unityName));
            Check("Foyer props are placed in the actual mansion scene", placedRoot != null && placedRoot.transform.childCount == expectedPlaced,
                $"childCount={(placedRoot != null ? placedRoot.transform.childCount : -1)} expected={expectedPlaced}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage6FoyerImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage6FoyerImporter] Static verification passed: {passed}/{total}.");
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
            instance.transform.localScale = Vector3.one * factor; // law 0.3 step 6: root only, never children

            bool needsConvex = entry.cls != PropClass.PureStatic;
            AddCollidersIfMissing(instance, needsConvex);
            AddClassificationComponents(instance, entry.cls);
            instance.name = entry.unityName;

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + entry.unityName + ".prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance); // law 0.3 step 7: delete the temp copy from the test bed

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
                mc.convex = convex; // PhysX requires convex hulls for anything that can become non-kinematic
            }
        }

        private static void AddClassificationComponents(GameObject root, PropClass cls)
        {
            switch (cls)
            {
                case PropClass.Fallable:
                    root.AddComponent<FallableProp>();
                    break;
                case PropClass.FireSourceFallable:
                    root.AddComponent<FallableProp>();
                    root.AddComponent<FireSource>();
                    break;
                case PropClass.CarryLight:
                    var carry = root.AddComponent<CarryableObject>();
                    carry.weightClass = CarryableObject.WeightClass.Light;
                    break;
                case PropClass.Pushable:
                    root.AddComponent<PushableProp>();
                    break;
                case PropClass.PureStatic:
                    break;
            }
        }

        // ---------------------------------------------------------------
        private static void PlaceIntoFoyer(List<(Entry entry, GameObject prefab, float measuredDim, float factor)> results)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existingRoot = GameObject.Find(FoyerPropsRootName);
            if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot);
            var root = new GameObject(FoyerPropsRootName).transform;

            var foyer = MansionSpec.Foyer;
            var stair = MansionSpec.StaircaseFootprint;
            const float margin = 1.5f;
            const float spacing = 2f;

            float xMin = foyer.x + margin, xMax = foyer.xMax - margin;
            float zMin = foyer.z + margin, zMax = foyer.zMax - margin;

            int placeCount = results.Count(r => r.prefab != null);
            var slots = FurnitureGridPlacement.BuildSlots(xMin, xMax, zMin, zMax, placeCount, spacing,
                (x, z) => x > stair.x - 0.5f && x < stair.xMax + 0.5f && z > stair.z - 0.5f && z < stair.zMax + 0.5f);

            int i = 0;
            foreach (var (entry, prefab, _, _) in results)
            {
                if (prefab == null || i >= slots.Count) continue;
                Vector2 slot = slots[i++];

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(slot.x, GetPlacementHeight(entry.unityName), slot.y);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MansionScenePath);
        }

        // DECISION: no interior-design coordinates are given anywhere in the document
        // (only structural coordinates). This simple keyword heuristic keeps ceiling/
        // wall-mounted pieces off the floor; a real art pass is a future refinement.
        private static float GetPlacementHeight(string unityName)
        {
            if (unityName.Contains("Chandelier")) return MansionSpec.Floor1CeilingY - 0.5f;
            if (unityName.Contains("Painting") || unityName.Contains("MirrorOrnate") || unityName.Contains("Sconce")) return 1.5f;
            return 0.1f;
        }

        // ---------------------------------------------------------------
        private static void WriteMissingLog(List<string> missing)
        {
            if (missing.Count == 0) return;
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(MissingAssetsLogPath, missing.Select(n =>
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Foyer/(Static|Throwable)/{n}.* | Part 4.2 Foyer prop - source file not found, no numbered variant either"));
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
                "=== Stage 6 - Foyer Prop Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Foyer_StaircaseDouble_01 and Foyer_Railing_01 (Part 4.2) are NOT processed",
                "   this stage. Swapping them for Stage 1's tested procedural blockout means",
                "   matching a real mesh's pivot/orientation to the exact Law-0.4-verified",
                "   footprint - not safely confirmable without visually inspecting the asset,",
                "   which headless batch mode cannot do. Doing it blind risks silently",
                "   breaking the already-verified wall-closure/traversal test. Deferred to a",
                "   pass where the result can be visually checked.",
                "",
                "2. Foyer_StaircaseDouble_01's own Part 4 entry (6.00m) also predates Part",
                "   3.3's revision (footprint now 18x12m after the x3 mansion resize) and",
                "   was evidently not updated when the owner revised Part 3 - another reason",
                "   to defer rather than scale-correct against a now-stale number.",
                "",
                "3. Foyer_MainDoor_01's source file was found at",
                "   Assets/_Project/Models/Architecture/arch_maindoor_01.glb, not under",
                "   Foyer/Static/ as its Unity name would suggest - used anyway since it is",
                "   clearly the intended asset (only one door-shaped model in the project).",
                "   Only its static model is corrected/placed here; the hinge/open-close",
                "   behavior noted in its Part 4.2 row (\"مفصلي - الجزء 8\") is Stage 11's job.",
                "",
                "4. Dynamic props (Fallable/CarryLight/Pushable/FireSourceFallable) get",
                "   convex MeshColliders (PhysX requires a convex hull for any collider that",
                "   can end up on a non-kinematic Rigidbody) - an approximation of the real",
                "   mesh silhouette, not pixel-perfect collision, which is normal/expected",
                "   for a first import pass. Purely static props keep exact non-convex",
                "   MeshColliders.",
                "",
                "5. Part 4.1's \"ثابت قابل للسقوط\" tip threshold reuses Part 7.3's already-",
                "   established 5 m/s \"hard impact\" definition (see FallableProp.cs) rather",
                "   than inventing a second, separate number - the document gives no value",
                "   of its own for this.",
                "",
                "6. Scene placement uses a simple non-overlapping grid scatter across the",
                "   Foyer's open floor (2m spacing, 1.5m wall margin, staircase footprint",
                "   excluded) since the document gives no interior-design coordinates for",
                "   furniture anywhere - only structural ones. Chandelier is hung near the",
                "   ceiling and Painting/MirrorOrnate/Sconce are placed at a fixed 1.5m wall",
                "   height by a simple name-keyword rule; everything else sits on the floor.",
                "   A real art-direction pass (matching the sofa/coffee-table/rug cluster to",
                "   Part 3.3's reserved center-of-foyer seating area specifically) is a",
                "   future refinement, not attempted here.",
                "",
                $"Missing this stage ({missing.Count}): {string.Join(", ", missing)}",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
