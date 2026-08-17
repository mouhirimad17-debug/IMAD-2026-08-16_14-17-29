using System.Collections.Generic;
using System.IO;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 3 setup: wires PlayerCarry onto the real gameplay Player (built in Stage
    /// 2) and drops a few placeholder props into the foyer so the carry/wind systems
    /// (Part 7.1 + 7.2) are actually playable in-scene, not just covered by the
    /// automated test. Real props (Part 4's Foyer_ChampagneBottle_01, a light item,
    /// and Foyer_CandelabraStanding_01, a fire source) are not imported until Stage 6
    /// - per Law 0.2, that's logged as missing and a simple placeholder stands in so
    /// this stage doesn't have to wait for them.
    /// </summary>
    public static class Stage3CarryWindSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string PlaceholdersLogPath = "Assets/_ProjectLogs/Generated_Placeholders_Log.txt";
        private const string PlaceholderRootName = "Stage3_CarryWindPlaceholders";

        [MenuItem("PrankMansion/Stage 3 - Build Carry & Wind Systems")]
        public static void BuildCarryAndWind()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Stage3CarryWindSetup] No 'Player' in the scene - run Stage 2 first.");
                return;
            }

            if (player.GetComponent<PlayerCarry>() == null)
                player.AddComponent<PlayerCarry>();

            BuildPlaceholderProps(player.transform.position);

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.GetComponent<PlayerCarry>() == null)
                prefabRoot.AddComponent<PlayerCarry>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

            WriteAssetLogs();
            Debug.Log("[Stage3CarryWindSetup] Carry + wind systems wired onto Player; placeholder props placed.");
        }

        [MenuItem("PrankMansion/Stage 3 - Build And Run Carry & Wind Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildCarryAndWind();

            var testGo = new GameObject("Stage3_CarryWindTestRunner");
            testGo.AddComponent<Stage3CarryWindTest>();

            Debug.Log("[Stage3CarryWindSetup] Entering Play Mode to run carry/wind system test...");
            EditorApplication.isPlaying = true;
        }

        private static void BuildPlaceholderProps(Vector3 nearPosition)
        {
            var existing = GameObject.Find(PlaceholderRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(PlaceholderRootName).transform;

            // Light carryable placeholder (stand-in for Foyer_ChampagneBottle_01 etc.)
            var light = CreatePlaceholderCube("Placeholder_LightCarryable",
                nearPosition + new Vector3(0f, 0.15f, 1.5f), Vector3.one * 0.3f, new Color(0.8f, 0.7f, 0.3f));
            var lightCarry = light.AddComponent<CarryableObject>();
            lightCarry.weightClass = CarryableObject.WeightClass.Light;

            // Heavy carryable placeholder (stand-in for the watermelon / printer / etc.)
            var heavy = CreatePlaceholderCube("Placeholder_HeavyCarryable",
                nearPosition + new Vector3(2f, 0.35f, 1.5f), Vector3.one * 0.7f, new Color(0.6f, 0.3f, 0.2f));
            heavy.transform.SetParent(root, true);
            var heavyCarry = heavy.AddComponent<CarryableObject>();
            heavyCarry.weightClass = CarryableObject.WeightClass.Heavy;

            light.transform.SetParent(root, true);

            // Fire source placeholder (stand-in for Foyer_CandelabraStanding_01)
            var fire = CreatePlaceholderCube("Placeholder_FireSource",
                nearPosition + new Vector3(-2f, 0.75f, 1.5f), new Vector3(0.2f, 1.5f, 0.2f), new Color(1f, 0.5f, 0.1f));
            fire.transform.SetParent(root, true);
            fire.AddComponent<FireSource>();
            var fireCollider = fire.GetComponent<Collider>();
            if (fireCollider != null) Object.DestroyImmediate(fireCollider); // decorative only, detection is a distance check
        }

        private static GameObject CreatePlaceholderCube(string name, Vector3 position, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = size;

            var renderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = $"Placeholder_{name}" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.sharedMaterial = mat;

            if (go.GetComponent<Rigidbody>() == null) go.AddComponent<Rigidbody>();
            return go;
        }

        private static void WriteAssetLogs()
        {
            var missingDir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(missingDir)) Directory.CreateDirectory(missingDir);

            var missingLines = new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Foyer/Throwable/Foyer_ChampagneBottle_01.* | Stage 3 light-carryable test prop (Part 4.2)",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Kitchen/Throwable/Kitchen_Watermelon_01.* | Stage 3 heavy-carryable test prop (Part 4.3)",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Foyer/Static/Foyer_CandelabraStanding_01.* | Stage 3 fire-source test prop (Part 4.2)",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Audio/SFX/Fart/*.wav | Part 7.2 wind loop + ignition explosion cues",
            };
            File.AppendAllLines(MissingAssetsLogPath, missingLines);

            var placeholderDir = Path.GetDirectoryName(PlaceholdersLogPath);
            if (!Directory.Exists(placeholderDir)) Directory.CreateDirectory(placeholderDir);

            var placeholderLines = new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_LightCarryable (0.3m cube) | stands in for a Part 4.2 light-carryable prop",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_HeavyCarryable (0.7m cube) | stands in for a Part 4 heavy-carryable prop",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_FireSource (0.2x1.5x0.2m box) | stands in for Foyer_CandelabraStanding_01",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | PlaceholderAudio.GenerateTone procedural clips | stand in for Part 7.2 wind-loop and ignition-explosion SFX",
            };
            File.AppendAllLines(PlaceholdersLogPath, placeholderLines);
        }
    }
}
