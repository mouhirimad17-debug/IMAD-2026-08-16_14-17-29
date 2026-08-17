using UnityEngine;

namespace PrankMansion.Player
{
    public enum Team { None, Team1, Team2 }

    /// <summary>
    /// Part 9.5's team membership + Part 9.5.2's fixed distinguishing color (blue for
    /// Team1, red for Team2). Assigned once per round by TeamAssignmentService, never
    /// by the player's own choice or lobby join order. Doesn't touch the character's
    /// own model/appearance at all (9.5.2: "لا يغيّر مظهر الشخصية نفسها") - purely
    /// data that other systems (scoring, and eventually Part 11.5's UI) read.
    /// </summary>
    public class PlayerTeam : MonoBehaviour
    {
        public static readonly Color Team1Color = Color.blue;
        public static readonly Color Team2Color = Color.red;

        public Team Team { get; private set; } = Team.None;

        public void SetTeam(Team team) => Team = team;

        public static Color GetColor(Team team) => team switch
        {
            Team.Team1 => Team1Color,
            Team.Team2 => Team2Color,
            _ => Color.white
        };

        public static Team Opponent(Team team) => team switch
        {
            Team.Team1 => Team.Team2,
            Team.Team2 => Team.Team1,
            _ => Team.None
        };
    }
}
