using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.3's per-item impact notes layered on top of a normal carryable
    /// classification (these items are still "خفيف قابل للحمل" - this is an extra
    /// one-shot visual reaction, not a different physical class): the flour bag
    /// "ينفجر بصرياً عند اصطدام قوي" (explodes on a hard hit), the plate "ينكسر عند
    /// اصطدام قوي" (breaks on a hard hit), the milk carton "يسيل سائل بصرياً عند
    /// السقوط" (leaks when it lands - no "قوي" qualifier, so any landing counts,
    /// not gated by the hard-impact speed threshold).
    ///
    /// No FlourExplosion/SplashPour VFX assets exist yet (both folders are empty),
    /// so this spawns a simple procedural particle burst per Law 0.2 - logged as a
    /// placeholder by Stage7KitchenImporter.
    /// </summary>
    public class ImpactReactionProp : MonoBehaviour
    {
        public enum ReactionType { Explode, Shatter, Leak }

        // DECISION: reuses Part 7.3's already-established 5 m/s "قوي" definition for
        // Explode/Shatter, consistent with FallableProp's own reuse of the same value.
        // Leak has no "قوي" qualifier in the document ("عند السقوط" only) so it
        // triggers on any landing, unconditional on impact speed.
        public const float HardImpactThreshold = 5f;

        public ReactionType reactionType = ReactionType.Explode;
        public bool Triggered { get; private set; }

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (Triggered) return;

            bool qualifies = reactionType == ReactionType.Leak || collision.relativeVelocity.magnitude > HardImpactThreshold;
            if (!qualifies) return;

            Triggered = true;
            AudioService.PlayOneShotSfx(audioSource, GetReactionClip());
            SpawnReactionBurst();
        }

        // Part 13.1: "صوت انفجار الدقيق" for Explode; Shatter/Leak get their own
        // distinct placeholder tones for the same reason (Law 0.2 - no real SFX
        // assets exist for any of the three yet).
        private static AudioClip explodeClip, shatterClip, leakClip;

        private AudioClip GetReactionClip()
        {
            switch (reactionType)
            {
                case ReactionType.Explode:
                    return explodeClip ??= PlaceholderAudio.GenerateTone("Placeholder_FlourExplode", 100f, 0.3f, 0.35f);
                case ReactionType.Shatter:
                    return shatterClip ??= PlaceholderAudio.GenerateTone("Placeholder_PlateShatter", 1600f, 0.2f, 0.3f);
                default:
                    return leakClip ??= PlaceholderAudio.GenerateTone("Placeholder_MilkLeak", 300f, 0.25f, 0.2f);
            }
        }

        private void SpawnReactionBurst()
        {
            var go = new GameObject($"{name}_ImpactReaction_{reactionType}");
            go.transform.position = transform.position;

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;

            Color color;
            float lifetime, speed, size;
            int count;

            switch (reactionType)
            {
                case ReactionType.Explode: // flour puff
                    color = new Color(0.95f, 0.93f, 0.85f, 0.9f);
                    lifetime = 1.0f; speed = 2.5f; size = 0.15f; count = 25;
                    break;
                case ReactionType.Shatter: // plate shards
                    color = new Color(0.85f, 0.85f, 0.9f);
                    lifetime = 0.8f; speed = 3.5f; size = 0.04f; count = 12;
                    break;
                default: // Leak: milk puddle spread
                    color = new Color(0.97f, 0.97f, 0.93f, 0.85f);
                    lifetime = 1.5f; speed = 0.8f; size = 0.08f; count = 15;
                    break;
            }

            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = reactionType == ReactionType.Leak ? ParticleSystemShapeType.Circle : ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ps.Play();
            Destroy(go, lifetime + 0.2f);
        }
    }
}
