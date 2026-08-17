using PrankMansion.Player;
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

        public Rigidbody Body { get; private set; }
        public PlayerCarry PrimaryCarrier { get; private set; }
        public PlayerCarry SecondaryCarrier { get; private set; }
        public int CarrierCount => (PrimaryCarrier != null ? 1 : 0) + (SecondaryCarrier != null ? 1 : 0);

        private Collider[] colliders;

        private void Awake()
        {
            Body = GetComponent<Rigidbody>();
            colliders = GetComponentsInChildren<Collider>();
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
