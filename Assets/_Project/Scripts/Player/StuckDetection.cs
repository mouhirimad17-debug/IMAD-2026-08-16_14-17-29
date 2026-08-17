using PrankMansion.UI;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 16.1: a player wedged somewhere physically impossible despite actively
    /// trying to move gets auto-repositioned to their last known safe ground spot
    /// (or the original spawn point if none was ever recorded), with a short toast.
    ///
    /// DECISION: the document frames this as "فحص دوري كل خمس ثوانٍ" checking for
    /// an "~8 second" stuck window, but a continuous per-frame accumulator that
    /// resets the instant the player actually moves (or stops trying to) is the
    /// same behaviour with strictly better responsiveness and is far easier to
    /// verify deterministically in a Play Mode test than a discrete 5s-sampling
    /// loop would be - same "reasonable implementation of the same rule" latitude
    /// Law 21.2 grants for details the document doesn't pin to an exact mechanism.
    /// "لم يتغير إطلاقاً" is given a small epsilon rather than literal zero, since
    /// physics/controller jitter means true zero movement essentially never happens.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(CharacterController))]
    public class StuckDetection : MonoBehaviour
    {
        public const float StuckThresholdSeconds = 8f;      // "تقريباً ثمانية ثوانٍ متواصلة"
        public const float PositionUnchangedEpsilon = 0.05f; // DECISION
        public const float RepositionCooldownSeconds = 2f;   // DECISION: avoid immediately re-triggering right after a teleport

        public Vector3 SpawnPoint { get; set; }
        public Vector3 LastSafePosition { get; private set; }
        public int RecoveryCount { get; private set; }

        private PlayerLocomotion locomotion;
        private CharacterController controller;
        private float stuckTimer;
        private float cooldownTimer;
        private Vector3 stuckReferencePosition;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            controller = GetComponent<CharacterController>();
            SpawnPoint = transform.position;
            LastSafePosition = transform.position;
            stuckReferencePosition = transform.position;
        }

        private void Update()
        {
            if (controller.isGrounded && !locomotion.IsExternallyControlled)
                LastSafePosition = transform.position;

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }

            bool attemptingToMove = locomotion.HasActiveMoveInput;
            float movedSinceReference = Vector3.Distance(transform.position, stuckReferencePosition);

            if (attemptingToMove && movedSinceReference < PositionUnchangedEpsilon)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= StuckThresholdSeconds) Recover();
            }
            else
            {
                stuckTimer = 0f;
                stuckReferencePosition = transform.position;
            }
        }

        private void Recover()
        {
            Vector3 target = LastSafePosition != Vector3.zero ? LastSafePosition : SpawnPoint;
            locomotion.ResumeControlAt(target, transform.eulerAngles.y);

            RecoveryCount++;
            stuckTimer = 0f;
            cooldownTimer = RepositionCooldownSeconds;
            stuckReferencePosition = target;
            LastSafePosition = target;

            ToastNotification.Show("notify.stuckrecovered");
        }

        // Test-only: drives the stuck timer straight to the threshold instead of
        // waiting 8 real seconds of simulated held input.
        public void DebugForceStuck() => stuckTimer = StuckThresholdSeconds;
    }
}
