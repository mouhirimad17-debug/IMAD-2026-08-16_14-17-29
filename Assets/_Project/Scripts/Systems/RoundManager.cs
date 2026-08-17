using System;
using System.Collections.Generic;
using PrankMansion.Entities;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Systems
{
    /// <summary>
    /// Part 9's score/round orchestration, and the trigger point for Part 9.5's team
    /// split. StartRound stands in for "host presses start in the Part 10.4 lobby"
    /// (Stage 15's real multiplayer lobby doesn't exist yet) - a future lobby just
    /// calls this with its own connected-player list. The actual on-screen widgets
    /// (Part 9.2's score/timer display, the red countdown color, Part 9.5.3's team-
    /// reveal screen, Part 11.6's end screen) are Part 11's UI, Stage 16 - this stage
    /// exposes the data those screens will read (ScoreTeam1/2, TimeRemaining,
    /// IsFinalCountdown, WinnerTeam, IsTie) rather than building placeholder UI for
    /// them. The one Part 9.2 behaviour that doesn't need a screen to exist first -
    /// the pulsing final-30-seconds alert sound - is built now.
    /// </summary>
    public class RoundManager : MonoBehaviour
    {
        public const float ShortRoundSeconds = 300f;  // "خمس دقائق"
        public const float LongRoundSeconds = 600f;   // "عشر دقائق"
        public const float FinalCountdownSeconds = 30f;
        public const float FinalCountdownBeepIntervalSeconds = 5f;
        public const float ThrowHitMinSpeed = 3f;              // Part 9.1
        public const float EndScreenTransitionSeconds = 2f;    // Part 9.3 (Stage 16 owns the actual transition)

        public static RoundManager Instance { get; private set; }

        public int ScoreTeam1 { get; private set; }
        public int ScoreTeam2 { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool RoundActive { get; private set; }
        public bool IsFinalCountdown => RoundActive && TimeRemaining <= FinalCountdownSeconds && TimeRemaining > 0f;
        public Team WinnerTeam { get; private set; } = Team.None;
        public bool IsTie { get; private set; }
        public bool HasEnded { get; private set; }

        public event Action OnRoundEnded;

        private AudioSource alertAudio;
        private AudioClip alertClip;
        private float beepTimer;

        private void Awake()
        {
            Instance = this;
            alertAudio = gameObject.AddComponent<AudioSource>();
            alertAudio.playOnAwake = false;
            alertAudio.spatialBlend = 0f; // Part 9.2's alert is a global cue, not positional
            alertClip = PlaceholderAudio.GenerateTone("Placeholder_RoundAlert", 880f, 0.15f, 0.3f);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void StartRound(IList<PlayerTeam> connectedPlayers, float durationSeconds)
        {
            TeamAssignmentService.AssignRandomly(connectedPlayers);
            SlipZone.ClearAllForNewRound(); // Part 7.4: "تُمسح كل الفخاخ تلقائياً عند بداية جولة جديدة"

            ScoreTeam1 = 0;
            ScoreTeam2 = 0;
            TimeRemaining = durationSeconds;
            RoundActive = true;
            HasEnded = false;
            WinnerTeam = Team.None;
            IsTie = false;
            beepTimer = 0f;
            PlayerInputReader.RoundInputFrozen = false;
        }

        public void RegisterPoint(Team scoringTeam)
        {
            if (!RoundActive || scoringTeam == Team.None) return;
            if (scoringTeam == Team.Team1) ScoreTeam1++;
            else ScoreTeam2++;
        }

        private void Update()
        {
            if (!RoundActive) return;

            TimeRemaining -= Time.deltaTime;

            if (IsFinalCountdown)
            {
                beepTimer += Time.deltaTime;
                if (beepTimer >= FinalCountdownBeepIntervalSeconds)
                {
                    beepTimer = 0f;
                    alertAudio.PlayOneShot(alertClip);
                }
            }

            if (TimeRemaining <= 0f)
            {
                TimeRemaining = 0f;
                EndRound();
            }
        }

        private void EndRound()
        {
            RoundActive = false;
            HasEnded = true;
            // Part 9.3: "تعطيل مؤقت لمدخلات الحركة والتفاعل، بلا تجميد الفيزياء نفسها"
            PlayerInputReader.RoundInputFrozen = true;

            if (ScoreTeam1 == ScoreTeam2) IsTie = true;
            else WinnerTeam = ScoreTeam1 > ScoreTeam2 ? Team.Team1 : Team.Team2;

            OnRoundEnded?.Invoke();
        }
    }
}
