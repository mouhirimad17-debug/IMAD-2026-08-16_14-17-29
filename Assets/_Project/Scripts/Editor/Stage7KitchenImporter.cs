using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Entry = PrankMansion.Blockout.Stage7KitchenPropSpec.Entry;
using PropClass = PrankMansion.Blockout.Stage7KitchenPropSpec.PropClass;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 7: imports and scale-corrects the Kitchen's real prop models (Part 4.3)
    /// following Law 0.3's exact protocol, same pipeline as Stage 6's Foyer import.
    ///
    /// New this stage vs Stage 6:
    /// - Watermelon is the project's first real Heavy-carryable prop (CarryHeavy).
    /// - Three items carry an extra one-shot impact reaction on top of their normal
    ///   Light-carryable classification (FlourBag explodes, Plate shatters, MilkCarton
    ///   leaks) via the new ImpactReactionProp component.
    /// - Several items have more real source-file variants than Part 4.3's own "_01
    ///   to _02/_03" range mentions (or fewer) - each found file becomes its own
    ///   correctly-numbered prefab rather than forcing an exact count.
    /// - Kitchen_FullCombo_01's internal sub-doors are deliberately left untouched
    ///   (Part 4.3: "تُستخدم بأسمائها الأصلية ... دون إعادة تسمية") - Law 0.3 step 6
    ///   (root-only scaling) already guarantees this by construction.
    /// - Kitchen_Door_01's hinge behavior (like Foyer_MainDoor_01's) is Stage 11's
    ///   job; only the static model is corrected/placed here.
    /// </summary>
    public static class Stage7KitchenImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage7_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage7_StaticVerification_Report.txt";
        private const float DimensionTolerancePct = 0.10f;

        private static Entry[] Table => Stage7KitchenPropSpec.Table;
        private static string PrefabDir => Stage7KitchenPropSpec.PrefabDir;
        private static string PropsRootName => Stage7KitchenPropSpec.KitchenPropsRootName;

        [MenuItem("PrankMansion/Stage 7 - Import & Correct Kitchen Props")]
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

            PlaceIntoKitchen(results);
            WriteMissingLog(missing);
            WriteWarningsLog(warnings);
            WriteDecisionsLog(missing, warnings);

            Debug.Log($"[Stage7KitchenImporter] Done. Corrected {results.Count(r => r.prefab != null)}, missing {missing.Count}.");
        }

        // Fix-up entry point: re-runs ONLY PlaceIntoKitchen against the prefabs
        // that already exist on disk (no re-import) - see Stage6's counterpart.
        [MenuItem("PrankMansion/Stage 7 - Regenerate Kitchen Placement Only")]
        public static void RegeneratePlacementOnly()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            foreach (var entry in Table)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + entry.unityName + ".prefab");
                results.Add((entry, prefab, entry.expectedMaxDim, 1f));
            }
            PlaceIntoKitchen(results);
            Debug.Log($"[Stage7KitchenImporter] Re-placed {results.Count(r => r.prefab != null)} existing Kitchen prefabs.");
        }

        [MenuItem("PrankMansion/Stage 7 - Import And Run Kitchen Props Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage7_KitchenPropsTestRunner");
            testGo.AddComponent<Stage7KitchenPropsTest>();

            Debug.Log("[Stage7KitchenImporter] Entering Play Mode to run dynamic prop verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 7 - Static Verification (Part 4.3 + Law 0.3 + Law 0.2) ===", "" };
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
                    PropClass.Pushable => prefab.GetComponent<PushableProp>() != null,
                    PropClass.PureStatic => prefab.GetComponent<Rigidbody>() == null,
                    _ => false
                };
                Check($"{entry.unityName}: correct Part 4.1 classification component(s)", classOk, $"class={entry.cls}");

                if (entry.reaction.HasValue)
                {
                    var reactionComp = prefab.GetComponent<ImpactReactionProp>();
                    Check($"{entry.unityName}: has the Part 4.3 impact-reaction note ({entry.reaction})",
                        reactionComp != null && reactionComp.reactionType == entry.reaction.Value,
                        $"found={(reactionComp != null ? reactionComp.reactionType.ToString() : "none")}");
                }
            }

            var placedRoot = GameObject.Find(PropsRootName);
            int expectedPlaced = Table.Count(e => !missingSet.Contains(e.unityName));
            Check("Kitchen props are placed in the actual mansion scene", placedRoot != null && placedRoot.transform.childCount == expectedPlaced,
                $"childCount={(placedRoot != null ? placedRoot.transform.childCount : -1)} expected={expectedPlaced}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage7KitchenImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage7KitchenImporter] Static verification passed: {passed}/{total}.");
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
            AddClassificationComponents(instance, entry);
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
                case PropClass.Pushable:
                    root.AddComponent<PushableProp>();
                    break;
                case PropClass.PureStatic:
                    break;
            }

            if (entry.reaction.HasValue)
            {
                var reaction = root.AddComponent<ImpactReactionProp>();
                reaction.reactionType = entry.reaction.Value;
            }
        }

        // ---------------------------------------------------------------
        private static void PlaceIntoKitchen(List<(Entry entry, GameObject prefab, float measuredDim, float factor)> results)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existingRoot = GameObject.Find(PropsRootName);
            if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot);
            var root = new GameObject(PropsRootName).transform;

            var kitchen = MansionSpec.Kitchen;
            const float margin = 1.5f;
            const float spacing = 2f;

            float xMin = kitchen.x + margin, xMax = kitchen.xMax - margin;
            float zMin = kitchen.z + margin, zMax = kitchen.zMax - margin;

            int placeCount = results.Count(r => r.prefab != null);
            var slots = FurnitureGridPlacement.BuildSlots(xMin, xMax, zMin, zMax, placeCount, spacing);

            int i = 0;
            foreach (var (entry, prefab, _, _) in results)
            {
                if (prefab == null || i >= slots.Count) continue;
                Vector2 slot = slots[i++];

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(slot.x, 0.1f, slot.y);
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
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Kitchen/(Static|Throwable)/{n}.* | Part 4.3 Kitchen prop - source file not found, no numbered variant either"));
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
                "=== Stage 7 - Kitchen Prop Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Watermelon is the project's first real Heavy-carryable prop - tagged",
                "   CarryableObject.WeightClass.Heavy, which already wires it into the full",
                "   Part 7.1/7.2 solo-carry speed penalty + wind-timer system built in",
                "   Stage 3, with no new carry code needed.",
                "",
                "2. FlourBag/Plate/MilkCarton get the new ImpactReactionProp component on",
                "   top of their normal Light-carryable classification (Explode/Shatter/",
                "   Leak respectively) - Part 4.3 describes these as an extra one-shot",
                "   visual note on an otherwise ordinary carryable item, not a separate",
                "   physical class. Explode/Shatter reuse Part 7.3's 5 m/s \"قوي\" impact",
                "   definition; Leak has no such qualifier in the document (\"عند السقوط\"",
                "   only) so it fires on any landing. No FlourExplosion/SplashPour VFX",
                "   assets exist yet (both folders are empty) - simple procedural particle",
                "   bursts stand in, per Law 0.2.",
                "",
                "3. Several items have a different number of real source-file variants than",
                "   Part 4.3's own range mentions suggest: Apple/Orange/Tomato/Plate each",
                "   have only one file (not the two/three implied), while Banana/Egg/Knife",
                "   each have two despite the document listing them as single items. Each",
                "   actually-found file becomes its own correctly-numbered prefab rather",
                "   than forcing a specific count - Law 0.2's variant-set handling is about",
                "   using what exists, not manufacturing placeholders for a hinted-at range",
                "   when the base item itself was already found.",
                "",
                "4. Kitchen_FullCombo_01's internal sub-door hierarchy is left completely",
                "   untouched (Part 4.3: \"تُستخدم بأسمائها الأصلية ... دون إعادة تسمية\") -",
                "   Law 0.3 step 6 (scale correction applies to the root node only) already",
                "   guarantees this by construction; no special-case code was needed.",
                "",
                "5. Kitchen_Door_01's hinge/open-close behavior (\"مفصلي داخلي\") is Stage 11's",
                "   job, same as Foyer_MainDoor_01 in Stage 6 - only the static model is",
                "   corrected/placed here.",
                "",
                "6. Scene placement uses the same undirected grid-scatter approach as Stage",
                "   6 (no interior-design coordinates are given anywhere in the document),",
                "   tighter spacing (1m) since kitchen items are mostly small countertop",
                "   props rather than room-scale furniture.",
                "",
                $"Missing this stage ({missing.Count}): {string.Join(", ", missing)}",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
