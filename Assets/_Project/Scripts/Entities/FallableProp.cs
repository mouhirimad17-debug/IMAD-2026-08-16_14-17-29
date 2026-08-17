using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.1's "ثابت قابل للسقوط" classification: no physics by default, but
    /// converts to a fully dynamic Rigidbody the moment it takes a hard enough hit
    /// (falls or topples over, then settles under normal physics from then on).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class FallableProp : MonoBehaviour
    {
        // DECISION: Part 4.1 says only "اصطدام قوي كافٍ" (a strong enough impact)
        // without a number of its own. Reuses Part 7.3's already-established "قوي"
        // collision threshold (5 m/s) for a single consistent definition of "hard
        // impact" across the whole game rather than inventing a second one.
        public const float TipCollisionSpeedThreshold = 5f;

        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (body.isKinematic && collision.relativeVelocity.magnitude > TipCollisionSpeedThreshold)
                body.isKinematic = false;
        }
    }
}
