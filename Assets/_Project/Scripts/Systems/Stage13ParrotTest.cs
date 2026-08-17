using System.Collections;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode half of Stage 13's verification (static half already ran in
    /// Stage13ParrotImporter.RunStaticVerification). Proves Part 6's stationary-cage
    /// parrot actually reacts to real downed players in real time, respects its
    /// detection radius, returns to idle afterward, and is truly immovable - not
    /// just that the right components got attached.
    /// </summary>
    public class Stage13ParrotTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage13_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 13 - Dynamic Parrot Test (Part 6, Play Mode) ===");
            report.AppendLine();

            yield return TestIdleAndMockOnNearbyRagdoll();
            yield return TestNoMockBeyondDetectionRadius();
            yield return TestUnconsciousAlsoTriggersMockery();
            yield return TestCageIsImmovable();
            yield return TestMockPickDistribution();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 13 parrot system matches Part 6 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage13DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage13DynamicTest] DONE");

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
        private GameObject BuildTestPlayer(string name, Vector3 position, out PlayerRagdoll ragdoll, out PlayerCapture capture)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            go.AddComponent<PlayerLocomotion>();
            ragdoll = go.AddComponent<PlayerRagdoll>();
            capture = go.AddComponent<PlayerCapture>();
            return go;
        }

        private ParrotController SpawnParrot(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var controller = go.AddComponent<ParrotController>();
            var visual = new GameObject("Visual");
            visual.transform.SetParent(go.transform, false);
            controller.visualRoot = visual.transform;
            // No audio clips assigned in these synthetic test rigs (the real prefab's
            // clips are covered by static verification) - BeginMocking() must still
            // work with zero clips (mockDuration falls back to the 1.5s floor).
            return controller;
        }

        // ---------------------------------------------------------------
        private IEnumerator TestIdleAndMockOnNearbyRagdoll()
        {
            var parrot = SpawnParrot("Stage13Test_Parrot_A", new Vector3(0f, 1.75f, 0f));
            yield return null;
            Check("Parrot starts in Idle state (Part 6.1)", parrot.State == ParrotController.ParrotState.Idle, $"state={parrot.State}");

            var player = BuildTestPlayer("Stage13Test_Player_A", new Vector3(5f, 0.05f, 0f), out var ragdoll, out _); // 5m: within the 10m radius
            yield return null;

            ragdoll.TriggerRagdoll();
            yield return new WaitForSeconds(0.6f); // past the 0.5s detection check interval

            Check("Parrot mocks a ragdolled player within 10m (Part 6.2/6.3)", parrot.State == ParrotController.ParrotState.Mocking,
                $"state={parrot.State} triggerCount={parrot.MockTriggerCount}");
            Check("Mockery fires immediately - no approach phase (Part 6.3)", parrot.MockTriggerCount == 1, $"triggerCount={parrot.MockTriggerCount}");

            // Move the player out of range before it wakes up: the document never
            // says the parrot mocks a given fall only once, and this player is still
            // ragdolled well past the 1.5s minimum mockery duration, so leaving it in
            // range risks a legitimate second detection cycle re-triggering mockery
            // right around the check below. Moving it away isolates "does mockery end
            // on its own timer" from that separate (also-correct) repeat-mock behavior.
            player.transform.position = new Vector3(1000f, 0.05f, 0f);
            yield return new WaitForSeconds(2f); // past the 1.5s minimum mockery duration
            Check("Parrot returns to Idle after the mockery duration ends (Part 6.3)", parrot.State == ParrotController.ParrotState.Idle,
                $"state={parrot.State}");

            Destroy(player);
            Destroy(parrot.gameObject);
            yield return null;
        }

        private IEnumerator TestNoMockBeyondDetectionRadius()
        {
            var parrot = SpawnParrot("Stage13Test_Parrot_B", new Vector3(0f, 1.75f, 0f));
            var player = BuildTestPlayer("Stage13Test_Player_B", new Vector3(15f, 0.05f, 0f), out var ragdoll, out _); // 15m: beyond the 10m radius
            yield return null;

            ragdoll.TriggerRagdoll();
            yield return new WaitForSeconds(0.6f);

            Check("Parrot does NOT mock a ragdolled player beyond 10m (Part 6.2)", parrot.State == ParrotController.ParrotState.Idle,
                $"state={parrot.State}");

            Destroy(player);
            Destroy(parrot.gameObject);
            yield return null;
        }

        private IEnumerator TestUnconsciousAlsoTriggersMockery()
        {
            // Part 6.2's target condition is "IsRagdolled OR Unconscious". The instant
            // a real ragdoll settles (PlayerRagdoll.StandUp), IsRagdolled flips back to
            // false and PlayerCapture.State flips to Unconscious in the same handoff -
            // so letting one actually settle on a real ground plane exercises exactly
            // the Unconscious-only branch, not just IsRagdolled again.
            var parrot = SpawnParrot("Stage13Test_Parrot_C", new Vector3(0f, 1.75f, 0f));
            // Settle far outside the 10m detection radius first, so the earlier
            // IsRagdolled-only phase (which is ALSO eligible per Part 6.2) can't
            // trigger its own mock/idle cycle and race with the check below - this
            // isolates the Unconscious branch specifically, from a clean Idle state.
            var player = BuildTestPlayer("Stage13Test_Player_C", new Vector3(200f, 0.05f, 0f), out var ragdoll, out var capture);
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.transform.position = new Vector3(200f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            yield return null;

            ragdoll.TriggerRagdoll();

            float elapsed = 0f;
            while (capture.State != CaptureState.Unconscious && elapsed < 8f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Check("Ragdoll settles into PlayerCapture.Unconscious within a reasonable time (Part 7.3/7.5 prerequisite)",
                capture.State == CaptureState.Unconscious, $"state={capture.State} elapsed={elapsed:F1}s");
            Check("At settle time, IsRagdolled has already flipped back to false (isolates the Unconscious branch)",
                !ragdoll.IsRagdolled, $"isRagdolled={ragdoll.IsRagdolled}");
            Check("Parrot did not mock while the target was out of range (setup check)",
                parrot.MockTriggerCount == 0, $"triggerCount={parrot.MockTriggerCount}");

            // Now bring the (already-Unconscious, no-longer-ragdolled) player into range.
            player.transform.position = new Vector3(4f, player.transform.position.y, 0f);
            yield return new WaitForSeconds(0.6f); // past the next 0.5s detection check interval
            Check("Parrot mocks an Unconscious (non-ragdolled) player within 10m (Part 6.2: ragdoll OR unconscious)",
                parrot.State == ParrotController.ParrotState.Mocking, $"state={parrot.State} triggerCount={parrot.MockTriggerCount}");

            Destroy(ground);
            Destroy(player);
            Destroy(parrot.gameObject);
            yield return null;
        }

        private IEnumerator TestCageIsImmovable()
        {
            var parrot = SpawnParrot("Stage13Test_Parrot_D", new Vector3(0f, 1.75f, 0f));
            var col = parrot.gameObject.AddComponent<BoxCollider>();
            col.size = Vector3.one * 0.5f;
            Vector3 startPos = parrot.transform.position;
            yield return null;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.transform.position = startPos + new Vector3(-2f, 0f, 0f);
            var body = projectile.AddComponent<Rigidbody>();
            body.mass = 5f;
            body.linearVelocity = new Vector3(20f, 0f, 0f); // fast, direct hit toward the cage

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Cage carries no Rigidbody (Part 6.4)", parrot.GetComponent<Rigidbody>() == null, "");
            Check("Cage does not move even after a direct hard hit (Part 6.4: zero physical reaction)",
                Vector3.Distance(parrot.transform.position, startPos) < 0.001f,
                $"startPos={startPos} endPos={parrot.transform.position}");

            Destroy(projectile);
            Destroy(parrot.gameObject);
            yield return null;
        }

        private IEnumerator TestMockPickDistribution()
        {
            var parrot = SpawnParrot("Stage13Test_Parrot_E", new Vector3(0f, 1.75f, 0f));
            yield return null;

            int laughCount = 0, sentenceCount = 0;
            const int samples = 300;
            for (int i = 0; i < samples; i++)
            {
                parrot.DebugSampleMockPick();
                if (parrot.LastMockCategory == "laugh") laughCount++;
                else sentenceCount++;
            }

            Check("Mockery pick produces both the simple-laugh and full-sentence outcomes (Part 6.3: 60/40 split)",
                laughCount > 0 && sentenceCount > 0, $"laugh={laughCount} sentence={sentenceCount} of {samples}");

            Destroy(parrot.gameObject);
            yield return null;
        }
    }
}
