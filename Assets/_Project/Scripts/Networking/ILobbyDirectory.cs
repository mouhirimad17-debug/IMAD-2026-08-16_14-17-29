using System;
using System.Collections.Generic;

namespace PrankMansion.Networking
{
    /// <summary>
    /// Part 10.2/10.3's "create/search/join a friends-only Steam lobby" abstracted
    /// behind an interface so Part 10.4's waiting-room logic (LobbyManager) can be
    /// built and verified NOW against LocalLobbyDirectory. A future
    /// SteamLobbyDirectory (Steamworks.NET, once an App ID exists) implements the
    /// exact same contract and swaps in transparently - the "build the logic now,
    /// wire the real backend later" pattern this project already uses for other
    /// unavailable dependencies (Law 0.2).
    /// </summary>
    public interface ILobbyDirectory
    {
        (string lobbyId, string code) CreateLobby(LobbySettings settings, PlayerLobbyEntry host);
        List<LobbyInfo> SearchByName(string query);
        JoinResult TryJoin(string lobbyId, string code, PlayerLobbyEntry joiningPlayer);
        List<PlayerLobbyEntry> GetPlayers(string lobbyId);
        LobbySettings GetSettings(string lobbyId);
        void SetReady(string lobbyId, string playerId, bool ready);
        void SetCharacter(string lobbyId, string playerId, int characterIndex);
        void Leave(string lobbyId, string playerId);

        /// Part 10.6: host left, migration succeeded - (lobbyId, newHostPlayerId).
        event Action<string, string> OnHostChanged;

        /// Part 10.6's "تعذّر تقنياً" fallback: migration could not complete, the
        /// round must end safely for whoever remains.
        event Action<string> OnLobbyClosed;
    }
}
