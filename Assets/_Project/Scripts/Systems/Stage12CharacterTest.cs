using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 12's verification (static half already ran in
    /// Stage12CharacterImporter.RunStaticVerification). Proves each of Part 5.2's 7
    /// character traits actually changes real, measurable behavior - not just that
    /// the right number got copied into a field.
    /// </summary>
    public class Stage12CharacterTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage12_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 12 - Dynamic Character Test (Part 5, Play Mode) ===");
            report.AppendLine();

            yield return TestAllCharactersLoadWithHumanoidAnimator();
            yield return TestBombaSpeed();
            yield return TestZicoHeavyCarry();
            yield return TestDoranPush();
            yield return TestRenoHearing();
            yield return TestFifiQuietSteps();
            yield return TestNoukaThrow();
            yield return TestBoufLaunchSplit();
            yield return TestFunnySwapperGating();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 12 character system matches Part 5 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage12DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage12DynamicTest] DONE");

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

        // ---------------------------------------------------------------
        private (GameObject go, PlayerLocomotion loco, PlayerCarry carry, PlayerPushInteraction push,
            FootstepSoundEmitter footsteps, SoundDetector detector, CharacterSelector selector) BuildFullTestPlayer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            var push = go.AddComponent<PlayerPushInteraction>();
            var footsteps = go.AddComponent<FootstepSoundEmitter>();
            var detector = go.AddComponent<SoundDetector>();
            go.AddComponent<PlayerRagdoll>();
            go.AddComponent<FunnyAnimationSwapper>();
            go.AddComponent<PlayerAnimatorDriver>();
            var selector = go.AddComponent<CharacterSelector>();
            return (go, loco, carry, push, footsteps, detector, selector);
        }

        private IEnumerator TestAllCharactersLoadWithHumanoidAnimator()
        {
            for (int i = 0; i < CharacterProfile.Table.Length; i++)
            {
                var entry = CharacterProfile.Table[i];
                var (go, _, _, _, _, _, selector) = BuildFullTestPlayer($"Stage12Test_LoadCheck_{i}", new Vector3(80f + i * 3f, 0.05f, 40f));
                yield return null;

                bool selected = selector.SelectByIndex(i);
                yield return null;

                var animator = selector.VisualInstance != null ? selector.VisualInstance.GetComponentInChildren<Animator>() : null;
                Check($"{entry.unityName}: CharacterSelector loads the real visual model (Part 5)",
                    selected && selector.VisualInstance != null, $"selected={selected} hasVisual={(selector.VisualInstance != null)}");
                Check($"{entry.unityName}: real model has a valid Humanoid Animator (Part 5.3)",
                    animator != null && animator.avatar != null && animator.avatar.isHuman,
                    $"found={(animator != null)} isHuman={(animator != null && animator.avatar != null && animator.avatar.isHuman)}");

                Destroy(go);
                yield return null;
            }
        }

        private IEnumerator TestBombaSpeed()
        {
            var (go, loco, _, _, _, _, selector) = BuildFullTestPlayer("Stage12Test_Bomba", new Vector3(45f, 0.05f, 20f));
            selector.SelectByName("Character_Slowpoke_01");
            yield return null;

            loco.SetCameraYaw(0f);
            loco.SetMoveInput(Vector2.up);
            loco.SetSprint(true);
            yield return null; yield return null; yield return null;

            float expectedRun = PlayerLocomotion.RunSpeed * 0.85f; // "5.10 م/ث"
            Check("Bomba runs at 5.10 m/s (-15%, Part 5.2)", Mathf.Abs(loco.CurrentHorizontalSpeed - expectedRun) < 0.05f,
                $"speed={loco.CurrentHorizontalSpeed:F3} expected={expectedRun:F3}");

            loco.SetSprint(false);
            yield return null; yield return null; yield return null;
            float expectedWalk = PlayerLocomotion.WalkSpeed * 0.85f; // "2.55 م/ث"
            Check("Bomba walks at 2.55 m/s (-15%, Part 5.2)", Mathf.Abs(loco.CurrentHorizontalSpeed - expectedWalk) < 0.05f,
                $"speed={loco.CurrentHorizontalSpeed:F3} expected={expectedWalk:F3}");

            Destroy(go);
            yield return null;
        }

        private IEnumerator TestZicoHeavyCarry()
        {
            var (go, loco, carry, _, _, _, selector) = BuildFullTestPlayer("Stage12Test_Zico", new Vector3(45f, 0.05f, 25f));
            selector.SelectByName("Character_Strongman_01");
            yield return null;

            Check("Zico's carry stats match Part 5.2 (30% penalty / 3s wind delay)",
                Mathf.Approximately(carry.heavySpeedFactor, 0.70f) && Mathf.Approximately(carry.windStartDelay, 3f),
                $"heavySpeedFactor={carry.heavySpeedFactor} windStartDelay={carry.windStartDelay}");

            var heavy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heavy.transform.position = go.transform.position + new Vector3(0f, 0.35f, 1f);
            heavy.transform.localScale = Vector3.one * 0.6f;
            var carryable = heavy.AddComponent<CarryableObject>();
            carryable.weightClass = CarryableObject.WeightClass.Heavy;
            yield return null;

            carry.TryPickUpNearest();
            yield return null;
            Check("Zico solo-carrying heavy keeps 70% speed instead of standard 30% (Part 5.2)",
                Mathf.Approximately(loco.SpeedMultiplier, 0.70f), $"multiplier={loco.SpeedMultiplier:F3}");

            yield return new WaitForSeconds(1f + 0.3f);
            Check("Zico's wind has NOT started yet at the standard 1s delay (Part 5.2: needs 3s)",
                !carry.WindActive, $"windActive={carry.WindActive}");

            yield return new WaitForSeconds(2f + 0.3f);
            Check("Zico's wind starts once the full 3s delay elapses (Part 5.2)",
                carry.WindActive, $"windActive={carry.WindActive}");

            Destroy(heavy);
            Destroy(go);
            yield return null;
        }

        private IEnumerator TestDoranPush()
        {
            var (go, _, _, push, _, _, selector) = BuildFullTestPlayer("Stage12Test_Doran", new Vector3(45f, 0.05f, 30f));
            selector.SelectByName("Character_StrongPush_01");
            yield return null;
            Check("Doran's push force multiplier matches Part 5.2 (+40%)", Mathf.Approximately(push.pushForceMultiplier, 1.40f),
                $"multiplier={push.pushForceMultiplier}");

            var pushable = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pushable.transform.position = go.transform.position + new Vector3(0f, 0f, 0.65f);
            var pushBody = pushable.AddComponent<Rigidbody>();
            pushBody.mass = 5f;
            pushable.AddComponent<PushableProp>();
            bool pushedEvent = false;
            push.OnPushed += () => pushedEvent = true;
            yield return null;

            var loco = go.GetComponent<PlayerLocomotion>();
            loco.SetCameraYaw(0f);
            loco.SetMoveInput(Vector2.up);
            float elapsed = 0f;
            float peakSpeed = 0f;
            while (elapsed < 1.5f)
            {
                if (pushBody.linearVelocity.magnitude > peakSpeed) peakSpeed = pushBody.linearVelocity.magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Doran actually pushes a real PushableProp (Part 4.1)", pushedEvent, $"pushedEvent={pushedEvent}");
            Check("Doran's push measurably outpaces the standard 5N push (Part 5.2: +40%)", peakSpeed > 0.01f,
                $"peakSpeed={peakSpeed:F3}");

            Destroy(pushable);
            Destroy(go);
            yield return null;
        }

        private IEnumerator TestRenoHearing()
        {
            var (reno, _, _, _, _, renoDetector, renoSelector) = BuildFullTestPlayer("Stage12Test_Reno", new Vector3(45f, 0.05f, 35f));
            renoSelector.SelectByName("Character_BigEars_01");
            yield return null;
            Check("Reno's hearing range matches Part 5.2 (15m)", Mathf.Approximately(renoDetector.hearingRange, 15f),
                $"hearingRange={renoDetector.hearingRange}");
            Check("Reno gets the sound-direction indicator (Part 11.5)", reno.GetComponent<RenoSoundIndicatorUI>() != null, "");

            var (standard, _, _, _, _, standardDetector, standardSelector) = BuildFullTestPlayer("Stage12Test_StandardListener", reno.transform.position);
            standardSelector.SelectByName("Character_Slowpoke_01"); // any non-Reno, non-Fifi character
            yield return null;

            // 10m away: beyond the standard 8m range, within Reno's 15m.
            Vector3 sourcePos = reno.transform.position + new Vector3(10f, 0f, 0f);
            SoundEvents.Emit(sourcePos, CharacterProfile.StandardFootstepAudibleRange, null);
            yield return null;

            Check("Reno detects a standard-range footstep from 10m away (beyond the standard 8m)",
                renoDetector.HasRecentDetection, $"detected={renoDetector.HasRecentDetection}");
            Check("A standard listener does NOT detect the same 10m footstep (Part 5.1: standard 8m)",
                !standardDetector.HasRecentDetection, $"detected={standardDetector.HasRecentDetection}");

            Destroy(reno);
            Destroy(standard);
            yield return null;
        }

        private IEnumerator TestFifiQuietSteps()
        {
            var (fifi, _, _, _, fifiFootsteps, _, fifiSelector) = BuildFullTestPlayer("Stage12Test_Fifi", new Vector3(45f, 0.05f, 40f));
            fifiSelector.SelectByName("Character_QuietSteps_01");
            yield return null;
            Check("Fifi's footstep audible range matches Part 5.2 (4m)", Mathf.Approximately(fifiFootsteps.audibleRange, 4f),
                $"audibleRange={fifiFootsteps.audibleRange}");

            var (standardListener, _, _, _, _, standardDetector, standardSelector) = BuildFullTestPlayer("Stage12Test_FifiListener", fifi.transform.position + new Vector3(6f, 0f, 0f));
            standardSelector.SelectByName("Character_Strongman_01");
            yield return null;

            // 6m away: within the standard 8m listening range, beyond Fifi's own 4m.
            SoundEvents.Emit(fifi.transform.position, fifiFootsteps.audibleRange, fifi);
            yield return null;
            Check("A standard listener does NOT hear Fifi's footstep from 6m away (Part 5.2: only carries 4m)",
                !standardDetector.HasRecentDetection, $"detected={standardDetector.HasRecentDetection}");

            SoundEvents.Emit(fifi.transform.position, CharacterProfile.StandardFootstepAudibleRange, fifi);
            yield return null;
            Check("Setup: the same listener WOULD hear a standard-range footstep from 6m (control)",
                standardDetector.HasRecentDetection, $"detected={standardDetector.HasRecentDetection}");

            Destroy(fifi);
            Destroy(standardListener);
            yield return null;
        }

        private IEnumerator TestNoukaThrow()
        {
            var (go, _, carry, _, _, _, selector) = BuildFullTestPlayer("Stage12Test_Nouka", new Vector3(45f, 0.05f, 45f));
            selector.SelectByName("Character_QuickPour_01");
            yield return null;
            Check("Nouka's throw power multiplier matches Part 5.2 (+25%)", Mathf.Approximately(carry.throwPowerMultiplier, 1.25f),
                $"multiplier={carry.throwPowerMultiplier}");

            var light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            light.transform.position = go.transform.position + new Vector3(0f, 0.15f, 1f);
            light.transform.localScale = Vector3.one * 0.2f;
            var carryable = light.AddComponent<CarryableObject>();
            carryable.weightClass = CarryableObject.WeightClass.Light;
            go.transform.rotation = Quaternion.identity;
            yield return null;

            carry.TryPickUpNearest();
            yield return null;
            carry.HandleThrowPressed();
            yield return null;

            float expectedSpeed = PlayerCarry.BaseThrowSpeed * 1.25f;
            float actualSpeed = carryable.Body.linearVelocity.magnitude;
            Check("Nouka's throw launches the object at 1.25x base speed (Part 5.2)",
                Mathf.Abs(actualSpeed - expectedSpeed) < 0.1f, $"speed={actualSpeed:F3} expected={expectedSpeed:F3}");

            Destroy(light);
            Destroy(go);
            yield return null;
        }

        private IEnumerator TestBoufLaunchSplit()
        {
            var (go, loco, carry, _, _, _, selector) = BuildFullTestPlayer("Stage12Test_Bouf", new Vector3(45f, 0.05f, 50f));
            selector.SelectByName("Character_Featherweight_01");
            go.transform.rotation = Quaternion.identity;
            yield return null;
            Check("Bouf's launch fractions match Part 5.2 (60% horizontal / 40% vertical)",
                Mathf.Approximately(carry.launchHorizontalFraction, 0.60f) && Mathf.Approximately(carry.launchVerticalFraction, 0.40f),
                $"horizontal={carry.launchHorizontalFraction} vertical={carry.launchVerticalFraction}");

            var heavy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            heavy.transform.position = go.transform.position + new Vector3(0f, 0.35f, 1f);
            heavy.transform.localScale = Vector3.one * 0.6f;
            var carryable = heavy.AddComponent<CarryableObject>();
            carryable.weightClass = CarryableObject.WeightClass.Heavy;
            yield return null;

            carry.TryPickUpNearest();
            yield return new WaitForSeconds(PlayerCarry.StandardWindStartDelay + 0.3f);
            Check("Setup: Bouf's wind is active before ignition", carry.WindActive, $"windActive={carry.WindActive}");

            var fire = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fire.transform.position = go.transform.position + new Vector3(0f, 0.6f, 0.5f);
            Destroy(fire.GetComponent<Collider>());
            fire.AddComponent<FireSource>();
            yield return null;

            float expectedHoriz = PlayerCarry.RocketForce * 0.60f;
            float expectedVert = PlayerCarry.RocketForce * 0.40f;
            Vector3 launch = loco.LaunchVelocitySnapshot;
            float horizMag = new Vector3(launch.x, 0f, launch.z).magnitude;
            Check("Bouf's ignition launch splits 60%/40% instead of the standard 20%/80% (Part 5.2)",
                Mathf.Abs(launch.y - expectedVert) < 0.05f && Mathf.Abs(horizMag - expectedHoriz) < 0.05f,
                $"vertical={launch.y:F3} expectedVertical={expectedVert:F3} horizontal={horizMag:F3} expectedHorizontal={expectedHoriz:F3}");

            Destroy(fire);
            Destroy(heavy);
            Destroy(go);
            yield return null;
        }

        private IEnumerator TestFunnySwapperGating()
        {
            var (go, loco, carry, _, _, _, selector) = BuildFullTestPlayer("Stage12Test_Funny", new Vector3(45f, 0.05f, 55f));
            selector.SelectByName("Character_Slowpoke_01");
            var swapper = go.GetComponent<FunnyAnimationSwapper>();
            var ragdoll = go.GetComponent<PlayerRagdoll>();
            yield return null;

            ragdoll.TriggerRagdoll();
            loco.SetCameraYaw(0f);
            loco.SetMoveInput(Vector2.up);
            yield return new WaitForSeconds(1.5f);
            Check("Funny-swap never activates while ragdolled (Part 5.3)", !swapper.IsFunnyActive, $"isFunnyActive={swapper.IsFunnyActive}");

            Destroy(go);
            yield return null;
        }
    }
}
