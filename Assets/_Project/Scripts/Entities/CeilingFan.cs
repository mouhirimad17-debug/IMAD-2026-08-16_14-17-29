using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 2.2's ambient ceiling fan ("مروحة السقف تدور ببطء دائم") doubling as
    /// Part 7.5's second insult option ("التثبيت على مروحة السقف"). The real
    /// Bedroom2 fan prop isn't imported until Stage 7 (Part 4.7-equivalent); this is
    /// a simple placeholder standing in per Law 0.2, logged by Stage5CaptureSetup.
    /// </summary>
    public class CeilingFan : MonoBehaviour
    {
        public const float SlowAmbientDegPerSec = 30f;  // Law 2.2 "ببطء دائم" - DECISION, not numerically pinned
        public const float FastMountedDegPerSec = 720f; // Part 7.5 "معدل كوميدي سريع" - DECISION

        public PlayerCapture MountedCaptive { get; private set; }

        public void AttachCaptive(PlayerCapture captive) => MountedCaptive = captive;
        public void DetachCaptive() => MountedCaptive = null;

        private void Update()
        {
            float speed = MountedCaptive != null ? FastMountedDegPerSec : SlowAmbientDegPerSec;
            transform.Rotate(Vector3.up, speed * Time.deltaTime, Space.Self);

            if (MountedCaptive != null)
                MountedCaptive.transform.position = transform.TransformPoint(new Vector3(0.3f, -0.25f, 0f));
        }
    }
}
