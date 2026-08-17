using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 5.3's funny-animation-swap logic: an 8% chance, checked every second
    /// while walking/running normally, to switch to the funny variant for 2-3s then
    /// auto-revert. Explicitly excluded during any special state ("السقوط الحر
    /// الفيزيائي، فاقد الوعي، مقيّد، أو أثناء الإطلاق الصاروخي") and while carrying
    /// anything (the funny walk/run clips have no carrying counterpart). Purely a
    /// state flag - PlayerAnimatorDriver reads IsFunnyActive to pick which clip to
    /// actually play.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(PlayerCarry))]
    [RequireComponent(typeof(PlayerRagdoll))]
    public class FunnyAnimationSwapper : MonoBehaviour
    {
        public const float CheckIntervalSeconds = 1f;  // "يُفحص كل ثانية"
        public const float TriggerProbability = 0.08f; // "احتمال ... ثمانية بالمئة"
        public const float FunnyMinSeconds = 2f;
        public const float FunnyMaxSeconds = 3f;

        public bool IsFunnyActive { get; private set; }

        private PlayerLocomotion locomotion;
        private PlayerCarry carry;
        private PlayerRagdoll ragdoll;
        private PlayerCapture capture; // optional - not every test rig has one

        private float checkTimer;
        private float funnyTimer;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            carry = GetComponent<PlayerCarry>();
            ragdoll = GetComponent<PlayerRagdoll>();
            capture = GetComponent<PlayerCapture>();
        }

        private void Update()
        {
            if (IsFunnyActive)
            {
                funnyTimer -= Time.deltaTime;
                if (funnyTimer <= 0f) IsFunnyActive = false;
                return;
            }

            bool inSpecialState = locomotion.IsExternallyControlled || locomotion.IsFreeFlight || ragdoll.IsRagdolled
                || (capture != null && capture.State != CaptureState.None);
            bool eligible = !inSpecialState && carry.Held == null
                && locomotion.IsGrounded && locomotion.CurrentHorizontalSpeed > FootstepSoundEmitter.MovingSpeedThreshold;

            if (!eligible) { checkTimer = 0f; return; }

            checkTimer += Time.deltaTime;
            if (checkTimer < CheckIntervalSeconds) return;
            checkTimer = 0f;

            if (Random.value < TriggerProbability)
            {
                IsFunnyActive = true;
                funnyTimer = Random.Range(FunnyMinSeconds, FunnyMaxSeconds);
            }
        }
    }
}
