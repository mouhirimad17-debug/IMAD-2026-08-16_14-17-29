using System;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Base locomotion per PrankMansion_MasterDocument.md Part 5.1 (standard values,
    /// before any per-character bonus - those land in Stage 12) and the control
    /// mapping in Part 14.4. Movement direction is relative to the camera's current
    /// yaw (set each frame by PlayerInputReader from PlayerCameraRig), which is the
    /// standard third-person convention and matches Part 7.1's "player's current
    /// look/facing direction" pickup-cone language.
    ///
    /// Input is injected via SetMoveInput/SetSprint/RequestJump rather than read
    /// directly from the Input System, so the Stage 2 physics test can drive this
    /// component with exact synthetic values without needing simulated device input.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerLocomotion : MonoBehaviour
    {
        public const float WalkSpeed = 3f;    // Part 5.1: "سرعة المشي الأساسية القياسية"
        public const float RunSpeed = 6f;     // Part 5.1: "سرعة الجري الأساسية القياسية"
        public const float JumpHeight = 1.2f; // Part 5.1: "ارتفاع القفزة الأساسية القياسية"

        // DECISION (not numerically specified): body turn-to-face-movement rate.
        private const float TurnRateDegPerSec = 720f;
        private const float GroundedStickVelocity = -1f;

        // Law 0.5's "brief freeze then resume falling" beat on a mid-flight ceiling hit.
        private const float CeilingStopBeatSeconds = 0.05f;

        private CharacterController controller;
        private Vector2 moveInput;
        private bool sprintHeld;
        private bool jumpRequested;
        private float verticalVelocity;
        private float cameraYaw;
        private Vector3 freeFlightVelocity;
        private float ceilingStopTimer;

        public bool IsGrounded => controller.isGrounded;
        public float CurrentHorizontalSpeed { get; private set; }

        // Part 7.1: multiplies Walk/RunSpeed while carrying (1 = no penalty, e.g. light
        // objects; <1 = heavy-carry speed decay, per-character value from Part 5.2).
        public float SpeedMultiplier { get; set; } = 1f;

        // Part 5.2's permanent per-character base speed trait (Bomba's -15%, everyone
        // else 1). Separate from SpeedMultiplier above, which is a TEMPORARY state
        // penalty (heavy carry, joint carry) that resets on its own - this one is set
        // once at character selection and stays for the whole session. Stacks
        // multiplicatively with SpeedMultiplier.
        public float CharacterSpeedMultiplier { get; set; } = 1f;

        // Part 7.2: continuous additive push applied on top of normal movement (the
        // wind system's constant forward shove while active). Owned/updated every
        // frame by whichever system needs it (PlayerCarry); this component just adds
        // it into the resolved velocity.
        public Vector3 ExternalVelocity { get; set; } = Vector3.zero;

        // Part 7.2: the rocket-launch flight after ignition. While true, direct
        // WASD/jump control is suspended and the character instead flies on a simple
        // ballistic path (gravity + the launch impulse), same as a real projectile,
        // until it lands, at which point OnFreeFlightLanded fires (Stage 4's
        // PlayerRagdoll subscribes and takes over with the real Part 7.3 ragdoll).
        public bool IsFreeFlight { get; private set; }

        /// Fires the instant a free-flight landing is detected, before this component
        /// resumes any movement logic of its own - lets Part 7.3's ragdoll (or any
        /// other external controller) claim control for that same frame.
        public event Action OnFreeFlightLanded;

        // Snapshot of the launch impulse passed to BeginFreeFlight, kept constant
        // (unlike the live, gravity-decaying freeFlightVelocity) purely so tests can
        // verify Part 7.2's horizontal/vertical launch-force split was applied correctly.
        public Vector3 LaunchVelocitySnapshot { get; private set; }

        public Vector3 CurrentVelocity { get; private set; }

        // Part 7.3: while true (ragdoll active, or any other external controller has
        // claimed the body), this component's Update() does nothing at all - no
        // input, no gravity integration, no CharacterController.Move calls.
        public bool IsExternallyControlled { get; set; }

        /// Snaps the controller to a settled ragdoll's resting position/facing and
        /// clears IsExternallyControlled, handing normal movement control back.
        public void ResumeControlAt(Vector3 position, float yawDegrees)
        {
            controller.enabled = false;
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);
            controller.enabled = true;
            verticalVelocity = 0f;
            IsExternallyControlled = false;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        public void SetMoveInput(Vector2 input) => moveInput = Vector2.ClampMagnitude(input, 1f);
        public void SetSprint(bool held) => sprintHeld = held;
        public void RequestJump() => jumpRequested = true;
        public void SetCameraYaw(float yawDegrees) => cameraYaw = yawDegrees;

        public void BeginFreeFlight(Vector3 launchVelocity)
        {
            controller.enabled = true; // may still be off from a ragdoll trigger (Part 7.5's balcony throw hands off directly)
            IsFreeFlight = true;
            freeFlightVelocity = launchVelocity;
            LaunchVelocitySnapshot = launchVelocity;
            ceilingStopTimer = 0f;
        }

        private void Update()
        {
            if (IsExternallyControlled) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (IsFreeFlight)
            {
                UpdateFreeFlight(dt);
                return;
            }

            Quaternion camYawRot = Quaternion.Euler(0f, cameraYaw, 0f);
            Vector3 wishDir = camYawRot * new Vector3(moveInput.x, 0f, moveInput.y);
            if (wishDir.sqrMagnitude > 1f) wishDir.Normalize();

            float targetSpeed = (sprintHeld ? RunSpeed : WalkSpeed) * SpeedMultiplier * CharacterSpeedMultiplier;
            Vector3 horizontalVelocity = wishDir * targetSpeed + ExternalVelocity;
            CurrentHorizontalSpeed = horizontalVelocity.magnitude;

            if (controller.isGrounded)
            {
                if (verticalVelocity < 0f) verticalVelocity = GroundedStickVelocity;
                if (jumpRequested)
                    verticalVelocity = Mathf.Sqrt(2f * -Physics.gravity.y * JumpHeight);
            }
            else
            {
                verticalVelocity += Physics.gravity.y * dt;
            }
            jumpRequested = false;

            if (wishDir.sqrMagnitude > 0.0001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, TurnRateDegPerSec * dt);
            }

            Vector3 motion = horizontalVelocity;
            motion.y = verticalVelocity;
            CurrentVelocity = motion;
            controller.Move(motion * dt);
        }

        private void UpdateFreeFlight(float dt)
        {
            if (ceilingStopTimer > 0f)
            {
                ceilingStopTimer -= dt;
                return; // law 0.5: frozen in place for the ceiling-impact beat
            }

            freeFlightVelocity += Physics.gravity * dt;
            CollisionFlags flags = controller.Move(freeFlightVelocity * dt);
            CurrentVelocity = freeFlightVelocity;

            if ((flags & CollisionFlags.Above) != 0 && freeFlightVelocity.y > 0f)
            {
                freeFlightVelocity.y = 0f;
                ceilingStopTimer = CeilingStopBeatSeconds;
                return;
            }

            if (controller.isGrounded && freeFlightVelocity.y <= 0f)
            {
                IsFreeFlight = false;
                freeFlightVelocity = Vector3.zero;
                verticalVelocity = 0f;
                OnFreeFlightLanded?.Invoke(); // Part 7.2 -> 7.3: full ragdoll takes over on landing
            }
        }
    }
}
