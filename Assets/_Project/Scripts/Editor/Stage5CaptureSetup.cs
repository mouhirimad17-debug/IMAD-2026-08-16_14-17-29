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
    /// Stage 5 setup: wires PlayerCapture onto the real gameplay Player (Part 7.5)
    /// and drops a placeholder rope near the spawn point plus a placeholder ceiling
    /// fan in the Bedroom2 wing so both restrain and the fan-mount insult are
    /// playable in-scene. Neither the real rope prop nor the real Bedroom2 fan is
    /// imported yet (Stage 6/7) - per Law 0.2, logged as missing with simple
    /// placeholders standing in.
    /// </summary>
    public static class Stage5CaptureSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string PlaceholdersLogPath = "Assets/_ProjectLogs/Generated_Placeholders_Log.txt";
        private const string PlaceholderRootName = "Stage5_CapturePlaceholders";

        [MenuItem("PrankMansion/Stage 5 - Build Capture & Restraint System")]
        public static void BuildCaptureSystem()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Stage5CaptureSetup] No 'Player' in the scene - run Stages 2-4 first.");
                return;
            }

            if (player.GetComponent<PlayerCapture>() == null)
                player.AddComponent<PlayerCapture>();

            BuildPlaceholders(player.transform.position);

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.GetComponent<PlayerCapture>() == null)
                prefabRoot.AddComponent<PlayerCapture>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

            WriteAssetLogs();
            Debug.Log("[Stage5CaptureSetup] Capture/restraint system wired onto Player; placeholders placed.");
        }

        [MenuItem("PrankMansion/Stage 5 - Build And Run Capture Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildCaptureSystem();

            var testGo = new GameObject("Stage5_CaptureTestRunner");
            testGo.AddComponent<Stage5CaptureTest>();

            Debug.Log("[Stage5CaptureSetup] Entering Play Mode to run capture/restraint system test...");
            EditorApplication.isPlaying = true;
        }

        private static void BuildPlaceholders(Vector3 nearPosition)
        {
            var existing = GameObject.Find(PlaceholderRootName);
            if (existing != null) Object.DestroyImmediate(existing);
            var root = new GameObject(PlaceholderRootName).transform;

            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rope.name = "Placeholder_Rope";
            rope.transform.SetParent(root, true);
            rope.transform.position = nearPosition + new Vector3(1.5f, 0.1f, -1.5f);
            rope.transform.localScale = new Vector3(0.6f, 0.08f, 0.08f);
            SetColor(rope, new Color(0.75f, 0.65f, 0.4f));
            if (rope.GetComponent<Rigidbody>() == null) rope.AddComponent<Rigidbody>();
            var ropeCarry = rope.AddComponent<CarryableObject>();
            ropeCarry.weightClass = CarryableObject.WeightClass.Light;
            ropeCarry.isRope = true;

            var fan = new GameObject("Placeholder_CeilingFan_Bedroom2");
            fan.transform.SetParent(root, true);
            var bed2 = MansionSpec.Bedroom2Wing;
            fan.transform.position = new Vector3(bed2.centerX, MansionSpec.Floor2CeilingY - 0.2f, bed2.centerZ);
            var blades = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blades.name = "FanBlades";
            blades.transform.SetParent(fan.transform, false);
            blades.transform.localScale = new Vector3(1.2f, 0.05f, 0.15f);
            Object.DestroyImmediate(blades.GetComponent<Collider>());
            SetColor(blades, new Color(0.3f, 0.3f, 0.32f));
            fan.AddComponent<CeilingFan>();
        }

        private static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = $"Placeholder_{go.name}" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.sharedMaterial = mat;
        }

        private static void WriteAssetLogs()
        {
            var missingDir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(missingDir)) Directory.CreateDirectory(missingDir);
            File.AppendAllLines(MissingAssetsLogPath, new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models (restrain rope prop) | Stage 5 restrain-mechanic test prop (Part 7.5)",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models/Bedroom2_Bathroom2/Static (ceiling fan prop) | Stage 5 fan-mount test prop (Part 2.2/7.5)",
            });

            var placeholderDir = Path.GetDirectoryName(PlaceholdersLogPath);
            if (!Directory.Exists(placeholderDir)) Directory.CreateDirectory(placeholderDir);
            File.AppendAllLines(PlaceholdersLogPath, new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_Rope (0.6x0.08x0.08m box) | stands in for the Part 7.5 restrain rope",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_CeilingFan_Bedroom2 | stands in for the real Bedroom2 ceiling fan prop",
            });
        }
    }
}
