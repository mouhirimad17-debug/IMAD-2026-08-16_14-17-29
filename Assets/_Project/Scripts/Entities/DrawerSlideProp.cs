using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 8.3's "الأدراج القابلة للفتح" (openable drawers) - applies to
    /// Office_FilingCabinet_01 (Bedroom1_Dresser_01 keeps its own Stage 9 physical
    /// grab-and-drag system instead, per the owner's explicit choice when this part
    /// was reached - see Stage11_Decisions_Log.txt). Same press-to-toggle activation
    /// as DoorProp, but the resulting motion is a straight LINEAR slide along the
    /// drawer's own local horizontal axis instead of a rotation, ~0.40m, same 0.4s
    /// ease-out timing ("نفس منطق الباب العام ... لكن الحركة الناتجة انزلاقية").
    /// </summary>
    public class DrawerSlideProp : MonoBehaviour
    {
        public const float InteractRange = DoorProp.SubDoorInteractRange; // Part 8.2's shorter range - a drawer is a small fixture like a sub-door
        public const float SlideDistance = 0.40f;
        public const float SlideDurationSeconds = DoorProp.OpenDurationSeconds;

        public enum SlideState { Closed, Opening, Open, Closing }
        public SlideState State { get; private set; } = SlideState.Closed;

        private Vector3 closedWorldPos;
        private Vector3 slideWorldAxis = Vector3.forward;
        private float motionElapsed;
        private AudioSource audioSource;
        private AudioClip slideClip;

        private void Awake()
        {
            // World-space, not local: a child under a non-uniformly-scaled parent
            // (this placeholder cabinet's body) would otherwise have its slide
            // distance silently scaled along with the parent instead of staying a
            // real 0.40m in the actual scene.
            closedWorldPos = transform.position;
            slideWorldAxis = transform.forward;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            slideClip = PlaceholderAudio.GenerateTone("Placeholder_DrawerSlide", 180f, 0.12f, 0.2f);
        }

        public bool TryToggle(Transform opener)
        {
            if (Vector3.Distance(opener.position, transform.position) > InteractRange) return false;
            if (State == SlideState.Opening || State == SlideState.Closing) return false;

            State = State == SlideState.Closed ? SlideState.Opening : SlideState.Closing;
            motionElapsed = 0f;
            audioSource.PlayOneShot(slideClip);
            return true;
        }

        private void Update()
        {
            if (State != SlideState.Opening && State != SlideState.Closing) return;

            motionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(motionElapsed / SlideDurationSeconds);
            float eased = 1f - (1f - t) * (1f - t); // same quadratic ease-out as DoorProp
            float openDistance = eased * SlideDistance;

            float distanceFromClosed = State == SlideState.Opening ? openDistance : SlideDistance - openDistance;
            transform.position = closedWorldPos + slideWorldAxis * distanceFromClosed;

            if (t >= 1f)
                State = State == SlideState.Opening ? SlideState.Open : SlideState.Closed;
        }
    }
}
