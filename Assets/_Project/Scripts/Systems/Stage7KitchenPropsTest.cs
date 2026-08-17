using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 7's verification (static half already ran in
    /// Stage7KitchenImporter.RunStaticVerification before Play Mode was entered).
    /// Proves the real Kitchen meshes work with Stage 3's carry/wind system
    /// (including the project's first real Heavy-carryable prop) and the new
    /// ImpactReactionProp behavior on a real mesh.
    /// </summary>
    public class Stage7KitchenPropsTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage7_DynamicPropsTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 7 - Dynamic Kitchen Props Test (real meshes, Play Mode) ===");
            report.AppendLine();

            var root = GameObject.Find(Stage7KitchenPropSpec.KitchenPropsRootName);
            Check("Setup: Kitchen props root exists in the scene", root != null, $"found={(root != null)}");

            if (root != null)
            {
                yield return TestPickUpRealMesh(root, "Kitchen_Apple_01");
                yield return TestHeavyCarryRealMesh(root, "Kitchen_Watermelon_01");
                yield return TestImpactReactionRealMesh(root, "Kitchen_FlourBag_01");
                yield return TestLeakOnAnyLandingRealMesh(root, "Kitchen_MilkCarton_01");
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 7 real-mesh Kitchen props work correctly with the existing physics systems."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage7DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage7DynamicTest] DONE");

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

            var player = BuildTestPlayer("Stage7Test_PickupPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            Check($"A real Kitchen prop mesh ({unityName}) can be picked up (Part 7.1)", picked, $"picked={picked}");

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
            Check($"{unityName} is classified Heavy (Part 4.3's first real Heavy prop)",
                carryable != null && carryable.weightClass == CarryableObject.WeightClass.Heavy,
                $"weightClass={(carryable != null ? carryable.weightClass.ToString() : "none")}");
            if (carryable == null) yield break;

            var player = BuildTestPlayer("Stage7Test_HeavyPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            var loco = player.GetComponent<PlayerLocomotion>();
            Check($"Carrying the real watermelon solo applies the 30% speed factor (Part 5.1/7.1)",
                picked && Mathf.Approximately(loco.SpeedMultiplier, PlayerCarry.StandardHeavySpeedFactor),
                $"picked={picked} multiplier={loco.SpeedMultiplier:F3}");

            player.Drop();
            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestImpactReactionRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var reaction = propTransform.GetComponent<ImpactReactionProp>();
            Check($"{unityName} has an ImpactReactionProp (Explode)",
                reaction != null && reaction.reactionType == ImpactReactionProp.ReactionType.Explode,
                $"found={(reaction != null)}");
            if (reaction == null) yield break;

            var impactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactor.transform.localScale = Vector3.one * 0.2f;
            impactor.transform.position = propTransform.position + new Vector3(0f, 0.05f, -0.5f);
            var body = impactor.AddComponent<Rigidbody>();
            body.mass = 5f;
            body.linearVelocity = Vector3.forward * 7f; // > ImpactReactionProp.HardImpactThreshold (5 m/s)

            float elapsed = 0f;
            while (!reaction.Triggered && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }

            Check($"A hard impact makes the real flour bag explode (Part 4.3)", reaction.Triggered,
                $"triggered={reaction.Triggered} elapsed={elapsed:F2}s");

            Destroy(impactor);
            yield return null;
        }

        private IEnumerator TestLeakOnAnyLandingRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var reaction = propTransform.GetComponent<ImpactReactionProp>();
            Check($"{unityName} has an ImpactReactionProp (Leak)",
                reaction != null && reaction.reactionType == ImpactReactionProp.ReactionType.Leak,
                $"found={(reaction != null)}");
            if (reaction == null) yield break;

            // A gentle bump well under the 5 m/s hard-impact threshold - Leak has no
            // "قوي" qualifier in Part 4.3, so even this should trigger it.
            var impactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactor.transform.localScale = Vector3.one * 0.15f;
            impactor.transform.position = propTransform.position + new Vector3(0f, 0.02f, -0.15f);
            var body = impactor.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.linearVelocity = Vector3.forward * 0.5f;

            float elapsed = 0f;
            while (!reaction.Triggered && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }

            Check($"Even a gentle landing (no 'قوي' qualifier) makes the milk carton leak (Part 4.3)", reaction.Triggered,
                $"triggered={reaction.Triggered} elapsed={elapsed:F2}s");

            Destroy(impactor);
            yield return null;
        }
    }
}
