using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Blockout;
using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Runtime (Play Mode) verification of Stage 5 (Part 7.5 - capture/restraint).
    /// Same self-contained-rig philosophy as Stages 1-4's tests.
    /// </summary>
    public class Stage5CaptureTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage5_CaptureTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 5 - Capture & Restraint Test (Part 7.5) ===");
            report.AppendLine();

            var realPlayer = GameObject.Find("Player");
            if (realPlayer != null) realPlayer.SetActive(false);
            var scenePlaceholders = GameObject.Find("Stage5_CapturePlaceholders");
            if (scenePlaceholders != null) scenePlaceholders.SetActive(false);

            yield return TestUnconsciousThenAutoWake();
            yield return TestRestrainAndJointCarry();
            yield return TestBalconyThrow();
            yield return TestFanMount();
            yield return TestReleaseAnywhereAnd30sCap();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 5 capture/restraint system matches Part 7.5."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage5Test] Report written to " + reportPath);
            Debug.Log(report.ToString());

            if (realPlayer != null) realPlayer.SetActive(true);
            if (scenePlaceholders != null) scenePlaceholders.SetActive(true);

            yield return null;
            Debug.Log("[Stage5Test] DONE");

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

        private (PlayerLocomotion loco, PlayerCarry carry, PlayerRagdoll ragdoll, PlayerCapture capture) BuildVictim(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            var ragdoll = go.AddComponent<PlayerRagdoll>();
            var capture = go.AddComponent<PlayerCapture>();
            return (loco, carry, ragdoll, capture);
        }

        private (PlayerLocomotion loco, PlayerCarry carry) BuildRescuer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            return (loco, carry);
        }

        private CarryableObject SpawnRope(Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Stage5Test_Rope";
            go.transform.position = position;
            go.transform.localScale = new Vector3(0.4f, 0.08f, 0.08f);
            var carry = go.AddComponent<CarryableObject>();
            carry.weightClass = CarryableObject.WeightClass.Light;
            carry.isRope = true;
            return carry;
        }

        private IEnumerator WaitUntilOrTimeout(System.Func<bool> condition, float timeoutSeconds)
        {
            float elapsed = 0f;
            while (!condition() && elapsed < timeoutSeconds)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        // -----------------------------------------------------------------
        private IEnumerator TestUnconsciousThenAutoWake()
        {
            var (loco, _, ragdoll, capture) = BuildVictim("Stage5_TestWake", new Vector3(45f, 0.05f, 10f));
            yield return null;

            ragdoll.TriggerRagdoll();
            yield return WaitUntilOrTimeout(() => !ragdoll.IsRagdolled, 8f);

            Check("Ragdoll settling enters Unconscious, not direct standing (Part 7.5)",
                capture.State == CaptureState.Unconscious, $"state={capture.State}");
            Check("Player stays lying still (control suspended) while unconscious",
                loco.IsExternallyControlled, $"isExternallyControlled={loco.IsExternallyControlled}");

            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.None, PlayerCapture.UnconsciousDuration + 2f);

            Check("Unconscious auto-wakes after 6s with no rescue (Part 7.5)",
                capture.State == CaptureState.None && !loco.IsExternallyControlled,
                $"state={capture.State} isExternallyControlled={loco.IsExternallyControlled}");

            Destroy(capture.gameObject);
            yield return null;
        }

        private IEnumerator TestRestrainAndJointCarry()
        {
            var (loco, _, ragdoll, capture) = BuildVictim("Stage5_TestRestrain", new Vector3(45f, 0.05f, 10f));
            yield return null;
            ragdoll.TriggerRagdoll();
            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.Unconscious, 8f);
            Check("Setup: victim is unconscious", capture.State == CaptureState.Unconscious, $"state={capture.State}");

            var (rescueLoco, rescueCarry) = BuildRescuer("Stage5_TestRescuer1", capture.transform.position + new Vector3(1f, 0f, 0f));
            var rope = SpawnRope(rescueCarry.transform.position + new Vector3(0f, 0.15f, 0.5f));
            yield return null;
            rescueCarry.TryPickUpNearest();
            yield return null;
            Check("Setup: rescuer picked up the rope", rescueCarry.Held != null && rescueCarry.Held.isRope, $"held={(rescueCarry.Held != null)}");

            rescueCarry.transform.position = capture.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            bool restrained = rescueCarry.TryRestrainNearestUnconscious();

            Check("Approaching an unconscious victim with a rope restrains them (Part 7.5)", restrained, $"restrained={restrained}");
            Check("Restraining consumes the rope", rescueCarry.Held == null, $"held={(rescueCarry.Held != null)}");
            Check("Victim is now Restrained", capture.State == CaptureState.Restrained, $"state={capture.State}");

            var (headLoco, headCarry) = BuildRescuer("Stage5_TestGrabHead", ragdoll.HeadPosition + new Vector3(0.3f, 0f, 0f));
            var (feetLoco, feetCarry) = BuildRescuer("Stage5_TestGrabFeet", ragdoll.FeetPosition + new Vector3(0.3f, 0f, 0f));
            yield return null;

            bool grabbedHead = headCarry.TryGrabNearestRestrainedEnd();
            bool grabbedFeet = feetCarry.TryGrabNearestRestrainedEnd();
            yield return null;

            Check("Two different rescuers can each grab an end (Part 7.5)", grabbedHead && grabbedFeet,
                $"head={grabbedHead} feet={grabbedFeet}");
            Check("Grabbing both ends within the sync window activates joint carry",
                capture.IsJointCarried, $"isJointCarried={capture.IsJointCarried}");
            Check("Both carriers take the 20% speed penalty (Part 7.5)",
                Mathf.Approximately(headLoco.SpeedMultiplier, PlayerCapture.JointCarrySpeedFactor) &&
                Mathf.Approximately(feetLoco.SpeedMultiplier, PlayerCapture.JointCarrySpeedFactor),
                $"head={headLoco.SpeedMultiplier:F2} feet={feetLoco.SpeedMultiplier:F2} expected={PlayerCapture.JointCarrySpeedFactor}");

            // Leave this rig's objects in place for the next subtests to reuse/destroy explicitly.
            _sharedVictim = (loco, capture, ragdoll);
            _sharedHead = headCarry;
            _sharedFeet = feetCarry;
        }

        // Carried across subtests deliberately - the balcony/fan/release tests each
        // need a jointly-carried Restrained victim, which is expensive to set up
        // (unconscious -> restrain -> dual grab), so each test repositions this same
        // rig instead of rebuilding it from scratch three times.
        private (PlayerLocomotion loco, PlayerCapture capture, PlayerRagdoll ragdoll) _sharedVictim;
        private PlayerCarry _sharedHead, _sharedFeet;

        private IEnumerator TestBalconyThrow()
        {
            var (loco, capture, ragdoll) = _sharedVictim;
            Check("Setup: victim is jointly carried before the balcony test", capture.IsJointCarried, $"isJointCarried={capture.IsJointCarried}");

            Vector3 balconyPos = new Vector3(MansionSpec.Opening.x - 0.3f, MansionSpec.Floor2FloorY, MansionSpec.Opening.centerZ);
            capture.transform.position = balconyPos;
            _sharedHead.transform.position = balconyPos + new Vector3(0.3f, 0f, 0f);
            _sharedFeet.transform.position = balconyPos + new Vector3(-0.3f, 0f, 0f);
            yield return null;

            _sharedHead.TryResolveJointCarryInsult();
            yield return null;

            Check("Resolving the insult at the balcony edge throws the victim (Part 7.5)",
                capture.State == CaptureState.None && loco.IsFreeFlight,
                $"state={capture.State} isFreeFlight={loco.IsFreeFlight}");

            yield return WaitUntilOrTimeout(() => ragdoll.IsRagdolled, 5f);
            Check("Landing from the balcony throw re-triggers full ragdoll (Part 7.3/7.5)",
                ragdoll.IsRagdolled, $"isRagdolled={ragdoll.IsRagdolled}");

            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.Unconscious, 8f);
            Check("The thrown victim recovers into Unconscious again afterward",
                capture.State == CaptureState.Unconscious, $"state={capture.State}");
        }

        private IEnumerator TestFanMount()
        {
            var (loco, capture, ragdoll) = _sharedVictim;

            // The balcony throw's landing spot depends on exactly where the ragdoll's
            // colliders happened to settle (possibly caught on the balcony railing
            // rather than the ground floor) - relocate to an unambiguous, ordinary
            // spot before setting up this subtest's own scenario, since this test only
            // cares about the fan, not re-verifying the fall itself (already covered).
            capture.transform.position = new Vector3(45f, 0.05f, 14f);
            yield return null;

            // Re-restrain and re-grab (the balcony throw fully released the victim).
            // Rope must land in the rescuer's 45deg pickup cone (their forward, +Z by
            // default) rather than merely near their position - see Part 7.1's cone.
            _sharedHead.transform.position = capture.transform.position + new Vector3(1f, 0f, 0f);
            yield return null;
            var rope = SpawnRope(_sharedHead.transform.position + new Vector3(0f, 0.15f, 0.5f));
            yield return null;
            bool pickedUpRope = _sharedHead.TryPickUpNearest();
            yield return null;
            Check("Setup: rescuer re-picked up a rope for the fan test", pickedUpRope, $"pickedUp={pickedUpRope}");

            _sharedHead.transform.position = capture.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            bool reRestrained = _sharedHead.TryRestrainNearestUnconscious();
            Check("Setup: re-restrained for the fan test", reRestrained, $"restrained={reRestrained}");
            if (rope != null) Destroy(rope.gameObject); // in case the restrain attempt failed and left it unconsumed

            _sharedHead.transform.position = ragdoll.HeadPosition + new Vector3(0.3f, 0f, 0f);
            _sharedFeet.transform.position = ragdoll.FeetPosition + new Vector3(0.3f, 0f, 0f);
            yield return null;
            _sharedHead.TryGrabNearestRestrainedEnd();
            _sharedFeet.TryGrabNearestRestrainedEnd();
            yield return null;
            Check("Setup: re-restrained and re-grabbed for the fan test", capture.IsJointCarried, $"isJointCarried={capture.IsJointCarried}");

            var fanGo = new GameObject("Stage5Test_Fan");
            fanGo.transform.position = capture.transform.position + new Vector3(0.5f, 0f, 0f);
            var fan = fanGo.AddComponent<CeilingFan>();
            yield return null;

            _sharedHead.TryResolveJointCarryInsult();
            yield return null;

            Check("Resolving the insult near a fan mounts the victim on it (Part 7.5)",
                fan.MountedCaptive == capture, $"mounted={(fan.MountedCaptive == capture)}");
            Check("Joint carry ends once mounted", !capture.IsJointCarried, $"isJointCarried={capture.IsJointCarried}");
            Check("Victim stays Restrained while mounted (no auto-release)", capture.State == CaptureState.Restrained, $"state={capture.State}");

            Vector3 posBefore = capture.transform.position;
            yield return null;
            yield return null;
            Check("Mounted victim's position follows the spinning fan", capture.transform.position != posBefore,
                $"moved={(capture.transform.position != posBefore)}");

            fan.DetachCaptive();
            Destroy(fanGo);

            // This rig's last use - clean it and its rescuers up here rather than
            // carrying it into the final (self-contained) subtest.
            Destroy(capture.gameObject);
            Destroy(_sharedHead.gameObject);
            Destroy(_sharedFeet.gameObject);
            yield return null;
        }

        private IEnumerator TestReleaseAnywhereAnd30sCap()
        {
            var (loco, _, ragdoll, capture) = BuildVictim("Stage5_TestTimeout", new Vector3(45f, 0.05f, 10f));
            yield return null;
            ragdoll.TriggerRagdoll();
            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.Unconscious, 8f);

            var (_, headCarry) = BuildRescuer("Stage5_TestTimeoutHead", capture.transform.position + new Vector3(1f, 0f, 0f));
            var (_, feetCarry) = BuildRescuer("Stage5_TestTimeoutFeet", capture.transform.position + new Vector3(-1f, 0f, 0f));
            var rope = SpawnRope(headCarry.transform.position + new Vector3(0f, 0.15f, 0.5f)); // in headCarry's forward pickup cone
            yield return null;
            bool pickedUpRope = headCarry.TryPickUpNearest();
            yield return null;
            Check("Setup: rescuer picked up a rope for the timeout test", pickedUpRope, $"pickedUp={pickedUpRope}");

            headCarry.transform.position = capture.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            bool restrained = headCarry.TryRestrainNearestUnconscious();
            Check("Setup: restrained for the release/timeout test", restrained, $"restrained={restrained}");
            if (rope != null) Destroy(rope.gameObject);

            headCarry.transform.position = ragdoll.HeadPosition + new Vector3(0.3f, 0f, 0f);
            feetCarry.transform.position = ragdoll.FeetPosition + new Vector3(0.3f, 0f, 0f);
            yield return null;
            headCarry.TryGrabNearestRestrainedEnd();
            feetCarry.TryGrabNearestRestrainedEnd();
            yield return null;

            // Nowhere near a balcony edge or a fan -> option 3, plain release.
            headCarry.TryResolveJointCarryInsult();
            yield return null;

            Check("Resolving the insult away from any special spot just lets go (Part 7.5 option 3)",
                !capture.IsJointCarried && capture.State == CaptureState.Restrained,
                $"isJointCarried={capture.IsJointCarried} state={capture.State}");

            yield return WaitUntilOrTimeout(() => capture.State == CaptureState.None, PlayerCapture.MaxRestrainedSeconds + 2f);

            Check("Restrained state auto-releases after its 30s cap (Part 7.5)",
                capture.State == CaptureState.None, $"state={capture.State}");

            Destroy(capture.gameObject);
            Destroy(headCarry.gameObject);
            Destroy(feetCarry.gameObject);
            yield return null;
        }
    }
}
