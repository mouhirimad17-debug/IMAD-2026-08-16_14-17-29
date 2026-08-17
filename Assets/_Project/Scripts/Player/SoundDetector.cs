using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// The listening half of Part 5.1/5.2's hearing mechanic. hearingRange is how
    /// far this listener can hear a standard-volume sound (standard 8m, Reno 15m);
    /// emitterAudibleRange is how far THIS particular sound carries relative to
    /// that same standard (standard 8m, Fifi 4m). The emitter's deviation from
    /// standard is applied on top of the listener's own range, so a listener's
    /// hearing bonus/penalty and a sound's loudness bonus/penalty both matter
    /// instead of the louder trait always being capped by the quieter one.
    /// </summary>
    public class SoundDetector : MonoBehaviour
    {
        // DECISION: how long a detection stays "recent" (e.g. for Reno's directional
        // indicator to keep pointing at it) isn't given a number - long enough to be
        // readable, short enough to feel responsive to the actual sound.
        public const float DetectionHoldSeconds = 1.5f;

        public float hearingRange = CharacterProfile.StandardHearingRange;

        public Vector3 LastDetectedDirection { get; private set; }
        public float LastDetectedTime { get; private set; } = -999f;
        public bool HasRecentDetection => Time.time - LastDetectedTime < DetectionHoldSeconds;

        private void OnEnable() => SoundEvents.OnSound += HandleSound;
        private void OnDisable() => SoundEvents.OnSound -= HandleSound;

        private void HandleSound(Vector3 position, float emitterAudibleRange, GameObject source)
        {
            if (source == gameObject) return;

            float effectiveRange = hearingRange + (emitterAudibleRange - CharacterProfile.StandardFootstepAudibleRange);
            Vector3 toSource = position - transform.position;
            if (toSource.magnitude > effectiveRange) return;

            LastDetectedDirection = toSource.normalized;
            LastDetectedTime = Time.time;
        }
    }
}
