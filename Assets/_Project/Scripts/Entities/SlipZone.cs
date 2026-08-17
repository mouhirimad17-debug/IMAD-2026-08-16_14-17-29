using System.Collections.Generic;
using PrankMansion.Player;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 7.4's slip trap: a persistent circular hazard created by pouring a
    /// pourable object (Part 4.1/7.4). Detector-only per the document ("غطاء تصادم
    /// من نوع كاشف فقط") - a SphereCollider trigger is the simplest reasonable stand-
    /// in for "circular, 1.2m radius" (a true flat disc needs a mesh collider, out of
    /// scope for a blockout-stage hazard - logged as a DECISION).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SlipZone : MonoBehaviour
    {
        public const float Radius = 1.2f;             // "نصف قطر متر وعشرين سنتيمتراً"
        public const float RandomPushForce = 3f;       // "دفعة أفقية ... بقوة تقريبية ثلاث وحدات"
        public const float SpinRadiansPerSecond = 2f * Mathf.PI / 0.4f; // Law 0.5: full turn every ~0.4s

        public static readonly List<SlipZone> AllActive = new List<SlipZone>();

        private SphereCollider trigger;

        private void Awake()
        {
            trigger = GetComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = Radius;

            BuildVisualDecal();
        }

        private void BuildVisualDecal()
        {
            var decal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            decal.name = "SlipZone_VisualDecal";
            Destroy(decal.GetComponent<Collider>()); // this SlipZone's own SphereCollider is the real trigger
            decal.transform.SetParent(transform, false);
            decal.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            decal.transform.localScale = new Vector3(Radius * 2f, 0.01f, Radius * 2f);

            var renderer = decal.GetComponent<MeshRenderer>();
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = "Placeholder_SlipZoneDecal" };
            var shineColor = new Color(0.7f, 0.9f, 1f, 0.85f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", shineColor);
            else if (mat.HasProperty("_Color")) mat.SetColor("_Color", shineColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.95f);
            renderer.sharedMaterial = mat;
        }

        private void OnEnable() => AllActive.Add(this);
        private void OnDisable() => AllActive.Remove(this);

        private void OnTriggerEnter(Collider other)
        {
            // "بلا استثناء أو حصانة، حتى من وضعها بنفسه" - no exemptions, not even the placer.
            var ragdoll = other.GetComponentInParent<PlayerRagdoll>();
            if (ragdoll == null) return;

            Vector2 randomDir2D = Random.insideUnitCircle.normalized;
            Vector3 push = new Vector3(randomDir2D.x, 0f, randomDir2D.y) * RandomPushForce;
            ragdoll.TriggerRagdoll(push, SpinRadiansPerSecond);
        }

        /// Part 7.4: "تُمسح كل الفخاخ تلقائياً عند بداية جولة جديدة" - hook for the
        /// round system (Stage 9, not built yet) to call once it exists.
        public static void ClearAllForNewRound()
        {
            foreach (var zone in new List<SlipZone>(AllActive))
                if (zone != null) Destroy(zone.gameObject);
        }
    }
}
