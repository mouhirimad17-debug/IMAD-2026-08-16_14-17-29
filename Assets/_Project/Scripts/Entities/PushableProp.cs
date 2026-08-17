using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.1's "قابل للدفع" classification: never liftable by hand, but a player
    /// can shove it along the floor by walking into it (see PlayerPushInteraction).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PushableProp : MonoBehaviour
    {
        private void Awake()
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }
    }
}
