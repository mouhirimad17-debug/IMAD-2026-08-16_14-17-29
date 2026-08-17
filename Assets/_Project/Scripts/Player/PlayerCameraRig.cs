using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Custom third-person orbit camera per PrankMansion_MasterDocument.md Part 14.
    /// Hand-written (rather than Cinemachine, though the package is available in the
    /// project) so every literal number in Part 14 - distance, pivot height, pitch
    /// range, and especially the "instant, no smoothing" rotation requirement - is
    /// guaranteed rather than depending on an unfamiliar component's damping
    /// defaults. Distance recovery after an obstruction clears is the one place the
    /// document explicitly asks for gradual easing (14.3); rotation response and the
    /// obstacle pull-in itself are both instant, per 14.2 and 14.3.
    /// </summary>
    public class PlayerCameraRig : MonoBehaviour
    {
        public const float PivotHeight = 1.5f;       // 14.1
        public const float DefaultDistance = 3.5f;   // 14.1
        public const float DefaultPitch = -15f;      // 14.1 default downward tilt (negative = down)
        public const float MinPitch = -20f;          // 14.2 look-down limit
        public const float MaxPitch = 50f;           // 14.2 look-up limit

        // DECISIONS (not numerically specified in Part 14):
        public const float MouseSensitivity = 0.15f;
        public const float MinDistance = 0.3f;
        public const float ObstacleSkin = 0.15f;
        public const float DistanceRecoverySpeedPerSec = 6f;

        // Law 0.5: "اصطدام قوي بين جسمين ... اهتزاز خفيف للشاشة، سعة تقريبية 0.15
        // وحدة، مدة 0.2 ثانية، يتلاشى تدريجياً". The single local player's own
        // camera rig - callers reach it via LocalInstance rather than an event bus,
        // matching RoundManager.Instance's existing singleton-ish pattern.
        public const float ShakeAmplitude = 0.15f;
        public const float ShakeDurationSeconds = 0.2f;

        public static PlayerCameraRig LocalInstance { get; private set; }

        public Transform target;
        public LayerMask obstacleMask = ~0;

        private float yaw;
        private float pitch = DefaultPitch;
        private float currentDistance = DefaultDistance;
        private float shakeTimer;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float CurrentDistance => currentDistance;
        public Vector3 Pivot => target.position + Vector3.up * PivotHeight;
        public bool IsShaking => shakeTimer > 0f;

        // Test-only: lets a Play Mode test inspect a triggered shake's remaining
        // duration without waiting for it to actually fade.
        public float DebugShakeTimer => shakeTimer;

        private void Awake()
        {
            pitch = DefaultPitch;
            currentDistance = DefaultDistance;
            LocalInstance = this;
        }

        private void OnDestroy()
        {
            if (LocalInstance == this) LocalInstance = null;
        }

        public void TriggerShake() => shakeTimer = ShakeDurationSeconds;

        /// <summary>Instant response - no damping/lerp on yaw or pitch (Part 14.2).</summary>
        public void SetLookDelta(Vector2 delta)
        {
            yaw = Mathf.Repeat(yaw + delta.x * MouseSensitivity, 360f);
            pitch = Mathf.Clamp(pitch + delta.y * MouseSensitivity, MinPitch, MaxPitch);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            ApplyCamera(Time.deltaTime);
        }

        /// <summary>Exposed separately so the headless Play Mode test can step this
        /// deterministically without relying on LateUpdate's frame timing.</summary>
        public void ApplyCamera(float dt)
        {
            Vector3 pivot = Pivot;
            Quaternion rot = Quaternion.Euler(-pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;

            float desiredDistance = DefaultDistance;
            if (Physics.Raycast(pivot, back, out RaycastHit hit, DefaultDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform != target && !hit.transform.IsChildOf(target))
                    desiredDistance = Mathf.Clamp(hit.distance - ObstacleSkin, MinDistance, DefaultDistance);
            }

            currentDistance = desiredDistance < currentDistance
                ? desiredDistance // instant pull-in: never allow clipping, even for one frame
                : Mathf.MoveTowards(currentDistance, desiredDistance, DistanceRecoverySpeedPerSec * dt);

            Vector3 position = pivot + back * currentDistance;
            if (shakeTimer > 0f)
            {
                shakeTimer = Mathf.Max(0f, shakeTimer - dt);
                float fade = shakeTimer / ShakeDurationSeconds; // "يتلاشى تدريجياً"
                position += Random.insideUnitSphere * (ShakeAmplitude * fade);
            }

            transform.position = position;
            transform.rotation = rot;
        }
    }
}
