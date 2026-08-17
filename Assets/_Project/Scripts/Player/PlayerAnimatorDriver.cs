using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Drives the shared PlayerAnimator controller (Stage12CharacterImporter builds
    /// one AnimatorController asset used by all 7 characters, each with its own
    /// Humanoid Avatar for Mecanim retargeting) from the existing movement/carry
    /// state, plus Part 5.3's funny-variant swap. States are driven directly via
    /// CrossFadeInFixedTime by exact clip name rather than a fully wired parameter
    /// graph - simpler and just as correct for this stage's actual requirement (the
    /// right clip plays under the right condition), and avoids hand-authoring dozens
    /// of transition edges for what the document describes as a flat clip list, not
    /// a blend tree.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerCarry))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        public const float LocomotionCrossFade = 0.15f;
        public const float OneShotCrossFade = 0.05f;
        public const float RunSpeedThreshold = PlayerLocomotion.WalkSpeed * 1.5f; // DECISION: walk/run clip cutover, no number given

        public Animator animator; // set by CharacterSelector once the visual model is instantiated

        private PlayerLocomotion locomotion;
        private PlayerCarry carry;
        private FunnyAnimationSwapper funnySwapper;
        private PlayerInputReader inputReader;
        private PlayerPushInteraction pushInteraction;

        private string currentLocomotionState = "";
        private bool wasGrounded = true;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            carry = GetComponent<PlayerCarry>();
            funnySwapper = GetComponent<FunnyAnimationSwapper>();
            inputReader = GetComponent<PlayerInputReader>();
            pushInteraction = GetComponent<PlayerPushInteraction>();

            if (inputReader != null)
            {
                inputReader.OnInteractPressed += HandleInteract;
                inputReader.OnThrowPressed += HandleThrow;
            }
            if (pushInteraction != null) pushInteraction.OnPushed += HandlePushed;
        }

        private void OnDestroy()
        {
            if (inputReader != null)
            {
                inputReader.OnInteractPressed -= HandleInteract;
                inputReader.OnThrowPressed -= HandleThrow;
            }
            if (pushInteraction != null) pushInteraction.OnPushed -= HandlePushed;
        }

        private void HandleInteract() => PlayOneShot("interact");
        private void HandleThrow() { if (carry.Held != null) PlayOneShot("Throw"); }
        private void HandlePushed() => PlayOneShot("Push");

        public void PlayOneShot(string stateName)
        {
            if (animator == null) return;
            animator.CrossFadeInFixedTime(stateName, OneShotCrossFade);
            currentLocomotionState = ""; // force the next Update to re-assert locomotion afterward
        }

        private void Update()
        {
            if (animator == null) return;

            bool grounded = locomotion.IsGrounded;
            if (!locomotion.IsExternallyControlled && !locomotion.IsFreeFlight)
            {
                if (wasGrounded && !grounded && locomotion.CurrentVelocity.y > 0f) PlayOneShot("Jumpstart");
                else if (!wasGrounded && grounded) PlayOneShot("Land");
            }
            wasGrounded = grounded;

            if (locomotion.IsExternallyControlled || locomotion.IsFreeFlight) return; // ragdoll/fan/flight - no locomotion clip fighting the physics

            string desired = ComputeLocomotionState(grounded);
            if (desired != currentLocomotionState)
            {
                animator.CrossFadeInFixedTime(desired, LocomotionCrossFade);
                currentLocomotionState = desired;
            }
        }

        private string ComputeLocomotionState(bool grounded)
        {
            if (!grounded) return "Fall";

            bool carryingHeavy = carry.Held != null && carry.Held.weightClass == CarryableObject.WeightClass.Heavy;
            bool carryingLight = carry.Held != null && carry.Held.weightClass == CarryableObject.WeightClass.Light;
            float speed = locomotion.CurrentHorizontalSpeed;
            bool moving = speed > FootstepSoundEmitter.MovingSpeedThreshold;

            if (carryingHeavy) return "WalkCarryHeavy"; // Part 5.3 has no heavy-carry idle clip - held steady while stationary too
            if (carryingLight) return moving ? "Walkcarrylight" : "idleCarrylight";
            if (!moving) return "Idle";

            bool funny = funnySwapper != null && funnySwapper.IsFunnyActive;
            bool running = speed > RunSpeedThreshold;
            return funny ? (running ? "Run_silly" : "Walk_silly") : (running ? "Run" : "Walk");
        }
    }
}
