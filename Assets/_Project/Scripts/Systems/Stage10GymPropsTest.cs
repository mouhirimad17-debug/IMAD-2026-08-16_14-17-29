using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 10's verification (static half already ran in
    /// Stage10GymImporter.RunStaticVerification before Play Mode was entered).
    /// Proves the real Gym meshes work with the existing carry system (including a
    /// second real Heavy-carryable prop after Stage 7's watermelon) and the new
    /// PunchingBagProp swing-on-impact behavior on a real mesh.
    /// </summary>
    public class Stage10GymPropsTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage10_DynamicPropsTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 10 - Dynamic Gym Props Test (real meshes, Play Mode) ===");
            report.AppendLine();

            var root = GameObject.Find(Stage10GymPropSpec.GymPropsRootName);
            Check("Setup: Gym props root exists in the scene", root != null, $"found={(root != null)}");

            if (root != null)
            {
                yield return TestPickUpRealMesh(root, "Gym_WaterBottle_01");
                yield return TestHeavyCarryRealMesh(root, "Gym_Barbell_01");
                yield return TestPunchingBagSwing(root, "Gym_PunchingBag_01");
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 10 real-mesh Gym props work correctly with the existing physics systems."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage10DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage10DynamicTest] DONE");

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

            var player = BuildTestPlayer("Stage10Test_PickupPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            Check($"A real Gym prop mesh ({unityName}) can be picked up (Part 7.1)", picked, $"picked={picked}");

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
            Check($"{unityName} is classified Heavy (Part 4.7)",
                carryable != null && carryable.weightClass == CarryableObject.WeightClass.Heavy,
                $"weightClass={(carryable != null ? carryable.weightClass.ToString() : "none")}");
            if (carryable == null) yield break;

            var player = BuildTestPlayer("Stage10Test_HeavyPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            var loco = player.GetComponent<PlayerLocomotion>();
            Check("Carrying the real barbell solo applies the 30% speed factor (Part 5.1/7.1)",
                picked && Mathf.Approximately(loco.SpeedMultiplier, PlayerCarry.StandardHeavySpeedFactor),
                $"picked={picked} multiplier={loco.SpeedMultiplier:F3}");

            player.Drop();
            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestPunchingBagSwing(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var bag = propTransform.GetComponent<PunchingBagProp>();
            Check($"{unityName} has the real PunchingBagProp component (Part 4.7)", bag != null, $"found={(bag != null)}");
            if (bag == null) yield break;

            Check("The real punching bag is anchored via a ConfigurableJoint (Part 4.7)",
                bag.Joint != null && bag.Joint.connectedBody == null, $"found={(bag.Joint != null)}");

            var body = propTransform.GetComponent<Rigidbody>();
            Quaternion initialRotation = propTransform.rotation;

            // A hard sideways hit at chest height - well off the hanging pivot at the
            // top, so it should swing (rotate about the anchor), not just translate.
            body.AddForceAtPosition(Vector3.forward * 15f, propTransform.position, ForceMode.Impulse);

            float elapsed = 0f;
            float maxAngle = 0f;
            while (elapsed < 2f)
            {
                maxAngle = Mathf.Max(maxAngle, Quaternion.Angle(initialRotation, propTransform.rotation));
                elapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            Check("A hard hit makes the real punching bag swing on its suspension point (Part 4.7)",
                maxAngle > 2f, $"maxAngle={maxAngle:F2}deg elapsed={elapsed:F2}s");

            yield return null;
        }
    }
}
