using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// A single drawer under an InteractiveContainerProp (Bedroom1_Dresser_01's
    /// Part 4.5 "أدراج تُفتح" mechanic). Physically constrained by a
    /// ConfigurableJoint to only slide along its own local +Z (the owner's spec:
    /// X/Y Locked, Z Limited) - grabbing and pulling it just applies an ordinary
    /// drag force, same as GrabbableDragProp, and the joint alone keeps it from
    /// coming loose or moving sideways.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(ConfigurableJoint))]
    public class DrawerProp : MonoBehaviour
    {
        public const float DrawerMass = 3.0f;
        public const float LinearLimit = 0.35f;
        public const float LimitSpring = 0f;
        public const float LimitDamper = 5f;

        // DECISION: "المسك والسحب" (grab and drag) reuses the same ordinary drag
        // force / grab range as GrabbableDragProp for one consistent "how hard do
        // you pull on things" feel across both of this stage's new mechanics. Kept
        // as documentation even though the active drag itself follows DragSpeed
        // below rather than applying this as a raw AddForce (see FixedUpdate).
        public const float DragForce = PlayerPushInteraction.BasePushForce;
        public const float DragSpeed = GrabbableDragProp.DragSpeed;
        public const float GrabRange = PlayerCarry.PickupRange;

        public const float FallOutOpenThreshold = 0.30f; // owner spec: contents fall out past this
        public const float TipRelevantOpenThreshold = 0.25f; // owner spec: counts toward the tip-over check

        public Rigidbody Body { get; private set; }
        public Transform GrabPoint { get; private set; }

        // Open distance along the drawer's own build-time forward axis, in world
        // units - independent of Transform parenting quirks under a jointed parent.
        public float OpenDistance => Vector3.Dot(Body.position - closedWorldPos, openAxisWorld);

        private ConfigurableJoint joint;
        private Transform dragger;
        private Vector3 closedWorldPos;
        private Vector3 openAxisWorld;
        private bool hasEjectedContents;

        // Runs on EVERY real instantiation of this component (edit-time bake,
        // PlaceIntoBedrooms's PrefabUtility.InstantiatePrefab, and again when Play
        // Mode's domain reload re-Awakes the scene) - unlike Setup() below, this is
        // safe/correct to repeat every time since it only re-caches references and
        // captures "wherever I currently am" as closed, which is always correct
        // because a drawer is only ever placed already-closed.
        private void Awake() => CacheReferences();

        private void CacheReferences()
        {
            Body = GetComponent<Rigidbody>();
            joint = GetComponent<ConfigurableJoint>();
            closedWorldPos = transform.position;
            openAxisWorld = transform.forward;

            // GrabPoint is a plain (non-serialized) property - re-link it from the
            // already-baked "GrabPoint_Handle" child on every real instantiation,
            // same reasoning as GrabbableDragProp's GrabPoints array.
            var handle = transform.Find("GrabPoint_Handle");
            if (handle != null) GrabPoint = handle;
        }

        // One-time structural setup (joint config + handle collider) - only ever
        // called by InteractiveContainerProp right after CREATING a brand new
        // drawer, never on an already-built one reloaded from a saved prefab/scene
        // (that path just re-links the existing DrawerProp reference instead).
        // Self-contained (re-caches its own references) rather than relying on
        // Awake() having already run - Editor scripting's AddComponent does not
        // reliably fire Awake synchronously in headless batch mode.
        public void Setup(Rigidbody dresserBody, Vector3 handleWorldOffset)
        {
            CacheReferences();
            Body.mass = DrawerMass;
            Body.isKinematic = false;

            joint.connectedBody = dresserBody;
            joint.autoConfigureConnectedAnchor = false;
            joint.anchor = Vector3.zero;
            joint.connectedAnchor = dresserBody.transform.InverseTransformPoint(transform.position);
            joint.axis = Vector3.right;
            joint.secondaryAxis = Vector3.up;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Limited;
            joint.linearLimit = new SoftJointLimit { limit = LinearLimit };
            joint.linearLimitSpring = new SoftJointLimitSpring { spring = LimitSpring, damper = LimitDamper };

            var handleGo = new GameObject("GrabPoint_Handle");
            handleGo.transform.SetParent(transform, true);
            handleGo.transform.position = transform.position + handleWorldOffset;
            var col = handleGo.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = GrabbableDragProp.GrabPointRadius;
            GrabPoint = handleGo.transform;
        }

        public bool TryGrab(Transform grabberTransform)
        {
            if (dragger != null || GrabPoint == null) return false;
            if (Vector3.Distance(grabberTransform.position, GrabPoint.position) > GrabRange) return false;
            dragger = grabberTransform;
            return true;
        }

        public void ReleaseGrab() => dragger = null;

        private void FixedUpdate()
        {
            if (dragger != null)
            {
                // BUGFIX: a raw AddForce fighting the ConfigurableJoint's Limited-
                // motion solver produced unreliable results in testing (overshoot
                // well past the 0.35m hard limit under sustained force). Instead,
                // track how far the puller has moved past the drawer's closed spot
                // along the open axis, clamp that to [0, LinearLimit] ourselves,
                // and MovePosition toward it - deterministic and can never exceed
                // the limit. The joint (X/Y Locked, Z Limited exactly as specified)
                // still physically enforces the same bound independently, and its
                // Spring/Damper still govern release/bump behavior.
                float pulledOpen = Vector3.Dot(dragger.position - closedWorldPos, openAxisWorld);
                float targetOpen = Mathf.Clamp(pulledOpen, 0f, LinearLimit);
                Vector3 targetPos = closedWorldPos + openAxisWorld * targetOpen;
                Body.MovePosition(Vector3.MoveTowards(Body.position, targetPos, DragSpeed * Time.fixedDeltaTime));
            }

            if (!hasEjectedContents && OpenDistance > FallOutOpenThreshold)
                EjectContents();
        }

        private void EjectContents()
        {
            hasEjectedContents = true;

            // DECISION: no prop in Part 4's tables is ever placed "inside" a
            // drawer - detected the same way GrabbableDragProp finds "objects on
            // the bed": any other Rigidbody found in a small box in front of the
            // drawer's open cavity gets released and given a small outward pop
            // (Law 0.5) so the mechanism is real and testable even with nothing
            // currently authored to sit inside one.
            Vector3 cavityCenter = Body.position - openAxisWorld * 0.1f;
            foreach (var col in Physics.OverlapBox(cavityCenter, new Vector3(0.3f, 0.2f, 0.3f)))
            {
                var rb = col.attachedRigidbody;
                // Excludes this drawer's own collider AND the dresser body it's
                // jointed to (its convex mesh colliders sit right behind the
                // cavity) - only a 1-drawer pull should ever eject contents, never
                // touch the dresser's own kinematic/tip state.
                if (rb == null || rb == Body || rb == joint.connectedBody) continue;
                rb.isKinematic = false;
                rb.AddForce(-openAxisWorld * 1f + Vector3.up * 1f, ForceMode.Impulse);
            }
        }
    }
}
