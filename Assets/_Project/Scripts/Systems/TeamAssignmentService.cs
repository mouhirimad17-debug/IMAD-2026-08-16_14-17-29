using System.Collections.Generic;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Systems
{
    /// <summary>
    /// Part 9.5.1's team split: always exactly 2 teams, split perfectly evenly, in
    /// FULL random order (not lobby join order - "لتفادي أي انحياز أو تجمع أصدقاء
    /// مقصود يُخل بتوازن الفريقين"). Room sizes are always even (2/4/6/8 per Part
    /// 10), so an odd count is explicitly "won't happen, no handling needed" per the
    /// document - any leftover here just goes to Team1, a harmless fallback for a
    /// case the document says can't occur.
    /// </summary>
    public static class TeamAssignmentService
    {
        public static void AssignRandomly(IList<PlayerTeam> players)
        {
            var shuffled = new List<PlayerTeam>(players);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            int half = shuffled.Count / 2;
            for (int i = 0; i < shuffled.Count; i++)
                shuffled[i].SetTeam(i < half ? Team.Team1 : Team.Team2);
        }
    }
}
