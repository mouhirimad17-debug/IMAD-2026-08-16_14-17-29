using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 6's verification (the static half - prefab existence,
    /// Law 0.3 dimension accuracy, Part 4.1 classification components - already ran
    /// in Stage6FoyerImporter.RunStaticVerification before Play Mode was entered).
    /// This proves the real imported meshes (convex hull colliders included) actually
    /// work with the physics systems built in Stages 3/4, not just the placeholder
    /// primitive cubes those stages' own tests used.
    /// </summary>
    public class Stage6FoyerPropsTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage6_DynamicPropsTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 6 - Dynamic Foyer Props Test (real meshes, Play Mode) ===");
            report.AppendLine();

            var root = GameObject.Find(Stage6FoyerPropSpec.FoyerPropsRootName);
            Check("Setup: Foyer props root exists in the scene", root != null, $"found={(root != null)}");

            if (root != null)
            {
                yield return TestPickUpRealMesh(root, "Foyer_ChampagneGlass_01");
                yield return TestFallableRealMesh(root, "Foyer_CoffeeTable_01");
                yield return TestFireSourceRealMesh(root, "Foyer_CandelabraStanding_01");
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 6 real-mesh props work correctly with the existing physics systems."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage6DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage6DynamicTest] DONE");

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

            var carryable = propTransform.GetComponent<CarryableObject>();
            Check($"{unityName} has a CarryableObject (real mesh + convex collider)", carryable != null, $"found={(carryable != null)}");
            if (carryable == null) yield break;

            var player = BuildTestPlayer("Stage6Test_PickupPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            player.transform.rotation = Quaternion.LookRotation(propTransform.position - player.transform.position, Vector3.up);
            yield return null;

            bool picked = player.TryPickUpNearest();
            yield return null;

            Check($"A real prop mesh ({unityName}) can actually be picked up (Part 7.1)",
                picked && player.Held == carryable, $"picked={picked} held={(player.Held == carryable)}");

            player.Drop();
            Destroy(player.gameObject);
            yield return null;
        }

        private IEnumerator TestFallableRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var fallable = propTransform.GetComponent<FallableProp>();
            var body = propTransform.GetComponent<Rigidbody>();
            Check($"{unityName} has a FallableProp + Rigidbody (real mesh + convex collider)",
                fallable != null && body != null, $"fallable={(fallable != null)} body={(body != null)}");
            if (fallable == null || body == null) yield break;

            Check($"{unityName} starts kinematic (stands still until hit hard)", body.isKinematic, $"isKinematic={body.isKinematic}");

            var impactor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impactor.name = "Stage6Test_Impactor";
            impactor.transform.localScale = Vector3.one * 0.3f;
            impactor.transform.position = propTransform.position + new Vector3(0f, 0.1f, -1f);
            var impactorBody = impactor.AddComponent<Rigidbody>();
            impactorBody.mass = 8f;
            impactorBody.linearVelocity = Vector3.forward * 7f; // > FallableProp.TipCollisionSpeedThreshold (5 m/s)

            float elapsed = 0f;
            while (body.isKinematic && elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check($"A hard impact (>5 m/s) tips a real Fallable prop over (Part 4.1)", !body.isKinematic,
                $"isKinematic={body.isKinematic} elapsed={elapsed:F2}s");

            Destroy(impactor);
            yield return null;
        }

        private IEnumerator TestFireSourceRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var fireSource = propTransform.GetComponent<FireSource>();
            var fallable = propTransform.GetComponent<FallableProp>();
            Check($"{unityName} has both FireSource and FallableProp (Part 4.2's fire-source classification)",
                fireSource != null && fallable != null, $"fireSource={(fireSource != null)} fallable={(fallable != null)}");
            if (fireSource == null) yield break;

            Check($"{unityName} is registered in FireSource.Active", FireSource.Active.Contains(fireSource),
                $"registered={FireSource.Active.Contains(fireSource)}");

            // Prove a wind-active carrier actually ignites against this REAL prop's
            // position, not just a synthetic test cube (Stage 3's own test used one).
            var player = BuildTestPlayer("Stage6Test_IgnitePlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            yield return null;
            var heavy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heavy.transform.position = player.transform.position + new Vector3(0f, 0.35f, 1f);
            heavy.transform.localScale = Vector3.one * 0.6f;
            var heavyCarry = heavy.AddComponent<CarryableObject>();
            heavyCarry.weightClass = CarryableObject.WeightClass.Heavy;
            yield return null;

            player.TryPickUpNearest();
            yield return new WaitForSeconds(PlayerCarry.StandardWindStartDelay + 0.3f);
            Check("Setup: wind active before approaching the real fire source", player.WindActive, $"windActive={player.WindActive}");

            player.transform.position = propTransform.position + new Vector3(0f, 0f, -0.5f); // within 1m
            yield return null;

            Check("Wind ignites against a real Foyer fire-source prop (Part 7.2)", !player.WindActive, $"windActive={player.WindActive}");

            Destroy(player.gameObject);
            if (heavy != null) Destroy(heavy);
            yield return null;
        }
    }
}
