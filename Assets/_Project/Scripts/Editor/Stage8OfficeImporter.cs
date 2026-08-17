using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Entry = PrankMansion.Blockout.Stage8OfficePropSpec.Entry;
using PropClass = PrankMansion.Blockout.Stage8OfficePropSpec.PropClass;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 8: imports and scale-corrects the Office's real prop models (Part 4.4)
    /// following Law 0.3's exact protocol, same pipeline as Stages 6/7.
    ///
    /// KNOWN DOCUMENT GAP (not implemented, flagged only): Office_ChairWheels_01's
    /// row says "خاص: قابل لتثبيت خصم مقيّد عليه (الجزء 7.5)" (special: a restrained
    /// victim can be mounted on it), citing Part 7.5. But Part 7.5's actual text
    /// (already fully built in Stage 5) only defines five things: rope-restrain,
    /// joint-carry, balcony-throw, fan-mount, and the 30s timeout-release - nothing
    /// about a wheeled chair anywhere. This is a genuine gap between the Office
    /// table's note and Part 7.5's own content, not an oversight in Stage 5's build.
    /// Per the document's own "no guessing" principle, no new finishing-move
    /// mechanic is invented here to fill it - the chair is imported and classified
    /// Pushable (its other stated property) and the gap is logged for the project
    /// owner to resolve by extending Part 7.5 if this mechanic is actually wanted.
    ///
    /// Also unlike Stages 6/7, many Office_*.glb files use different words than
    /// Part 4.4's Unity names (office_desk_01 for DeskExec, office_meetingchair_01
    /// for ChairGuest, etc.) and several table items have no plausible match at all
    /// (13 of 26) - see Stage8OfficePropSpec's own doc comment and this file's
    /// decisions log for the per-item reasoning.
    /// </summary>
    public static class Stage8OfficeImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage8_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage8_StaticVerification_Report.txt";
        private const float DimensionTolerancePct = 0.10f;

        private static Entry[] Table => Stage8OfficePropSpec.Table;
        private static string PrefabDir => Stage8OfficePropSpec.PrefabDir;
        private static string PropsRootName => Stage8OfficePropSpec.OfficePropsRootName;

        [MenuItem("PrankMansion/Stage 8 - Import & Correct Office Props")]
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

            PlaceIntoOffice(results);
            WriteMissingLog(missing);
            WriteWarningsLog(warnings);
            WriteDecisionsLog(missing, warnings);

            Debug.Log($"[Stage8OfficeImporter] Done. Corrected {results.Count(r => r.prefab != null)}, missing {missing.Count}.");
        }

        [MenuItem("PrankMansion/Stage 8 - Import And Run Office Props Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage8_OfficePropsTestRunner");
            testGo.AddComponent<Stage8OfficePropsTest>();

            Debug.Log("[Stage8OfficeImporter] Entering Play Mode to run dynamic prop verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 8 - Static Verification (Part 4.4 + Law 0.3 + Law 0.2) ===", "" };
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
                    PropClass.Fallable => prefab.GetComponent<FallableProp>() != null && prefab.GetComponent<Rigidbody>() != null,
                    PropClass.CarryLight => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Light,
                    PropClass.CarryHeavy => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Heavy,
                    PropClass.Pushable => prefab.GetComponent<PushableProp>() != null,
                    PropClass.PureStatic => prefab.GetComponent<Rigidbody>() == null,
                    _ => false
                };
                Check($"{entry.unityName}: correct Part 4.1 classification component(s)", classOk, $"class={entry.cls}");
            }

            var placedRoot = GameObject.Find(PropsRootName);
            int expectedPlaced = Table.Count(e => !missingSet.Contains(e.unityName));
            Check("Office props are placed in the actual mansion scene", placedRoot != null && placedRoot.transform.childCount == expectedPlaced,
                $"childCount={(placedRoot != null ? placedRoot.transform.childCount : -1)} expected={expectedPlaced}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage8OfficeImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage8OfficeImporter] Static verification passed: {passed}/{total}.");
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
            instance.transform.localScale = Vector3.one * factor;

            bool needsConvex = entry.cls != PropClass.PureStatic;
            AddCollidersIfMissing(instance, needsConvex);
            AddClassificationComponents(instance, entry.cls);
            instance.name = entry.unityName;

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + entry.unityName + ".prefab";
            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            UnityEngine.Object.DestroyImmediate(instance);

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

        private static void AddClassificationComponents(GameObject root, PropClass cls)
        {
            switch (cls)
            {
                case PropClass.Fallable:
                    root.AddComponent<FallableProp>();
                    break;
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
        }

        // ---------------------------------------------------------------
        // The Office wing is on floor 2 (MansionSpec.OfficeWing), unlike the Foyer/
        // Kitchen's ground-floor placement in Stages 6/7.
        private static void PlaceIntoOffice(List<(Entry entry, GameObject prefab, float measuredDim, float factor)> results)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existingRoot = GameObject.Find(PropsRootName);
            if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot);
            var root = new GameObject(PropsRootName).transform;

            var office = MansionSpec.OfficeWing;
            const float margin = 1.5f;
            const float spacing = 1f;

            float xMin = office.x + margin, xMax = office.xMax - margin;
            float zMin = office.z + margin, zMax = office.zMax - margin;

            var slots = new List<Vector2>();
            for (float z = zMin; z <= zMax; z += spacing)
            for (float x = xMin; x <= xMax; x += spacing)
                slots.Add(new Vector2(x, z));

            float floorY = MansionSpec.Floor2FloorY + 0.1f;
            int i = 0;
            foreach (var (entry, prefab, _, _) in results)
            {
                if (prefab == null || i >= slots.Count) continue;
                Vector2 slot = slots[i++];

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(slot.x, floorY, slot.y);
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
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Office/(Static|Throwable)/{n}.* | Part 4.4 Office prop - source file not found, no numbered variant or clear synonym either"));
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
                "=== Stage 8 - Office Prop Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. DOCUMENT GAP (flagged, not implemented): Office_ChairWheels_01's row says",
                "   \"خاص: قابل لتثبيت خصم مقيّد عليه (الجزء 7.5)\" citing Part 7.5, but Part",
                "   7.5's own text (fully built in Stage 5: rope-restrain, joint-carry,",
                "   balcony-throw, fan-mount, 30s timeout) never mentions a wheeled chair",
                "   anywhere. This is a genuine gap between this table row and Part 7.5's",
                "   actual content, not a missed spec in Stage 5. No new finishing-move",
                "   mechanic was invented to fill it, per the document's own no-guessing",
                "   principle - the chair is imported and classified Pushable only (its",
                "   other stated property). Recommend the project owner extend Part 7.5",
                "   with the missing numeric spec if this mechanic is actually wanted.",
                "",
                "2. Several source files use different words than Part 4.4's Unity names -",
                "   matched on the same single-plausible-candidate judgment as Stage 6's",
                "   sofa/champagne/MainDoor: office_desk_01->DeskExec, office_meetingchair_01",
                "   ->ChairGuest, Office_Bookschelf_01->BookshelfLarge (near-certain typo),",
                "   office_lamp_01->LampDesk, Office_clock_01->ClockWall,",
                "   office_wastebasketmetal_01->TrashBin, office_bulletinboard_01->",
                "   Whiteboard, office_worldmap_01->Painting, office_penholder_01->",
                "   DeskOrganizer. The last three are lower-confidence functional analogues",
                "   (a corkboard/map/pen-holder standing in for a whiteboard/painting/",
                "   organizer) rather than near-certain renames - flagged here in case a",
                "   more exact asset is added later.",
                "",
                "3. Office_CoffeeMug_01 was NOT matched to office_waterglass_01 despite both",
                "   being drinking vessels - a water glass and a coffee mug are visually and",
                "   functionally distinct enough that forcing the match seemed less honest",
                "   than logging CoffeeMug as missing. Office_Marker_01 was similarly NOT",
                "   matched to the three office_pen_*.glb files (a pen is not a whiteboard",
                "   marker) - those pens aren't in Part 4.4's table at all and are left",
                "   unprocessed as bonus assets.",
                "",
                "4. The Office models folder contains numerous other bonus assets with no",
                "   corresponding Part 4.4 table row at all (safe, shredder, leather sofa,",
                "   meeting table, desk fan, Newton's cradle, scissors, tape dispenser,",
                "   water pitcher/glass, cigar ashtray, humidor, calendar, card holder,",
                "   push pin, stamp, monitor, umbrella) - left unprocessed this stage since",
                "   Stage 8's scope is fulfilling Part 4.4's table, not importing every file",
                "   in the folder. Available for a future document revision if the owner",
                "   wants them formally added.",
                "",
                "5. Placement uses the same grid-scatter approach as Stages 6/7, on floor 2",
                "   within MansionSpec.OfficeWing at Floor2FloorY (unlike the Foyer/",
                "   Kitchen's ground-floor placement).",
                "",
                $"Missing this stage ({missing.Count}): {string.Join(", ", missing)}",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
