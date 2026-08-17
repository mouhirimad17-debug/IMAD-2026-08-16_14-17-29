using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 9's verification (static half already ran in
    /// Stage9BedroomsImporter.RunStaticVerification before Play Mode was entered).
    /// This is the first stage where a real prop feeds directly into an already-
    /// built numeric system end to end: the real soap bottle through Part 7.4's pour
    /// mechanic, and the real ceiling fan through Part 7.5's fan-mount finishing move.
    /// </summary>
    public class Stage9BedroomsPropsTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage9_DynamicPropsTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 9 - Dynamic Bedroom Props Test (real meshes, Play Mode) ===");
            report.AppendLine();

            var root = GameObject.Find(Stage9BedroomsPropSpec.BedroomsPropsRootName);
            Check("Setup: Bedrooms props root exists in the scene", root != null, $"found={(root != null)}");

            if (root != null)
            {
                yield return TestPickUpRealMesh(root, "Bedroom1_Pillow_01");
                yield return TestRealPourableSoapBottle(root, "Bathroom1_SoapBottle_01");
                yield return TestRealCeilingFanMount(root, "Bedroom2_CeilingFan_01");
                yield return TestBlanketDrag(root, "Bedroom1_Blanket_01");
                yield return TestDresserDrawers(root, "Bedroom1_Dresser_01");
            }

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 9 real-mesh Bedroom props work correctly with the existing physics systems."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage9DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage9DynamicTest] DONE");

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

        private (PlayerLocomotion loco, PlayerCarry carry, PlayerRagdoll ragdoll, PlayerCapture capture) BuildFullPlayer(string name, Vector3 position)
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

        private IEnumerator TestPickUpRealMesh(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var (_, carry, _, _) = BuildFullPlayer("Stage9Test_PickupPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            carry.transform.rotation = Quaternion.LookRotation(propTransform.position - carry.transform.position, Vector3.up);
            yield return null;

            bool picked = carry.TryPickUpNearest();
            yield return null;

            Check($"A real Bedroom prop mesh ({unityName}) can be picked up (Part 7.1)", picked, $"picked={picked}");

            carry.Drop();
            Destroy(carry.gameObject);
            yield return null;
        }

        private IEnumerator TestRealPourableSoapBottle(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var carryable = propTransform.GetComponent<CarryableObject>();
            Check($"{unityName} is the real pourable prop (Part 7.4)", carryable != null && carryable.isPourable,
                $"pourable={(carryable != null && carryable.isPourable)}");
            if (carryable == null || !carryable.isPourable) yield break;

            var (_, carry, _, _) = BuildFullPlayer("Stage9Test_PourPlayer", propTransform.position + new Vector3(0f, 0f, -1.5f));
            carry.transform.rotation = Quaternion.LookRotation(propTransform.position - carry.transform.position, Vector3.up);
            yield return null;

            carry.TryPickUpNearest();
            yield return null;
            Check("Setup: real soap bottle was picked up", carry.Held == carryable, $"held={(carry.Held == carryable)}");

            carry.SimulateInteractHeld = true;
            float elapsed = 0f;
            while (carry.PourCount == 0 && elapsed < PlayerCarry.PourHoldSeconds + 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            carry.SimulateInteractHeld = false;

            Check("Long-pressing with the real soap bottle actually pours a slip zone (Part 7.4)",
                carry.PourCount == 1, $"pourCount={carry.PourCount} elapsed={elapsed:F2}s");

            var zone = FindFirstObjectByType<SlipZone>();
            Check("A real slip zone was spawned by the real soap bottle", zone != null, $"found={(zone != null)}");

            Destroy(carry.gameObject);
            if (zone != null) Destroy(zone.gameObject);
            yield return null;
        }

        private IEnumerator TestRealCeilingFanMount(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var fan = propTransform.GetComponent<CeilingFan>();
            Check($"{unityName} has the real CeilingFan component (Part 2.2/7.5)", fan != null, $"found={(fan != null)}");
            if (fan == null) yield break;

            float initialYaw = propTransform.eulerAngles.y;
            yield return null;
            yield return null;
            Check("The real fan spins ambiently even with nobody mounted (Law 2.2)",
                !Mathf.Approximately(propTransform.eulerAngles.y, initialYaw),
                $"initialYaw={initialYaw:F2} nowYaw={propTransform.eulerAngles.y:F2}");

            // Full pipeline: ragdoll -> unconscious -> restrain -> joint carry -> mount on the REAL fan.
            // Spawned at floor level (the fan itself sits near the ceiling) so the
            // ragdoll has an actual floor to land and settle on before being carried
            // up to the fan for the mount attempt below.
            Vector3 floorSpawn = new Vector3(propTransform.position.x, MansionSpec.Floor2FloorY + 0.5f, propTransform.position.z + 2f);
            var (victimLoco, _, victimRagdoll, victimCapture) = BuildFullPlayer("Stage9Test_Victim", floorSpawn);
            yield return null;
            victimRagdoll.TriggerRagdoll();
            float settleElapsed = 0f;
            while (victimCapture.State != CaptureState.Unconscious && settleElapsed < 8f) { settleElapsed += Time.deltaTime; yield return null; }

            var (_, ropeCarrier, _, _) = BuildFullPlayer("Stage9Test_RopeCarrier", victimCapture.transform.position + new Vector3(1f, 0f, 0f));
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rope.transform.position = ropeCarrier.transform.position + new Vector3(0f, 0.15f, 0.5f);
            rope.transform.localScale = new Vector3(0.4f, 0.08f, 0.08f);
            var ropeObj = rope.AddComponent<CarryableObject>();
            ropeObj.isRope = true;
            yield return null;
            ropeCarrier.TryPickUpNearest();
            yield return null;
            ropeCarrier.transform.position = victimCapture.transform.position + new Vector3(0.5f, 0f, 0f);
            yield return null;
            bool restrained = ropeCarrier.TryRestrainNearestUnconscious();
            Check("Setup: victim restrained for the real-fan mount test", restrained, $"restrained={restrained}");

            var (_, headGrabber, _, _) = BuildFullPlayer("Stage9Test_HeadGrabber", victimRagdoll.HeadPosition + new Vector3(0.3f, 0f, 0f));
            var (_, feetGrabber, _, _) = BuildFullPlayer("Stage9Test_FeetGrabber", victimRagdoll.FeetPosition + new Vector3(0.3f, 0f, 0f));
            yield return null;
            headGrabber.TryGrabNearestRestrainedEnd();
            feetGrabber.TryGrabNearestRestrainedEnd();
            yield return null;
            Check("Setup: victim jointly carried toward the real fan", victimCapture.IsJointCarried, $"isJointCarried={victimCapture.IsJointCarried}");

            // Move BOTH grabbers near the fan - PlayerCapture.Update() recomputes the
            // victim's position every frame as the midpoint between them, so only
            // moving one would pull the victim back toward the other's old (floor-
            // level) spot instead of up near the fan.
            headGrabber.transform.position = propTransform.position + new Vector3(0.5f, 0f, 0f);
            feetGrabber.transform.position = propTransform.position + new Vector3(-0.3f, 0f, 0f);
            yield return null;

            headGrabber.TryResolveJointCarryInsult();
            yield return null;

            Check("Mounting on the REAL Bedroom2 ceiling fan works end to end (Part 7.5)",
                fan.MountedCaptive == victimCapture, $"mounted={(fan.MountedCaptive == victimCapture)}");

            fan.DetachCaptive();
            Destroy(victimCapture.gameObject);
            Destroy(ropeCarrier.gameObject);
            Destroy(headGrabber.gameObject);
            Destroy(feetGrabber.gameObject);
            yield return null;
        }

        // Part 2 of the owner's Stage 9 fix-up: Bedroom1_Blanket_01's real
        // GrabbableDragProp mechanic end to end - grab a corner, drag it more than
        // PulledDisplacementThreshold from its original spot, and confirm both the
        // one-shot OnBlanketPulled event fires AND a dummy kinematic body sitting
        // "on the bed" gets released to fall.
        private IEnumerator TestBlanketDrag(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var blanket = propTransform.GetComponent<GrabbableDragProp>();
            Check($"{unityName} has the real GrabbableDragProp component (Part 2 fix-up)", blanket != null, $"found={(blanket != null)}");
            if (blanket == null) yield break;

            Check("The real blanket has 4 corner grab points (Part 2 fix-up)", blanket.GrabPoints != null && blanket.GrabPoints.Length == 4,
                $"count={(blanket.GrabPoints != null ? blanket.GrabPoints.Length : -1)}");

            bool pulledEventFired = false;
            blanket.OnBlanketPulled += () => pulledEventFired = true;

            var dummyOnBed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummyOnBed.name = "Stage9Test_DummyOnBed";
            dummyOnBed.transform.position = propTransform.position + Vector3.up * 0.1f;
            dummyOnBed.transform.localScale = Vector3.one * 0.1f;
            var dummyRb = dummyOnBed.AddComponent<Rigidbody>();
            dummyRb.isKinematic = true;

            var puller = new GameObject("Stage9Test_BlanketPuller").transform;
            puller.position = blanket.GrabPoints[0].position + new Vector3(2f, 0f, 0f);
            bool grabbed = blanket.TryGrab(puller);
            Check("The real blanket can be grabbed by a corner (Part 2 fix-up)", grabbed, $"grabbed={grabbed}");

            float elapsed = 0f;
            while (!blanket.HasTriggeredPull && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }

            Check("Dragging the real blanket past 0.4m fires OnBlanketPulled (Part 2 fix-up)",
                blanket.HasTriggeredPull && pulledEventFired, $"triggered={blanket.HasTriggeredPull} eventFired={pulledEventFired} elapsed={elapsed:F2}s");
            Check("A dummy body \"on the bed\" is released (isKinematic=false) once pulled (Part 2 fix-up)",
                !dummyRb.isKinematic, $"isKinematic={dummyRb.isKinematic}");

            blanket.ReleaseGrab();
            Destroy(puller.gameObject);
            Destroy(dummyOnBed);
            yield return null;
        }

        // Part 3 of the owner's Stage 9 fix-up: Bedroom1_Dresser_01's real
        // InteractiveContainerProp/DrawerProp mechanic end to end - open one drawer
        // past FallOutOpenThreshold to eject a dummy item inside it, then open a
        // second drawer past TipRelevantOpenThreshold too and confirm the dresser
        // itself tips over (HasTipped, Rigidbody no longer kinematic).
        private IEnumerator TestDresserDrawers(GameObject root, string unityName)
        {
            var propTransform = FindChildByName(root.transform, unityName);
            Check($"Setup: {unityName} instance found in scene", propTransform != null, $"found={(propTransform != null)}");
            if (propTransform == null) yield break;

            var container = propTransform.GetComponent<InteractiveContainerProp>();
            Check($"{unityName} has the real InteractiveContainerProp component (Part 3 fix-up)", container != null, $"found={(container != null)}");
            if (container == null) yield break;

            Check("The real dresser has its 3 drawers built (Part 3 fix-up)",
                container.Drawers != null && container.Drawers.Length == InteractiveContainerProp.DrawerCount,
                $"count={(container.Drawers != null ? container.Drawers.Length : -1)}");
            Check("The real dresser starts kinematic (not tipped yet) (Part 3 fix-up)", container.Body.isKinematic, $"isKinematic={container.Body.isKinematic}");

            var drawer0 = container.Drawers[0];
            var dummyInDrawer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummyInDrawer.name = "Stage9Test_DummyInDrawer";
            dummyInDrawer.transform.position = drawer0.transform.position; // safely inside EjectContents' overlap box once the drawer has slid open
            dummyInDrawer.transform.localScale = Vector3.one * 0.05f;
            var dummyRb = dummyInDrawer.AddComponent<Rigidbody>();
            dummyRb.isKinematic = true;

            var puller = new GameObject("Stage9Test_DrawerPuller").transform;
            puller.position = drawer0.GrabPoint.position + drawer0.transform.forward * 2f;
            puller.forward = drawer0.transform.forward;

            bool grabbed0 = drawer0.TryGrab(puller);
            Check("The real drawer's handle can be grabbed (Part 3 fix-up)", grabbed0, $"grabbed={grabbed0}");

            float elapsed = 0f;
            while (drawer0.OpenDistance < DrawerProp.FallOutOpenThreshold && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }
            Check("Pulling the real drawer past 0.30m ejects a dummy item inside it (Part 3 fix-up)",
                !dummyRb.isKinematic, $"isKinematic={dummyRb.isKinematic} openDistance={drawer0.OpenDistance:F3}");

            var drawer1 = container.Drawers[1];
            puller.position = drawer1.GrabPoint.position + drawer1.transform.forward * 2f;
            drawer0.ReleaseGrab();
            bool grabbed1 = drawer1.TryGrab(puller);
            Check("Setup: a second real drawer can be grabbed too (Part 3 fix-up)", grabbed1, $"grabbed={grabbed1}");

            elapsed = 0f;
            while (!container.HasTipped && elapsed < 5f)
            {
                elapsed += Time.deltaTime;
                yield return new WaitForFixedUpdate();
            }
            Check("Opening 2 real drawers past 0.25m each tips the real dresser over (Part 3 fix-up)",
                container.HasTipped && !container.Body.isKinematic,
                $"hasTipped={container.HasTipped} isKinematic={container.Body.isKinematic} d0={drawer0.OpenDistance:F3} d1={drawer1.OpenDistance:F3}");

            drawer1.ReleaseGrab();
            Destroy(puller.gameObject);
            Destroy(dummyInDrawer);
            yield return null;
        }
    }
}
