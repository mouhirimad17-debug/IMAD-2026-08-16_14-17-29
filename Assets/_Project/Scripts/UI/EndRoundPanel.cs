using System;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.6's end-of-round screen. The optional MVP stat ("قسم إحصائيات
    /// مختصر اختياري") is skipped - Stage 14's RoundManager only tracks TEAM-level
    /// scores, not per-player attribution, and retrofitting every Part 9.1 scoring
    /// call site to also track an individual scorer isn't justified for a feature
    /// the document itself marks optional. Per-personal-result win/lose music
    /// selection IS built (comparing the local player's own team against
    /// RoundManager.WinnerTeam) using PlaceholderAudio tones, since no real music
    /// assets exist yet (Part 13's full mix is Stage 18's job) but the SELECTION
    /// LOGIC itself doesn't need to wait for real audio files any more than
    /// Stage 14's alert beep did.
    /// </summary>
    public class EndRoundPanel : MonoBehaviour
    {
        public event Action OnPlayAgain;
        public event Action OnMainMenu;

        public Text WinnerText { get; private set; }
        public Text ScoresText { get; private set; }
        public AudioClip LastPlayedClip { get; private set; }

        private AudioSource musicSource;

        public void BuildUI(RoundManager roundManager, Team localTeam)
        {
            var canvas = UIBuilder.CreateScreenCanvas("EndRoundCanvas", transform);

            string winnerLabel = roundManager.IsTie ? "It's a Tie!" : $"{TeamName(roundManager.WinnerTeam)} Wins!";
            WinnerText = UIBuilder.CreateText(canvas.transform, "WinnerText", winnerLabel, 56,
                roundManager.IsTie ? Color.white : PlayerTeam.GetColor(roundManager.WinnerTeam));

            ScoresText = UIBuilder.CreateText(canvas.transform, "ScoresText",
                $"Team 1: {roundManager.ScoreTeam1}    Team 2: {roundManager.ScoreTeam2}", 28, Color.white);

            var playAgain = UIBuilder.CreateButton(canvas.transform, "PlayAgainButton", "Play Again", new Color(0.2f, 0.7f, 0.3f), Color.white);
            playAgain.onClick.AddListener(() => OnPlayAgain?.Invoke());

            var mainMenu = UIBuilder.CreateButton(canvas.transform, "MainMenuButton", "Main Menu", new Color(0.4f, 0.4f, 0.4f), Color.white);
            mainMenu.onClick.AddListener(() => OnMainMenu?.Invoke());

            PlayPersonalResultMusic(roundManager, localTeam);
        }

        private void PlayPersonalResultMusic(RoundManager roundManager, Team localTeam)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;

            AudioClip toPlay;
            if (roundManager.IsTie) toPlay = PlaceholderAudio.GenerateTone("Placeholder_TieJingle", 330f, 1f, 0.3f);
            else if (roundManager.WinnerTeam == localTeam) toPlay = PlaceholderAudio.GenerateTone("Placeholder_VictoryJingle", 523f, 1f, 0.3f);
            else toPlay = PlaceholderAudio.GenerateTone("Placeholder_DefeatJingle", 220f, 1f, 0.3f);

            LastPlayedClip = toPlay;
            musicSource.clip = toPlay;
            musicSource.Play();
        }

        private static string TeamName(Team team) => team == Team.Team1 ? "Team 1" : "Team 2";
    }
}
