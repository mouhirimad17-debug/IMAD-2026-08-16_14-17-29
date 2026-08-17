using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Marks a prop as pickupable per Part 4.1's physical classification and drives
    /// its attach/detach state for Part 7.1's carry system. Only the two carryable
    /// classes need this component - "ثابت صرف" / "ثابت قابل للسقوط" / "قابل للدفع"
    /// props are never picked up, so they don't get one.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(OutOfBoundsRecovery))] // Part 16.2
    public class CarryableObject : MonoBehaviour
    {
        public enum WeightClass
        {
            Light,  // Part 4.1: 0.5-2kg, one-handed, no speed penalty
            Heavy   // Part 4.1: 15-40kg, needs Part 7.1's speed penalty + wind timer
        }

        public WeightClass weightClass = WeightClass.Light;

        // Part 4.1: "قابل للسكب ... يُضاف لتصنيف 'خفيف قابل للحمل'" - pourable is an
        // additional tag on top of Light, not a separate weight class.
        public bool isPourable;

        // Part 7.5: the restrain rope - "يُستهلك عند الاستخدام، بنفس آلية استهلاك
        // القنينة القابلة للسكب" (consumed on use, same mechanism as the pourable).
        public bool isRope;

        // Part 13.1's material-tagged collision sound (Systems.ImpactSoundLibrary) -
        // defaults to Wood (most common furniture material); individual props can
        // override once/if a finer per-item pass assigns a real material.
        public ImpactMaterial material = ImpactMaterial.Wood;

        // Part 15.2: "تعطيل المحاكاة الفيزيائية النشطة تلقائياً (تجميد الجسم) لأي
        // غرض قابل للحمل بقي بلا حركة فعلية لفترة طويلة (تقريباً خمس ثوانٍ)،
        // وإعادة تفعيلها فوراً عند أي تفاعل جديد معه" - Rigidbody.Sleep() rather
        // than toggling isKinematic, since Unity's own sleep state already wakes
        // automatically the instant another moving body touches it or a force/
        // velocity is applied, satisfying "إعادة تفعيلها فوراً" for free.
        public const float IdleSleepDelaySeconds = 5f;
        private const float IdleVelocitySqrEpsilon = 0.01f * 0.01f;
        private float idleTimer;

        public Rigidbody Body { get; private set; }
        public PlayerCarry PrimaryCarrier { get; private set; }
        public PlayerCarry SecondaryCarrier { get; private set; }
        public int CarrierCount => (PrimaryCarrier != null ? 1 : 0) + (SecondaryCarrier != null ? 1 : 0);

        // Part 9.1's throw-hit scoring event: "إصابة خصم بغرض مرمي بشكل مباشر".
        public bool WasThrown { get; private set; }
        public Team ThrownByTeam { get; private set; } = Team.None;
        private GameObject thrownFromObject;

        private Collider[] colliders;
        private AudioSource impactAudio;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();

            impactAudio = gameObject.AddComponent<AudioSource>();
            impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 1f;
        }

        private void Update()
        {
            // Held/kinematic objects don't idle-sleep (they aren't simulating
            // anyway), and an already-sleeping body needs no further timing.
            if (Body.isKinematic || Body.IsSleeping()) { idleTimer = 0f; return; }

            if (Body.linearVelocity.sqrMagnitude < IdleVelocitySqrEpsilon && Body.angularVelocity.sqrMagnitude < IdleVelocitySqrEpsilon)
            {
                idleTimer += Time.deltaTime;
                if (idleTimer >= IdleSleepDelaySeconds) Body.Sleep();
            }
            else
            {
                idleTimer = 0f;
            }
        }

        // Test-only: lets Play Mode tests verify the sleep transition without a
        // real 5-second wait.
        public void DebugForceIdleTimerAt(float seconds) => idleTimer = seconds;

        public void MarkThrown(Team throwerTeam, GameObject thrower)
        {
            WasThrown = true;
            ThrownByTeam = throwerTeam;
            thrownFromObject = thrower;
        }

        // Part 13.1's material-tagged collision sound + Law 0.5's camera shake -
        // fires on ANY sufficiently hard collision, independent of the throw-hit
        // scoring logic below (which only cares about thrown-and-not-yet-bounced
        // hits on an opposing player).
        public const float HardImpactSpeedThreshold = 5f; // reuses the project's established "قوي" value (FallableProp, PlayerRagdoll)

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude >= HardImpactSpeedThreshold)
            {
                AudioService.PlayOneShotSfx(impactAudio, ImpactSoundLibrary.GetClip(material));
                PlayerCameraRig.LocalInstance?.TriggerShake();
            }

            if (!WasThrown) return;
            // The hand-attach release point sits close enough to the thrower's own
            // capsule to spuriously brush it on release (well within
            // CharacterController's default skin width) - that's not a "hit", so it
            // doesn't consume the throw's one direct-hit eligibility below.
            if (collision.gameObject == thrownFromObject) return;

            WasThrown = false; // "مباشر" (direct) - only the very first real contact after a throw counts, no bounces

            var victimTeam = collision.gameObject.GetComponentInParent<PlayerTeam>();
            if (victimTeam == null || victimTeam.Team == Team.None || victimTeam.Team == ThrownByTeam) return;
            if (collision.relativeVelocity.magnitude < RoundManager.ThrowHitMinSpeed) return;

            RoundManager.Instance?.RegisterPoint(ThrownByTeam);
        }

        public void AttachPrimary(PlayerCarry carrier)
        {
            PrimaryCarrier = carrier;
            // Part 7.1: "غطاء التصادم الخاص بالغرض يُعطَّل مؤقتاً طوال فترة الحمل"
            SetCollidersEnabled(false);
            Body.isKinematic = true;
        }

        public void AttachSecondary(PlayerCarry carrier)
        {
            SecondaryCarrier = carrier;
        }

        public void DetachAll()
        {
            PrimaryCarrier = null;
            SecondaryCarrier = null;
            Body.isKinematic = false;
            SetCollidersEnabled(true);
        }

        private void SetCollidersEnabled(bool enabled)
        {
            foreach (var c in colliders) c.enabled = enabled;
        }
    }
}
