using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace PrankMansion.UI
{
    /// <summary>
    /// Part 11.5's in-round HUD: per-team score counters (team-colored), the round
    /// timer (red + blinking in the final 30s per RoundManager.IsFinalCountdown),
    /// and the local player's carried-item/heavy-carry-warning indicator. Floating
    /// per-player nameplates are PlayerNameplate.cs (world-space, not part of this
    /// screen-space overlay). Reno's own directional arrow already exists
    /// (RenoSoundIndicatorUI, built in Stage 12) and needs no changes here.
    /// </summary>
    public class GameplayHUD : MonoBehaviour
    {
        public const float FinalCountdownBlinkHz = 2f; // DECISION: "يومض بخفة" has no exact rate given

        private RoundManager roundManager;
        private PlayerCarry localPlayerCarry;

        public Text ScoreTeam1Text { get; private set; }
        public Text ScoreTeam2Text { get; private set; }
        public Text TimerText { get; private set; }
        public GameObject CarryWarningIcon { get; private set; }

        public void BuildUI(RoundManager manager, PlayerCarry localCarry)
        {
            roundManager = manager;
            localPlayerCarry = localCarry;

            var canvas = UIBuilder.CreateScreenCanvas("GameplayHUDCanvas", transform);

            ScoreTeam1Text = UIBuilder.CreateText(canvas.transform, "ScoreTeam1", "0", 32, PlayerTeam.Team1Color);
            ScoreTeam2Text = UIBuilder.CreateText(canvas.transform, "ScoreTeam2", "0", 32, PlayerTeam.Team2Color);
            TimerText = UIBuilder.CreateText(canvas.transform, "Timer", "0:00", 36, Color.white);

            var warningGo = new GameObject("CarryWarningIcon");
            warningGo.transform.SetParent(canvas.transform, false);
            var warningImg = warningGo.AddComponent<Image>();
            warningImg.color = new Color(1f, 0.85f, 0f); // yellow exclamation-mark placeholder
            CarryWarningIcon = warningGo;
            CarryWarningIcon.SetActive(false);
        }

        private void Update()
        {
            if (roundManager == null) return;

            ScoreTeam1Text.text = roundManager.ScoreTeam1.ToString();
            ScoreTeam2Text.text = roundManager.ScoreTeam2.ToString();

            int minutes = Mathf.FloorToInt(roundManager.TimeRemaining / 60f);
            int seconds = Mathf.FloorToInt(roundManager.TimeRemaining % 60f);
            TimerText.text = $"{minutes}:{seconds:00}";

            if (roundManager.IsFinalCountdown)
            {
                bool blinkOn = Mathf.FloorToInt(Time.time * FinalCountdownBlinkHz * 2f) % 2 == 0;
                TimerText.color = blinkOn ? Color.red : new Color(1f, 0.4f, 0.4f);
            }
            else
            {
                TimerText.color = Color.white;
            }

            // Part 11.5: "علامة تعجب صفراء صغيرة تظهر تلقائياً عند دخول حالة حمل
            // ثقيل بمفرد" - shown while WindActive (heavy-carry-solo state).
            bool showWarning = localPlayerCarry != null && localPlayerCarry.WindActive;
            if (CarryWarningIcon.activeSelf != showWarning) CarryWarningIcon.SetActive(showWarning);
        }
    }
}
