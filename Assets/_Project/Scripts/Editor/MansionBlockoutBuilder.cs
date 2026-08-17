using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 1 builder: "Mansion structure with basic cubes (Blockout)".
    /// Builds strictly from PrankMansion_MasterDocument.md Part 3 (architecture)
    /// and Law 0.4 (closed mansion). No props (Part 4), no doors (Part 8), no
    /// characters — those are later stages. Every numeric value is taken from
    /// MansionSpec, which mirrors the document's Part 3 numbers exactly; the
    /// handful of values the document does not spell out numerically are marked
    /// as logged decisions in MansionSpec and in Stage1_Decisions_Log.txt.
    ///
    /// REVISION: rebuilt for the corrected Part 3 (x3 footprint, side staircase,
    /// mandatory door gaps in every interior wall - law 3.6).
    /// </summary>
    public static class MansionBlockoutBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string RootName = "PrankMansion_Blockout";
        private const string DecisionsLogPath = "Assets/_ProjectLogs/Stage1_Decisions_Log.txt";

        private static readonly Dictionary<Color, Material> MaterialCache = new Dictionary<Color, Material>();

        [MenuItem("PrankMansion/Stage 1 - Build Mansion Blockout")]
        public static void BuildBlockout()
        {
            OpenOrCreateScene();
            ClearExistingRoot();
            var root = new GameObject(RootName);

            BuildExteriorWalls(root.transform);
            BuildGroundSlab(root.transform);
            BuildFloor2Slab(root.transform);
            BuildRoofSlab(root.transform);
            BuildGroundFloorInteriorWalls(root.transform);
            BuildFloor2InteriorWalls(root.transform);
            BuildBalconyRailing(root.transform);
            BuildStaircase(root.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            WriteDecisionsLog();

            Debug.Log("[MansionBlockoutBuilder] Stage 1 blockout (revised x3 spec) built and saved to " + ScenePath);
        }

        [MenuItem("PrankMansion/Stage 1 - Build And Run Wall Closure Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildBlockout();

            var testGo = new GameObject("Stage1_WallClosureTestRunner");
            testGo.AddComponent<StageOneWallClosureTest>();

            Debug.Log("[MansionBlockoutBuilder] Entering Play Mode to run real-physics wall closure test...");
            EditorApplication.isPlaying = true;
        }

        // ---------------------------------------------------------------
        private static void OpenOrCreateScene()
        {
            var dir = Path.GetDirectoryName(ScenePath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            if (File.Exists(ScenePath))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
        }

        private static void ClearExistingRoot()
        {
            var existing = GameObject.Find(RootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var oldTest = GameObject.Find("Stage1_WallClosureTestRunner");
            if (oldTest != null) Object.DestroyImmediate(oldTest);
        }

        // ---------------------------------------------------------------
        private static void BuildExteriorWalls(Transform parent)
        {
            var group = new GameObject("ExteriorWalls").transform;
            group.SetParent(parent);

            float t = MansionSpec.ExtWallT;
            float lenX = MansionSpec.LenX;
            float widZ = MansionSpec.WidZ;
            float top = MansionSpec.WallTop;
            Color c = new Color(0.82f, 0.74f, 0.60f); // warm exterior stone

            // Front / back walls run the FULL length (they absorb the corners).
            CreateBox("Wall_Front", group,
                center: new Vector3(lenX * 0.5f, top * 0.5f, t * 0.5f),
                size: new Vector3(lenX, top, t), color: c);

            CreateBox("Wall_Back", group,
                center: new Vector3(lenX * 0.5f, top * 0.5f, widZ - t * 0.5f),
                size: new Vector3(lenX, top, t), color: c);

            // Left / right walls fit snugly BETWEEN the front/back walls (no corner overlap).
            float sideLen = widZ - 2f * t;
            CreateBox("Wall_Left", group,
                center: new Vector3(t * 0.5f, top * 0.5f, widZ * 0.5f),
                size: new Vector3(t, top, sideLen), color: c);

            CreateBox("Wall_Right", group,
                center: new Vector3(lenX - t * 0.5f, top * 0.5f, widZ * 0.5f),
                size: new Vector3(t, top, sideLen), color: c);
        }

        private static void BuildGroundSlab(Transform parent)
        {
            var group = new GameObject("Slabs").transform;
            group.SetParent(parent);

            float ty = MansionSpec.GroundSlabT;
            CreateBox("GroundSlab", group,
                center: new Vector3(MansionSpec.LenX * 0.5f, -ty * 0.5f, MansionSpec.WidZ * 0.5f),
                size: new Vector3(MansionSpec.LenX, ty, MansionSpec.WidZ),
                color: new Color(0.55f, 0.55f, 0.58f));
        }

        private static void BuildRoofSlab(Transform parent)
        {
            var slabs = parent.Find("Slabs");
            float y0 = MansionSpec.WallTop;
            float ty = MansionSpec.RoofT;
            CreateBox("RoofSlab", slabs,
                center: new Vector3(MansionSpec.LenX * 0.5f, y0 + ty * 0.5f, MansionSpec.WidZ * 0.5f),
                size: new Vector3(MansionSpec.LenX, ty, MansionSpec.WidZ),
                color: new Color(0.35f, 0.30f, 0.28f));
        }

        // REVISED: the floor-2 slab now has TWO independent openings (the central
        // light-well over the seating area, and the relocated staircase's own shaft)
        // that partially overlap where the 36m-deep foyer squeezes them together (see
        // MansionSpec comments). Rather than hand-authoring a fixed piece count that
        // only works for one hole, this cuts the slab on a rectilinear grid built from
        // both openings' boundaries and skips any cell whose center falls inside either
        // opening - a direct generalization of the old 4-piece frame that resolves the
        // overlap correctly with no seam and no double material.
        private static void BuildFloor2Slab(Transform parent)
        {
            var slabs = parent.Find("Slabs");
            float y0 = MansionSpec.Floor1CeilingY;
            float ty = MansionSpec.SlabT;
            float yc = y0 + ty * 0.5f;
            Color c = new Color(0.55f, 0.55f, 0.58f);

            var openings = new[] { MansionSpec.Opening, MansionSpec.StaircaseFootprint };

            var xs = new List<float> { 0f, MansionSpec.LenX };
            var zs = new List<float> { 0f, MansionSpec.WidZ };
            foreach (var o in openings)
            {
                xs.Add(o.x); xs.Add(o.xMax);
                zs.Add(o.z); zs.Add(o.zMax);
            }
            xs = xs.Distinct().OrderBy(v => v).ToList();
            zs = zs.Distinct().OrderBy(v => v).ToList();

            int idx = 0;
            for (int i = 0; i < xs.Count - 1; i++)
            {
                float cx = (xs[i] + xs[i + 1]) * 0.5f;
                float sizeX = xs[i + 1] - xs[i];
                if (sizeX < 0.001f) continue;

                for (int j = 0; j < zs.Count - 1; j++)
                {
                    float cz = (zs[j] + zs[j + 1]) * 0.5f;
                    float sizeZ = zs[j + 1] - zs[j];
                    if (sizeZ < 0.001f) continue;

                    bool isVoid = openings.Any(o => cx > o.x && cx < o.xMax && cz > o.z && cz < o.zMax);
                    if (isVoid) continue;

                    CreateBox($"Floor2Slab_{idx++:00}", slabs,
                        center: new Vector3(cx, yc, cz),
                        size: new Vector3(sizeX, ty, sizeZ), color: c);
                }
            }
        }

        // ---------------------------------------------------------------
        private static void BuildGroundFloorInteriorWalls(Transform parent)
        {
            var group = new GameObject("GroundFloorInteriorWalls").transform;
            group.SetParent(parent);

            float t = MansionSpec.IntWallT;
            float h = MansionSpec.FloorClearH;
            float yc = h * 0.5f;
            Color c = new Color(0.80f, 0.80f, 0.78f);

            var gym = MansionSpec.Gym;
            var foyer = MansionSpec.Foyer;
            var kitchen = MansionSpec.Kitchen;

            // A: Gym/Foyer divider (spans the deeper of the two rooms, foyer's Z range) — runs along Z
            CreateInteriorWallWithDoor("Wall_Gym_Foyer", group,
                center: new Vector3(foyer.x, yc, foyer.centerZ),
                size: new Vector3(t, h, foyer.sizeZ), runAxis: 1, floorY: 0f, color: c);

            // B: Foyer/Kitchen divider (spans foyer's Z range) — runs along Z
            CreateInteriorWallWithDoor("Wall_Foyer_Kitchen", group,
                center: new Vector3(kitchen.x, yc, foyer.centerZ),
                size: new Vector3(t, h, foyer.sizeZ), runAxis: 1, floorY: 0f, color: c);

            // C: Gym / back corridor divider — runs along X
            CreateInteriorWallWithDoor("Wall_Gym_Corridor", group,
                center: new Vector3(gym.centerX, yc, gym.zMax),
                size: new Vector3(gym.sizeX, h, t), runAxis: 0, floorY: 0f, color: c);

            // D: Foyer / back corridor divider — runs along X
            CreateInteriorWallWithDoor("Wall_Foyer_Corridor", group,
                center: new Vector3(foyer.centerX, yc, foyer.zMax),
                size: new Vector3(foyer.sizeX, h, t), runAxis: 0, floorY: 0f, color: c);

            // E: Kitchen / back corridor divider — runs along X
            CreateInteriorWallWithDoor("Wall_Kitchen_Corridor", group,
                center: new Vector3(kitchen.centerX, yc, kitchen.zMax),
                size: new Vector3(kitchen.sizeX, h, t), runAxis: 0, floorY: 0f, color: c);
        }

        private static void BuildFloor2InteriorWalls(Transform parent)
        {
            var group = new GameObject("Floor2InteriorWalls").transform;
            group.SetParent(parent);

            float t = MansionSpec.IntWallT;
            float bt = MansionSpec.BathWallT;
            float h = MansionSpec.FloorClearH;
            float y0 = MansionSpec.Floor2FloorY;
            float yc = y0 + h * 0.5f;
            Color c = new Color(0.80f, 0.80f, 0.78f);
            Color bathC = new Color(0.70f, 0.82f, 0.85f);

            var office = MansionSpec.OfficeWing;
            var bed2 = MansionSpec.Bedroom2Wing;
            var bed1 = MansionSpec.Bedroom1Wing;

            // Office wing (hallway-facing sides only; the other 2 sides are exterior walls)
            CreateInteriorWallWithDoor("Wall_Office_Hallway_X", group,
                center: new Vector3(office.xMax, yc, office.centerZ),
                size: new Vector3(t, h, office.sizeZ), runAxis: 1, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Office_Hallway_Z", group,
                center: new Vector3(office.centerX, yc, office.z),
                size: new Vector3(office.sizeX, h, t), runAxis: 0, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Office_Bathroom", group,
                center: new Vector3(MansionSpec.OfficeBathSplitX, yc, office.centerZ),
                size: new Vector3(bt, h, office.sizeZ), runAxis: 1, floorY: y0, color: bathC);

            // Bedroom 2 wing
            CreateInteriorWallWithDoor("Wall_Bedroom2_Hallway_X", group,
                center: new Vector3(bed2.xMax, yc, bed2.centerZ),
                size: new Vector3(t, h, bed2.sizeZ), runAxis: 1, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Bedroom2_Hallway_Z", group,
                center: new Vector3(bed2.centerX, yc, bed2.zMax),
                size: new Vector3(bed2.sizeX, h, t), runAxis: 0, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Bedroom2_Bathroom", group,
                center: new Vector3(MansionSpec.Bedroom2BathSplitX, yc, bed2.centerZ),
                size: new Vector3(bt, h, bed2.sizeZ), runAxis: 1, floorY: y0, color: bathC);

            // Bedroom 1 wing
            CreateInteriorWallWithDoor("Wall_Bedroom1_Hallway_X", group,
                center: new Vector3(bed1.x, yc, bed1.centerZ),
                size: new Vector3(t, h, bed1.sizeZ), runAxis: 1, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Bedroom1_Hallway_Z", group,
                center: new Vector3(bed1.centerX, yc, bed1.z),
                size: new Vector3(bed1.sizeX, h, t), runAxis: 0, floorY: y0, color: c);
            CreateInteriorWallWithDoor("Wall_Bedroom1_Bathroom", group,
                center: new Vector3(MansionSpec.Bedroom1BathSplitX, yc, bed1.centerZ),
                size: new Vector3(bt, h, bed1.sizeZ), runAxis: 1, floorY: y0, color: bathC);
        }

        // REVISED: railing is now built independently around BOTH floor-2 openings
        // (the central light-well and the relocated staircase's shaft) using a generic
        // per-edge clipping pass instead of one hand-placed gap. For each opening, each
        // of its 4 edges is emitted in full UNLESS the other opening's footprint covers
        // part (or all) of that edge's line - in which case only the still-exposed
        // portion(s) get a railing segment. This automatically produces exactly one
        // open seam where the staircase's shaft meets the light-well (the real arrival
        // point) with no manual gap coordinates to keep in sync by hand.
        private static void BuildBalconyRailing(Transform parent)
        {
            var group = new GameObject("BalconyRailing").transform;
            group.SetParent(parent);

            float rt = MansionSpec.RailingThickness;
            float rh = MansionSpec.RailingHeight;
            float y0 = MansionSpec.Floor2FloorY;
            float yc = y0 + rh * 0.5f;
            Color c = new Color(0.25f, 0.22f, 0.20f);

            BuildRailingForOpening(group, MansionSpec.Opening, MansionSpec.StaircaseFootprint, "LightWell", yc, rh, rt, c);
            BuildRailingForOpening(group, MansionSpec.StaircaseFootprint, MansionSpec.Opening, "Stair", yc, rh, rt, c);
        }

        private static void BuildRailingForOpening(Transform parent, XZRect self, XZRect other, string label,
            float yc, float rh, float rt, Color c)
        {
            // West edge (x = self.x), varies along Z
            EmitEdgeSegments(parent, $"Railing_{label}_West", axisIsX: true, fixedCoord: self.x,
                rangeMin: self.z, rangeMax: self.zMax,
                otherCovers: other.x < self.x && self.x < other.xMax,
                otherRangeMin: other.z, otherRangeMax: other.zMax,
                yc: yc, rh: rh, rt: rt, c: c);

            // East edge (x = self.xMax), varies along Z
            EmitEdgeSegments(parent, $"Railing_{label}_East", axisIsX: true, fixedCoord: self.xMax,
                rangeMin: self.z, rangeMax: self.zMax,
                otherCovers: other.x < self.xMax && self.xMax < other.xMax,
                otherRangeMin: other.z, otherRangeMax: other.zMax,
                yc: yc, rh: rh, rt: rt, c: c);

            // South edge (z = self.z), varies along X
            EmitEdgeSegments(parent, $"Railing_{label}_South", axisIsX: false, fixedCoord: self.z,
                rangeMin: self.x, rangeMax: self.xMax,
                otherCovers: other.z < self.z && self.z < other.zMax,
                otherRangeMin: other.x, otherRangeMax: other.xMax,
                yc: yc, rh: rh, rt: rt, c: c);

            // North edge (z = self.zMax), varies along X
            EmitEdgeSegments(parent, $"Railing_{label}_North", axisIsX: false, fixedCoord: self.zMax,
                rangeMin: self.x, rangeMax: self.xMax,
                otherCovers: other.z < self.zMax && self.zMax < other.zMax,
                otherRangeMin: other.x, otherRangeMax: other.xMax,
                yc: yc, rh: rh, rt: rt, c: c);
        }

        private static void EmitEdgeSegments(Transform parent, string name, bool axisIsX, float fixedCoord,
            float rangeMin, float rangeMax, bool otherCovers, float otherRangeMin, float otherRangeMax,
            float yc, float rh, float rt, Color c)
        {
            var segments = new List<(float a, float b)>();
            if (otherCovers)
            {
                float cutMin = Mathf.Max(rangeMin, otherRangeMin);
                float cutMax = Mathf.Min(rangeMax, otherRangeMax);
                if (cutMax <= cutMin)
                {
                    segments.Add((rangeMin, rangeMax));
                }
                else
                {
                    if (cutMin > rangeMin) segments.Add((rangeMin, cutMin));
                    if (cutMax < rangeMax) segments.Add((cutMax, rangeMax));
                }
            }
            else
            {
                segments.Add((rangeMin, rangeMax));
            }

            int i = 0;
            foreach (var (a, b) in segments)
            {
                if (b - a < 0.05f) continue;
                float mid = (a + b) * 0.5f;
                float len = b - a;
                Vector3 center = axisIsX ? new Vector3(fixedCoord, yc, mid) : new Vector3(mid, yc, fixedCoord);
                Vector3 size = axisIsX ? new Vector3(rt, rh, len) : new Vector3(len, rh, rt);
                CreateBox($"{name}_{i++}", parent, center, size, c);
            }
        }

        // ---------------------------------------------------------------
        private static void BuildStaircase(Transform parent)
        {
            var group = new GameObject("Staircase").transform;
            group.SetParent(parent);
            Color c = new Color(0.45f, 0.30f, 0.18f); // wood-brown placeholder

            float run = MansionSpec.StairStepRun;
            float rise = MansionSpec.StairStepRise;
            int steps = MansionSpec.StairStepsPerFlight;
            float subW = MansionSpec.StairSubFlightWidth;

            var stair = MansionSpec.StaircaseFootprint;
            float zA0 = stair.z;                              // sub-flight A: [zMin, zMin+5.4]
            float zB1 = stair.zMax;                            // sub-flight B: [zMax-5.4, zMax]

            // Flight 1: bottom -> mid landing, two parallel sub-flights with a center gap.
            for (int i = 0; i < steps; i++)
            {
                float xStart = MansionSpec.StairFlightStartX + i * run;
                float xEnd = xStart + run;
                float topY = (i + 1) * rise;

                CreateBox($"Flight1_A_Step{i:00}", group,
                    center: new Vector3((xStart + xEnd) * 0.5f, topY * 0.5f, zA0 + subW * 0.5f),
                    size: new Vector3(run, topY, subW), color: c);

                CreateBox($"Flight1_B_Step{i:00}", group,
                    center: new Vector3((xStart + xEnd) * 0.5f, topY * 0.5f, zB1 - subW * 0.5f),
                    size: new Vector3(run, topY, subW), color: c);
            }

            // Mid landing (where the two flights physically meet), full footprint width.
            float midY = steps * rise; // 1.75
            CreateBox("MidLanding", group,
                center: new Vector3((MansionSpec.StairMidLandingStartX + MansionSpec.StairMidLandingEndX) * 0.5f,
                    midY * 0.5f, stair.centerZ),
                size: new Vector3(MansionSpec.StairMidLandingEndX - MansionSpec.StairMidLandingStartX, midY, stair.sizeZ),
                color: c);

            // Flight 2: mid landing -> top (flush with the Floor 2 slab edge), separate again.
            for (int i = 0; i < steps; i++)
            {
                float xStart = MansionSpec.StairMidLandingEndX + i * run;
                float xEnd = xStart + run;
                float topY = midY + (i + 1) * rise;

                CreateBox($"Flight2_A_Step{i:00}", group,
                    center: new Vector3((xStart + xEnd) * 0.5f, (midY + topY) * 0.5f, zA0 + subW * 0.5f),
                    size: new Vector3(run, topY - midY, subW), color: c);

                CreateBox($"Flight2_B_Step{i:00}", group,
                    center: new Vector3((xStart + xEnd) * 0.5f, (midY + topY) * 0.5f, zB1 - subW * 0.5f),
                    size: new Vector3(run, topY - midY, subW), color: c);
            }
        }

        // ---------------------------------------------------------------
        // NEW (law 3.6): every interior wall must carry a clear ~1m x 2.1m door gap
        // from the Blockout stage itself. Splits the wall into a left/right jamb
        // segment (full height) plus a lintel piece spanning only the gap's width,
        // covering the remaining height above the door up to the ceiling — instead of
        // one solid CreateBox call. The gap defaults to the wall's own center, which is
        // a reasonable placement everywhere this stage's walls are used (plain
        // straight dividers) since exact door positions are not given per-wall in the
        // document and doors themselves are only imported in Stage 11 (Part 8).
        private static void CreateInteriorWallWithDoor(string name, Transform parent, Vector3 center, Vector3 size,
            int runAxis, float floorY, Color color)
        {
            float doorW = MansionSpec.DoorWidth;
            float doorH = MansionSpec.DoorHeight;
            float wallH = size.y;
            float lintelH = wallH - doorH;
            float lintelCenterY = floorY + doorH + lintelH * 0.5f;

            if (runAxis == 0) // wall runs along X, thickness along Z
            {
                float runLen = size.x;
                float doorCenter = center.x;
                float runStart = center.x - runLen * 0.5f;
                float runEnd = center.x + runLen * 0.5f;
                float leftLen = (doorCenter - doorW * 0.5f) - runStart;
                float rightLen = runEnd - (doorCenter + doorW * 0.5f);

                if (leftLen > 0.01f)
                    CreateBox(name + "_A", parent,
                        new Vector3(runStart + leftLen * 0.5f, center.y, center.z),
                        new Vector3(leftLen, wallH, size.z), color);
                if (rightLen > 0.01f)
                    CreateBox(name + "_B", parent,
                        new Vector3(runEnd - rightLen * 0.5f, center.y, center.z),
                        new Vector3(rightLen, wallH, size.z), color);

                CreateBox(name + "_Lintel", parent,
                    new Vector3(doorCenter, lintelCenterY, center.z),
                    new Vector3(doorW, lintelH, size.z), color);
            }
            else // wall runs along Z, thickness along X
            {
                float runLen = size.z;
                float doorCenter = center.z;
                float runStart = center.z - runLen * 0.5f;
                float runEnd = center.z + runLen * 0.5f;
                float leftLen = (doorCenter - doorW * 0.5f) - runStart;
                float rightLen = runEnd - (doorCenter + doorW * 0.5f);

                if (leftLen > 0.01f)
                    CreateBox(name + "_A", parent,
                        new Vector3(center.x, center.y, runStart + leftLen * 0.5f),
                        new Vector3(size.x, wallH, leftLen), color);
                if (rightLen > 0.01f)
                    CreateBox(name + "_B", parent,
                        new Vector3(center.x, center.y, runEnd - rightLen * 0.5f),
                        new Vector3(size.x, wallH, rightLen), color);

                CreateBox(name + "_Lintel", parent,
                    new Vector3(center.x, lintelCenterY, doorCenter),
                    new Vector3(size.x, lintelH, doorW), color);
            }
        }

        // ---------------------------------------------------------------
        private static GameObject CreateBox(string name, Transform parent, Vector3 center, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = center;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial(color);

            // Mark static for the future occlusion/static-batching pass (Part 15.2) -
            // safe now since every blockout piece here is a "purely static" element.
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic
                | StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic
                | StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);

            return go;
        }

        private static Material GetMaterial(Color color)
        {
            if (MaterialCache.TryGetValue(color, out var mat) && mat != null) return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { name = $"Blockout_{ColorUtility.ToHtmlStringRGB(color)}" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);

            MaterialCache[color] = mat;
            return mat;
        }

        private static void WriteDecisionsLog()
        {
            var dir = Path.GetDirectoryName(DecisionsLogPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var lines = new List<string>
            {
                "=== Stage 1 Blockout - Logged Technical Decisions (Law 21.2) ===",
                "REVISION: rebuilt per the owner's 3-point correction to Part 3 (x3 footprint,",
                "side staircase, mandatory interior door gaps).",
                "",
                "1. Floor-to-floor rise = 3.20m clear height + 0.30m slab = 3.50m total.",
                "   Part 3.1 gives clear height (3.20) and slab thickness (0.30) as two",
                "   separate explicit values; Part 3.3's staircase note \"(3.20 m)\" for",
                "   reaching floor 2 reads as shorthand for the floor-height constant, not",
                "   a separately re-derived total rise. Used 3.50m as the authoritative rise.",
                "   Unaffected by the x3 revision (only horizontal dimensions scaled).",
                "",
                "2. Roof slab (0.30m) and ground slab (0.30m) added for a closed volume.",
                "   Not numerically specified in Part 3; reused the inter-floor slab",
                "   thickness for consistency. Purely technical, no gameplay effect.",
                "",
                "3. Staircase built as literal stacked cube steps (10 steps per flight,",
                "   0.75m tread / 0.175m rise) since Part 3.3 gives only overall footprint",
                "   (18x12m) and total rise, not step count. Tread run scaled x3 with the",
                "   footprint; rise/step-count left unchanged since floor height did not",
                "   scale. Matches this stage's own name (\"blockout WITH CUBES\"). Will be",
                "   replaced by Foyer_StaircaseDouble_01 in Stage 6.",
                "",
                "4. Floor 2 wing corner assignment: Office = back-west, Bedroom2 = front-west,",
                "   Bedroom1 = back-east; front-east corner left open as the stair-arrival",
                "   hallway hub (the document names three corners without coordinates).",
                "   Carried over unchanged from the original build, coordinates rescaled x3.",
                "",
                "5. Balcony railing thickness: 0.10m placeholder (not specified numerically).",
                "",
                "6. NEW - staircase relocated to the foyer's long wall opposite the main",
                "   entrance (z = Foyer.zMax = 36, shared with the back service corridor),",
                "   footprint 18m along the wall x 12m into the room, flush against it.",
                "   \"Long side wall\" read as the wall running along the foyer's longer",
                "   (42m) dimension. The central light-well opening stays independently",
                "   centered on the foyer per the document's explicit instruction that its",
                "   position does not follow the stairs. Because the foyer is only 36m",
                "   deep, the centered 18m-deep opening (z:9-27) and the wall-flush 12m-",
                "   deep stair shaft (z:24-36) unavoidably overlap by 3m given both",
                "   footprints and positions are individually pinned by the document -",
                "   resolved by building both the floor-2 slab and its balcony railing",
                "   with a generic rectilinear cut-around-N-holes algorithm rather than a",
                "   hand-placed single gap, so the merged void and its one real opening",
                "   seam are derived automatically and stay correct if either rect moves.",
                "",
                "7. NEW - law 3.6 (every interior wall needs a ~1m x 2.1m door gap from",
                "   Blockout onward) applied literally to ALL 14 interior wall pieces on",
                "   both floors (ground: gym/foyer, foyer/kitchen, and all 3 room/corridor",
                "   dividers; floor 2: both hallway-facing walls plus the bathroom split",
                "   wall in each of the 3 wings) - read from the document's own stronger,",
                "   unconditional restatement (\"لا يُقبل أن تكون الجدران الداخلية بين أي",
                "   غرفتين مصمتة بالكامل\") rather than only the walls where Part 4",
                "   happens to name a door prop. Door gap defaults to each wall's own",
                "   center; exact placement is not given per-wall in the document and real",
                "   door props are only imported in Stage 11.",
                "",
                "All decisions are easily adjustable - see MansionSpec.cs, which is the",
                "single source of truth both the builder and the wall-closure test read from."
            };

            File.WriteAllLines(DecisionsLogPath, lines);
        }
    }
}
