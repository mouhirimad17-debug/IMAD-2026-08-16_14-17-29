using System;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 7.3's full physical ragdoll. No character model/skeleton exists yet
    /// (Part 5's seven characters are only imported in Stage 12) so, per Law 0.2,
    /// this builds a simple procedural placeholder rig at Law 0.1's 1.000m reference
    /// height: 12 capsule/sphere Rigidbodies (pelvis root, spine, chest, head, both
    /// upper arms, both lower arms, both thighs, both lower legs - comfortably over
    /// Part 7.3's "at least 11 main points") linked by CharacterJoints. It stays
    /// kinematic and hidden inside the player capsule until triggered, then goes
    /// fully physical for a Part 7.3-timed fall before handing control back.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerRagdoll : MonoBehaviour
    {
        // ---- Part 7.3 ----
        public const float FallHeightThreshold = 1.5f;      // "السقوط من ارتفاع أكبر من متر ونصف"
        public const float CollisionSpeedThreshold = 5f;    // "سرعة نسبية أكبر من خمسة أمتار في الثانية"
        public const float MinDurationSeconds = 2f;
        public const float MaxDurationSeconds = 4f;
        public const float SettleSpeedThreshold = 0.5f;     // "أقل من نصف متر في الثانية تقريباً"
        public const float ReferenceHeight = 1.0f;          // Law 0.1

        // ---- Law 0.5 (stars over the head during the whole ragdoll duration) ----
        public const int StarCountMin = 3;
        public const int StarCountMax = 5;
        public const float StarAngularSpeedDegPerSec = 180f;
        public const float StarFadeOutSeconds = 0.5f;

        public bool IsRagdolled { get; private set; }
        public int TriggerCount { get; private set; }

        /// Fires the instant the ragdoll settles (Part 7.3's speed threshold met, after
        /// the timed duration), with the pelvis's resting world position and facing yaw.
        /// If nothing subscribes, PlayerRagdoll falls back to resuming direct control
        /// itself (Stage 4's original, self-contained behaviour). From Stage 5 onward,
        /// PlayerCapture subscribes and takes over instead - Part 7.5 explicitly
        /// changes this handoff: "بدل العودة المباشرة للوقوف، الشخصية تدخل حالة فاقد
        /// الوعي الوسيطة" (instead of standing directly, the character goes
        /// unconscious first).
        public event Action<Vector3, float> OnSettled;

        public Vector3 HeadPosition => parts[3].T.position;   // index matches BuildRig's `parts` order
        public Vector3 FeetPosition => (parts[9].T.position + parts[11].T.position) * 0.5f; // both lower legs

        private PlayerLocomotion locomotion;
        private CharacterController controller;

        private BodyPart[] parts;
        private BodyPart pelvis;

        private bool wasGroundedLastFrame;
        private float fallPeakY;

        private float ragdollTimer;
        private float ragdollDuration;
        private Transform starsAnchor;
        private GameObject[] stars;

        private struct BodyPart
        {
            public Transform T;
            public Rigidbody Body;
            public Collider Col;
            public Vector3 RestLocalPos;
            public Quaternion RestLocalRot;
        }

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            controller = GetComponent<CharacterController>();
            BuildRig();
            BuildStarsAnchor();

            locomotion.OnFreeFlightLanded += HandleFreeFlightLanded;
            wasGroundedLastFrame = controller.isGrounded;
            fallPeakY = transform.position.y;
        }

        private void OnDestroy()
        {
            if (locomotion != null) locomotion.OnFreeFlightLanded -= HandleFreeFlightLanded;
        }

        private void HandleFreeFlightLanded() => TriggerRagdoll(); // Part 7.2 -> 7.3

        /// Public entry point for any of Part 7.3's activation conditions. extraImpulse/
        /// extraSpinRadPerSec let a caller (e.g. Part 7.4's slip zone) add its own kick
        /// on top of whatever velocity the player already had.
        public void TriggerRagdoll(Vector3? extraImpulse = null, float? extraSpinRadPerSec = null)
        {
            if (IsRagdolled) return;
            TriggerCount++;
            IsRagdolled = true;
            locomotion.IsExternallyControlled = true;
            controller.enabled = false;

            ragdollDuration = UnityEngine.Random.Range(MinDurationSeconds, MaxDurationSeconds);
            ragdollTimer = 0f;

            Vector3 initialVelocity = locomotion.CurrentVelocity + (extraImpulse ?? Vector3.zero);
            foreach (var p in parts)
            {
                p.Col.enabled = true;
                p.Body.isKinematic = false;
                p.Body.linearVelocity = initialVelocity;
            }
            if (extraSpinRadPerSec.HasValue)
                pelvis.Body.angularVelocity = Vector3.up * extraSpinRadPerSec.Value;

            SpawnStars();
        }

        private void Update()
        {
            if (IsRagdolled)
            {
                UpdateRagdollState(Time.deltaTime);
                return;
            }

            bool grounded = controller.isGrounded;
            if (!grounded)
            {
                if (wasGroundedLastFrame) fallPeakY = transform.position.y;
                else if (transform.position.y > fallPeakY) fallPeakY = transform.position.y;
            }
            else if (!wasGroundedLastFrame)
            {
                float fallDistance = fallPeakY - transform.position.y;
                if (fallDistance > FallHeightThreshold) TriggerRagdoll();
            }
            wasGroundedLastFrame = grounded;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (IsRagdolled) return;
            if (hit.rigidbody == null || hit.rigidbody.isKinematic) return; // Part 7.3: "لاعب آخر، غرض قابل للدفع متحرك"

            Vector3 relativeVelocity = locomotion.CurrentVelocity - hit.rigidbody.linearVelocity;
            if (relativeVelocity.magnitude > CollisionSpeedThreshold) TriggerRagdoll();
        }

        private void UpdateRagdollState(float dt)
        {
            ragdollTimer += dt;
            UpdateStars(dt);

            if (ragdollTimer >= ragdollDuration && pelvis.Body.linearVelocity.magnitude < SettleSpeedThreshold)
                StandUp();
            // else: keeps waiting past ragdollDuration, checking every frame, until it
            // settles - Part 7.3's "تمديد المدة تلقائياً حتى استقرار الجسم فيزيائياً".
        }

        private void StandUp()
        {
            Vector3 pelvisPos = pelvis.T.position;
            float yaw = pelvis.T.eulerAngles.y;

            foreach (var p in parts)
            {
                p.Body.linearVelocity = Vector3.zero;
                p.Body.angularVelocity = Vector3.zero;
                p.Body.isKinematic = true;
                p.Col.enabled = false;
                p.T.SetLocalPositionAndRotation(p.RestLocalPos, p.RestLocalRot);
            }

            IsRagdolled = false;
            DespawnStars();

            float groundY = pelvisPos.y;
            if (Physics.Raycast(pelvisPos + Vector3.up * 0.5f, Vector3.down, out var hit, 5f))
                groundY = hit.point.y;

            wasGroundedLastFrame = true;
            fallPeakY = groundY;

            Vector3 restPosition = new Vector3(pelvisPos.x, groundY + 0.02f, pelvisPos.z);
            if (OnSettled != null) OnSettled.Invoke(restPosition, yaw);
            else locomotion.ResumeControlAt(restPosition, yaw); // Stage 4 fallback: no PlayerCapture listening
        }

        // ---------------------------------------------------------------
        private void BuildRig()
        {
            // Local Y matches CharacterController's own convention (root = feet, +1.0 = head).
            var pelvisGo = CreatePart("Ragdoll_Pelvis", transform, new Vector3(0f, 0.46f, 0f),
                capsuleRadius: 0.13f, capsuleHeight: 0.16f, isSphere: false, parentBody: null);
            var spineGo = CreatePart("Ragdoll_Spine", pelvisGo.T, WorldToLocal(pelvisGo.T, new Vector3(0f, 0.56f, 0f)),
                0.11f, 0.14f, false, pelvisGo.Body);
            var chestGo = CreatePart("Ragdoll_Chest", spineGo.T, WorldToLocal(spineGo.T, new Vector3(0f, 0.68f, 0f)),
                0.13f, 0.16f, false, spineGo.Body);
            var headGo = CreatePart("Ragdoll_Head", chestGo.T, WorldToLocal(chestGo.T, new Vector3(0f, 0.85f, 0f)),
                0.09f, 0f, true, chestGo.Body);

            var upperArmL = CreatePart("Ragdoll_UpperArm_L", chestGo.T, WorldToLocal(chestGo.T, new Vector3(-0.18f, 0.62f, 0f)),
                0.05f, 0.14f, false, chestGo.Body);
            var lowerArmL = CreatePart("Ragdoll_LowerArm_L", upperArmL.T, WorldToLocal(upperArmL.T, new Vector3(-0.22f, 0.50f, 0f)),
                0.045f, 0.14f, false, upperArmL.Body);
            var upperArmR = CreatePart("Ragdoll_UpperArm_R", chestGo.T, WorldToLocal(chestGo.T, new Vector3(0.18f, 0.62f, 0f)),
                0.05f, 0.14f, false, chestGo.Body);
            var lowerArmR = CreatePart("Ragdoll_LowerArm_R", upperArmR.T, WorldToLocal(upperArmR.T, new Vector3(0.22f, 0.50f, 0f)),
                0.045f, 0.14f, false, upperArmR.Body);

            var thighL = CreatePart("Ragdoll_Thigh_L", pelvisGo.T, WorldToLocal(pelvisGo.T, new Vector3(-0.09f, 0.30f, 0f)),
                0.07f, 0.22f, false, pelvisGo.Body);
            var lowerLegL = CreatePart("Ragdoll_LowerLeg_L", thighL.T, WorldToLocal(thighL.T, new Vector3(-0.09f, 0.10f, 0f)),
                0.06f, 0.18f, false, thighL.Body);
            var thighR = CreatePart("Ragdoll_Thigh_R", pelvisGo.T, WorldToLocal(pelvisGo.T, new Vector3(0.09f, 0.30f, 0f)),
                0.07f, 0.22f, false, pelvisGo.Body);
            var lowerLegR = CreatePart("Ragdoll_LowerLeg_R", thighR.T, WorldToLocal(thighR.T, new Vector3(0.09f, 0.10f, 0f)),
                0.06f, 0.18f, false, thighR.Body);

            pelvis = pelvisGo;
            parts = new[]
            {
                pelvisGo, spineGo, chestGo, headGo,
                upperArmL, lowerArmL, upperArmR, lowerArmR,
                thighL, lowerLegL, thighR, lowerLegR
            };

            foreach (var p in parts)
            {
                p.Col.enabled = false;
                p.Body.isKinematic = true;
            }
        }

        private static Vector3 WorldToLocal(Transform parent, Vector3 worldPosUnderRoot)
        {
            // Callers pass positions expressed in the PLAYER ROOT's local space (matching
            // the CharacterController's own foot-at-0/head-at-1 convention) regardless of
            // which body part they're parenting under - convert through world space once.
            return parent.InverseTransformPoint(parent.parent != null ? parent.parent.TransformPoint(worldPosUnderRoot) : worldPosUnderRoot);
        }

        private BodyPart CreatePart(string name, Transform parent, Vector3 localPos,
            float capsuleRadius, float capsuleHeight, bool isSphere, Rigidbody parentBody)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;

            Collider col;
            if (isSphere)
            {
                var sc = go.AddComponent<SphereCollider>();
                sc.radius = capsuleRadius;
                col = sc;
            }
            else
            {
                var cc = go.AddComponent<CapsuleCollider>();
                cc.radius = capsuleRadius;
                cc.height = capsuleHeight + capsuleRadius * 2f;
                cc.direction = 1; // Y axis
                col = cc;
            }

            var body = go.AddComponent<Rigidbody>();
            body.mass = 3f; // DECISION: not specified per-part; uniform placeholder mass

            if (parentBody != null)
            {
                var joint = go.AddComponent<CharacterJoint>();
                joint.connectedBody = parentBody;
                joint.enableProjection = true;
            }

            return new BodyPart
            {
                T = go.transform,
                Body = body,
                Col = col,
                RestLocalPos = localPos,
                RestLocalRot = Quaternion.identity
            };
        }

        // ---------------------------------------------------------------
        private void BuildStarsAnchor()
        {
            var go = new GameObject("Ragdoll_StarsAnchor");
            starsAnchor = go.transform;
            starsAnchor.SetParent(null); // world-space follower, independent of the (frozen) root during ragdoll
            go.SetActive(false);
        }

        private void SpawnStars()
        {
            int count = UnityEngine.Random.Range(StarCountMin, StarCountMax + 1);
            stars = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                var star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                star.name = $"Ragdoll_Star_{i}";
                Destroy(star.GetComponent<Collider>());
                star.transform.SetParent(starsAnchor, false);
                float angle = i * (360f / count) * Mathf.Deg2Rad;
                star.transform.localPosition = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 0.2f;
                star.transform.localScale = Vector3.one * 0.06f;
                stars[i] = star;
            }
            starsAnchor.gameObject.SetActive(true);
        }

        private void UpdateStars(float dt)
        {
            if (starsAnchor == null || !starsAnchor.gameObject.activeSelf) return;

            starsAnchor.position = pelvis.T.position + Vector3.up * 0.55f;
            starsAnchor.Rotate(Vector3.up, StarAngularSpeedDegPerSec * dt, Space.World);

            float remaining = ragdollDuration - ragdollTimer;
            if (remaining <= StarFadeOutSeconds && stars != null)
            {
                float scale = Mathf.Clamp01(remaining / StarFadeOutSeconds) * 0.06f;
                foreach (var s in stars)
                    if (s != null) s.transform.localScale = Vector3.one * scale;
            }
        }

        private void DespawnStars()
        {
            if (stars != null)
                foreach (var s in stars)
                    if (s != null) Destroy(s);
            stars = null;
            if (starsAnchor != null) starsAnchor.gameObject.SetActive(false);
        }
    }
}
