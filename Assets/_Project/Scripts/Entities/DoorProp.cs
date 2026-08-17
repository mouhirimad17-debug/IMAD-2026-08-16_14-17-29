using System;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 8.1's general interior door: press-to-toggle, swings 90 deg around its
    /// hinge (a vertical edge of its own bounds, not its transform origin) away
    /// from whoever opened it, 0.4s ease-out, a sound at the start of each motion.
    /// Also covers Part 8.2's kitchen sub-doors via the same component with
    /// different openAngleDeg/interactRange/hinge-axis values (see Setup()).
    /// </summary>
    [DisallowMultipleComponent]
    public class DoorProp : MonoBehaviour
    {
        public const float MainInteractRange = 2f;      // Part 8.1
        public const float SubDoorInteractRange = 1.2f;  // Part 8.2
        public const float MainOpenAngleDeg = 90f;       // Part 8.1
        public const float OpenDurationSeconds = 0.4f;   // Part 8.1

        public enum HingeAxis { Vertical, Horizontal } // Part 8.2: vertical doors vs. the washer's circular door

        public float interactRange = MainInteractRange;
        public float openAngleDeg = MainOpenAngleDeg;
        public HingeAxis hingeAxis = HingeAxis.Vertical;

        public enum DoorState { Closed, Opening, Open, Closing }
        public DoorState State { get; private set; } = DoorState.Closed;

        private Vector3 hingeWorldPoint;
        private Vector3 rotationAxis;
        private float currentAngle;
        private float swingSign = 1f;
        private float motionElapsed;
        private AudioSource audioSource;
        private AudioClip doorClip;
        private bool boundsReady;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            doorClip = PlaceholderAudio.GenerateTone("Placeholder_DoorSwing", 220f, 0.15f, 0.25f);
        }

        private void EnsureBounds()
        {
            if (boundsReady) return;
            boundsReady = true;

            var renderers = GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            // DECISION: hinge side not specified in the document - the door's own
            // local -X edge is used consistently as the hinge (matching how every
            // door prop in this project shares the same source-kit orientation).
            if (hingeAxis == HingeAxis.Vertical)
            {
                hingeWorldPoint = new Vector3(b.min.x, b.center.y, b.center.z);
                rotationAxis = Vector3.up;
            }
            else
            {
                // Washer-style circular door: rotates around a horizontal axis
                // (Part 8.2: "تدوير حول محور أفقي") through the door's own bottom edge.
                hingeWorldPoint = new Vector3(b.center.x, b.min.y, b.center.z);
                rotationAxis = Vector3.right;
            }
        }

        public bool TryToggle(Transform opener)
        {
            EnsureBounds();
            if (Vector3.Distance(opener.position, hingeWorldPoint) > interactRange) return false;
            if (State == DoorState.Opening || State == DoorState.Closing) return false;

            if (State == DoorState.Closed)
            {
                // Swing away from whoever opened it.
                Vector3 toOpener = opener.position - hingeWorldPoint;
                swingSign = Vector3.Dot(transform.forward, toOpener) >= 0f ? -1f : 1f;
                State = DoorState.Opening;
            }
            else
            {
                State = DoorState.Closing;
            }

            motionElapsed = 0f;
            audioSource.PlayOneShot(doorClip);

            // Part 5.2 (Reno): "سماع خطوات وفتح أبواب" - door open/close is a
            // detectable sound event too, same standard 8m range as footsteps.
            SoundEvents.Emit(hingeWorldPoint, CharacterProfile.StandardFootstepAudibleRange, gameObject);
            return true;
        }

        private void Update()
        {
            if (State != DoorState.Opening && State != DoorState.Closing) return;

            motionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(motionElapsed / OpenDurationSeconds);
            // Quadratic ease-out (Part 8.1: "تبدأ سريعة وتتباطأ قليلاً قرب
            // النهاية") - exact 0.4s total duration regardless of frame rate,
            // rather than an approximated per-frame speed curve.
            float eased = 1f - (1f - t) * (1f - t);
            float angleFromStart = eased * openAngleDeg;

            float newAngle = State == DoorState.Opening ? angleFromStart : openAngleDeg - angleFromStart;
            newAngle = Mathf.Clamp(newAngle, 0f, openAngleDeg);
            float delta = newAngle - currentAngle;
            currentAngle = newAngle;

            transform.RotateAround(hingeWorldPoint, rotationAxis, delta * swingSign);

            if (t >= 1f)
                State = State == DoorState.Opening ? DoorState.Open : DoorState.Closed;
        }
    }
}
