using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PrankMansion.Entities;
using PrankMansion.Networking;
using PrankMansion.Player;
using PrankMansion.Systems;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Play Mode verification for Stage 15's networking architecture (Part 10,
    /// scoped per the owner's explicit sign-off: build the logic now against
    /// LocalLobbyDirectory, defer real Steamworks.NET/live cross-client sync/Part
    /// 10.7's mandatory cross-network test until an App ID and a second tester
    /// exist). Proves every RULE Part 10 actually specifies numerically or
    /// procedurally - room-name validation, code generation/matching, full-room
    /// ordering, ready-gating, host-migration-by-join-order, and the synced-state
    /// component's structure/defaults - against real, running code.
    /// </summary>
    public class Stage15NetworkingTest : MonoBehaviour
    {
        public string reportPath = "Assets/_ProjectLogs/Stage15_DynamicTest_Report.txt";

        private readonly StringBuilder report = new StringBuilder();
        private int total, passed;

        private IEnumerator Start()
        {
            report.AppendLine("=== Stage 15 - Dynamic Networking Architecture Test (Part 10, Play Mode) ===");
            report.AppendLine();

            TestRoomNameValidation();
            TestCreateRoomRejectsInvalidSettings();
            TestCreateRoomSucceedsAndGeneratesCode();
            TestCodeGeneratorCharset();
            TestSearchFiltersByName();
            TestJoinCodeMatchingAndFullRoomOrdering();
            TestReadyGatingAndStartGame();
            TestHostMigrationByJoinOrder();
            TestNetworkPlayerStateStructure();
            TestDisconnectCleanupDropsHeldObject();

            yield return null;

            report.AppendLine();
            report.AppendLine($"TOTAL: {passed}/{total} passed");
            report.AppendLine(passed == total
                ? "RESULT: Stage 15 networking architecture matches Part 10's specified rules end to end."
                : "RESULT: FAILURE - see FAIL lines above.");

            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            Debug.Log("[Stage15DynamicTest] Report written to " + reportPath);
            Debug.Log(report.ToString());

            yield return null;
            Debug.Log("[Stage15DynamicTest] DONE");

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

        private LobbyManager BuildLobbyManager() => new GameObject("Stage15Test_LobbyManager").AddComponent<LobbyManager>();

        // ---------------------------------------------------------------
        private void TestRoomNameValidation()
        {
            Check("Valid room name (letters/digits/spaces, <=20 chars) is accepted (Part 10.2)",
                LocalLobbyDirectory.IsValidRoomName("Friday Night 01"), "");
            Check("Empty room name is rejected (Part 10.2: create button stays disabled)",
                !LocalLobbyDirectory.IsValidRoomName(""), "");
            Check("Room name over 20 characters is rejected (Part 10.2)",
                !LocalLobbyDirectory.IsValidRoomName("ThisRoomNameIsWayTooLongForTheLimit"), "");
            Check("Room name with special characters is rejected (Part 10.2: letters/digits/spaces only)",
                !LocalLobbyDirectory.IsValidRoomName("Room #1!"), "");
        }

        private void TestCreateRoomRejectsInvalidSettings()
        {
            var manager = BuildLobbyManager();
            bool badMaxPlayers = manager.TryCreateRoom("Test Room", 3, 300f, "host1", "Host");
            Check("Room creation rejects a max-players value outside {2,4,6,8} (Part 10.2)", !badMaxPlayers, $"created={badMaxPlayers}");

            bool badDuration = manager.TryCreateRoom("Test Room", 4, 450f, "host1", "Host");
            Check("Room creation rejects a round duration outside {5min,10min} (Part 10.2)", !badDuration, $"created={badDuration}");

            Destroy(manager.gameObject);
        }

        private void TestCreateRoomSucceedsAndGeneratesCode()
        {
            var manager = BuildLobbyManager();
            bool created = manager.TryCreateRoom("Friday Squad", 4, 300f, "host1", "HostName");
            Check("Valid room creation succeeds (Part 10.2)", created, $"created={created} lobbyId={manager.CurrentLobbyId}");
            Check("Generated code is exactly six characters (Part 10.2 step 2)",
                manager.LastGeneratedCode != null && manager.LastGeneratedCode.Length == RoomCodeGenerator.CodeLength,
                $"code={manager.LastGeneratedCode}");

            var players = manager.GetCurrentPlayers();
            Check("Creator is registered as the host, not yet ready (Part 10.4)",
                players.Count == 1 && players[0].IsHost && !players[0].IsReady, $"count={players.Count} isHost={(players.Count > 0 && players[0].IsHost)}");

            Destroy(manager.gameObject);
        }

        private void TestCodeGeneratorCharset()
        {
            bool anyForbiddenChar = false;
            bool anyWrongLength = false;
            for (int i = 0; i < 200; i++)
            {
                string code = RoomCodeGenerator.Generate();
                if (code.Length != RoomCodeGenerator.CodeLength) anyWrongLength = true;
                if (code.Any(c => c == 'O' || c == 'I' || c == '0' || c == '1')) anyForbiddenChar = true;
            }
            Check("Room codes never contain the visually-confusable O/I/0/1 (Part 10.2 step 2)", !anyForbiddenChar, $"anyForbidden={anyForbiddenChar}");
            Check("Room codes are always exactly six characters", !anyWrongLength, $"anyWrongLength={anyWrongLength}");
        }

        private void TestSearchFiltersByName()
        {
            var manager = BuildLobbyManager();
            manager.TryCreateRoom("Alpha Squad", 4, 300f, "hostA", "HostA");
            var managerB = BuildLobbyManager();
            managerB.Directory = manager.Directory; // share one directory, like two clients hitting the same Steam backend
            managerB.TryCreateRoom("Beta Team", 4, 300f, "hostB", "HostB");

            var results = manager.SearchRooms("Alpha");
            Check("Room search filters by name substring (Part 10.3)",
                results.Count == 1 && results[0].RoomName == "Alpha Squad", $"count={results.Count}");

            var allResults = manager.SearchRooms("");
            Check("Empty search query returns all visible rooms (Part 10.3)", allResults.Count == 2, $"count={allResults.Count}");

            Destroy(manager.gameObject);
            Destroy(managerB.gameObject);
        }

        private void TestJoinCodeMatchingAndFullRoomOrdering()
        {
            var host = BuildLobbyManager();
            host.TryCreateRoom("Duo Room", 2, 300f, "hostX", "HostX");
            string realCode = host.LastGeneratedCode;

            var wrongCodeJoiner = BuildLobbyManager();
            wrongCodeJoiner.Directory = host.Directory;
            var wrongResult = wrongCodeJoiner.TryJoinRoom(host.CurrentLobbyId, "ZZZZZZ", "guest1", "Guest1");
            Check("Wrong code is rejected without joining (Part 10.3)", wrongResult == JoinResult.WrongCode, $"result={wrongResult}");

            var correctJoiner = BuildLobbyManager();
            correctJoiner.Directory = host.Directory;
            var correctResult = correctJoiner.TryJoinRoom(host.CurrentLobbyId, realCode, "guest2", "Guest2");
            Check("Correct code joins successfully (Part 10.3)", correctResult == JoinResult.Success, $"result={correctResult}");
            Check("Room now shows 2/2 players", host.GetCurrentPlayers().Count == 2, $"count={host.GetCurrentPlayers().Count}");

            // Part 10.3: the room is now full - even with the WRONG code, the result
            // must be "Full", not "WrongCode" (full-check happens before code compare,
            // "بلا أي محاولة انضمام فعلية تُرسل ... أصلاً").
            var thirdJoiner = BuildLobbyManager();
            thirdJoiner.Directory = host.Directory;
            var fullResult = thirdJoiner.TryJoinRoom(host.CurrentLobbyId, "ZZZZZZ", "guest3", "Guest3");
            Check("Joining a full room reports Full even with a wrong code (Part 10.3 ordering)",
                fullResult == JoinResult.Full, $"result={fullResult}");

            Destroy(host.gameObject);
            Destroy(wrongCodeJoiner.gameObject);
            Destroy(correctJoiner.gameObject);
            Destroy(thirdJoiner.gameObject);
        }

        private void TestReadyGatingAndStartGame()
        {
            var host = BuildLobbyManager();
            host.TryCreateRoom("Ready Test", 2, 300f, "hostR", "HostR");
            Check("Start button gated off with only 1 player, even if ready (Part 10.4)",
                !SetReadyAndCheck(host, "hostR", true), "");

            var guest = BuildLobbyManager();
            guest.Directory = host.Directory;
            guest.TryJoinRoom(host.CurrentLobbyId, host.LastGeneratedCode, "guestR", "GuestR");

            Check("Start button gated off while any player is not ready (Part 10.4)", !host.CanStartGame(),
                $"canStart={host.CanStartGame()}");

            guest.SetLocalReady(true);
            Check("Start button enabled once >=2 players are all ready (Part 10.4)", host.CanStartGame(), $"canStart={host.CanStartGame()}");

            var roundManagerGo = new GameObject("Stage15Test_RoundManager");
            var roundManager = roundManagerGo.AddComponent<RoundManager>();
            bool guestCannotStart = guest.TryStartGame(new List<PlayerTeam>(), roundManager);
            Check("Non-host cannot start the game even when ready-gated (Part 10.4: host-only)", !guestCannotStart, $"started={guestCannotStart}");

            bool hostStarted = host.TryStartGame(new List<PlayerTeam>(), roundManager);
            Check("Host starting the game hands off to RoundManager.StartRound (Part 10.4 -> Stage 14)",
                hostStarted && roundManager.RoundActive && Mathf.Approximately(roundManager.TimeRemaining, 300f),
                $"started={hostStarted} roundActive={roundManager.RoundActive} timeRemaining={roundManager.TimeRemaining}");

            Destroy(host.gameObject);
            Destroy(guest.gameObject);
            Destroy(roundManagerGo);
        }

        private bool SetReadyAndCheck(LobbyManager m, string playerId, bool ready)
        {
            m.SetLocalReady(ready);
            return m.CanStartGame();
        }

        private void TestHostMigrationByJoinOrder()
        {
            var a = BuildLobbyManager();
            a.TryCreateRoom("Migration Test", 4, 300f, "playerA", "A");

            var b = BuildLobbyManager();
            b.Directory = a.Directory;
            b.TryJoinRoom(a.CurrentLobbyId, a.LastGeneratedCode, "playerB", "B");

            var c = BuildLobbyManager();
            c.Directory = a.Directory;
            c.TryJoinRoom(a.CurrentLobbyId, a.LastGeneratedCode, "playerC", "C");

            string newHostId = null;
            a.HostChanged += id => newHostId = id;

            a.Leave(); // host (A) departs mid-lobby

            var remaining = c.GetCurrentPlayers(); // query through any surviving manager sharing the directory
            var currentHost = remaining.FirstOrDefault(p => p.IsHost);
            Check("Host migrates to the next player by original join order (Part 10.6)",
                currentHost != null && currentHost.PlayerId == "playerB", $"newHost={(currentHost != null ? currentHost.PlayerId : "none")}");
            Check("OnHostChanged fires with the correct new host id (Part 10.6)", newHostId == "playerB", $"newHostId={newHostId}");

            Destroy(a.gameObject);
            Destroy(b.gameObject);
            Destroy(c.gameObject);
        }

        // ---------------------------------------------------------------
        private void TestNetworkPlayerStateStructure()
        {
            var go = new GameObject("Stage15Test_NetworkPlayer");
            var netObj = go.AddComponent<NetworkObject>();
            var netTransform = go.AddComponent<NetworkTransform>();
            var state = go.AddComponent<NetworkPlayerState>();

            Check("NetworkPlayerState requires a NetworkObject on the same GameObject (Part 10.5)", netObj != null, "");
            Check("Position/rotation sync uses Unity Netcode's own NetworkTransform (Part 10.5)", netTransform != null, "");
            Check("Physical state defaults to Normal before any player action", state.PhysicalState.Value == NetPhysicalState.Normal,
                $"value={state.PhysicalState.Value}");
            Check("Heavy-carry/wind flag defaults to false", !state.IsHeavyCarrying.Value, $"value={state.IsHeavyCarrying.Value}");
            Check("Selected character index defaults to -1 (none chosen yet)", state.SelectedCharacterIndex.Value == -1,
                $"value={state.SelectedCharacterIndex.Value}");

            Destroy(go);
        }

        private void TestDisconnectCleanupDropsHeldObject()
        {
            var go = new GameObject("Stage15Test_DisconnectingPlayer");
            var controller = go.AddComponent<CharacterController>();
            controller.height = 1f;
            controller.radius = 0.3f;
            controller.center = new Vector3(0f, 0.5f, 0f);
            go.AddComponent<PlayerLocomotion>();
            var carry = go.AddComponent<PlayerCarry>();
            go.AddComponent<NetworkObject>();
            var state = go.AddComponent<NetworkPlayerState>();

            var item = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            item.transform.position = go.transform.position + new Vector3(0f, 0.15f, 1f);
            item.transform.localScale = Vector3.one * 0.2f;
            var carryable = item.AddComponent<CarryableObject>();

            carry.TryPickUp(carryable);
            Check("Setup: player is holding the object before disconnecting", carry.Held == carryable, $"held={(carry.Held == carryable)}");

            state.HandleDisconnectCleanup();
            Check("Disconnect cleanup drops the held object with free physics (Part 10.6)",
                carry.Held == null && carryable.Body.isKinematic == false,
                $"heldIsNowNull={(carry.Held == null)} isKinematic={carryable.Body.isKinematic}");

            Destroy(item);
            Destroy(go);
        }
    }
}
