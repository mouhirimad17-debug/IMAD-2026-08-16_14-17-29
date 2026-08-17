using System;
using System.Collections.Generic;
using System.Linq;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Networking
{
    /// <summary>
    /// Part 10.2 (create), 10.3 (search/join), 10.4 (waiting room + start-game
    /// gating) business logic, driven by ILobbyDirectory (LocalLobbyDirectory for
    /// now - Law 0.2). The actual screens (Part 11.4) are Stage 16's job; this
    /// exposes the callable API/state those screens will drive and press against,
    /// the same "logic now, widgets later" pattern every prior stage with unbuilt
    /// UI has used (e.g. Stage 12's Reno indicator, Stage 14's RoundManager data).
    /// </summary>
    public class LobbyManager : MonoBehaviour
    {
        public const int MinPlayersToStart = 2; // Part 10.4: "الحد الأدنى المطلق للسماح باللعب"

        // Settable (not just constructed in Awake) because in the real Steam
        // implementation this is one shared backend every client talks to, not an
        // instance each LobbyManager owns privately - the default LocalLobbyDirectory
        // created in Awake is a convenience for standalone use; test code (and
        // eventually a real bootstrap) can assign a SHARED instance across multiple
        // LobbyManagers before use to simulate/drive multiple clients against the
        // same lobby. The custom setter re-wires event subscriptions so swapping
        // directories mid-lifetime (as tests do) never leaves stale/missing hooks.
        private ILobbyDirectory directory;
        public ILobbyDirectory Directory
        {
            get => directory;
            set
            {
                if (directory != null)
                {
                    directory.OnHostChanged -= HandleHostChanged;
                    directory.OnLobbyClosed -= HandleLobbyClosed;
                }
                directory = value;
                if (directory != null)
                {
                    directory.OnHostChanged += HandleHostChanged;
                    directory.OnLobbyClosed += HandleLobbyClosed;
                }
            }
        }

        public string CurrentLobbyId { get; private set; }
        public string LocalPlayerId { get; private set; }
        public string LastGeneratedCode { get; private set; }
        public JoinResult LastJoinResult { get; private set; } = JoinResult.NotFound;

        /// Part 10.6: fires when the host changes (migration succeeded) while this
        /// client is in the affected lobby.
        public event Action<string> HostChanged;
        /// Part 10.6's fallback: migration failed, the round must end safely.
        public event Action LobbyClosed;
        public event Action OnRoundStarted;

        private void Awake()
        {
            if (Directory == null) Directory = new LocalLobbyDirectory();
        }

        private void OnDestroy()
        {
            Directory = null; // runs the setter's unsubscribe path
        }

        private void HandleHostChanged(string lobbyId, string newHostId)
        {
            if (lobbyId == CurrentLobbyId) HostChanged?.Invoke(newHostId);
        }

        private void HandleLobbyClosed(string lobbyId)
        {
            if (lobbyId != CurrentLobbyId) return;
            CurrentLobbyId = null;
            LobbyClosed?.Invoke();
        }

        // ---- Part 10.2 ----
        public bool CanCreateRoom(string roomName) => LocalLobbyDirectory.IsValidRoomName(roomName);

        public bool TryCreateRoom(string roomName, int maxPlayers, float roundDurationSeconds, string localPlayerId, string displayName)
        {
            if (!CanCreateRoom(roomName)) return false;
            if (Array.IndexOf(LobbySettings.AllowedMaxPlayers, maxPlayers) < 0) return false;
            if (Array.IndexOf(LobbySettings.AllowedRoundDurations, roundDurationSeconds) < 0) return false;

            LocalPlayerId = localPlayerId;
            var host = new PlayerLobbyEntry { PlayerId = localPlayerId, DisplayName = displayName };
            var settings = new LobbySettings { RoomName = roomName, MaxPlayers = maxPlayers, RoundDurationSeconds = roundDurationSeconds };
            var (lobbyId, code) = Directory.CreateLobby(settings, host);

            CurrentLobbyId = lobbyId;
            LastGeneratedCode = code;
            return true;
        }

        // ---- Part 10.3 ----
        public List<LobbyInfo> SearchRooms(string query) => Directory.SearchByName(query);

        public JoinResult TryJoinRoom(string lobbyId, string code, string localPlayerId, string displayName)
        {
            LocalPlayerId = localPlayerId;
            var entry = new PlayerLobbyEntry { PlayerId = localPlayerId, DisplayName = displayName };
            var result = Directory.TryJoin(lobbyId, code, entry);

            LastJoinResult = result;
            if (result == JoinResult.Success) CurrentLobbyId = lobbyId;
            return result;
        }

        // ---- Part 10.4 ----
        public List<PlayerLobbyEntry> GetCurrentPlayers() =>
            CurrentLobbyId == null ? new List<PlayerLobbyEntry>() : Directory.GetPlayers(CurrentLobbyId);

        public void SetLocalReady(bool ready) => Directory.SetReady(CurrentLobbyId, LocalPlayerId, ready);
        public void SetLocalCharacter(int characterIndex) => Directory.SetCharacter(CurrentLobbyId, LocalPlayerId, characterIndex);

        public bool IsLocalHost() => GetCurrentPlayers().FirstOrDefault(p => p.PlayerId == LocalPlayerId)?.IsHost ?? false;

        // Part 10.4: start button enabled only once >=2 players are connected AND
        // every single one of them (host included) is marked ready.
        public bool CanStartGame()
        {
            var players = GetCurrentPlayers();
            return players.Count >= MinPlayersToStart && players.All(p => p.IsReady);
        }

        /// Hands off to Stage 14's RoundManager - Part 10.4's "بدء تحميل مشهد
        /// القصر ... بشكل متزامن" is RoundManager.StartRound's job once this
        /// returns true; the scene-load/spawn-point choreography itself is out of
        /// this stage's scope (needs the real UI/scene flow from Stage 16).
        public bool TryStartGame(List<PlayerTeam> connectedPlayerTeams, RoundManager roundManager)
        {
            if (!IsLocalHost() || !CanStartGame() || roundManager == null) return false;

            var settings = Directory.GetSettings(CurrentLobbyId);
            roundManager.StartRound(connectedPlayerTeams, settings.RoundDurationSeconds);
            OnRoundStarted?.Invoke();
            return true;
        }

        public void Leave()
        {
            if (CurrentLobbyId == null) return;
            Directory.Leave(CurrentLobbyId, LocalPlayerId);
            CurrentLobbyId = null;
        }
    }
}
