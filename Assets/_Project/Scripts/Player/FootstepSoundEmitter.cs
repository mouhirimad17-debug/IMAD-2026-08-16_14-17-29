using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 5.1's "نطاق سماع صوت الخطوات القياسي بين اللاعبين" - periodically emits a
    /// footstep SoundEvents pulse while actually walking/running on the ground.
    /// audibleRange is the per-character trait Stage 12's CharacterSelector sets
    /// (standard 8m; Fifi 4m per Part 5.2 - quieter to everyone else, not himself).
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    public class FootstepSoundEmitter : MonoBehaviour
    {
        // DECISION: no footstep cadence number is given anywhere in the document -
        // a brisk, natural-feeling interval while actually moving.
        public const float StepIntervalSeconds = 0.4f;
        public const float MovingSpeedThreshold = 0.5f;

        public float audibleRange = CharacterProfile.StandardFootstepAudibleRange;

        private PlayerLocomotion locomotion;
        private float stepTimer;

        private void Awake() => locomotion = GetComponent<PlayerLocomotion>();

        private void Update()
        {
            if (!locomotion.IsGrounded || locomotion.CurrentHorizontalSpeed < MovingSpeedThreshold)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer += Time.deltaTime;
            if (stepTimer < StepIntervalSeconds) return;
            stepTimer = 0f;
            SoundEvents.Emit(transform.position, audibleRange, gameObject);
        }
    }
}
