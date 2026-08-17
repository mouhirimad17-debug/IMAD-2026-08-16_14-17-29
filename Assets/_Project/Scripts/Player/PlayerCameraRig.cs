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

        public Transform target;
        public LayerMask obstacleMask = ~0;

        private float yaw;
        private float pitch = DefaultPitch;
        private float currentDistance = DefaultDistance;

        public float Yaw => yaw;
        public float Pitch => pitch;
        public float CurrentDistance => currentDistance;
        public Vector3 Pivot => target.position + Vector3.up * PivotHeight;

        private void Awake()
        {
            pitch = DefaultPitch;
            currentDistance = DefaultDistance;
        }

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

            transform.position = pivot + back * currentDistance;
            transform.rotation = rot;
        }
    }
}
