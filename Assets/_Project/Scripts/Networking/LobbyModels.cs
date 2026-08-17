namespace PrankMansion.Networking
{
    public enum JoinResult { Success, WrongCode, Full, NotFound }

    /// Part 10.2's three mandatory create-room fields.
    public struct LobbySettings
    {
        public string RoomName;
        public int MaxPlayers;             // one of 2, 4, 6, 8
        public float RoundDurationSeconds; // 300 (5min) or 600 (10min)

        public static readonly int[] AllowedMaxPlayers = { 2, 4, 6, 8 };
        public static readonly float[] AllowedRoundDurations = { 300f, 600f };
    }

    /// Part 10.3's search-result row: room name, current/max count, round duration.
    public struct LobbyInfo
    {
        public string LobbyId;
        public string RoomName;
        public int CurrentPlayerCount;
        public int MaxPlayers;
        public float RoundDurationSeconds;
    }

    /// Part 10.4's waiting-room row: visual model (via SelectedCharacterIndex),
    /// display name, ready indicator. JoinOrder backs Part 10.6's host migration.
    public class PlayerLobbyEntry
    {
        public string PlayerId;
        public string DisplayName;
        public int SelectedCharacterIndex = -1;
        public bool IsReady;
        public bool IsHost;
        public int JoinOrder;
    }
}
