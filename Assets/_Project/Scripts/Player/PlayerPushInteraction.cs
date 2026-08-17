using System;
using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 4.1's "قابل للدفع" side of the interaction: walking into a PushableProp
    /// shoves it along the floor. Separate component (rather than folded into
    /// PlayerRagdoll, which already implements OnControllerColliderHit for a
    /// different reason) since Unity dispatches that message to every component on
    /// the GameObject that defines it - no conflict, cleaner separation of concerns.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerPushInteraction : MonoBehaviour
    {
        // DECISION: Part 4/7 never give a push force number, only Doran's Part 5.2
        // relative bonus ("+40% فوق القوة القياسية"). This base value is a
        // reasonable, logged placeholder; pushForceMultiplier is the per-character
        // hook Stage 12 sets for Doran (1.4) and leaves at 1.0 for everyone else.
        public const float BasePushForce = 5f;
        public float pushForceMultiplier = 1f;

        // Stage 12: lets PlayerAnimatorDriver play Part 5.3's "دفع" (Push) clip on
        // an actual push, without this component needing to know Animator exists.
        public event Action OnPushed;

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.rigidbody == null || hit.rigidbody.isKinematic) return;
            var pushable = hit.collider.GetComponentInParent<PushableProp>();
            if (pushable == null) return;

            Vector3 pushDir = hit.moveDirection;
            pushDir.y = 0f;
            if (pushDir.sqrMagnitude < 0.0001f) return;

            hit.rigidbody.AddForceAtPosition(pushDir.normalized * (BasePushForce * pushForceMultiplier),
                hit.point, ForceMode.Impulse);
            OnPushed?.Invoke();
        }
    }
}
