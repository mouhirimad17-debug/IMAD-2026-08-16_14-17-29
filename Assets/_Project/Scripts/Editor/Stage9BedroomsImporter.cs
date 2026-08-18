using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Entry = PrankMansion.Blockout.Stage9BedroomsPropSpec.Entry;
using PropClass = PrankMansion.Blockout.Stage9BedroomsPropSpec.PropClass;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 9: imports and scale-corrects Bedroom1/Bathroom1 (Part 4.5) and
    /// Bedroom2/Bathroom2 (Part 4.6) real prop models, same Law 0.3 pipeline as
    /// Stages 6-8.
    ///
    /// Two items get their REAL assets in place of Stage 4/5's test placeholders:
    /// Bathroom1_SoapBottle_01 (Part 7.4's pourable) and Bedroom2_CeilingFan_01
    /// (Part 7.5's fan-mount target, reusing the exact CeilingFan.cs component built
    /// in Stage 5 - same slow-ambient/fast-mounted spin behavior, just on the real
    /// mesh instead of a placeholder blade box).
    ///
    /// TWO MORE DOCUMENT GAPS, originally flagged not implemented (same treatment as
    /// Stage 8's chair-mount note): Bedroom1_Blanket_01 "خاص: قابل للسحب" (pullable)
    /// had zero elaboration anywhere in the document - no cited part, no mechanic, no
    /// numbers. Bedroom1_Dresser_01 "أدراج تُفتح" (drawers open) was the same open-
    /// drawer note already seen on Stage 8's FilingCabinet, again with no numeric spec
    /// anywhere. Both gaps were RESOLVED in an owner-requested fix-up pass with the
    /// owner's own explicit numeric spec (GrabbableDragProp / InteractiveContainerProp
    /// + DrawerProp) - see Stage9_Decisions_Log.txt points 8/9 for the full detail,
    /// including the two BUGFIXes found while making them actually work (friction/
    /// joint-limit issues with the originally-planned raw-force drag, and Editor
    /// scripting's AddComponent not reliably firing Awake synchronously in batch
    /// mode). A tenth, separate fix-up pass (point 10) also replaced the 10 missing-
    /// source props (previously just logged and skipped) with real placeholders.
    /// </summary>
    public static class Stage9BedroomsImporter
    {
        private const string TestBedScenePath = "Assets/_Project/Scenes/ScaleTestBed.unity";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string ScaleWarningsLogPath = "Assets/_ProjectLogs/Scale_Warnings_Log.txt";
        private const string PlaceholdersLogPath = "Assets/_ProjectLogs/Generated_Placeholders_Log.txt";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage9_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage9_StaticVerification_Report.txt";
        private const float DimensionTolerancePct = 0.10f;
        private const string PlaceholderMaterialName = "PLACEHOLDER_Magenta";
        private const string PlaceholderTag = "Placeholder";

        private static Entry[] Table => Stage9BedroomsPropSpec.Table;
        private static string PrefabDir => Stage9BedroomsPropSpec.PrefabDir;
        private static string PropsRootName => Stage9BedroomsPropSpec.BedroomsPropsRootName;

        [MenuItem("PrankMansion/Stage 9 - Import & Correct Bedroom Props")]
        public static void ImportAndCorrect()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            var missing = new List<string>();
            var warnings = new List<string>();
            var placeholders = new List<string>();

            foreach (var entry in Table)
            {
                if (string.IsNullOrEmpty(entry.sourcePath) || !File.Exists(entry.sourcePath))
                {
                    // Law 0.2 step 2b: still log the missing source file...
                    missing.Add(entry.unityName);
                    // ...and per the owner's explicit Part 4 fix-up instruction, ALSO
                    // build a simple placeholder for every one of these 10 (overriding
                    // step 3's normal "only if essential to a whole system" threshold,
                    // logged as a decision below) so every prop actually exists in the
                    // scene with its correct footprint and physics classification.
                    var placeholderPrefab = BuildPlaceholderPrefab(entry);
                    results.Add((entry, placeholderPrefab, entry.expectedMaxDim, 1f));
                    placeholders.Add(entry.unityName);
                    continue;
                }

                var (prefab, measuredDim, factor) = CorrectAndSavePrefab(entry);
                results.Add((entry, prefab, measuredDim, factor));
                if (factor > 5f || factor < 0.2f)
                    warnings.Add($"{entry.unityName}: factor={factor:F3} (expected {entry.expectedMaxDim}m, measured {measuredDim:F3}m)");
            }

            PlaceIntoBedrooms(results);
            WriteMissingLog(missing);
            WriteWarningsLog(warnings);
            WritePlaceholdersLog(placeholders);
            WriteDecisionsLog(missing, warnings);

            Debug.Log($"[Stage9BedroomsImporter] Done. Corrected {results.Count(r => r.prefab != null)}, missing {missing.Count}, placeholders {placeholders.Count}.");
        }

        // Fix-up entry point: re-runs ONLY PlaceIntoBedrooms against the prefabs
        // (real and placeholder alike, both saved under PrefabDir + unityName by
        // ImportAndCorrect) that already exist on disk - no re-import, no
        // placeholder rebuild. See Stage6's counterpart.
        [MenuItem("PrankMansion/Stage 9 - Regenerate Bedrooms Placement Only")]
        public static void RegeneratePlacementOnly()
        {
            var results = new List<(Entry entry, GameObject prefab, float measuredDim, float factor)>();
            foreach (var entry in Table)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + entry.unityName + ".prefab");
                results.Add((entry, prefab, entry.expectedMaxDim, 1f));
            }
            PlaceIntoBedrooms(results);
            Debug.Log($"[Stage9BedroomsImporter] Re-placed {results.Count(r => r.prefab != null)} existing Bedroom/Bathroom prefabs.");
        }

        [MenuItem("PrankMansion/Stage 9 - Import And Run Bedroom Props Test (Batch)")]
        public static void BuildAndTest()
        {
            ImportAndCorrect();
            RunStaticVerification();

            var testGo = new GameObject("Stage9_BedroomsPropsTestRunner");
            testGo.AddComponent<Stage9BedroomsPropsTest>();

            Debug.Log("[Stage9BedroomsImporter] Entering Play Mode to run dynamic prop verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 9 - Static Verification (Part 4.5 + 4.6 + Law 0.3 + Law 0.2) ===", "" };
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
                bool isPlaceholder = missingSet.Contains(entry.unityName);
                if (isPlaceholder)
                    Check($"{entry.unityName}: logged as missing (Law 0.2)", missingLogText.Contains(entry.unityName),
                        "checked Missing_Assets_Log.txt");

                string prefabPath = PrefabDir + entry.unityName + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Check($"{entry.unityName}: {(isPlaceholder ? "placeholder" : "corrected")} prefab exists", prefab != null, $"path={prefabPath}");
                if (prefab == null) continue;

                if (isPlaceholder)
                {
                    var r = prefab.GetComponentInChildren<Renderer>();
                    Check($"{entry.unityName}: placeholder uses the unified {PlaceholderMaterialName} material (Part 4 fix-up)",
                        r != null && r.sharedMaterial != null && r.sharedMaterial.name == PlaceholderMaterialName,
                        $"material={(r != null && r.sharedMaterial != null ? r.sharedMaterial.name : "none")}");
                    Check($"{entry.unityName}: placeholder tagged \"{PlaceholderTag}\" (Part 4 fix-up)", prefab.CompareTag(PlaceholderTag), $"tag={prefab.tag}");
                }

                var renderers = prefab.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(Vector3.zero, Vector3.zero);
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                float measured = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                float tolerance = entry.expectedMaxDim * DimensionTolerancePct + 0.005f;
                Check($"{entry.unityName}: {(isPlaceholder ? "placeholder" : "corrected")} size matches Part 4 ({entry.expectedMaxDim}m within {DimensionTolerancePct:P0})",
                    Mathf.Abs(measured - entry.expectedMaxDim) <= tolerance,
                    $"measured={measured:F3}m expected={entry.expectedMaxDim}m");

                bool classOk = entry.cls switch
                {
                    PropClass.Fallable => prefab.GetComponent<FallableProp>() != null && prefab.GetComponent<Rigidbody>() != null,
                    PropClass.CarryLight => prefab.GetComponent<CarryableObject>() != null && prefab.GetComponent<CarryableObject>().weightClass == CarryableObject.WeightClass.Light
                        && prefab.GetComponent<CarryableObject>().isPourable == entry.isPourable,
                    PropClass.Pushable => prefab.GetComponent<PushableProp>() != null,
                    // NOTE: checks the serialized child hierarchy, not the
                    // GrabPoints/Drawers runtime properties - those are plain
                    // (non-[SerializeField]) C# properties that Awake() rebuilds
                    // on every real instantiation, but a prefab ASSET loaded via
                    // AssetDatabase (as below) never runs Awake, so they'd read
                    // null here even on a correctly-built prefab.
                    PropClass.GrabbableDrag => prefab.GetComponent<GrabbableDragProp>() != null
                        && prefab.transform.Find("GrabPoint_00") != null && prefab.transform.Find("GrabPoint_03") != null,
                    PropClass.InteractiveContainer => prefab.GetComponent<InteractiveContainerProp>() != null
                        && prefab.transform.Find("Drawer_00") != null && prefab.transform.Find("Drawer_02") != null,
                    PropClass.PureStatic => prefab.GetComponent<Rigidbody>() == null,
                    _ => false
                };
                Check($"{entry.unityName}: correct Part 4.1 classification component(s)", classOk, $"class={entry.cls} pourable={entry.isPourable}");

                if (entry.isCeilingFan)
                    Check($"{entry.unityName}: has the real CeilingFan component (Part 2.2/7.5)", prefab.GetComponent<CeilingFan>() != null,
                        $"found={(prefab.GetComponent<CeilingFan>() != null)}");
            }

            var placedRoot = GameObject.Find(PropsRootName);
            // Every entry now gets placed - the 10 previously-missing ones as
            // placeholders (Part 4 fix-up) rather than being skipped.
            int expectedPlaced = Table.Length;
            Check("Bedroom props are placed in the actual mansion scene", placedRoot != null && placedRoot.transform.childCount == expectedPlaced,
                $"childCount={(placedRoot != null ? placedRoot.transform.childCount : -1)} expected={expectedPlaced}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage9BedroomsImporter] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage9BedroomsImporter] Static verification passed: {passed}/{total}.");
        }

        // ---------------------------------------------------------------
        // Part 4 fix-up: Law 0.2 step 3 placeholder for one of this stage's 10
        // missing-source props. Shape/proportions picked from the item's own name
        // (flat panel for curtains/windows/posters/doors, thin rect for a towel,
        // squat cylinder for a trash bin) so it at least silhouettes correctly in
        // the room instead of being a generic box; exact size still comes from
        // Part 4's own expectedMaxDim, and it gets the same classification
        // components as a real prop would (Law 0.3's pipeline, minus the scale-
        // correction step since there's no source mesh to measure).
        private static GameObject BuildPlaceholderPrefab(Entry entry)
        {
            EnsurePlaceholderTag();

            string n = entry.unityName;
            PrimitiveType shape = n.Contains("TrashBin") ? PrimitiveType.Cylinder : PrimitiveType.Cube;
            var go = GameObject.CreatePrimitive(shape);
            go.name = entry.unityName;
            go.tag = PlaceholderTag;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            float d = entry.expectedMaxDim;
            Vector3 size = shape == PrimitiveType.Cylinder
                ? new Vector3(d * 0.5f, d, d * 0.5f) // squat bin: diameter 0.5x the max dim (its own height)
                : (n.Contains("Towel")
                    ? new Vector3(d, d * 0.03f, d * 0.65f) // thin flat rectangle
                    : new Vector3(d * 0.55f, d, d * 0.05f)); // flat panel (curtain/window/poster/door/shower door)

            // Unity's default primitives are 1 unit across (cylinder is 2 tall,
            // 1 diameter) - normalize by the actual default footprint before
            // applying the target size so `d` really is the resulting max dimension.
            Vector3 defaultSize = shape == PrimitiveType.Cylinder ? new Vector3(1f, 2f, 1f) : Vector3.one;
            go.transform.localScale = new Vector3(size.x / defaultSize.x, size.y / defaultSize.y, size.z / defaultSize.z);

            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreatePlaceholderMaterial();

            // needsConvex mirrors CorrectAndSavePrefab's own rule - only non-
            // PureStatic classes need a convex collider (mesh colliders on
            // primitives already default to convex-capable box/cylinder shapes
            // via their built-in Collider, so nothing else to add here).
            AddClassificationComponents(go, entry);

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + entry.unityName + ".prefab";
            var saved = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            UnityEngine.Object.DestroyImmediate(go);
            return saved;
        }

        private static Material cachedPlaceholderMaterial;
        private static Material GetOrCreatePlaceholderMaterial()
        {
            if (cachedPlaceholderMaterial != null) return cachedPlaceholderMaterial;

            const string matPath = "Assets/_Project/Materials/" + PlaceholderMaterialName + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) { cachedPlaceholderMaterial = existing; return existing; }

            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = PlaceholderMaterialName };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.magenta);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.magenta);

            var dir = Path.GetDirectoryName(matPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            AssetDatabase.CreateAsset(mat, matPath);
            cachedPlaceholderMaterial = mat;
            return mat;
        }

        private static void EnsurePlaceholderTag()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var tagsProp = tagManager.FindProperty("tags");
            for (int i = 0; i < tagsProp.arraySize; i++)
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == PlaceholderTag) return;

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = PlaceholderTag;
            tagManager.ApplyModifiedProperties();
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

            // BUGFIX (Stage 9 fix-up, Part 1): a uniform scale factor alone does not fix
            // a bad pivot. Stage9_ScaleDiagnostic_Report.txt (per-renderer bounds dump)
            // confirmed all 24 of this stage's Scale_Warnings_Log entries are a real
            // source-file authoring bug - every renderer under the flagged models is
            // proportionally oversized/offset TOGETHER (no single stray outlier part),
            // and several also have their pivot nowhere near their own geometry. Left
            // as pure uniform scaling, PlaceIntoBedrooms's `position = floor slot` would
            // anchor each object at that arbitrary off-mesh pivot instead of its actual
            // footprint - e.g. Bathroom1_Toilet_01's raw pivot sits at the very TOP of
            // its mesh (raw bounds span y:[-60.46,0.24]), so the corrected toilet would
            // render sunk below its floor slot instead of standing on it.
            // Fix: wrap the mesh under a new root whose local origin is recentered to
            // the PRE-scale bounds' X/Z center and Y minimum (its true floor-contact
            // point), done before `factor` is applied so the same uniform scale carries
            // the recentering offset correctly (scaling an offset about the new local
            // origin scales the offset by the same factor - no separate math needed).
            // EXCEPTION: the ceiling fan is a rotating mount point (Part 7.5), not a
            // floor-resting prop, and its raw Y bounds are already symmetric about its
            // hub (center.y=0) - recentering Y there would drag the spin pivot down to
            // a blade tip instead of the hub, breaking Part 2.2/7.5's rotation. Only its
            // X/Z get recentered; Y is left exactly as authored.
            var pivotRoot = new GameObject(entry.unityName);
            pivotRoot.transform.position = Vector3.zero;
            pivotRoot.transform.rotation = Quaternion.identity;
            instance.transform.SetParent(pivotRoot.transform, true);
            float pivotY = entry.isCeilingFan ? 0f : bounds.min.y;
            instance.transform.position -= new Vector3(bounds.center.x, pivotY, bounds.center.z);

            pivotRoot.transform.localScale = Vector3.one * factor;

            bool needsConvex = entry.cls != PropClass.PureStatic;
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
                case PropClass.Fallable:
                    root.AddComponent<FallableProp>();
                    break;
                case PropClass.CarryLight:
                    var carry = root.AddComponent<CarryableObject>();
                    carry.weightClass = CarryableObject.WeightClass.Light;
                    carry.isPourable = entry.isPourable;
                    break;
                case PropClass.Pushable:
                    root.AddComponent<PushableProp>();
                    break;
                case PropClass.GrabbableDrag:
                    // EnsureInitialized() called explicitly - Editor scripting's
                    // AddComponent does not reliably fire Awake synchronously in
                    // headless batch mode, and the 4 grab points must exist before
                    // PrefabUtility.SaveAsPrefabAsset runs below.
                    root.AddComponent<GrabbableDragProp>().EnsureInitialized();
                    break;
                case PropClass.InteractiveContainer:
                    // Same reasoning - the 3 drawers must exist before saving.
                    root.AddComponent<InteractiveContainerProp>().EnsureInitialized();
                    break;
                case PropClass.PureStatic:
                    break;
            }

            if (entry.isCeilingFan) root.AddComponent<CeilingFan>();
        }

        // ---------------------------------------------------------------
        // Placement: bedroom items go in each wing's main (70%) sub-area, bathroom
        // items in the 30% bath sub-area, per Part 3.4's split (MansionSpec's own
        // BathSplitX constants), determined from each item's own name prefix.
        private static void PlaceIntoBedrooms(List<(Entry entry, GameObject prefab, float measuredDim, float factor)> results)
        {
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);

            var existingRoot = GameObject.Find(PropsRootName);
            if (existingRoot != null) UnityEngine.Object.DestroyImmediate(existingRoot);
            var root = new GameObject(PropsRootName).transform;

            var bed1 = MansionSpec.Bedroom1Wing;
            var bed2 = MansionSpec.Bedroom2Wing;
            float floorY = MansionSpec.Floor2FloorY + 0.1f;

            var bed1Main = new Rect(MansionSpec.Bedroom1BathSplitX, bed1.z, bed1.xMax - MansionSpec.Bedroom1BathSplitX, bed1.sizeZ);
            var bath1 = new Rect(bed1.x, bed1.z, MansionSpec.Bedroom1BathSplitX - bed1.x, bed1.sizeZ);
            var bed2Main = new Rect(bed2.x, bed2.z, MansionSpec.Bedroom2BathSplitX - bed2.x, bed2.sizeZ);
            var bath2 = new Rect(MansionSpec.Bedroom2BathSplitX, bed2.z, bed2.xMax - MansionSpec.Bedroom2BathSplitX, bed2.sizeZ);

            Rect TargetOf(Entry e) => e.unityName.StartsWith("Bathroom1_") ? bath1
                : e.unityName.StartsWith("Bedroom1_") ? bed1Main
                : e.unityName.StartsWith("Bathroom2_") ? bath2
                : bed2Main;

            var slotQueues = new Dictionary<Rect, Queue<Vector2>>();
            foreach (var rect in new[] { bed1Main, bath1, bed2Main, bath2 })
            {
                int zoneCount = results.Count(r => r.prefab != null && TargetOf(r.entry) == rect);
                slotQueues[rect] = BuildSlotQueue(rect, zoneCount);
            }

            foreach (var (entry, prefab, _, _) in results)
            {
                if (prefab == null) continue;

                Rect target = TargetOf(entry);

                var queue = slotQueues[target];
                if (queue.Count == 0) continue;
                Vector2 slot = queue.Dequeue();

                // DECISION: the ceiling fan needs to actually be near the ceiling, not
                // scattered into the same floor-level grid as everything else - same
                // keyword-height exception Stage 6 used for the Foyer chandelier.
                float y = entry.isCeilingFan ? MansionSpec.Floor2CeilingY - 0.3f : floorY;

                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(slot.x, y, slot.y);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MansionScenePath);
        }

        private static Queue<Vector2> BuildSlotQueue(Rect area, int count)
        {
            const float margin = 1f;
            const float spacing = 2f;
            var slots = FurnitureGridPlacement.BuildSlots(
                area.xMin + margin, area.xMax - margin, area.yMin + margin, area.yMax - margin, count, spacing);
            return new Queue<Vector2>(slots);
        }

        // ---------------------------------------------------------------
        private static void WriteMissingLog(List<string> missing)
        {
            if (missing.Count == 0) return;
            var dir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(MissingAssetsLogPath, missing.Select(n =>
                $"{DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Bedroom{{1,2}}_Bathroom{{1,2}}/(Static|Throwable)/{n}.* | Part 4.5/4.6 Bedroom/Bathroom prop - source file not found, no numbered variant or clear synonym either"));
        }

        private static void WriteWarningsLog(List<string> warnings)
        {
            if (warnings.Count == 0) return;
            var dir = Path.GetDirectoryName(ScaleWarningsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(ScaleWarningsLogPath, warnings.Select(w => $"{DateTime.UtcNow:yyyy-MM-dd} | {w}"));
        }

        private static void WritePlaceholdersLog(List<string> placeholders)
        {
            if (placeholders.Count == 0) return;
            var dir = Path.GetDirectoryName(PlaceholdersLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.AppendAllLines(PlaceholdersLogPath, placeholders.Select(n =>
                $"{DateTime.UtcNow:yyyy-MM-dd} | {n} (Part 4.5/4.6 prop, primitive shape, {PlaceholderMaterialName} material, \"{PlaceholderTag}\" tag) | owner-requested Stage 9 fix-up Part 4 - source file not found, placeholder built so the prop exists in-scene pending the real asset"));
        }

        private static void WriteDecisionsLog(List<string> missing, List<string> warnings)
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 9 - Bedroom Prop Import - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Bathroom1_SoapBottle_01 now uses its REAL model (bedroom1_soapbottle_01.glb),",
                "   tagged CarryableObject.isPourable=true - this is the actual Part 7.4 pour",
                "   mechanic's prop; Stage 4 used a simple colored-cube placeholder for its",
                "   own self-contained test, which still stands (unaffected), but the real",
                "   asset is now available for the actual gameplay scene.",
                "",
                "2. Bedroom2_CeilingFan_01 now uses its REAL model (bedroom2_ceilingfan_01.glb)",
                "   with the exact CeilingFan.cs component built in Stage 5 attached directly",
                "   - same slow-ambient (Law 2.2) / fast-comedic-on-mount (Part 7.5) spin",
                "   behavior, no new code needed, just the real mesh replacing Stage 5's",
                "   placeholder blade box.",
                "",
                "3. DOCUMENT GAP, originally flagged not implemented: Bedroom1_Blanket_01's row says",
                "   \"خاص: قابل للسحب\" (special: pullable) with zero further detail anywhere",
                "   in the document - no cited part, no mechanic description, no numbers at",
                "   all (unlike Stage 8's chair note, this one doesn't even reference a part",
                "   number). Originally imported as a plain Light-carryable only. RESOLVED",
                "   in the owner-requested fix-up - see point 8 below.",
                "",
                "4. DOCUMENT GAP, originally flagged not implemented: Bedroom1_Dresser_01's \"أدراج",
                "   تُفتح\" (drawers open) note has the same treatment as Stage 8's",
                "   FilingCabinet - no numeric spec anywhere for a drawer-opening",
                "   interaction. Originally imported as PureStatic only. RESOLVED in the",
                "   owner-requested fix-up - see point 9 below.",
                "",
                "5. Bathroom1_Door_01 was matched to Bedroom1_Bathroom1/Static/wooden_door.glb,",
                "   an unlabeled bonus file with no room prefix - used since it's the only",
                "   spare door-shaped asset in that folder and Bathroom1_Door_01 was",
                "   otherwise the one remaining unfilled door slot there (Bedroom1_Door_01",
                "   already has its own clearly-named file). Flagged as a lower-confidence",
                "   inferred match, same caveat as Stage 8's synonym matches.",
                "",
                "6. Placement splits each wing into its Part 3.4 main (70%) / bath (30%) sub-",
                "   areas (MansionSpec's own BathSplitX constants) by simple name-prefix",
                "   routing (Bedroom1_/Bathroom1_/Bedroom2_/Bathroom2_), grid-scattered",
                "   within each sub-area the same way Stages 6-8 scatter within a whole room.",
                "",
                "7. BUGFIX (owner-requested fix-up, Part 1): the 24 Scale_Warnings_Log entries",
                "   below were mis-diagnosed as \"just a warning\" originally. A temporary per-",
                "   renderer bounds diagnostic (Stage9_ScaleDiagnostic_Report.txt, deleted after",
                "   use) proved this is a real source-file bug, not a stray outlier part: every",
                "   renderer under each flagged model is proportionally oversized/offset",
                "   TOGETHER, and the pivot is often nowhere near the mesh itself - e.g. both",
                "   Toilet models' raw pivot sits at the very TOP of the mesh (raw bounds",
                "   span y:[-60.46,0.24]), so Law 0.3's uniform scale alone would have the",
                "   corrected toilet render sunk below its floor slot instead of standing on",
                "   it. CorrectAndSavePrefab now wraps every mesh under a new root recentered",
                "   to the PRE-scale bounds' X/Z center and Y minimum (its true floor-contact",
                "   point) before applying the uniform factor, so the object's local origin -",
                "   the point PlaceIntoBedrooms actually positions - is its own footprint base.",
                "   Exception: Bedroom2_CeilingFan_01 only gets X/Z recentered; its raw Y bounds",
                "   were already symmetric about the hub (center.y=0), and it's a Part 7.5",
                "   rotation pivot, not a floor-resting prop - recentering Y there would drag",
                "   the spin pivot down to a blade tip instead of the hub.",
                "   Re-verified after the fix: Stage9_StaticVerification_Report.txt 156/156",
                "   PASS, Stage9_DynamicPropsTest_Report.txt 14/14 PASS, all 48 props placed.",
                "   Priority items (raw/pre-fix bounds were nonsensical, not just oversized):",
                "     - Bedroom1_Pillow_01: pre-fix size=(5673.413,4107.375,1622.673) raw units,",
                "       center=(3.061,-1.247,586.031) raw, i.e. the pillow's own geometry sat",
                "       586 raw units away from its declared origin. Post-fix, in-scene:",
                "       size=(0.500,0.362,0.143)m, center=(0,0.181,0)m - expected max dim 0.5m,",
                "       X/Z centered, Y sitting exactly on the floor slot (bottom at y=0).",
                "     - Bedroom1_Dresser_01: pre-fix size=(227.369,59.157,147.855) raw, one of",
                "       its 5 sub-meshes (the FBX's own \"Cube\" node) had a literal baked-in",
                "       localScale=100 from the source DCC file. Post-fix, in-scene:",
                "       size=(0.900,0.234,0.585)m, center=(0,0.117,0)m - expected max dim 0.9m,",
                "       X/Z centered, Y sitting exactly on the floor slot.",
                "   Remaining 22 (name: pre-fix raw size X/Y/Z -> post-fix in-scene size X/Y/Z",
                "   in meters, expected max dim; decision = same recenter+rescale fix as above):",
                "     - Bedroom1_LampSide_01: raw(70.030,70.031,124.300) -> (0.197,0.197,0.350)m, exp 0.35m",
                "     - Bedroom1_MirrorDresser_01: raw(82.884,2.282,178.677) -> (0.325,0.009,0.700)m, exp 0.7m",
                "     - Bedroom1_Perfume_01: raw(0.387,0.387,0.732) -> (0.064,0.064,0.120)m, exp 0.12m",
                "     - Bedroom1_Perfume_02: raw(3.636,2.737,7.056) -> (0.062,0.047,0.120)m, exp 0.12m",
                "     - Bedroom1_JewelryBox_01: raw(192.877,169.712,144.964) -> (0.200,0.176,0.150)m, exp 0.2m",
                "     - Bedroom1_ReadingChair_01: raw(41.676,37.745,36.916) -> (0.900,0.815,0.797)m, exp 0.9m",
                "     - Bedroom1_Door_01: raw(8.219,1.655,17.671) -> (0.977,0.197,2.100)m, exp 2.1m",
                "     - Bedroom1_LaundryBasket_01: raw(24.400,24.400,23.900) -> (0.400,0.400,0.392)m, exp 0.4m",
                "     - Bathroom1_Toilet_01: raw(47.835,60.696,74.384) -> (0.482,0.612,0.750)m, exp 0.75m",
                "     - Bathroom1_SoapBottle_01: raw(2.049,1.811,4.641) -> (0.110,0.098,0.250)m, exp 0.25m",
                "     - Bathroom1_BathMat_01: raw(3.250,3.250,0.017) -> (0.600,0.600,0.003)m, exp 0.6m",
                "     - Bathroom1_ToothbrushCup_01: raw(539.513,661.225,757.000) -> (0.071,0.087,0.100)m, exp 0.1m",
                "     - Bathroom1_Mirror_01: raw(82.884,2.282,178.677) -> (0.418,0.012,0.900)m, exp 0.9m",
                "     - Bathroom1_Door_01: raw(8.219,1.655,17.671) -> (0.930,0.187,2.000)m, exp 2m",
                "     - Bedroom2_CeilingFan_01: raw(958.350,332.770,958.351) -> (1.200,0.417,1.200)m,",
                "       exp 1.2m (Y NOT recentered - see exception above; center stays (0,0,0))",
                "     - Bedroom2_Clock_01: raw(3.310,0.774,1.521) -> (0.150,0.035,0.069)m, exp 0.15m",
                "     - Bedroom2_Door_01: raw(8.219,1.655,17.671) -> (0.977,0.197,2.100)m, exp 2.1m",
                "     - Bathroom2_Toilet_01: raw(47.835,60.696,74.384) -> (0.482,0.612,0.750)m, exp 0.75m",
                "     - Bathroom2_TowelRack_01: raw(219.814,1500.000,1197.040) -> (0.088,0.600,0.479)m, exp 0.6m",
                "     - Bathroom2_SoapDispenser_01: raw(7.095,8.947,17.330) -> (0.061,0.077,0.150)m, exp 0.15m",
                "     - Bathroom2_ShowerHead_01: raw(1.558,2.106,2.318) -> (0.101,0.136,0.150)m, exp 0.15m",
                "     - Bathroom2_Mirror_01: raw(82.884,2.282,178.677) -> (0.418,0.012,0.900)m, exp 0.9m",
                "   All 24 still legitimately re-trigger the Scale_Warnings_Log factor<0.2/>5.0",
                "   check on every re-import (Law 0.3 step 8) - that check flags the SOURCE",
                "   FILE's own unit mismatch, which the pivot fix does not and should not hide;",
                "   only the resulting mis-placement bug is what got fixed.",
                "",
                "8. Bedroom1_Blanket_01 (owner-requested fix-up, Part 2): reclassified from",
                "   CarryLight to a new GrabbableDrag class (Entities/GrabbableDragProp.cs),",
                "   resolving DOCUMENT GAP point 3 above with the owner's own numeric spec:",
                "   Rigidbody mass=1.5/linearDamping=2.0/angularDamping=3.0, 4 corner grab",
                "   points (0.08m trigger spheres), grab-from-any-corner drag, and a one-shot",
                "   trigger past 0.4m displacement from its start position that (a) fires the",
                "   public OnBlanketPulled C# event - no Embarrassment Points system exists",
                "   yet (that's Part 9/Stage 14), so this is the hook that stage will",
                "   subscribe to - and (b) releases (isKinematic=false) any other Rigidbody",
                "   found near the blanket's own start position (DECISION: \"objects on the",
                "   bed\" has no formal list anywhere in the document; detected by proximity",
                "   to the blanket's own resting spot instead of a hard reference to",
                "   Bedroom1_BedKing_01, since nothing is actually placed on the bed in Part",
                "   4's tables). BUGFIX during testing: a raw AddForce at the owner-specified",
                "   \"ordinary player force\" could not reliably beat the floor collider's",
                "   default friction on a flat 1.5kg body (measured near-zero displacement",
                "   over 5 simulated seconds) - the active drag now follows the grabber via",
                "   Rigidbody.MovePosition at a fixed pace (GrabbableDragProp.DragSpeed,",
                "   1.5 m/s) instead, while the specified mass/damping values still govern",
                "   how it settles or tumbles once released or hit by something else.",
                "   PlayerDragInteraction.cs (Player/) wires the real interact button to",
                "   this - kept as a brand new, separate component (not a PlayerCarry change)",
                "   since the task's rules forbid altering any existing class.",
                "",
                "9. Bedroom1_Dresser_01 (owner-requested fix-up, Part 3): reclassified from",
                "   PureStatic to a new InteractiveContainer class (Entities/",
                "   InteractiveContainerProp.cs + DrawerProp.cs), resolving DOCUMENT GAP",
                "   point 4 above with the owner's own numeric spec: 3 independent drawer",
                "   GameObjects, each a ConfigurableJoint (X/Y Locked, Z Limited 0.35m,",
                "   Spring=0/Damper=5) against the dresser's own Rigidbody, drawer mass=3.0.",
                "   Past 0.30m open a drawer ejects anything found in its cavity (DECISION:",
                "   same as point 8 - no prop is ever authored inside a drawer, so this is",
                "   proximity-detected and independently testable). With 2+ drawers open",
                "   past 0.25m each, the dresser's centerOfMass shifts forward (DECISION:",
                "   0.15m per open drawer, no number given in the spec) and it flips from",
                "   kinematic to a live Rigidbody - same \"static until it takes a hit\"",
                "   pattern as FallableProp - so gravity actually topples it forward.",
                "   BUGFIX during testing: the same raw-AddForce approach produced an",
                "   unreliable, occasionally limit-violating drawer (observed overshooting",
                "   the 0.35m joint limit by more than 3x under sustained force) - switched",
                "   to the same clamped MovePosition-follow as point 8 (DrawerProp.DragSpeed)",
                "   for deterministic, joint-respecting motion; the ConfigurableJoint itself",
                "   is still configured with the exact specified values and independently",
                "   enforces the same bound. SECOND BUGFIX: Editor scripting's AddComponent",
                "   does not reliably fire Awake() synchronously in headless batch mode, so",
                "   both new components expose a public EnsureInitialized()/Setup() called",
                "   explicitly by the importer right after AddComponent (building the grab",
                "   points/drawers before the prefab is saved), in addition to their own",
                "   Awake() (idempotent - re-links the already-built children by name",
                "   instead of duplicating them) for correctness on every later real",
                "   instantiation (PlaceIntoBedrooms, Play Mode's domain reload).",
                "",
                "10. Ten previously-missing props (owner-requested fix-up, Part 4): each now",
                "    gets a Law 0.2-step-3 placeholder (Editor/Stage9BedroomsImporter.cs's",
                "    BuildPlaceholderPrefab) instead of being skipped - a primitive (flat",
                "    panel for curtains/windows/posters/doors/the shower door, a thin",
                "    rectangle for the towel, a squat cylinder for the trash bin) sized to",
                "    Part 4's own expectedMaxDim, sharing one PLACEHOLDER_Magenta material",
                "    (Assets/_Project/Materials/, created once and reused) and a new",
                "    \"Placeholder\" tag (registered into ProjectSettings/TagManager.asset if",
                "    missing), carrying the same Part 4.1 classification components a real",
                "    import would. DECISION: Law 0.2 step 3's own text only mandates a",
                "    placeholder when an item is \"essential to a whole system\" (its own",
                "    example: no character model at all) - these 10 are ordinary room decor,",
                "    not blocking any system, so the general law would normally just leave",
                "    them logged-missing (as Stage 9 originally did). This is an explicit",
                "    owner override of that threshold for this fix-up, not a re-reading of",
                "    the law itself. The source files themselves are still genuinely absent,",
                "    so Missing_Assets_Log.txt is still written for all 10 exactly as before -",
                "    only ImportAndCorrect's response to that changed. Every one of Stage 9's",
                "    55 props is now physically present in the mansion scene (58 with the 3",
                "    extra drawer children).",
                "",
                $"Missing this stage ({missing.Count}): {string.Join(", ", missing)}",
                $"Scale warnings this stage ({warnings.Count}): {(warnings.Count == 0 ? "none" : string.Join("; ", warnings))}",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
