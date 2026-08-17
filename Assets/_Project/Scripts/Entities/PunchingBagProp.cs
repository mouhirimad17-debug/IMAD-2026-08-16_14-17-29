using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 4.7's Gym_PunchingBag_01: "ثابت صرف، خاص: يتأرجح فيزيائياً عند الاصطدام
    /// من نقطة تعليق علوية" (pure static, special: swings physically on impact from
    /// an upper suspension point). Anchored via a ConfigurableJoint at the top of its
    /// own bounds to a fixed world point - linear motion locked (the anchor itself
    /// never moves), angular motion free, so ordinary physics (a collision impulse
    /// from anything that hits it) makes it swing like a real hanging bag with no
    /// extra "on hit" code needed.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PunchingBagProp : MonoBehaviour
    {
        // DECISION: no swing-resistance number is given in the document - a light
        // angular damping keeps it from swinging forever after a single hit, without
        // deadening the comedic effect.
        public const float AngularDamping = 0.5f;

        public ConfigurableJoint Joint { get; private set; }

        private void Awake() => EnsureInitialized();

        // Called explicitly by Stage10GymImporter right after AddComponent, in
        // addition to Awake() above - Editor scripting's AddComponent does not
        // reliably fire Awake synchronously in headless batch mode, so the joint
        // must exist before PrefabUtility.SaveAsPrefabAsset runs, not just whenever
        // Awake eventually fires (Play Mode start is reliable, but that's after the
        // prefab asset itself was already saved). Safe to call more than once - see
        // the idempotency guard below.
        public void EnsureInitialized()
        {
            var body = GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.angularDamping = AngularDamping;

            // IDEMPOTENCY: Awake fires again on every real re-instantiation (edit-
            // time bake, PlaceIntoGym's InstantiatePrefab, Play Mode's domain
            // reload) - re-fetch rather than re-add if the joint already exists.
            Joint = GetComponent<ConfigurableJoint>();
            if (Joint != null) return;

            var renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            Vector3 topWorld = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            Joint = gameObject.AddComponent<ConfigurableJoint>();
            Joint.connectedBody = null; // anchored directly to the fixed world point below
            Joint.autoConfigureConnectedAnchor = false;
            Joint.anchor = transform.InverseTransformPoint(topWorld);
            Joint.connectedAnchor = topWorld;
            Joint.xMotion = ConfigurableJointMotion.Locked;
            Joint.yMotion = ConfigurableJointMotion.Locked;
            Joint.zMotion = ConfigurableJointMotion.Locked;
            Joint.angularXMotion = ConfigurableJointMotion.Free;
            Joint.angularZMotion = ConfigurableJointMotion.Free;
            // Y (spinning around its own hanging axis) stays tightly limited rather
            // than fully locked - a real bag can twist a little, just not freely spin.
            Joint.angularYMotion = ConfigurableJointMotion.Limited;
            Joint.angularYLimit = new SoftJointLimit { limit = 10f };
        }
    }
}
