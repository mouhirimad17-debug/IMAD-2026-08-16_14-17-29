using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 16.2: "إذا سقط أي غرض فيزيائي ... خارج الحدود الأرضية المعقولة للقصر
    /// ... يُعاد تلقائياً لموقعه الأصلي المحدد له في تصميم تلك الغرفة، مع إعادة
    /// تفعيل فيزيائه من جديد" - remembers each object's own designed spawn
    /// position/rotation and teleports it back there (velocity zeroed, woken up)
    /// the moment it falls below a sane floor threshold. A companion component on
    /// every physical prop class (CarryableObject/FallableProp/PushableProp all
    /// [RequireComponent] it) rather than a manual per-prefab opt-in, since Part
    /// 16.2 applies to "any physical object" without exception.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class OutOfBoundsRecovery : MonoBehaviour
    {
        // DECISION: "ارتفاع سلبي كبير جداً" has no exact number - -5m is well below
        // any real floor in this project's architecture (Part 3's floors sit at
        // y=0 and y=3.20m) while still tolerant of legitimate brief dips (falling
        // through a stairwell gap mid-animation, etc.).
        public const float FloorYThreshold = -5f;
        public const float CheckIntervalSeconds = 1f; // DECISION: no need to check every frame for a rare failure case

        private Rigidbody body;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private float checkTimer;

        public int RecoveryCount { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }

        private void Update()
        {
            checkTimer += Time.deltaTime;
            if (checkTimer < CheckIntervalSeconds) return;
            checkTimer = 0f;

            if (transform.position.y < FloorYThreshold) Recover();
        }

        public void Recover()
        {
            transform.SetPositionAndRotation(originalPosition, originalRotation);
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            if (body.IsSleeping()) body.WakeUp();
            RecoveryCount++;
        }
    }
}
