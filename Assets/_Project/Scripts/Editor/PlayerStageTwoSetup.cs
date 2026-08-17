using System.IO;
using PrankMansion.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Stage 2 setup: places a real, playable Player (CharacterController +
    /// PlayerLocomotion + PlayerInputReader) and wires the scene's Main Camera with
    /// PlayerCameraRig, in Scene_04_Mansion, then saves a reusable prefab. Built
    /// strictly from Part 14 (camera/controls) and Part 5.1 (base movement values).
    /// </summary>
    public static class PlayerStageTwoSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Scene_04_Mansion.unity";
        private const string ActionsAssetPath = "Assets/InputSystem_Actions.inputactions";
        private const string PrefabDir = "Assets/_Project/Prefabs/Characters";
        private const string PrefabPath = PrefabDir + "/Player.prefab";

        [MenuItem("PrankMansion/Stage 2 - Build Player + Camera")]
        public static void BuildPlayerAndCamera()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var existing = GameObject.Find("Player");
            if (existing != null) Object.DestroyImmediate(existing);

            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(45f, 0.05f, 18f); // open foyer middle (Stage 1 layout, revised x3 spec), clear of the staircase footprint (X[36,54] Z[24,36])

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.0f;   // Law 0.1: reference character height = 1.000m
            controller.radius = 0.3f;   // DECISION: not specified, reasonable for a 1m-tall character
            controller.center = new Vector3(0f, 0.5f, 0f);

            player.AddComponent<PlayerLocomotion>();

            var camObj = GameObject.Find("Main Camera");
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }
            var rig = camObj.GetComponent<PlayerCameraRig>();
            if (rig == null) rig = camObj.AddComponent<PlayerCameraRig>();
            rig.target = player.transform;

            var reader = player.AddComponent<PlayerInputReader>();
            var so = new SerializedObject(reader);
            so.FindProperty("actionsAsset").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsAssetPath);
            so.FindProperty("cameraRig").objectReferenceValue = rig;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
            Debug.Log("[PlayerStageTwoSetup] Player + camera rig built, saved prefab at " + PrefabPath);
        }

        [MenuItem("PrankMansion/Stage 2 - Build And Run Movement Test (Batch)")]
        public static void BuildAndTest()
        {
            BuildPlayerAndCamera();

            var testGo = new GameObject("Stage2_MovementTestRunner");
            testGo.AddComponent<StageTwoMovementTest>();

            Debug.Log("[PlayerStageTwoSetup] Entering Play Mode to run movement/camera test...");
            EditorApplication.isPlaying = true;
        }
    }
}
