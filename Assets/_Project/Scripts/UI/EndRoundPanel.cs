using System;
using PrankMansion.Localization;
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
        private RoundManager roundManagerRef;

        public void BuildUI(RoundManager roundManager, Team localTeam)
        {
            roundManagerRef = roundManager;
            var canvas = UIBuilder.CreateScreenCanvas("EndRoundCanvas", transform);

            WinnerText = UIBuilder.CreateText(canvas.transform, "WinnerText", "", 56,
                roundManager.IsTie ? Color.white : PlayerTeam.GetColor(roundManager.WinnerTeam));
            ScoresText = UIBuilder.CreateText(canvas.transform, "ScoresText", "", 28, Color.white);
            RefreshTexts();

            var playAgain = UIBuilder.CreateLocalizedButton(canvas.transform, "PlayAgainButton", "endround.playagain", new Color(0.2f, 0.7f, 0.3f), Color.white);
            playAgain.onClick.AddListener(() => OnPlayAgain?.Invoke());

            var mainMenu = UIBuilder.CreateLocalizedButton(canvas.transform, "MainMenuButton", "endround.mainmenu", new Color(0.4f, 0.4f, 0.4f), Color.white);
            mainMenu.onClick.AddListener(() => OnMainMenu?.Invoke());

            PlayPersonalResultMusic(roundManager, localTeam);
        }

        private void OnEnable() => LocalizationManager.OnLanguageChanged += RefreshTexts;
        private void OnDisable() => LocalizationManager.OnLanguageChanged -= RefreshTexts;

        private void RefreshTexts()
        {
            if (roundManagerRef == null) return;

            WinnerText.text = roundManagerRef.IsTie
                ? LocalizationManager.Get("endround.tie")
                : LocalizationManager.Format("endround.winner", TeamName(roundManagerRef.WinnerTeam));
            WinnerText.font = LocalizationManager.GetFont();

            ScoresText.text = LocalizationManager.Format("endround.scores", roundManagerRef.ScoreTeam1, roundManagerRef.ScoreTeam2);
            ScoresText.font = LocalizationManager.GetFont();
        }

        private void PlayPersonalResultMusic(RoundManager roundManager, Team localTeam)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            AudioService.RegisterMusic(musicSource); // Part 13.1: "موسيقى نهاية الجولة" - MUSIC slider, not SFX

            AudioClip toPlay;
            if (roundManager.IsTie) toPlay = PlaceholderAudio.GenerateTone("Placeholder_TieJingle", 330f, 1f, 0.3f);
            else if (roundManager.WinnerTeam == localTeam) toPlay = PlaceholderAudio.GenerateTone("Placeholder_VictoryJingle", 523f, 1f, 0.3f);
            else toPlay = PlaceholderAudio.GenerateTone("Placeholder_DefeatJingle", 220f, 1f, 0.3f);

            LastPlayedClip = toPlay;
            musicSource.clip = toPlay;
            musicSource.Play();
        }

        private static string TeamName(Team team) => LocalizationManager.Get(team == Team.Team1 ? "hud.team1" : "hud.team2");
    }
}
