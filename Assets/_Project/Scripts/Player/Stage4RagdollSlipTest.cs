using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Runtime (Play Mode) verification of Stage 4 (Part 7.3 ragdoll + Part 7.4 slip
    /// traps). Same self-contained-rig philosophy as Stages 1-3's tests: builds its
    /// own throwaway player rigs, obstacles, and slip zones rather than relying on
    /// the "real" Player/placeholders Stage4RagdollSlipSetup places in-scene.
    /// </summary>
    public class Stage4RagdollSlipTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage4_RagdollSlipTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 4 - Ragdoll & Slip Trap Test (Part 7.3 + 7.4) ===");
            report.AppendLine();

            var realPlayer = GameObject.Find("Player");
            if (realPlayer != null) realPlayer.SetActive(false);
            var scenePlaceholders = GameObject.Find("Stage4_RagdollSlipPlaceholders");
            if (scenePlaceholders != null) scenePlaceholders.SetActive(false);

            yield return TestFallHeightTriggersRagdoll();
            yield return TestDurationAndSettleStandUp();
            yield return TestReentrancyGuard();
            yield return TestCollisionTriggersRagdoll();
            yield return TestSlipZoneTriggersRagdollForAnyone();
            yield return TestPourMechanicSpawnsWorkingSlipZone();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 4 ragdoll & slip systems match Part 7.3 / Part 7.4."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage4Test] Report written to " + reportPath);
            Debug.Log(report.ToString());

            if (realPlayer != null) realPlayer.SetActive(true);
            if (scenePlaceholders != null) scenePlaceholders.SetActive(true);

            yield return null;
            Debug.Log("[Stage4Test] DONE");

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

        private (PlayerLocomotion loco, PlayerCarry carry, PlayerRagdoll ragdoll) BuildTestPlayer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position; // set BEFORE AddComponent so Awake() sees the right start height
            go.transform.rotation = Quaternion.identity;

            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            var ragdoll = go.AddComponent<PlayerRagdoll>();
            return (loco, carry, ragdoll);
        }

        private IEnumerator TestFallHeightTriggersRagdoll()
        {
            var (loco, _, ragdoll) = BuildTestPlayer("Stage4_TestFall", new Vector3(45f, 2.0f, 10f));
            yield return null;

            float elapsed = 0f;
            while (!ragdoll.IsRagdolled && elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Falling more than 1.5m triggers full ragdoll (Part 7.3)", ragdoll.IsRagdolled,
                $"isRagdolled={ragdoll.IsRagdolled} elapsed={elapsed:F2}s");

            // Let it finish and clean up before the next subtest reuses this space.
            float cleanupElapsed = 0f;
            while (ragdoll.IsRagdolled && cleanupElapsed < 8f)
            {
                cleanupElapsed += Time.deltaTime;
                yield return null;
            }
            Destroy(ragdoll.gameObject);
            yield return null;
        }

        private IEnumerator TestDurationAndSettleStandUp()
        {
            var (loco, _, ragdoll) = BuildTestPlayer("Stage4_TestDuration", new Vector3(45f, 0.05f, 10f));
            yield return null;

            ragdoll.TriggerRagdoll();
            Check("TriggerRagdoll() enters the ragdoll state immediately", ragdoll.IsRagdolled, $"isRagdolled={ragdoll.IsRagdolled}");
            Check("Player control is suspended while ragdolled (Part 7.3)", loco.IsExternallyControlled, $"isExternallyControlled={loco.IsExternallyControlled}");

            float elapsed = 0f;
            while (ragdoll.IsRagdolled && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Ragdoll lasts at least the 2s minimum duration (Part 7.3)", elapsed >= PlayerRagdoll.MinDurationSeconds - 0.05f,
                $"elapsed={elapsed:F2}s min={PlayerRagdoll.MinDurationSeconds}");
            Check("Ragdoll ends and returns control within a reasonable time", !ragdoll.IsRagdolled && !loco.IsExternallyControlled,
                $"isRagdolled={ragdoll.IsRagdolled} isExternallyControlled={loco.IsExternallyControlled} elapsed={elapsed:F2}s");

            Destroy(ragdoll.gameObject);
            yield return null;
        }

        private IEnumerator TestReentrancyGuard()
        {
            var (_, _, ragdoll) = BuildTestPlayer("Stage4_TestReentrancy", new Vector3(45f, 0.05f, 10f));
            yield return null;

            ragdoll.TriggerRagdoll();
            ragdoll.TriggerRagdoll();
            ragdoll.TriggerRagdoll();
            yield return null;

            Check("Triggering an already-ragdolled player is a no-op (no restart storm)", ragdoll.TriggerCount == 1,
                $"triggerCount={ragdoll.TriggerCount}");

            float elapsed = 0f;
            while (ragdoll.IsRagdolled && elapsed < 8f) { elapsed += Time.deltaTime; yield return null; }
            Destroy(ragdoll.gameObject);
            yield return null;
        }

        private IEnumerator TestCollisionTriggersRagdoll()
        {
            var (loco, _, ragdoll) = BuildTestPlayer("Stage4_TestCollision", new Vector3(45f, 0.05f, 10f));
            loco.SetCameraYaw(0f); // facing world +Z

            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "Stage4Test_Obstacle";
            obstacle.transform.position = new Vector3(45f, 0.5f, 12f); // ~2m ahead
            obstacle.transform.localScale = Vector3.one * 0.5f;
            var rb = obstacle.AddComponent<Rigidbody>();
            rb.mass = 5f;
            rb.useGravity = false; // stays put; the player supplies all the relative speed
            rb.linearDamping = 5f; // resists being shoved far away once hit, keeps the test tidy

            loco.SetSprint(true);
            loco.SetMoveInput(Vector2.up); // run north at 6 m/s toward the obstacle

            float elapsed = 0f;
            while (!ragdoll.IsRagdolled && elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Colliding at over 5 m/s relative speed triggers ragdoll (Part 7.3)", ragdoll.IsRagdolled,
                $"isRagdolled={ragdoll.IsRagdolled} elapsed={elapsed:F2}s");

            loco.SetMoveInput(Vector2.zero);
            loco.SetSprint(false);

            float cleanupElapsed = 0f;
            while (ragdoll.IsRagdolled && cleanupElapsed < 8f) { cleanupElapsed += Time.deltaTime; yield return null; }
            Destroy(ragdoll.gameObject);
            Destroy(obstacle);
            yield return null;
        }

        private IEnumerator TestSlipZoneTriggersRagdollForAnyone()
        {
            var (_, _, ragdoll) = BuildTestPlayer("Stage4_TestSlip", new Vector3(50f, 0.05f, 10f));
            yield return null;

            var zoneGo = new GameObject("Stage4Test_SlipZone");
            zoneGo.transform.position = new Vector3(52f, 0.05f, 10f);
            zoneGo.AddComponent<SlipZone>();
            yield return null;

            ragdoll.transform.position = zoneGo.transform.position; // walk straight onto it
            yield return null; // let the physics step register the trigger overlap
            yield return new WaitForFixedUpdate();
            yield return null;

            Check("Entering a slip zone triggers ragdoll, no exemption for anyone (Part 7.4)", ragdoll.IsRagdolled,
                $"isRagdolled={ragdoll.IsRagdolled}");

            float elapsed = 0f;
            while (ragdoll.IsRagdolled && elapsed < 8f) { elapsed += Time.deltaTime; yield return null; }
            Destroy(ragdoll.gameObject);
            Destroy(zoneGo);
            yield return null;
        }

        private IEnumerator TestPourMechanicSpawnsWorkingSlipZone()
        {
            var (loco, carry, ragdoll) = BuildTestPlayer("Stage4_TestPour", new Vector3(55f, 0.05f, 10f));
            yield return null;

            var pourable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pourable.name = "Stage4Test_Pourable";
            pourable.transform.position = carry.transform.position + new Vector3(0f, 0.15f, 1f);
            pourable.transform.localScale = Vector3.one * 0.2f;
            var carryable = pourable.AddComponent<CarryableObject>();
            carryable.weightClass = CarryableObject.WeightClass.Light;
            carryable.isPourable = true;
            yield return null;

            carry.TryPickUpNearest();
            yield return null;
            Check("Setup: pourable object was picked up", carry.Held != null, $"held={(carry.Held != null)}");

            carry.SimulateInteractHeld = true;
            float pourElapsed = 0f;
            while (carry.PourCount == 0 && pourElapsed < PlayerCarry.PourHoldSeconds + 1f)
            {
                pourElapsed += Time.deltaTime;
                yield return null;
            }
            carry.SimulateInteractHeld = false;

            Check("Holding interact for 0.8s while carrying a pourable pours it (Part 7.4)",
                carry.PourCount == 1, $"pourCount={carry.PourCount} elapsed={pourElapsed:F2}s");
            Check("The pourable is consumed on pour", carry.Held == null, $"held={(carry.Held != null)}");

            var zone = FindFirstObjectByType<SlipZone>();
            Check("A slip zone exists after pouring", zone != null, $"zoneFound={(zone != null)}");

            if (zone != null)
            {
                ragdoll.transform.position = zone.transform.position; // the pourer walks back onto their own trap
                yield return null;
                yield return new WaitForFixedUpdate();
                yield return null;

                Check("The pourer is not exempt from their own trap (Part 7.4)", ragdoll.IsRagdolled,
                    $"isRagdolled={ragdoll.IsRagdolled}");
            }

            float elapsed = 0f;
            while (ragdoll.IsRagdolled && elapsed < 8f) { elapsed += Time.deltaTime; yield return null; }
            Destroy(ragdoll.gameObject);
            if (zone != null) Destroy(zone.gameObject);
            yield return null;
        }
    }
}
