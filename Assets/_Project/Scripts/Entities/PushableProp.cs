using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.1's "قابل للدفع" classification: never liftable by hand, but a player
    /// can shove it along the floor by walking into it (see PlayerPushInteraction).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(OutOfBoundsRecovery))] // Part 16.2
    public class PushableProp : MonoBehaviour
    {
        // Part 13.1's material-tagged collision sound - see CarryableObject's own
        // field for the same Law 21.2 class-level-default reasoning.
        public ImpactMaterial material = ImpactMaterial.Wood;
        public const float HardImpactSpeedThreshold = 5f; // project-wide "قوي" value

        private AudioSource impactAudio;

        private void Awake()
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            impactAudio = gameObject.AddComponent<AudioSource>();
            impactAudio.playOnAwake = false;
            impactAudio.spatialBlend = 1f;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude < HardImpactSpeedThreshold) return;
            AudioService.PlayOneShotSfx(impactAudio, ImpactSoundLibrary.GetClip(material));
            PlayerCameraRig.LocalInstance?.TriggerShake();
        }
    }
}
