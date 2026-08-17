using PrankMansion.Blockout;
using PrankMansion.Entities;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Player
{
    public enum CaptureState { None, Unconscious, Restrained }

    /// <summary>
    /// Part 7.5's capture/restraint pipeline, layered on top of Part 7.3's ragdoll
    /// (PlayerRagdoll.OnSettled). Lives on the CAPTURED player; the acting player's
    /// side of restraining/grabbing is dispatched from PlayerCarry's Interact/Throw
    /// buttons (context-sensitive, matching that component's existing pattern),
    /// which is why this class exposes Try* entry points rather than reading input
    /// itself.
    /// </summary>
    [RequireComponent(typeof(PlayerRagdoll))]
    [RequireComponent(typeof(PlayerLocomotion))]
    public class PlayerCapture : MonoBehaviour
    {
        // ---- Unconscious ----
        public const float UnconsciousDuration = 6f;      // "ست ثوانٍ"

        // ---- Restrain (rope) ----
        public const float RestrainRange = 1.5f;           // "أقل من متر ونصف"
        public const float MaxRestrainedSeconds = 30f;      // "ثلاثين ثانية من لحظة بدء حالة مقيّد"

        // ---- Joint carry ----
        public const float JointCarryGrabRange = 1f;        // "أقل من متر واحد"
        public const float JointCarrySyncWindow = 2f;        // "نافذة زمنية مقدارها ثانيتان"
        public const float JointCarrySpeedFactor = 0.80f;    // "خصم بسيط قدره عشرون بالمئة"
        public const float JointCarrySwayAmplitude = 0.05f;  // DECISION: "تأرجح كوميدي خفيف"
        public const float JointCarrySwaySpeed = 3f;         // DECISION

        // ---- Insult option 1: balcony throw ----
        public const float BalconyEdgeDetectRange = 1f;      // DECISION: not numerically specified
        public const float BalconyThrowSpeed = 2f;           // DECISION: gentle horizontal push toward the opening

        // ---- Insult option 2: fan mount ----
        public const float FanMountRange = 1.5f;              // DECISION: "عند الاقتراب من مروحة السقف"

        public CaptureState State { get; private set; } = CaptureState.None;
        public bool IsJointCarried { get; private set; }
        public PlayerCarry HeadGrabber { get; private set; }
        public PlayerCarry FeetGrabber { get; private set; }

        private PlayerRagdoll ragdoll;
        private PlayerLocomotion locomotion;

        private float unconsciousTimer;
        private float restrainedTimer;
        private bool mountedOnFan;
        private Vector3 settledPosition;
        private float settledYaw;
        private float headGrabTime = -999f, feetGrabTime = -999f;
        private AudioSource audioSource;
        private AudioClip ropeTieClip;

        private void Awake()
        {
            ragdoll = GetComponent<PlayerRagdoll>();
            locomotion = GetComponent<PlayerLocomotion>();
            ragdoll.OnSettled += HandleRagdollSettled;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            ropeTieClip = PlaceholderAudio.GenerateTone("Placeholder_RopeTie", 200f, 0.25f, 0.3f); // Part 13.1: "صوت ربط الحبل"
        }

        private void OnDestroy()
        {
            if (ragdoll != null) ragdoll.OnSettled -= HandleRagdollSettled;
        }

        private void HandleRagdollSettled(Vector3 pos, float yaw)
        {
            settledPosition = pos;
            settledYaw = yaw;
            State = CaptureState.Unconscious;
            unconsciousTimer = 0f;
            // locomotion.IsExternallyControlled is already true from the ragdoll -
            // Part 7.5: "الشخصية مستلقية بلا حركة تماماً" while unconscious.
        }

        private void Update()
        {
            switch (State)
            {
                case CaptureState.Unconscious:
                    unconsciousTimer += Time.deltaTime;
                    if (unconsciousTimer >= UnconsciousDuration) WakeUp();
                    break;

                case CaptureState.Restrained:
                    restrainedTimer += Time.deltaTime;
                    if (IsJointCarried) UpdateJointCarryPosition();
                    if (!mountedOnFan && restrainedTimer >= MaxRestrainedSeconds) Release();
                    break;
            }
        }

        private void WakeUp()
        {
            State = CaptureState.None;
            locomotion.ResumeControlAt(settledPosition, settledYaw);
        }

        private void Release()
        {
            ReleaseJointCarry();
            State = CaptureState.None;
            locomotion.ResumeControlAt(transform.position, transform.eulerAngles.y);
        }

        // ---------------------------------------------------------------
        // Rescuer-side entry points (called by PlayerCarry on the OTHER player).
        public bool TryRestrainBy(PlayerCarry rescuer, CarryableObject rope)
        {
            if (State != CaptureState.Unconscious) return false;
            if (rope == null || !rope.isRope) return false;
            if (Vector3.Distance(rescuer.transform.position, transform.position) >= RestrainRange) return false;

            State = CaptureState.Restrained;
            restrainedTimer = 0f;
            mountedOnFan = false;
            AudioService.PlayOneShotSfx(audioSource, ropeTieClip); // Part 13.1: "يُشغَّل عند لحظة تحول أي لاعب ... لحالة مقيّد"
            Destroy(rope.gameObject); // "الحبل يُستهلك عند الاستخدام"

            // Part 9.1: point for successfully restraining an OPPONENT, at the exact
            // moment of the Unconscious -> Restrained transition.
            var rescuerTeam = rescuer.GetComponent<PlayerTeam>();
            var victimTeam = GetComponent<PlayerTeam>();
            if (rescuerTeam != null && victimTeam != null && rescuerTeam.Team != Team.None && rescuerTeam.Team != victimTeam.Team)
                RoundManager.Instance?.RegisterPoint(rescuerTeam.Team);

            return true;
        }

        public bool TryGrabEnd(PlayerCarry grabber)
        {
            if (State != CaptureState.Restrained || IsJointCarried) return false;

            float distHead = Vector3.Distance(grabber.transform.position, ragdoll.HeadPosition);
            float distFeet = Vector3.Distance(grabber.transform.position, ragdoll.FeetPosition);

            if (HeadGrabber == null && distHead < JointCarryGrabRange && distHead <= distFeet)
            {
                HeadGrabber = grabber;
                headGrabTime = Time.time;
                TryActivateJointCarry();
                return true;
            }
            if (FeetGrabber == null && distFeet < JointCarryGrabRange)
            {
                FeetGrabber = grabber;
                feetGrabTime = Time.time;
                TryActivateJointCarry();
                return true;
            }
            return false;
        }

        private void TryActivateJointCarry()
        {
            if (IsJointCarried || HeadGrabber == null || FeetGrabber == null) return;
            if (Mathf.Abs(headGrabTime - feetGrabTime) > JointCarrySyncWindow) return;

            IsJointCarried = true;
            HeadGrabber.GetComponent<PlayerLocomotion>().SpeedMultiplier = JointCarrySpeedFactor;
            FeetGrabber.GetComponent<PlayerLocomotion>().SpeedMultiplier = JointCarrySpeedFactor;
        }

        private void UpdateJointCarryPosition()
        {
            if (HeadGrabber == null || FeetGrabber == null) return;
            Vector3 mid = (HeadGrabber.transform.position + FeetGrabber.transform.position) * 0.5f;
            float sway = Mathf.Sin(Time.time * JointCarrySwaySpeed) * JointCarrySwayAmplitude;
            transform.position = mid + Vector3.up * (0.3f + sway);
        }

        public bool IsCarriedBy(PlayerCarry grabber) => IsJointCarried && (HeadGrabber == grabber || FeetGrabber == grabber);

        /// Option 3 - "الترك في مكان بعيد": carriers simply let go, no finishing move.
        public void ReleaseJointCarry()
        {
            if (HeadGrabber != null) { HeadGrabber.GetComponent<PlayerLocomotion>().SpeedMultiplier = 1f; HeadGrabber = null; }
            if (FeetGrabber != null) { FeetGrabber.GetComponent<PlayerLocomotion>().SpeedMultiplier = 1f; FeetGrabber = null; }
            IsJointCarried = false;
        }

        /// Option 1 - "الرمي من الشرفة".
        public bool TryThrowFromBalcony()
        {
            if (State != CaptureState.Restrained || !IsJointCarried) return false;
            if (transform.position.y < MansionSpec.Floor2FloorY - 0.5f) return false; // must be on floor 2
            if (!IsNearBalconyEdge(transform.position)) return false;

            ReleaseJointCarry();
            State = CaptureState.None;

            Vector3 towardOpening = new Vector3(MansionSpec.Opening.centerX, transform.position.y, MansionSpec.Opening.centerZ) - transform.position;
            towardOpening.y = 0f;
            towardOpening = towardOpening.sqrMagnitude > 0.0001f ? towardOpening.normalized : Vector3.forward;

            // Hand off to PlayerLocomotion's own ballistic flight (already wired to
            // PlayerRagdoll via OnFreeFlightLanded since Stage 4) - "يسقط بفيزياء
            // طبيعية كاملة ... يُفعِّل السقوط الحر الفيزيائي تلقائياً عند الارتطام".
            locomotion.IsExternallyControlled = false;
            locomotion.BeginFreeFlight(towardOpening * BalconyThrowSpeed);
            return true;
        }

        private static bool IsNearBalconyEdge(Vector3 pos)
        {
            var op = MansionSpec.Opening;
            float dx = Mathf.Max(op.x - pos.x, 0f, pos.x - op.xMax);
            float dz = Mathf.Max(op.z - pos.z, 0f, pos.z - op.zMax);
            return Mathf.Sqrt(dx * dx + dz * dz) <= BalconyEdgeDetectRange;
        }

        /// Option 2 - "التثبيت على مروحة السقف".
        public bool TryMountOnFan(CeilingFan fan)
        {
            if (State != CaptureState.Restrained || !IsJointCarried) return false;
            if (fan == null || Vector3.Distance(transform.position, fan.transform.position) >= FanMountRange) return false;

            ReleaseJointCarry();
            mountedOnFan = true;
            fan.AttachCaptive(this);
            locomotion.IsExternallyControlled = true; // position now driven entirely by the fan
            return true;
        }
    }
}
