using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Runtime (Play Mode) verification of Stage 2 (Part 14 + Part 5.1). Builds its
    /// own throwaway CharacterController/PlayerLocomotion/PlayerCameraRig rig (kept
    /// independent of the "real" Player prefab the Editor setup places in the scene,
    /// same self-contained-test philosophy as Stage 1's wall closure test) and
    /// drives it with synthetic input to measure real, physics-stepped results
    /// against every numeric value in Part 14 and Part 5.1.
    /// </summary>
    public class StageTwoMovementTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage2_MovementCameraTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            // Headless -batchmode -nographics has nothing to throttle the loop, so it
            // was free-running at ~2000fps. At that rate each frame's
            // CharacterController.Move() step is far smaller than the controller's
            // default 0.08m collision skin width, which made per-frame displacement
            // unreliable. Cap to a realistic frame rate so the physics step size is
            // sane - this also makes the test more representative of real gameplay
            // conditions, not just a workaround.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 2 - Movement & Camera Test (Part 14 + Part 5.1) ===");
            report.AppendLine();

            // The "real" gameplay Player that PlayerStageTwoSetup places in the scene
            // sits in the same open foyer area this test uses. Disable it for the
            // test's duration so its collider can't block the test player's measured
            // runs - it plays no part in this automated check, which builds and
            // drives its own isolated rig.
            var realPlayer = GameObject.Find("Player");
            if (realPlayer != null) realPlayer.SetActive(false);

            var playerGo = new GameObject("StageTwo_TestPlayer");
            var controller = playerGo.AddComponent<CharacterController>();
            controller.height = 1.0f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var locomotion = playerGo.AddComponent<PlayerLocomotion>();

            var camGo = new GameObject("StageTwo_TestCamera");
            var rig = camGo.AddComponent<PlayerCameraRig>();
            rig.target = playerGo.transform;

            playerGo.transform.position = new Vector3(45f, 0.05f, 18f);
            yield return null; // let CharacterController settle onto the ground slab

            yield return TestWalkSpeed(locomotion, playerGo);
            yield return TestRunSpeed(locomotion, playerGo);
            yield return TestJumpHeight(locomotion, playerGo);
            TestCameraDefaults(rig, playerGo);
            TestPitchClamp(rig);
            TestInstantRotation(rig);
            yield return TestObstacleAvoidance(rig, playerGo, camGo);

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 2 movement & camera match Part 14 / Part 5.1."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage2Test] Report written to " + reportPath);
            Debug.Log(report.ToString());

            if (realPlayer != null) realPlayer.SetActive(true);
            Object.Destroy(playerGo);
            Object.Destroy(camGo);

            yield return null;
            Debug.Log("[Stage2Test] DONE");

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

        // West strip (X=30) is clear of the Stage 1 staircase footprint (X[36,54] Z[24,36])
        // entirely; starting near the front (Z=1) and walking toward +Z leaves the
        // full ~34.9m to the foyer/corridor divider wall (Z~35.9) free to move in.
        private static readonly Vector3 MovementTestSpawn = new Vector3(30f, 0.05f, 1f);
        private const float MovementTestDuration = 1.2f; // walk needs 3.6m, run needs 7.2m - both well clear

        private IEnumerator TestWalkSpeed(PlayerLocomotion loco, GameObject player)
        {
            player.transform.position = MovementTestSpawn;
            yield return null;
            loco.SetCameraYaw(0f);
            loco.SetSprint(false);
            loco.SetMoveInput(Vector2.up);

            Vector3 start = player.transform.position;
            yield return new WaitForSeconds(MovementTestDuration);
            loco.SetMoveInput(Vector2.zero);

            float dist = Vector3.Distance(
                new Vector3(start.x, 0, start.z),
                new Vector3(player.transform.position.x, 0, player.transform.position.z));
            float measuredSpeed = dist / MovementTestDuration;

            Check("Walk speed (Part 5.1: 3 m/s)", Mathf.Abs(measuredSpeed - PlayerLocomotion.WalkSpeed) < 0.15f,
                $"measured={measuredSpeed:F3} m/s expected={PlayerLocomotion.WalkSpeed}");
        }

        private IEnumerator TestRunSpeed(PlayerLocomotion loco, GameObject player)
        {
            player.transform.position = MovementTestSpawn;
            yield return null;
            loco.SetCameraYaw(0f);
            loco.SetSprint(true);
            loco.SetMoveInput(Vector2.up);

            Vector3 start = player.transform.position;
            yield return new WaitForSeconds(MovementTestDuration);
            loco.SetMoveInput(Vector2.zero);
            loco.SetSprint(false);

            float dist = Vector3.Distance(
                new Vector3(start.x, 0, start.z),
                new Vector3(player.transform.position.x, 0, player.transform.position.z));
            float measuredSpeed = dist / MovementTestDuration;

            Check("Run speed (Part 5.1: 6 m/s)", Mathf.Abs(measuredSpeed - PlayerLocomotion.RunSpeed) < 0.2f,
                $"measured={measuredSpeed:F3} m/s expected={PlayerLocomotion.RunSpeed}");
        }

        private IEnumerator TestJumpHeight(PlayerLocomotion loco, GameObject player)
        {
            player.transform.position = new Vector3(45f, 0.05f, 18f);
            yield return null;
            loco.SetMoveInput(Vector2.zero);

            float groundY = player.transform.position.y;
            float peak = groundY;
            loco.RequestJump();

            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                if (player.transform.position.y > peak) peak = player.transform.position.y;
                elapsed += Time.deltaTime;
                yield return null;
            }

            float measuredHeight = peak - groundY;
            Check("Jump apex height (Part 5.1: ~1.2 m)", Mathf.Abs(measuredHeight - PlayerLocomotion.JumpHeight) < 0.1f,
                $"measured={measuredHeight:F3} m expected={PlayerLocomotion.JumpHeight}");
        }

        private void TestCameraDefaults(PlayerCameraRig rig, GameObject player)
        {
            player.transform.position = new Vector3(45f, 0.05f, 18f); // open foyer middle, clear of every wall
            for (int i = 0; i < 5; i++) rig.ApplyCamera(0.02f);

            Check("Default camera distance (Part 14.1: ~3.5m)", Mathf.Abs(rig.CurrentDistance - PlayerCameraRig.DefaultDistance) < 0.05f,
                $"distance={rig.CurrentDistance:F3}");
            Check("Default camera pitch (Part 14.1: ~-15deg tilt)", Mathf.Abs(rig.Pitch - PlayerCameraRig.DefaultPitch) < 0.01f,
                $"pitch={rig.Pitch:F3}");
            Check("Camera pivot height (Part 14.1: 1.5m above player)",
                Mathf.Abs(rig.Pivot.y - (player.transform.position.y + 1.5f)) < 0.01f,
                $"pivot.y={rig.Pivot.y:F3} player.y={player.transform.position.y:F3}");
        }

        private void TestPitchClamp(PlayerCameraRig rig)
        {
            for (int i = 0; i < 200; i++) rig.SetLookDelta(new Vector2(0f, 10000f));
            Check("Pitch upper clamp (Part 14.2: +50deg)", Mathf.Approximately(rig.Pitch, PlayerCameraRig.MaxPitch),
                $"pitch={rig.Pitch:F3}");

            for (int i = 0; i < 200; i++) rig.SetLookDelta(new Vector2(0f, -10000f));
            Check("Pitch lower clamp (Part 14.2: -20deg)", Mathf.Approximately(rig.Pitch, PlayerCameraRig.MinPitch),
                $"pitch={rig.Pitch:F3}");
        }

        private void TestInstantRotation(PlayerCameraRig rig)
        {
            float before = rig.Yaw;
            float delta = 20f / PlayerCameraRig.MouseSensitivity; // exactly +20 degrees of yaw
            rig.SetLookDelta(new Vector2(delta, 0f));
            float expected = Mathf.Repeat(before + 20f, 360f);

            Check("Instant rotation response (Part 14.2: no smoothing/lag)",
                Mathf.Abs(Mathf.DeltaAngle(rig.Yaw, expected)) < 0.01f,
                $"yaw={rig.Yaw:F3} expected={expected:F3} (applied in a single call, no frames waited)");
        }

        private IEnumerator TestObstacleAvoidance(PlayerCameraRig rig, GameObject player, GameObject camGo)
        {
            // 0.5m clear of the front exterior wall's inner face (Z=0.3), looking
            // straight at it (yaw=0) - the ideal 3.5m camera position would be well
            // outside the mansion, through the wall.
            player.transform.position = new Vector3(45f, 0.05f, 0.8f);
            rig.SetLookDelta(new Vector2(-rig.Yaw / PlayerCameraRig.MouseSensitivity, 0f)); // reset yaw to 0
            for (int i = 0; i < 10; i++) rig.ApplyCamera(0.02f);
            yield return null;

            bool pulledIn = rig.CurrentDistance < 1.0f;
            bool stayedInside = camGo.transform.position.z > 0f; // front wall's outer face

            Check("Camera obstacle pull-in engages (Part 14.3)", pulledIn,
                $"distance={rig.CurrentDistance:F3} (expected well under {PlayerCameraRig.DefaultDistance})");
            Check("Camera does not clip through the wall (Part 14.3)", stayedInside,
                $"camera.z={camGo.transform.position.z:F3} (must stay > 0, the wall's outer face)");
        }
    }
}
