using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrankMansion.Blockout;
using PrankMansion.Entities;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 11: Part 8's interior door system, wired onto props already imported by
    /// Stages 6-10 rather than a fresh Part 4 import pass. Three sub-systems:
    ///
    /// Part 8.1 (main doors): DoorProp added directly onto every already-corrected
    /// *_Door_01 prefab that actually exists (Kitchen, Bathroom1, Bedroom1,
    /// Bedroom2, Bathroom2, plus Bathroom2_ShowerDoor_01) - re-saved in place, no
    /// re-import needed. Office_Door_01 and Gym_Door_01 have no prop to attach to
    /// (both logged missing back in Stages 8/10, still unresolved) - nothing to do
    /// for those two until a source file exists.
    ///
    /// Part 8.2 (kitchen sub-doors): Kitchen_FullCombo_01's hierarchy was dumped
    /// (Stage11_KitchenHierarchy_Report.txt, deleted after use) and is entirely
    /// generic CAD-export names (Box012, Group026, VIFS113, ...) EXCEPT the washer,
    /// a separately-modeled "MIELE WWV 980 WPS PASSION" unit with real English part
    /// names - its "_doors" node gets a real DoorProp (horizontal hinge axis, Part
    /// 8.2's circular-door case). The fridge/cabinet/oven doors could NOT be
    /// reliably identified this way (see point 2 in Stage11_Decisions_Log.txt) -
    /// flagged rather than guessed, since rotating the wrong generic node would
    /// visibly break the combo mesh.
    ///
    /// Part 8.3 (drawers): Office_FilingCabinet_01 never got a source file (Stage 8)
    /// and Part 4.4 classifies it Pushable, unlike Bedroom1_Dresser_01's PureStatic
    /// base - built as a Law 0.2 step 3 placeholder (a real prop is needed to prove
    /// this stage's own door/drawer system actually works end to end) with
    /// PushableProp on the body and 3 DrawerSlideProp children. Bedroom1_Dresser_01
    /// keeps its Stage 9 physical grab-and-drag system instead of this simpler
    /// button+slide one, per the owner's explicit choice when this stage started.
    /// </summary>
    public static class Stage11DoorSystemSetup
    {
        private const string PropsPrefabDir = "Assets/_Project/Prefabs/Props/";
        private const string MansionScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage11_Decisions_Log.txt";
        private const string StaticReportPath = "Assets/_ProjectLogs/Stage11_StaticVerification_Report.txt";
        private const string PlaceholderMaterialName = "PLACEHOLDER_Magenta";
        private const string PlaceholderTag = "Placeholder";

        private static readonly (string name, DoorProp.HingeAxis axis)[] MainDoors =
        {
            ("Kitchen_Door_01", DoorProp.HingeAxis.Vertical),
            ("Bathroom1_Door_01", DoorProp.HingeAxis.Vertical),
            ("Bedroom1_Door_01", DoorProp.HingeAxis.Vertical),
            ("Bedroom2_Door_01", DoorProp.HingeAxis.Vertical),
            ("Bathroom2_Door_01", DoorProp.HingeAxis.Vertical),
            ("Bathroom2_ShowerDoor_01", DoorProp.HingeAxis.Vertical),
        };

        // DECISION: Part 8.2 gives a range, not one number, for sub-door open
        // angle ("تسعين إلى مئة وعشر درجات") - 100 deg (the midpoint) is used for
        // the one sub-door that could be safely identified (the washer).
        private const float KitchenSubDoorAngle = 100f;

        [MenuItem("PrankMansion/Stage 11 - Build Interior Door System")]
        public static void BuildDoorSystem()
        {
            int mainDoorsWired = WireMainDoors();
            bool washerWired = WireKitchenWasherDoor();
            BuildFilingCabinetPlaceholder();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MansionScenePath);

            WriteDecisionsLog(mainDoorsWired, washerWired);
            Debug.Log($"[Stage11DoorSystemSetup] Done. Main doors wired: {mainDoorsWired}/{MainDoors.Length}, washer wired: {washerWired}.");
        }

        [MenuItem("PrankMansion/Stage 11 - Build And Run Door System Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildDoorSystem();
            RunStaticVerification();

            var testGo = new GameObject("Stage11_DoorSystemTestRunner");
            testGo.AddComponent<Stage11DoorSystemTest>();

            Debug.Log("[Stage11DoorSystemSetup] Entering Play Mode to run dynamic door/drawer verification test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        private static int WireMainDoors()
        {
            int wired = 0;
            foreach (var (name, axis) in MainDoors)
            {
                string path = PropsPrefabDir + name + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                var door = root.GetComponent<DoorProp>();
                if (door == null) door = root.AddComponent<DoorProp>();
                door.interactRange = DoorProp.MainInteractRange;
                door.openAngleDeg = DoorProp.MainOpenAngleDeg;
                door.hingeAxis = axis;
                PrefabUtility.SaveAsPrefabAsset(root, path);
                PrefabUtility.UnloadPrefabContents(root);
                wired++;
            }
            return wired;
        }

        private static bool WireKitchenWasherDoor()
        {
            const string path = PropsPrefabDir + "Kitchen_FullCombo_01.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return false;

            var root = PrefabUtility.LoadPrefabContents(path);
            Transform doorNode = FindDeepChild(root.transform, "MIELE WWV 980 WPS PASSION_doors");
            if (doorNode == null) { PrefabUtility.UnloadPrefabContents(root); return false; }

            var door = doorNode.GetComponent<DoorProp>();
            if (door == null) door = doorNode.gameObject.AddComponent<DoorProp>();
            door.interactRange = DoorProp.SubDoorInteractRange;
            door.openAngleDeg = KitchenSubDoorAngle;
            door.hingeAxis = DoorProp.HingeAxis.Horizontal;

            if (doorNode.GetComponentInChildren<Collider>() == null)
            {
                foreach (var mf in doorNode.GetComponentsInChildren<MeshFilter>())
                {
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    mc.convex = true;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return true;
        }

        private static Transform FindDeepChild(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name) return child;
                var found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        // ---------------------------------------------------------------
        private static void BuildFilingCabinetPlaceholder()
        {
            const string prefabPath = PropsPrefabDir + "Office_FilingCabinet_01.prefab";

            EnsurePlaceholderTag();

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Office_FilingCabinet_01";
            body.tag = PlaceholderTag;
            body.transform.position = Vector3.zero;
            body.transform.rotation = Quaternion.identity;
            // Part 4.4: expectedMaxDim 1.30m - a filing cabinet is taller than it is
            // wide/deep. X/Z centered, Y bottom at 0 (matches every other prop's
            // floor-contact-point convention since Stage 9's pivot fix).
            const float height = 1.30f, width = 0.50f, depth = 0.60f;
            body.transform.localScale = new Vector3(width, height, depth);
            body.transform.position = new Vector3(0f, height * 0.5f, 0f);

            var renderer = body.GetComponent<Renderer>();
            renderer.sharedMaterial = GetOrCreatePlaceholderMaterial();

            body.AddComponent<PushableProp>(); // Part 4.4: "قابل للدفع"

            const int drawerCount = 3; // DECISION: no count given, matches Stage 9's dresser for consistency
            float slotHeight = height / drawerCount;
            for (int i = 0; i < drawerCount; i++)
            {
                // Computed in WORLD units first (drawer front sits flush with the
                // body's +Z face, stacked evenly up its height), then converted to
                // local scale/position by dividing out the body's own non-uniform
                // localScale - otherwise a child's local units get silently
                // stretched by the parent's scale (would break both the visual
                // size and DrawerSlideProp's real-world 0.40m slide distance).
                float worldDrawerWidth = width * 0.8f;
                float worldDrawerHeight = slotHeight * 0.8f;
                const float worldDrawerDepth = 0.05f;
                float worldY = (height * -0.5f) + slotHeight * (i + 0.5f);
                float worldZ = depth * 0.5f;

                var drawer = GameObject.CreatePrimitive(PrimitiveType.Cube);
                drawer.name = $"Drawer_{i:00}";
                drawer.tag = PlaceholderTag;
                drawer.transform.SetParent(body.transform, true);
                drawer.transform.localPosition = new Vector3(0f, worldY / height, worldZ / depth);
                drawer.transform.localScale = new Vector3(worldDrawerWidth / width, worldDrawerHeight / height, worldDrawerDepth / depth);
                drawer.GetComponent<Renderer>().sharedMaterial = GetOrCreatePlaceholderMaterial();
                drawer.AddComponent<DrawerSlideProp>();
            }

            if (!Directory.Exists(PropsPrefabDir)) Directory.CreateDirectory(PropsPrefabDir);
            PrefabUtility.SaveAsPrefabAsset(body, prefabPath);

            // Place it in the Office wing.
            var office = MansionSpec.OfficeWing;
            body.transform.position = new Vector3(office.centerX, height * 0.5f, office.centerZ);
            EditorSceneManager.OpenScene(MansionScenePath, OpenSceneMode.Single);
            var existing = GameObject.Find("Office_FilingCabinet_01");
            if (existing != null) UnityEngine.Object.DestroyImmediate(existing);
            var placed = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
            placed.transform.position = new Vector3(office.centerX, height * 0.5f, office.centerZ);

            UnityEngine.Object.DestroyImmediate(body);
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
        public static void RunStaticVerification()
        {
            var lines = new List<string> { "=== Stage 11 - Static Verification (Part 8 + Law 0.2) ===", "" };
            int total = 0, passed = 0;
            void Check(string name, bool ok, string detail)
            {
                total++;
                if (ok) passed++;
                lines.Add($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
            }

            foreach (var (name, _) in MainDoors)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropsPrefabDir + name + ".prefab");
                var door = prefab != null ? prefab.GetComponent<DoorProp>() : null;
                Check($"{name}: has a real DoorProp (Part 8.1)", door != null, $"found={(door != null)}");
                if (door != null)
                    Check($"{name}: main-door interact range/angle (Part 8.1)",
                        Mathf.Approximately(door.interactRange, DoorProp.MainInteractRange) && Mathf.Approximately(door.openAngleDeg, DoorProp.MainOpenAngleDeg),
                        $"range={door.interactRange} angle={door.openAngleDeg}");
            }

            var combo = AssetDatabase.LoadAssetAtPath<GameObject>(PropsPrefabDir + "Kitchen_FullCombo_01.prefab");
            Transform washerDoor = combo != null ? FindDeepChild(combo.transform, "MIELE WWV 980 WPS PASSION_doors") : null;
            var washerDoorProp = washerDoor != null ? washerDoor.GetComponent<DoorProp>() : null;
            Check("Kitchen_FullCombo_01's real washer door has a DoorProp (Part 8.2)", washerDoorProp != null, $"found={(washerDoorProp != null)}");

            var cabinet = AssetDatabase.LoadAssetAtPath<GameObject>(PropsPrefabDir + "Office_FilingCabinet_01.prefab");
            Check("Office_FilingCabinet_01 placeholder prefab exists (Part 8.3)", cabinet != null, $"path={PropsPrefabDir}Office_FilingCabinet_01.prefab");
            if (cabinet != null)
            {
                Check("Office_FilingCabinet_01 is Pushable (Part 4.4)", cabinet.GetComponent<PushableProp>() != null, $"found={(cabinet.GetComponent<PushableProp>() != null)}");
                var drawers = cabinet.GetComponentsInChildren<DrawerSlideProp>();
                Check("Office_FilingCabinet_01 has 3 DrawerSlideProp children (Part 8.3)", drawers.Length == 3, $"count={drawers.Length}");
            }

            var placedCabinet = GameObject.Find("Office_FilingCabinet_01");
            Check("Placeholder filing cabinet is placed in the actual mansion scene", placedCabinet != null, $"found={(placedCabinet != null)}");

            lines.Add("");
            lines.Add($"TOTAL: {passed}/{total} passed");
            lines.Add(passed == total ? "RESULT: PASS" : "RESULT: FAILURE - see FAIL lines above.");

            var dir = Path.GetDirectoryName(StaticReportPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            File.WriteAllLines(StaticReportPath, lines);

            if (passed != total) Debug.LogError($"[Stage11DoorSystemSetup] Static verification FAILED: {passed}/{total}. See {StaticReportPath}");
            else Debug.Log($"[Stage11DoorSystemSetup] Static verification passed: {passed}/{total}.");
        }

        // ---------------------------------------------------------------
        private static void WriteDecisionsLog(int mainDoorsWired, bool washerWired)
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 11 - Interior Door System - Logged Technical Decisions (Law 21.2) ===",
                "",
                "1. Part 8.1's general door (DoorProp, Entities/) was added directly onto",
                "   every already-corrected *_Door_01 prefab that actually has a source mesh",
                $"   ({mainDoorsWired}/{MainDoors.Length} wired: Kitchen_Door_01, Bathroom1_Door_01,",
                "   Bedroom1_Door_01, Bedroom2_Door_01, Bathroom2_Door_01,",
                "   Bathroom2_ShowerDoor_01) - re-saved in place rather than re-running each",
                "   stage's own importer. Office_Door_01 and Gym_Door_01 have no prop to",
                "   attach a door mechanic to at all (both logged missing back in Stages 8/10,",
                "   still unresolved) - nothing built for either.",
                "",
                "2. DOCUMENT/ASSET GAP: Part 8.2's fridge/cabinet/oven sub-doors inside",
                "   Kitchen_FullCombo_01 could NOT be reliably identified. The combo's full",
                "   hierarchy was dumped (temporary Stage11_KitchenHierarchyDump.cs, deleted",
                "   after use) and is almost entirely generic CAD-export names (Box012,",
                "   Group026, VIFS113, Line058, Cylinder001, ...) with zero semantic meaning -",
                "   there is no reliable way to tell \"this is the fridge door\" from \"this is a",
                "   countertop panel\" by name alone, and guessing would risk rotating the",
                "   wrong node and visibly breaking the model. The one exception is the",
                $"   washer, a separately-modeled \"MIELE WWV 980 WPS PASSION\" unit with real",
                $"   English part names - its own \"_doors\" node was found with full confidence",
                $"   and got a real DoorProp (horizontal hinge axis, Part 8.2's circular-door",
                "   case, wired=" + washerWired + "). Resolving the other three needs either",
                "   visual inspection in the Editor by someone who can identify them by eye, or",
                "   separate standalone fridge/cabinet/oven models instead of one combo mesh.",
                "",
                "3. Part 8.2's sub-door open-angle range (\"تسعين إلى مئة وعشر درجات\") has no",
                "   single value - 100 deg (the midpoint) is used for the washer door.",
                "",
                "4. Office_FilingCabinet_01 (Part 4.4: 1.30m, Pushable, \"أدراج تُفتح\") never",
                "   got a source file back in Stage 8 - built as a Law 0.2 step 3 placeholder",
                "   here (primitive body + 3 DrawerSlideProp children, PLACEHOLDER_Magenta",
                "   material, \"Placeholder\" tag, same pattern as Stage 9's Part 4) because a",
                "   real prop is needed to prove Part 8.3's drawer-slide system actually works",
                "   end to end in the scene - without one, this stage's own mechanic would ship",
                "   completely unused by anything. Drawer count (3) matches Bedroom1_Dresser_01",
                "   for consistency; no count is given in the document.",
                "",
                "5. Bedroom1_Dresser_01 explicitly keeps its Stage 9 physical grab-and-drag",
                "   system (InteractiveContainerProp/DrawerProp, ConfigurableJoint) instead of",
                "   Part 8.3's simpler button+slide mechanic, even though Part 8.3's own text",
                "   names it alongside the filing cabinet (\"خزانة الملفات وخزانة الأدراج فغرفة",
                "   النوم\") - owner's explicit choice when this stage was reached, keeping the",
                "   richer, already-tested mechanic rather than downgrading it.",
                "",
                "6. Sound: no real door-open/close SFX exists (Assets/_Project/Audio/SFX/Doors",
                "   is empty) - PlaceholderAudio.GenerateTone stands in per Law 0.2, same",
                "   pattern as Part 7.2's wind/ignition cues.",
                "",
                "7. Hinge side (\"which vertical edge does the door pivot on\") is not given a",
                "   number in the document - every door's own local -X edge is used",
                "   consistently, computed fresh from each prop's own world-space renderer",
                "   bounds at Awake time (robust regardless of whichever pivot convention that",
                "   particular prop happened to import with across Stages 6-10).",
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
