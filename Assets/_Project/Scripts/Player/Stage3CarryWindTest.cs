using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Runtime (Play Mode) verification of Stage 3 (Part 7.1 carry + Part 7.2 wind).
    /// Builds its own throwaway CharacterController/PlayerLocomotion/PlayerCarry rigs
    /// and CarryableObject/FireSource props (independent of the "real" Player and
    /// placeholder props Stage3CarryWindSetup places in-scene), same self-contained-
    /// test philosophy as Stage 1's wall closure test and Stage 2's movement test.
    /// </summary>
    public class Stage3CarryWindTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage3_CarryWindTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 3 - Carry & Wind System Test (Part 7.1 + 7.2) ===");
            report.AppendLine();

            var realPlayer = GameObject.Find("Player");
            if (realPlayer != null) realPlayer.SetActive(false);
            var scenePlaceholders = GameObject.Find("Stage3_CarryWindPlaceholders");
            if (scenePlaceholders != null) scenePlaceholders.SetActive(false);

            var playerA = BuildTestPlayer("Stage3_TestPlayerA", new Vector3(45f, 0.05f, 10f));
            var playerB = BuildTestPlayer("Stage3_TestPlayerB", new Vector3(60f, 0.05f, 10f));
            yield return null;

            yield return TestPickupRangeAndCone(playerA);
            yield return TestLightCarryNoSpeedPenalty(playerA);
            yield return TestHeavySoloCarrySpeedAndWindTimer(playerA);
            yield return TestDualCarryCancelsPenaltyAndWind(playerA, playerB);
            yield return TestIgnitionLaunch(playerA);

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 3 carry & wind systems match Part 7.1 / Part 7.2."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage3Test] Report written to " + reportPath);
            Debug.Log(report.ToString());

            if (realPlayer != null) realPlayer.SetActive(true);
            if (scenePlaceholders != null) scenePlaceholders.SetActive(true);

            yield return null;
            Debug.Log("[Stage3Test] DONE");

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

        private PlayerCarry BuildTestPlayer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            go.transform.position = position;
            go.transform.rotation = Quaternion.identity; // facing world +Z
            return carry;
        }

        private CarryableObject SpawnCarryable(string name, Vector3 position, CarryableObject.WeightClass weightClass)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.position = position;
            go.transform.localScale = Vector3.one * (weightClass == CarryableObject.WeightClass.Heavy ? 0.6f : 0.25f);
            var carry = go.AddComponent<CarryableObject>(); // RequireComponent adds the Rigidbody
            carry.weightClass = weightClass;
            return carry;
        }

        private FireSource SpawnFireSource(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stage3Test_FireSource";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);
            var collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider); // detection is a plain distance check, not a trigger
            return go.AddComponent<FireSource>();
        }

        private IEnumerator TestPickupRangeAndCone(PlayerCarry player)
        {
            Vector3 basePos = player.transform.position;
            player.transform.rotation = Quaternion.identity;

            var farObj = SpawnCarryable("Stage3Test_Far", basePos + new Vector3(0f, 0.15f, 3f), CarryableObject.WeightClass.Light);
            yield return null;
            bool pickedFar = player.TryPickUpNearest();
            Check("Pickup range rejects a target beyond 2m (Part 7.1)", !pickedFar, $"held={(player.Held != null)}");
            Destroy(farObj.gameObject);
            yield return null;

            var sideObj = SpawnCarryable("Stage3Test_Side", basePos + new Vector3(1.5f, 0.15f, 0f), CarryableObject.WeightClass.Light);
            yield return null;
            bool pickedSide = player.TryPickUpNearest();
            Check("Pickup cone rejects a target beyond 45deg off facing (Part 7.1)", !pickedSide, $"held={(player.Held != null)}");
            Destroy(sideObj.gameObject);
            yield return null;

            var goodObj = SpawnCarryable("Stage3Test_InCone", basePos + new Vector3(0f, 0.15f, 1.5f), CarryableObject.WeightClass.Light);
            yield return null;
            bool pickedGood = player.TryPickUpNearest();
            Check("Pickup succeeds within 2m range and 45deg cone (Part 7.1)", pickedGood, $"held={(player.Held != null)}");
            player.Drop();
            Destroy(goodObj.gameObject);
            yield return null;
        }

        private IEnumerator TestLightCarryNoSpeedPenalty(PlayerCarry player)
        {
            var obj = SpawnCarryable("Stage3Test_Light", player.transform.position + new Vector3(0f, 0.15f, 1f), CarryableObject.WeightClass.Light);
            yield return null;
            player.TryPickUpNearest();
            yield return null;

            var loco = player.GetComponent<PlayerLocomotion>();
            Check("Light carry applies no speed penalty (Part 7.1: 100% speed)",
                Mathf.Approximately(loco.SpeedMultiplier, 1f), $"multiplier={loco.SpeedMultiplier:F3}");

            player.Drop();
            Destroy(obj.gameObject);
            yield return null;
        }

        private IEnumerator TestHeavySoloCarrySpeedAndWindTimer(PlayerCarry player)
        {
            var obj = SpawnCarryable("Stage3Test_Heavy", player.transform.position + new Vector3(0f, 0.35f, 1f), CarryableObject.WeightClass.Heavy);
            yield return null;
            player.TryPickUpNearest();
            yield return null;

            var loco = player.GetComponent<PlayerLocomotion>();
            Check("Heavy solo carry applies the 30% speed factor (Part 5.1/7.1)",
                Mathf.Approximately(loco.SpeedMultiplier, PlayerCarry.StandardHeavySpeedFactor),
                $"multiplier={loco.SpeedMultiplier:F3} expected={PlayerCarry.StandardHeavySpeedFactor}");

            Check("Wind has not started immediately after pickup (Part 7.1: 1s delay)",
                !player.WindActive, $"windActive={player.WindActive}");

            yield return new WaitForSeconds(PlayerCarry.StandardWindStartDelay + 0.3f);

            Check("Wind starts after the 1s delay (Part 7.1)", player.WindActive, $"windActive={player.WindActive}");

            float expectedPush = PlayerLocomotion.RunSpeed * PlayerCarry.WindPushFraction;
            Check("Wind push magnitude matches 10% of run speed (Part 7.2)",
                Mathf.Abs(loco.ExternalVelocity.magnitude - expectedPush) < 0.01f,
                $"push={loco.ExternalVelocity.magnitude:F3} expected={expectedPush:F3}");

            player.Drop();
            Destroy(obj.gameObject);
            yield return null;

            Check("Dropping the heavy object stops the wind (Part 7.2)", !player.WindActive, $"windActive={player.WindActive}");
            Check("Dropping the heavy object restores full speed (Part 7.1)",
                Mathf.Approximately(loco.SpeedMultiplier, 1f), $"multiplier={loco.SpeedMultiplier:F3}");
        }

        private IEnumerator TestDualCarryCancelsPenaltyAndWind(PlayerCarry playerA, PlayerCarry playerB)
        {
            var locoA = playerA.GetComponent<PlayerLocomotion>();
            var locoB = playerB.GetComponent<PlayerLocomotion>();

            var obj = SpawnCarryable("Stage3Test_HeavyDual", playerA.transform.position + new Vector3(0f, 0.35f, 1f), CarryableObject.WeightClass.Heavy);
            yield return null;
            playerA.TryPickUpNearest();
            yield return new WaitForSeconds(PlayerCarry.StandardWindStartDelay + 0.3f);
            Check("Setup: wind is active before the second carrier joins", playerA.WindActive, $"windActive={playerA.WindActive}");

            playerB.transform.position = obj.transform.position + new Vector3(0.5f, 0f, 0f);
            playerB.transform.rotation = Quaternion.LookRotation(obj.transform.position - playerB.transform.position, Vector3.up);
            yield return null;
            bool joined = playerB.TryPickUpNearest();
            yield return null;

            Check("Second carrier joins a solo-held heavy object within 1.5m (Part 7.1)", joined, $"joined={joined}");
            Check("Both carriers return to 100% speed on joint carry (Part 7.1)",
                Mathf.Approximately(locoA.SpeedMultiplier, 1f) && Mathf.Approximately(locoB.SpeedMultiplier, 1f),
                $"A={locoA.SpeedMultiplier:F3} B={locoB.SpeedMultiplier:F3}");
            Check("Wind stops immediately when the second carrier joins (Part 7.1)",
                !playerA.WindActive, $"windActive={playerA.WindActive}");

            playerA.Drop();
            Destroy(obj.gameObject);
            playerB.transform.position = new Vector3(60f, 0.05f, 10f);
            yield return null;
        }

        private IEnumerator TestIgnitionLaunch(PlayerCarry player)
        {
            var loco = player.GetComponent<PlayerLocomotion>();
            player.transform.position = new Vector3(45f, 0.05f, 10f);
            player.transform.rotation = Quaternion.identity;
            yield return null;

            var obj = SpawnCarryable("Stage3Test_HeavyIgnite", player.transform.position + new Vector3(0f, 0.35f, 1f), CarryableObject.WeightClass.Heavy);
            yield return null;
            player.TryPickUpNearest();
            yield return new WaitForSeconds(PlayerCarry.StandardWindStartDelay + 0.3f);
            Check("Setup: wind active before ignition", player.WindActive, $"windActive={player.WindActive}");

            int ignitesBefore = player.IgniteCount;
            var fire = SpawnFireSource(player.transform.position + new Vector3(0f, 0.6f, 0.5f)); // well within 1m
            yield return null; // ignition check runs in Update

            Check("Fire proximity triggers ignition (Part 7.2: 1m radius)", player.IgniteCount == ignitesBefore + 1,
                $"igniteCount={player.IgniteCount}");
            Check("Ignition stops the wind immediately", !player.WindActive, $"windActive={player.WindActive}");
            Check("Ignition drops the carried object", player.Held == null, $"held={(player.Held != null)}");
            Check("Ignition launches the player into free flight (Part 7.2)", loco.IsFreeFlight, $"isFreeFlight={loco.IsFreeFlight}");

            float expectedHoriz = PlayerCarry.RocketForce * PlayerCarry.StandardLaunchHorizontalFraction;
            float expectedVert = PlayerCarry.RocketForce * PlayerCarry.StandardLaunchVerticalFraction;
            Vector3 launch = loco.LaunchVelocitySnapshot;
            float horizMag = new Vector3(launch.x, 0f, launch.z).magnitude;
            Check("Launch force splits 20% horizontal / 80% vertical (Part 5.2 standard)",
                Mathf.Abs(launch.y - expectedVert) < 0.01f && Mathf.Abs(horizMag - expectedHoriz) < 0.01f,
                $"vertical={launch.y:F3} expectedVertical={expectedVert:F3} horizontal={horizMag:F3} expectedHorizontal={expectedHoriz:F3}");

            float startY = player.transform.position.y;
            float elapsed = 0f;
            float peakY = startY;
            while (loco.IsFreeFlight && elapsed < 5f)
            {
                if (player.transform.position.y > peakY) peakY = player.transform.position.y;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Player gains height during the launch", peakY > startY + 0.3f, $"peakY={peakY:F3} startY={startY:F3}");
            Check("Player lands and free flight ends within a reasonable time", !loco.IsFreeFlight,
                $"isFreeFlight={loco.IsFreeFlight} elapsed={elapsed:F2}s");

            Destroy(fire.gameObject);
            Destroy(obj.gameObject);
            yield return null;
        }
    }
}
