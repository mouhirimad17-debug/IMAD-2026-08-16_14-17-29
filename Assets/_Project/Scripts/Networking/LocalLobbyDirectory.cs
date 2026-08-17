using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PrankMansion.Networking
{
    /// <summary>
    /// Local, single-process stand-in for Steam's lobby system (Law 0.2 - no
    /// Steamworks App ID exists yet, per the owner's explicit decision to build
    /// this stage's architecture now and defer real Steam I/O). Implements exactly
    /// Part 10.2/10.3/10.4's rules (room-name validation, six-character code
    /// matching, full-room detection, host-tracked join order) so LobbyManager and
    /// everything above it can be built and verified today; a future
    /// SteamLobbyDirectory implements the same ILobbyDirectory contract against
    /// real Steamworks.NET lobby calls and swaps in without touching LobbyManager.
    /// </summary>
    public class LocalLobbyDirectory : ILobbyDirectory
    {
        private class LobbyRecord
        {
            public LobbySettings Settings;
            public string Code;
            public readonly List<PlayerLobbyEntry> Players = new List<PlayerLobbyEntry>();
            public int NextJoinOrder;
        }

        private readonly Dictionary<string, LobbyRecord> lobbies = new Dictionary<string, LobbyRecord>();
        private int nextLobbyId = 1;

        public event Action<string, string> OnHostChanged;
        public event Action<string> OnLobbyClosed;

        // Part 10.2: "بحد أقصى عشرين حرفاً، مسموح فقط بالأحرف والأرقام والمسافات".
        public static bool IsValidRoomName(string name) =>
            !string.IsNullOrEmpty(name) && name.Length <= 20 && Regex.IsMatch(name, @"^[A-Za-z0-9 ]+$");

        public (string lobbyId, string code) CreateLobby(LobbySettings settings, PlayerLobbyEntry host)
        {
            string lobbyId = "local_" + nextLobbyId++;
            string code = RoomCodeGenerator.Generate();

            var record = new LobbyRecord { Settings = settings, Code = code };
            host.IsHost = true;
            host.JoinOrder = record.NextJoinOrder++;
            record.Players.Add(host);
            lobbies[lobbyId] = record;

            return (lobbyId, code);
        }

        public List<LobbyInfo> SearchByName(string query)
        {
            var results = new List<LobbyInfo>();
            foreach (var kv in lobbies)
            {
                var r = kv.Value;
                if (!string.IsNullOrEmpty(query) && r.Settings.RoomName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                results.Add(new LobbyInfo
                {
                    LobbyId = kv.Key,
                    RoomName = r.Settings.RoomName,
                    CurrentPlayerCount = r.Players.Count,
                    MaxPlayers = r.Settings.MaxPlayers,
                    RoundDurationSeconds = r.Settings.RoundDurationSeconds
                });
            }
            return results;
        }

        // Part 10.3: full-room check happens BEFORE the code comparison ("بلا أي
        // محاولة انضمام فعلية تُرسل ... أصلاً" if full) - checked in that order below.
        public JoinResult TryJoin(string lobbyId, string code, PlayerLobbyEntry joiningPlayer)
        {
            if (!lobbies.TryGetValue(lobbyId, out var record)) return JoinResult.NotFound;
            if (record.Players.Count >= record.Settings.MaxPlayers) return JoinResult.Full;
            if (!string.Equals(record.Code, code, StringComparison.Ordinal)) return JoinResult.WrongCode;

            joiningPlayer.IsHost = false;
            joiningPlayer.JoinOrder = record.NextJoinOrder++;
            record.Players.Add(joiningPlayer);
            return JoinResult.Success;
        }

        public List<PlayerLobbyEntry> GetPlayers(string lobbyId) =>
            lobbies.TryGetValue(lobbyId, out var record) ? new List<PlayerLobbyEntry>(record.Players) : new List<PlayerLobbyEntry>();

        public LobbySettings GetSettings(string lobbyId) => lobbies[lobbyId].Settings;

        public void SetReady(string lobbyId, string playerId, bool ready)
        {
            var p = lobbies[lobbyId].Players.FirstOrDefault(x => x.PlayerId == playerId);
            if (p != null) p.IsReady = ready;
        }

        public void SetCharacter(string lobbyId, string playerId, int characterIndex)
        {
            var p = lobbies[lobbyId].Players.FirstOrDefault(x => x.PlayerId == playerId);
            if (p != null) p.SelectedCharacterIndex = characterIndex;
        }

        public void Leave(string lobbyId, string playerId)
        {
            if (!lobbies.TryGetValue(lobbyId, out var record)) return;
            var leaving = record.Players.FirstOrDefault(x => x.PlayerId == playerId);
            if (leaving == null) return;

            bool wasHost = leaving.IsHost;
            record.Players.Remove(leaving);

            if (record.Players.Count == 0)
            {
                lobbies.Remove(lobbyId);
                return;
            }

            if (!wasHost) return;

            var next = HostMigrationController.SelectNextHost(record.Players);
            if (next != null)
            {
                next.IsHost = true;
                OnHostChanged?.Invoke(lobbyId, next.PlayerId);
            }
            else
            {
                // Unreachable locally (Players.Count > 0 already guaranteed above),
                // but models the real Steam implementation's genuine failure mode.
                lobbies.Remove(lobbyId);
                OnLobbyClosed?.Invoke(lobbyId);
            }
        }
    }
}
