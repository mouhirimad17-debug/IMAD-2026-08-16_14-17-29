using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 8's verification (static half already ran in
    /// Stage8OfficeImporter.RunStaticVerification before Play Mode was entered).
    /// </summary>
    public class Stage8OfficePropsTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage8_DynamicPropsTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 8 - Dynamic Office Props Test (real meshes, Play Mode) ===");
            report.AppendLine();

            var root = GameObject.Find(Stage8OfficePropSpec.OfficePropsRootName);
            Check("Setup: Office props root exists in the scene", root != null, $"found={(root != null)}");

            if (root != null)
            {
                yield return TestPickUpRealMesh(root, "Office_Book_01");
                yield return TestHeavyCarryRealMesh(root, "Office_Printer_01");
                yield return TestPushableRealMesh(root, "Office_ChairWheels_01");
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 8 real-mesh Office props work correctly with the existing physics systems."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage8DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage8DynamicTest] DONE");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            if (Application.isBatchMode)
                UnityEditor.EditorApplication.Exit(passed == total ? 0 : 1);
#endif
        }

        private void Check(string name, bool ok, string detail)
        {
            total++;
            if (ok) passed++;
            report.AppendLine($"[{(ok ? "PASS" : "FAIL")}] {name} -> {detail}");
        }

        private Transform FindChildByName(Transform root, string name)
        {
            foreach (Transform child in root)
                if (child.name == name || child.name == name + "(Clone)")
                    return child;
            return null;
        }

        private PlayerCarry BuildTestPlayer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            go.AddComponent<PlayerLocomotion>();
            return go.AddComponent<PlayerCarry>();
        }

        private IEnumerator TestPickUpRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var player = BuildTestPlayer("Stage8Test_PickupPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            Check($"A real Office prop mesh ({unityName}) can be picked up (Part 7.1)", picked, $"picked={picked}");

            player.Drop();
            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestHeavyCarryRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var carryable = propTransform.GetComponent<CarryableObject>();
            Check($"{unityName} is classified Heavy (Part 4.4)",
                carryable != null && carryable.weightClass == CarryableObject.WeightClass.Heavy,
                $"weightClass={(carryable != null ? carryable.weightClass.ToString() : "none")}");
            if (carryable == null) yield break;

            var player = BuildTestPlayer("Stage8Test_HeavyPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            var loco = player.GetComponent<PlayerLocomotion>();
            Check($"Carrying the real printer solo applies the 30% speed factor (Part 5.1/7.1)",
                picked && Mathf.Approximately(loco.SpeedMultiplier, PlayerCarry.StandardHeavySpeedFactor),
                $"picked={picked} multiplier={loco.SpeedMultiplier:F3}");

            player.Drop();
            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestPushableRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var pushable = propTransform.GetComponent<PushableProp>();
            var body = propTransform.GetComponent<Rigidbody>();
            Check($"{unityName} has a PushableProp + non-kinematic Rigidbody (Part 4.4)",
                pushable != null && body != null && !body.isKinematic,
                $"pushable={(pushable != null)} body={(body != null)} kinematic={(body != null && body.isKinematic)}");
            if (pushable == null || body == null) yield break;

            var player = BuildTestPlayer("Stage8Test_PushPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.gameObject.AddComponent<PlayerPushInteraction>();
            var loco = player.GetComponent<PlayerLocomotion>();
            loco.SetCameraYaw(0f);
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            float yaw = player.transform.eulerAngles.y;
            loco.SetCameraYaw(yaw);
            yield return null;

            Vector3 startPos = propTransform.position;
            loco.SetSprint(true);
            loco.SetMoveInput(Vector2.up);

            float elapsed = 0f;
            while (Vector3.Distance(propTransform.position, startPos) < 0.1f && elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check($"Walking into a real Pushable prop shoves it along the floor (Part 4.1)",
                Vector3.Distance(propTransform.position, startPos) >= 0.1f,
                $"moved={Vector3.Distance(propTransform.position, startPos):F3}m elapsed={elapsed:F2}s");

            loco.SetMoveInput(Vector2.zero);
            loco.SetSprint(false);
            Destroy(player.gameObject);
            yield return null;
        }
    }
}
