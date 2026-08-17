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
    /// Stage 4 setup: wires PlayerRagdoll onto the real gameplay Player (Part 7.3)
    /// and drops a placeholder pourable prop near the spawn point so Part 7.4's slip
    /// trap is playable in-scene. The real Foyer_SoapBottle-style prop isn't imported
    /// until Stage 6 - per Law 0.2, logged as missing with a simple placeholder
    /// standing in.
    /// </summary>
    public static class Stage4RagdollSlipSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string PrefabPath = "Assets/_Project/Prefabs/Characters/Player.prefab";
        private const string MissingAssetsLogPath = "Assets/_ProjectLogs/Missing_Assets_Log.txt";
        private const string PlaceholdersLogPath = "Assets/_ProjectLogs/Generated_Placeholders_Log.txt";
        private const string PlaceholderRootName = "Stage4_RagdollSlipPlaceholders";

        [MenuItem("PrankMansion/Stage 4 - Build Ragdoll & Slip Systems")]
        public static void BuildRagdollAndSlip()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Stage4RagdollSlipSetup] No 'Player' in the scene - run Stage 2 (and Stage 3) first.");
                return;
            }

            if (player.GetComponent<PlayerRagdoll>() == null)
                player.AddComponent<PlayerRagdoll>();

            BuildPlaceholderPourable(player.transform.position);

            var prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot.GetComponent<PlayerRagdoll>() == null)
                prefabRoot.AddComponent<PlayerRagdoll>();
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);

            WriteAssetLogs();
            Debug.Log("[Stage4RagdollSlipSetup] Ragdoll system wired onto Player; placeholder pourable placed.");
        }

        [MenuItem("PrankMansion/Stage 4 - Build And Run Ragdoll & Slip Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildRagdollAndSlip();

            var testGo = new GameObject("Stage4_RagdollSlipTestRunner");
            testGo.AddComponent<Stage4RagdollSlipTest>();

            Debug.Log("[Stage4RagdollSlipSetup] Entering Play Mode to run ragdoll/slip system test...");
            EditorApplication.isPlaying = true;
        }

        private static void BuildPlaceholderPourable(Vector3 nearPosition)
        {
            var existing = GameObject.Find(PlaceholderRootName);
            if (existing != null) Object.DestroyImmediate(existing);

            var root = new GameObject(PlaceholderRootName).transform;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Placeholder_PourableSoap";
            go.transform.SetParent(root, true);
            go.transform.position = nearPosition + new Vector3(-2f, 0.15f, 1.5f);
            go.transform.localScale = Vector3.one * 0.25f;

            var renderer = go.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Placeholder_PourableSoap" };
            var color = new Color(0.5f, 0.8f, 0.9f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            renderer.sharedMaterial = mat;

            if (go.GetComponent<Rigidbody>() == null) go.AddComponent<Rigidbody>();
            var carry = go.AddComponent<CarryableObject>();
            carry.weightClass = CarryableObject.WeightClass.Light;
            carry.isPourable = true;
        }

        private static void WriteAssetLogs()
        {
            var missingDir = Path.GetDirectoryName(MissingAssetsLogPath);
            if (!Directory.Exists(missingDir)) Directory.CreateDirectory(missingDir);
            File.AppendAllLines(MissingAssetsLogPath, new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Assets/_Project/Models (soap bottle pourable prop) | Stage 4 pour-mechanic test prop (Part 4.1/7.4)",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | No character skeleton/skinned mesh exists yet (Stage 12) | Part 7.3 ragdoll rig is a procedural capsule placeholder, not a skinned humanoid",
            });

            var placeholderDir = Path.GetDirectoryName(PlaceholdersLogPath);
            if (!Directory.Exists(placeholderDir)) Directory.CreateDirectory(placeholderDir);
            File.AppendAllLines(PlaceholdersLogPath, new List<string>
            {
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | Placeholder_PourableSoap (0.25m cube) | stands in for a Part 4 pourable prop",
                $"{System.DateTime.UtcNow:yyyy-MM-dd} | PlayerRagdoll's 12-capsule procedural rig | stands in for a real Part 5 character skeleton (Stage 12)",
            });
        }
    }
}
