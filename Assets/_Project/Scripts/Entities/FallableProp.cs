using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.1's "ثابت قابل للسقوط" classification: no physics by default, but
    /// converts to a fully dynamic Rigidbody the moment it takes a hard enough hit
    /// (falls or topples over, then settles under normal physics from then on).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(OutOfBoundsRecovery))] // Part 16.2
    public class FallableProp : MonoBehaviour
    {
        // DECISION: Part 4.1 says only "اصطدام قوي كافٍ" (a strong enough impact)
        // without a number of its own. Reuses Part 7.3's already-established "قوي"
        // collision threshold (5 m/s) for a single consistent definition of "hard
        // impact" across the whole game rather than inventing a second one.
        public const float TipCollisionSpeedThreshold = 5f;

        // Part 13.1's material-tagged collision sound - see CarryableObject's own
        // field for the same Law 21.2 class-level-default reasoning.
        public ImpactMaterial material = ImpactMaterial.Wood;

        private Rigidbody body;
        private AudioSource impactAudio;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;

            impactAudio = gameObject.AddComponent<AudioSource>();
            impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 1f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude <= TipCollisionSpeedThreshold) return;

            AudioService.PlayOneShotSfx(impactAudio, ImpactSoundLibrary.GetClip(material));
            PlayerCameraRig.LocalInstance?.TriggerShake();

            if (body.isKinematic) body.isKinematic = false;
        }
    }
}
