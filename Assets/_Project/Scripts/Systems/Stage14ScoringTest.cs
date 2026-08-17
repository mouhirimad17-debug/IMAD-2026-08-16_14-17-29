using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode verification for Stage 14 (Part 9 scoring/round + Part 9.5 team
    /// assignment). Proves the random-but-even team split, every one of Part 9.1's
    /// four scoring events (with correct cross-team-only crediting), the round
    /// timer's final-countdown flag and its actual end-of-round winner/tie
    /// determination, and the movement/interact input freeze - all against real,
    /// running components, not just field values.
    /// </summary>
    public class Stage14ScoringTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage14_DynamicTest_Report.txt";

        // This test runs inside the real, already-built mansion scene (Scene_04_
        // Mansion.unity, opened by Stage14ScoringSetup), which has real architecture
        // starting at world origin (MansionSpec.Gym begins at x:0 z:0). Every test
        // object below is offset far outside the mansion's footprint (x:[0,90]
        // z:[0,60]) into open space, so it can't collide with real walls/props -
        // same convention Stage 12/13's tests used (large offset coordinates).
        private static readonly Vector3 Origin = new Vector3(500f, 0f, 500f);

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;

            report.AppendLine("=== Stage 14 - Dynamic Scoring/Team Test (Part 9 + 9.5, Play Mode) ===");
            report.AppendLine();

            yield return TestTeamAssignmentEvenSplit();
            yield return TestRegisterPointAndFinalCountdown();
            yield return TestRoundEndWinnerAndFreeze();
            yield return TestRoundEndTie();
            yield return TestThrowHitScoring();
            yield return TestSlipZoneScoring();
            yield return TestRestrainScoring();

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 14 scoring/team system matches Part 9/9.5 end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage14DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage14DynamicTest] DONE");

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
        private (GameObject go, PlayerLocomotion loco, PlayerCarry carry, PlayerRagdoll ragdoll,
            PlayerCapture capture, PlayerTeam team) BuildTestPlayer(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = Origin + position;
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            var loco = go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            var ragdoll = go.AddComponent<PlayerRagdoll>();
            var capture = go.AddComponent<PlayerCapture>();
            var team = go.AddComponent<PlayerTeam>();
            return (go, loco, carry, ragdoll, capture, team);
        }

        private GameObject BuildGround(Vector3 position, Vector3 scale)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Stage14Test_Ground";
            ground.transform.position = Origin + position;
            ground.transform.localScale = scale;
            return ground;
        }

        private RoundManagerHost BuildManager()
        {
            var go = new GameObject("Stage14Test_RoundManager");
            var host = go.AddComponent<RoundManagerHost>();
            return host;
        }

        // MonoBehaviour wrapper purely so each test gets its own isolated RoundManager
        // instance (RoundManager.Instance is a simple last-one-wins singleton).
        private class RoundManagerHost : MonoBehaviour
        {
            public RoundManager Manager;
            private void Awake() => Manager = gameObject.AddComponent<RoundManager>();
        }

        // ---------------------------------------------------------------
        private IEnumerator TestTeamAssignmentEvenSplit()
        {
            var players = new List<PlayerTeam>();
            var gos = new List<GameObject>();
            for (int i = 0; i < 8; i++)
            {
                var go = new GameObject($"Stage14Test_TeamSplit_{i}");
                gos.Add(go);
                players.Add(go.AddComponent<PlayerTeam>());
            }
            yield return null;

            TeamAssignmentService.AssignRandomly(players);

            int team1 = 0, team2 = 0, none = 0;
            foreach (var p in players)
            {
                if (p.Team == Team.Team1) team1++;
                else if (p.Team == Team.Team2) team2++;
                else none++;
            }

            Check("Team split is perfectly even (Part 9.5.1: always exactly 2 teams)", team1 == 4 && team2 == 4 && none == 0,
                $"team1={team1} team2={team2} unassigned={none}");

            foreach (var go in gos) Destroy(go);
            yield return null;
        }

        private IEnumerator TestRegisterPointAndFinalCountdown()
        {
            var host = BuildManager();
            var manager = host.Manager;
            var players = new List<PlayerTeam> { new GameObject("Stage14Test_RP_A").AddComponent<PlayerTeam>(), new GameObject("Stage14Test_RP_B").AddComponent<PlayerTeam>() };
            yield return null;

            manager.StartRound(players, RoundManager.FinalCountdownSeconds + 0.3f);
            Check("Round starts with both scores at zero (Part 9.2)", manager.ScoreTeam1 == 0 && manager.ScoreTeam2 == 0,
                $"team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");
            Check("Final-countdown flag is false with more than 30s left", !manager.IsFinalCountdown,
                $"timeRemaining={manager.TimeRemaining:F1} isFinal={manager.IsFinalCountdown}");

            manager.RegisterPoint(Team.Team1);
            manager.RegisterPoint(Team.Team1);
            manager.RegisterPoint(Team.Team2);
            Check("RegisterPoint credits the correct team (Part 9.2 counters)", manager.ScoreTeam1 == 2 && manager.ScoreTeam2 == 1,
                $"team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            yield return new WaitForSeconds(0.5f); // crosses the 30s threshold
            Check("Final-countdown flag becomes true once 30s or less remain (Part 9.2)", manager.IsFinalCountdown,
                $"timeRemaining={manager.TimeRemaining:F1} isFinal={manager.IsFinalCountdown}");

            Destroy(host.gameObject);
            foreach (var p in players) Destroy(p.gameObject);
            yield return null;
        }

        private IEnumerator TestRoundEndWinnerAndFreeze()
        {
            var host = BuildManager();
            var manager = host.Manager;
            var players = new List<PlayerTeam> { new GameObject("Stage14Test_End_A").AddComponent<PlayerTeam>(), new GameObject("Stage14Test_End_B").AddComponent<PlayerTeam>() };
            yield return null;

            manager.StartRound(players, 0.3f);
            manager.RegisterPoint(Team.Team1);
            manager.RegisterPoint(Team.Team1);
            manager.RegisterPoint(Team.Team2);

            float elapsed = 0f;
            while (!manager.HasEnded && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }

            Check("Round ends on its own once the timer reaches zero (Part 9.3)", manager.HasEnded && !manager.RoundActive,
                $"hasEnded={manager.HasEnded} roundActive={manager.RoundActive} elapsed={elapsed:F1}s");
            Check("Higher-scoring team is declared winner (Part 9.3)", manager.WinnerTeam == Team.Team1 && !manager.IsTie,
                $"winner={manager.WinnerTeam} isTie={manager.IsTie} team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");
            Check("Movement/interact input freezes at round end (Part 9.3)", PlayerInputReader.RoundInputFrozen,
                $"frozen={PlayerInputReader.RoundInputFrozen}");

            Destroy(host.gameObject);
            foreach (var p in players) Destroy(p.gameObject);
            yield return null;
        }

        private IEnumerator TestRoundEndTie()
        {
            var host = BuildManager();
            var manager = host.Manager;
            var players = new List<PlayerTeam> { new GameObject("Stage14Test_Tie_A").AddComponent<PlayerTeam>(), new GameObject("Stage14Test_Tie_B").AddComponent<PlayerTeam>() };
            yield return null;

            manager.StartRound(players, 0.2f);
            manager.RegisterPoint(Team.Team1);
            manager.RegisterPoint(Team.Team2);

            float elapsed = 0f;
            while (!manager.HasEnded && elapsed < 3f) { elapsed += Time.deltaTime; yield return null; }

            Check("Equal scores at round end produce a tie, no winner (Part 9.3)", manager.IsTie && manager.WinnerTeam == Team.None,
                $"isTie={manager.IsTie} winner={manager.WinnerTeam} team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            Destroy(host.gameObject);
            foreach (var p in players) Destroy(p.gameObject);
            yield return null;
        }

        // ---------------------------------------------------------------
        private IEnumerator TestThrowHitScoring()
        {
            var host = BuildManager();
            var manager = host.Manager;
            manager.StartRound(new List<PlayerTeam>(), RoundManager.ShortRoundSeconds);

            var ground = BuildGround(new Vector3(0f, -0.5f, 1f), new Vector3(10f, 1f, 10f));

            var (thrower, _, throwerCarry, _, _, throwerTeam) = BuildTestPlayer("Stage14Test_Thrower", new Vector3(0f, 0.05f, 0f));
            throwerTeam.SetTeam(Team.Team1);
            var (victim, _, _, _, _, victimTeam) = BuildTestPlayer("Stage14Test_Victim", new Vector3(0f, 0.05f, 2f));
            victimTeam.SetTeam(Team.Team2);
            thrower.transform.rotation = Quaternion.identity; // faces +Z, straight at the victim

            var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ball.transform.position = thrower.transform.position + new Vector3(0f, 0.15f, 1f);
            ball.transform.localScale = Vector3.one * 0.2f;
            var carryable = ball.AddComponent<CarryableObject>();
            carryable.weightClass = CarryableObject.WeightClass.Light;
            yield return null;

            throwerCarry.TryPickUpNearest();
            yield return null;
            throwerCarry.HandleThrowPressed(); // throws forward at BaseThrowSpeed (8 m/s), well above the 3 m/s minimum

            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Check("Direct thrown-object hit on an OPPONENT above 3 m/s scores for the thrower's team (Part 9.1)",
                manager.ScoreTeam1 == 1 && manager.ScoreTeam2 == 0, $"team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            Destroy(ground);
            Destroy(ball);
            Destroy(thrower);
            Destroy(victim);
            Destroy(host.gameObject);
            yield return null;
        }

        private IEnumerator TestSlipZoneScoring()
        {
            var host = BuildManager();
            var manager = host.Manager;
            manager.StartRound(new List<PlayerTeam>(), RoundManager.ShortRoundSeconds);

            var ground = BuildGround(new Vector3(2.5f, -0.5f, 0f), new Vector3(15f, 1f, 10f));

            var zoneGo = new GameObject("Stage14Test_SlipZone");
            zoneGo.transform.position = Origin + new Vector3(0f, 0.05f, 0f);
            var zone = zoneGo.AddComponent<SlipZone>();
            zone.PlacerTeam = Team.Team1;
            yield return null;

            var (opponent, _, _, _, _, opponentTeam) = BuildTestPlayer("Stage14Test_SlipOpponent", new Vector3(5f, 0.05f, 0f));
            opponentTeam.SetTeam(Team.Team2);
            yield return null;

            opponent.transform.position = zoneGo.transform.position; // step into the zone
            yield return null;

            Check("Opponent entering an opposing-placed slip zone scores for the placer's team (Part 9.1)",
                manager.ScoreTeam1 == 1 && manager.ScoreTeam2 == 0, $"team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            // Removed (rather than left ragdolled nearby) so its post-push tumble can't
            // drift back through the trigger and re-score before the next check below.
            Destroy(opponent);
            yield return null;

            var (teammate, _, _, _, _, teammateTeam) = BuildTestPlayer("Stage14Test_SlipTeammate", new Vector3(5f, 0.05f, 0f));
            teammateTeam.SetTeam(Team.Team1); // same team as the placer
            yield return null;
            teammate.transform.position = zoneGo.transform.position;
            yield return null;

            Check("Same-team player entering their own team's slip zone does NOT score (Part 9.1)",
                manager.ScoreTeam1 == 1 && manager.ScoreTeam2 == 0, $"team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            Destroy(ground);
            Destroy(zoneGo);
            Destroy(teammate);
            Destroy(host.gameObject);
            yield return null;
        }

        private IEnumerator TestRestrainScoring()
        {
            var host = BuildManager();
            var manager = host.Manager;
            manager.StartRound(new List<PlayerTeam>(), RoundManager.ShortRoundSeconds);

            var ground = BuildGround(new Vector3(0f, -0.5f, 0f), new Vector3(10f, 1f, 10f));

            var (victim, _, _, ragdoll, capture, victimTeam) = BuildTestPlayer("Stage14Test_RestrainVictim", new Vector3(0f, 0.05f, 0f));
            victimTeam.SetTeam(Team.Team2);
            var (rescuer, _, rescuerCarry, _, _, rescuerTeam) = BuildTestPlayer("Stage14Test_Rescuer", new Vector3(0.5f, 0.05f, 0f));
            rescuerTeam.SetTeam(Team.Team1);
            yield return null;

            ragdoll.TriggerRagdoll();
            float elapsed = 0f;
            while (capture.State != CaptureState.Unconscious && elapsed < 8f) { elapsed += Time.deltaTime; yield return null; }
            Check("Setup: victim settles into Unconscious before the restrain attempt", capture.State == CaptureState.Unconscious,
                $"state={capture.State} elapsed={elapsed:F1}s");

            // rescuer was already spawned 0.5m from the victim - within both
            // PlayerCapture.RestrainRange (1.5m) and TryPickUpNearest's pickup cone.
            var rope = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rope.transform.position = rescuer.transform.position;
            var ropeCarry = rope.AddComponent<CarryableObject>();
            ropeCarry.isRope = true;
            yield return null;
            rescuerCarry.TryPickUpNearest();
            yield return null;

            bool restrained = rescuerCarry.TryRestrainNearestUnconscious();
            yield return null;

            Check("Restraining an OPPONENT scores for the rescuer's team, at the Unconscious->Restrained moment (Part 9.1)",
                restrained && manager.ScoreTeam1 == 1 && manager.ScoreTeam2 == 0,
                $"restrained={restrained} team1={manager.ScoreTeam1} team2={manager.ScoreTeam2}");

            Destroy(ground);
            Destroy(rope);
            Destroy(victim);
            Destroy(rescuer);
            Destroy(host.gameObject);
            yield return null;
        }
    }
}
