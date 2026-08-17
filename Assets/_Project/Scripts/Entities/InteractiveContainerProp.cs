using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Bedroom1_Dresser_01's Part 4.5 "أدراج تُفتح" mechanic (flagged as a document
    /// gap in Stage9_Decisions_Log point 4, then filled in per the owner's explicit
    /// numeric spec). Builds 3 DrawerProp children on the real dresser mesh at
    /// Awake, tracks how many are open past the tip-relevant threshold, and shifts
    /// the dresser's own center of mass forward as they open - same "ثابت قابل
    /// للسقوط" pattern as FallableProp (starts kinematic, flips to a live dynamic
    /// Rigidbody only once tipped) rather than being dynamic the whole time, so it
    /// doesn't wobble or get shoved around by nothing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class InteractiveContainerProp : MonoBehaviour
    {
        public const int DrawerCount = 3;
        public const int TipMinDrawersOpen = 2;

        // DECISION: "Center of Mass كيتبدل ديناميكياً حسب عدد الأدراج المفتوحة" has
        // no numeric offset of its own - 0.15m of forward CoM shift per drawer past
        // DrawerProp.TipRelevantOpenThreshold is a reasonable, logged value: with 2
        // drawers open it's already 0.30m forward of center, enough to guarantee a
        // forward-toppling torque once gravity takes over on the tip trigger below.
        public const float CenterOfMassShiftPerOpenDrawer = 0.15f;

        public Rigidbody Body { get; private set; }
        public DrawerProp[] Drawers { get; private set; }
        public bool HasTipped { get; private set; }

        private Vector3 baseCenterOfMass;

        private void Awake() => EnsureInitialized();

        // Called explicitly by Stage9BedroomsImporter right after AddComponent, in
        // addition to Awake() above - Editor scripting's AddComponent does not
        // reliably fire Awake synchronously in headless batch mode, so the 3
        // drawers must be built before PrefabUtility.SaveAsPrefabAsset runs, not
        // just whenever Awake eventually fires. Safe to call more than once
        // (BuildDrawers is idempotent).
        public void EnsureInitialized()
        {
            Body = GetComponent<Rigidbody>();
            Body.isKinematic = true; // "ثابت قابل للسقوط" until the tip condition fires
            baseCenterOfMass = Body.centerOfMass;

            BuildDrawers();
        }

        private void BuildDrawers()
        {
            // IDEMPOTENCY: Awake fires again on every real re-instantiation of this
            // prop (prefab-bake time, PlaceIntoBedrooms's PrefabUtility.
            // InstantiatePrefab, and once more when Play Mode's domain reload re-
            // Awakes the scene) - without this guard each pass would build 3 more
            // duplicate drawers (and re-run Setup, duplicating grab handles) on top
            // of the ones already baked in. Already-built drawers just get re-linked.
            var existing = new DrawerProp[DrawerCount];
            bool alreadyBuilt = true;
            for (int i = 0; i < DrawerCount; i++)
            {
                var found = transform.Find($"Drawer_{i:00}");
                if (found == null) { alreadyBuilt = false; break; }
                existing[i] = found.GetComponent<DrawerProp>();
            }
            if (alreadyBuilt) { Drawers = existing; return; }

            var renderers = GetComponentsInChildren<Renderer>();
            Bounds b = renderers.Length > 0 ? renderers[0].bounds : new Bounds(transform.position, Vector3.zero);
            foreach (var r in renderers) b.Encapsulate(r.bounds);

            // Drawer fronts sit flush with the dresser's own +Z face, stacked
            // evenly up its height, each 80% of the dresser's width/per-slot height
            // so there's a visible gap between them.
            float slotHeight = b.size.y / DrawerCount;
            float drawerWidth = b.size.x * 0.8f;
            float drawerHeight = slotHeight * 0.8f;
            const float drawerDepth = 0.03f;

            Drawers = new DrawerProp[DrawerCount];
            for (int i = 0; i < DrawerCount; i++)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = $"Drawer_{i:00}";
                go.transform.SetParent(transform, true);
                go.transform.rotation = transform.rotation;
                go.transform.localScale = new Vector3(drawerWidth, drawerHeight, drawerDepth);
                float y = b.min.y + slotHeight * (i + 0.5f);
                go.transform.position = new Vector3(b.center.x, y, b.center.z + b.extents.z);

                // [RequireComponent] on DrawerProp auto-adds the Rigidbody/Joint;
                // CreatePrimitive already gave it a correctly-sized BoxCollider.
                var drawer = go.AddComponent<DrawerProp>();
                drawer.Setup(Body, transform.forward * 0.1f);
                Drawers[i] = drawer;
            }
        }

        private void Update()
        {
            int openCount = 0;
            foreach (var d in Drawers)
                if (d.OpenDistance > DrawerProp.TipRelevantOpenThreshold)
                    openCount++;

            // Rigidbody.centerOfMass is in the body's own local space, and local
            // +Z is this dresser's chosen "front"/drawer-open direction (Vector3.
            // forward local == (0,0,1) regardless of the object's world rotation).
            Body.centerOfMass = baseCenterOfMass + Vector3.forward * (openCount * CenterOfMassShiftPerOpenDrawer);

            if (!HasTipped && openCount >= TipMinDrawersOpen)
            {
                HasTipped = true;
                Body.isKinematic = false;
            }
        }
    }
}
