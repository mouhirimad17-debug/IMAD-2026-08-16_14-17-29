using System;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Bedroom1_Blanket_01's "Grabbable_Drag" mechanic (Part 4.5's undocumented
    /// "خاص: قابل للسحب" row - flagged as a document gap in Stage9_Decisions_Log
    /// point 3, then filled in per the owner's explicit numeric spec). Unlike
    /// CarryableObject's kinematic hand-lock, this object stays a live, always-
    /// non-kinematic Rigidbody the whole time and gets physically dragged along the
    /// floor by a pulling force while held at one of its 4 corners - it can still
    /// collide, tumble, and get shoved mid-drag, and it never disappears.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableDragProp : MonoBehaviour
    {
        public const float Mass = 1.5f;
        public const float LinearDamping = 2.0f;
        public const float AngularDamping = 3.0f;
        public const float GrabPointRadius = 0.08f;
        public const float PulledDisplacementThreshold = 0.4f;

        // DECISION: "بقوة اللاعب العادية" (the player's ordinary force) has no
        // number of its own in the owner's spec - reuses Part 4.1's already-
        // established ordinary push force (PlayerPushInteraction.BasePushForce)
        // as the reference "how hard is an ordinary pull" value. Kept as a public
        // constant for that documentation purpose even though the active drag
        // itself moves at DragSpeed below (see FixedUpdate's BUGFIX note) rather
        // than applying this as a raw AddForce.
        public const float DragForce = PlayerPushInteraction.BasePushForce;

        // The actual per-frame drag pace (see FixedUpdate) - DECISION: no number
        // given for "how fast does dragging happen", 1.5 m/s is a brisk, clearly-
        // visible walking-pull pace.
        public const float DragSpeed = 1.5f;

        // DECISION: grab range not given a number either - reuses Part 7.1's
        // already-established pickup range (PlayerCarry.PickupRange).
        public const float GrabRange = PlayerCarry.PickupRange;

        // DECISION: "الأغراض اللي فوق السرير" (objects on top of the bed) has no
        // formal list anywhere in the document. Detected as any other Rigidbody
        // found in a sphere around the blanket's OWN original resting spot - the
        // blanket itself rests on the bed, so anything else resting near that same
        // spot reads as "on the bed" too. Keeps this component self-contained with
        // no hard reference to the Bedroom1_BedKing_01 prefab.
        public const float BedAreaCheckRadius = 1.2f;

        // Law 0.5: a plain isKinematic flip on an already-settled body wouldn't
        // visibly do anything on its own - a small pop sells "things go flying"
        // instead of a silent, unnoticed state change.
        public const float FallPopImpulse = 1.5f;

        public event Action OnBlanketPulled;

        public Rigidbody Body { get; private set; }
        public bool IsBeingDragged => dragger != null;
        public bool HasTriggeredPull { get; private set; }
        public Transform[] GrabPoints { get; private set; }

        private Transform dragger;
        private Vector3 originalPosition;

        private void Awake() => EnsureInitialized();

        // Called explicitly by Stage9BedroomsImporter right after AddComponent, in
        // addition to Awake() above - Editor scripting's AddComponent does not
        // reliably fire Awake synchronously in headless batch mode, so the grab
        // points must be built (and the Rigidbody configured) before
        // PrefabUtility.SaveAsPrefabAsset runs, not just whenever Awake eventually
        // fires. Safe to call more than once (BuildGrabPoints is idempotent).
        public void EnsureInitialized()
        {
            Body = GetComponent<Rigidbody>();
            Body.mass = Mass;
            Body.linearDamping = LinearDamping;
            Body.angularDamping = AngularDamping;
            Body.isKinematic = false;

            originalPosition = transform.position;
            BuildGrabPoints();
        }

        private void BuildGrabPoints()
        {
            // IDEMPOTENCY: Awake fires again on every real re-instantiation of this
            // prop (prefab-bake time, PlaceIntoBedrooms's PrefabUtility.
            // InstantiatePrefab, and once more when Play Mode's domain reload re-
            // Awakes the scene) - without this guard each pass would add 4 more
            // duplicate GrabPoint children on top of the ones already baked in.
            var existing = new Transform[4];
            bool alreadyBuilt = true;
            for (int i = 0; i < 4; i++)
            {
                existing[i] = transform.Find($"GrabPoint_{i:00}");
                if (existing[i] == null) { alreadyBuilt = false; break; }
            }
            if (alreadyBuilt) { GrabPoints = existing; return; }

            var renderers = GetComponentsInChildren<Renderer>();
            Bounds worldBounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers) worldBounds.Encapsulate(r.bounds);

            float halfX = worldBounds.extents.x;
            float halfZ = worldBounds.extents.z;
            float cornerY = worldBounds.center.y;
            Vector3 c = worldBounds.center;

            var corners = new[]
            {
                new Vector3(c.x - halfX, cornerY, c.z - halfZ),
                new Vector3(c.x + halfX, cornerY, c.z - halfZ),
                new Vector3(c.x - halfX, cornerY, c.z + halfZ),
                new Vector3(c.x + halfX, cornerY, c.z + halfZ),
            };

            GrabPoints = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var go = new GameObject($"GrabPoint_{i:00}");
                go.transform.SetParent(transform, true);
                go.transform.position = corners[i];
                var col = go.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = GrabPointRadius;
                GrabPoints[i] = go.transform;
            }
        }

        public bool TryGrab(Transform grabberTransform)
        {
            if (dragger != null) return false;
            foreach (var point in GrabPoints)
            {
                if (Vector3.Distance(grabberTransform.position, point.position) <= GrabRange)
                {
                    dragger = grabberTransform;
                    return true;
                }
            }
            return false;
        }

        public void ReleaseGrab()
        {
            dragger = null;
        }

        private void FixedUpdate()
        {
            if (dragger != null)
            {
                // BUGFIX: a raw AddForce(DragForce) fight against the floor
                // collider's default PhysicMaterial friction (~0.6 dynamic/static)
                // does not reliably win - measured in testing, a 1.5kg body flat on
                // the floor barely moved at all under 5N. MovePosition instead
                // follows the puller directly at a fixed pace, staying "dragged by
                // an ordinary pull" in feel without fighting friction tuned for
                // other props. Rigidbody.mass/linearDamping/angularDamping (as
                // specified) still govern how it settles, tumbles, or gets shoved
                // once RELEASED or hit by something else.
                Vector3 targetPoint = dragger.position + dragger.forward * 0.5f;
                Body.MovePosition(Vector3.MoveTowards(Body.position, targetPoint, DragSpeed * Time.fixedDeltaTime));
            }

            if (!HasTriggeredPull && Vector3.Distance(Body.position, originalPosition) > PulledDisplacementThreshold)
                TriggerPull();
        }

        private void TriggerPull()
        {
            HasTriggeredPull = true;

            foreach (var col in Physics.OverlapSphere(originalPosition, BedAreaCheckRadius))
            {
                var rb = col.attachedRigidbody;
                if (rb == null || rb == Body) continue;
                rb.isKinematic = false;
                rb.AddForce(Vector3.up * FallPopImpulse + UnityEngine.Random.insideUnitSphere * 0.5f, ForceMode.Impulse);
            }

            OnBlanketPulled?.Invoke();
        }
    }
}
