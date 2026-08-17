using PrankMansion.Entities;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Part 7.1 (carry) + Part 7.2 (wind/fart) in one component, since 7.2's whole
    /// lifecycle (start delay, ignition, launch) only exists while 7.1's solo-heavy-
    /// carry state is active - they're one state machine, not two independent ones.
    ///
    /// Per-character override fields (heavySpeedFactor, windStartDelay,
    /// launchHorizontalFraction/launchVerticalFraction) default to the standard
    /// values every character uses per Part 5.1/5.2, EXCEPT Zico (0.70 speed factor,
    /// 3s wind delay) and Bouf (60/40 launch split instead of 20/80). Stage 12 (the
    /// character roster) is what actually assigns those two characters' real values
    /// per prefab; this stage only wires the mechanism and documents the numbers.
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    public class PlayerCarry : MonoBehaviour
    {
        // ---- Part 7.1 ----
        public const float PickupRange = 2f;               // "مسافة الالتقاط القصوى ... مترين"
        public const float PickupHalfAngleDeg = 45f;        // "زاوية ... تسعين درجة إجمالية" / 2
        public const float SecondCarrierJoinRange = 1.5f;   // "أقل من متر ونصف"
        public const float StandardHeavySpeedFactor = 0.30f; // Part 5.1: 70% decut -> 30% of base speed
        public const float ZicoHeavySpeedFactor = 0.70f;     // Part 5.2 exception
        public const float StandardWindStartDelay = 1f;      // "بعد ثانية واحدة من بدء الحمل"
        public const float ZicoWindStartDelay = 3f;           // Part 5.2 exception ("ثلاث ثوانٍ كاملة")

        // ---- Part 7.2 ----
        public const float WindPushFraction = 0.10f;          // "عشرة بالمئة من قوة الجري العادية"
        public const float WindParticleRatePerSecond = 15f;
        public const float WindParticleLifetime = 1.5f;
        public const float RocketForce = 12f;                 // Unity units, ~70kg character mass
        public const float StandardLaunchHorizontalFraction = 0.20f;
        public const float StandardLaunchVerticalFraction = 0.80f;
        public const float BoufLaunchHorizontalFraction = 0.60f; // Part 5.2 exception
        public const float BoufLaunchVerticalFraction = 0.40f;
        public const float CeilingStopBeatSeconds = 0.05f;    // law 0.5 (mirrored in PlayerLocomotion)
        public const float IgniteFlashSeconds = 0.1f;         // law 0.5
        public const int IgniteSparkParticleCountMin = 20;    // law 0.5
        public const int IgniteSparkParticleCountMax = 30;    // law 0.5

        // ---- Part 7.4 (pour mechanic - the trap itself is Entities/SlipZone.cs) ----
        public const float PourHoldSeconds = 0.8f;   // "ضغطة مطوّلة ... لمدة ثمانية أعشار الثانية"
        public const float PourRaycastMaxDistance = 10f;

        [Header("Per-character overrides (standard values; Stage 12 assigns Zico/Bouf)")]
        public float heavySpeedFactor = StandardHeavySpeedFactor;
        public float windStartDelay = StandardWindStartDelay;
        public float launchHorizontalFraction = StandardLaunchHorizontalFraction;
        public float launchVerticalFraction = StandardLaunchVerticalFraction;

        public CarryableObject Held { get; private set; }
        public bool IsPrimaryCarrier { get; private set; }
        public bool WindActive { get; private set; }
        public int IgniteCount { get; private set; }

        private PlayerLocomotion locomotion;
        private Transform handAttach;
        private ParticleSystem windParticles;
        private AudioSource windAudio;
        private AudioSource oneShotAudio;
        private AudioClip windLoopClip;
        private AudioClip igniteClip;
        private float heavyCarryStartTime;
        private PlayerInputReader inputReader;
        private float pourHoldTimer;

        public int PourCount { get; private set; }

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();
            BuildHandAttachPoint();
            BuildWindEffects();

            inputReader = GetComponent<PlayerInputReader>();
            if (inputReader != null)
            {
                inputReader.OnInteractPressed += HandleInteractPressed;
                inputReader.OnThrowPressed += HandleThrowPressed; // out of scope numerically (see class doc) - used as "drop"
            }
        }

        private void OnDestroy()
        {
            if (inputReader != null)
            {
                inputReader.OnInteractPressed -= HandleInteractPressed;
                inputReader.OnThrowPressed -= HandleThrowPressed;
            }
        }

        // Part 7.5: context-sensitive, matching this component's existing dispatch
        // pattern - holding a rope tries to restrain a nearby unconscious player
        // first; otherwise, empty-handed tries to grab a restrained body's head/feet
        // end first; only then does it fall back to Part 7.1's normal object pickup.
        private void HandleInteractPressed()
        {
            if (Held != null && Held.isRope && TryRestrainNearestUnconscious()) return;
            if (Held == null && TryGrabNearestRestrainedEnd()) return;
            if (Held == null) TryPickUpNearest();
        }

        // Part 7.1/7.2 never give a throw mechanic a base numeric value - only Part
        // 5.2's per-character RELATIVE bonus (Nouka: +25%). DECISION (Stage 12):
        // BaseThrowSpeed below is a reasonable, logged value in the same physics
        // scale as the project's other established forces (RocketForce=12,
        // PlayerPushInteraction.BasePushForce=5). Part 7.5's three joint-carry
        // finishing moves also share this button, context-sensitive on location
        // (balcony edge / fan / anywhere else).
        public const float BaseThrowSpeed = 8f;
        public float throwPowerMultiplier = 1f; // Stage 12's CharacterSelector sets Nouka's to 1.25

        // Public (not just the private event handler) so test code can invoke it
        // directly on a rig with no PlayerInputReader, same established pattern as
        // TryPickUpNearest/Drop/TryRestrainNearestUnconscious below.
        public void HandleThrowPressed()
        {
            if (Held != null) { ThrowHeld(); return; }
            TryResolveJointCarryInsult();
        }

        private void ThrowHeld()
        {
            var obj = Held;
            bool wasPrimary = IsPrimaryCarrier;
            Held = null;
            IsPrimaryCarrier = false;

            if (!wasPrimary)
            {
                // A secondary (joint) carrier can't unilaterally throw a jointly-held
                // object - same as Drop()'s secondary-path, just lets go of their grip.
                var primaryOnly = obj.PrimaryCarrier;
                obj.AttachSecondary(null);
                if (primaryOnly != null)
                {
                    primaryOnly.locomotion.SpeedMultiplier = primaryOnly.heavySpeedFactor;
                    primaryOnly.heavyCarryStartTime = Time.time;
                }
                return;
            }

            if (obj.SecondaryCarrier != null) obj.SecondaryCarrier.ForceReleaseHeld();
            obj.DetachAll();
            locomotion.SpeedMultiplier = 1f;
            StopWind();
            obj.Body.linearVelocity = transform.forward * (BaseThrowSpeed * throwPowerMultiplier);

            var myTeam = GetComponent<PlayerTeam>();
            obj.MarkThrown(myTeam != null ? myTeam.Team : Team.None, gameObject); // Part 9.1: direct-hit throw scoring
        }

        public bool TryRestrainNearestUnconscious()
        {
            foreach (var capture in FindObjectsByType<PlayerCapture>(FindObjectsSortMode.None))
            {
                if (capture.State != CaptureState.Unconscious) continue;
                if (Vector3.Distance(transform.position, capture.transform.position) >= PlayerCapture.RestrainRange) continue;
                if (capture.TryRestrainBy(this, Held))
                {
                    // The rope's GameObject was just destroyed inside TryRestrainBy -
                    // drop the now-dangling reference instead of leaving Held stuck
                    // pointing at it (mirrors DetachAll's role after a normal Drop()).
                    Held = null;
                    IsPrimaryCarrier = false;
                    return true;
                }
            }
            return false;
        }

        public bool TryGrabNearestRestrainedEnd()
        {
            foreach (var capture in FindObjectsByType<PlayerCapture>(FindObjectsSortMode.None))
            {
                if (capture.State != CaptureState.Restrained || capture.IsJointCarried) continue;
                if (capture.TryGrabEnd(this)) return true;
            }
            return false;
        }

        public void TryResolveJointCarryInsult()
        {
            foreach (var capture in FindObjectsByType<PlayerCapture>(FindObjectsSortMode.None))
            {
                if (!capture.IsCarriedBy(this)) continue;

                if (capture.TryThrowFromBalcony()) return;

                CeilingFan nearestFan = null;
                float nearestDist = PlayerCapture.FanMountRange;
                foreach (var fan in FindObjectsByType<CeilingFan>(FindObjectsSortMode.None))
                {
                    float d = Vector3.Distance(capture.transform.position, fan.transform.position);
                    if (d < nearestDist) { nearestDist = d; nearestFan = fan; }
                }
                if (nearestFan != null && capture.TryMountOnFan(nearestFan)) return;

                capture.ReleaseJointCarry(); // option 3: nothing special nearby, just let go
                return;
            }
        }

        public bool TryPickUpNearest()
        {
            CarryableObject best = null;
            float bestDist = float.MaxValue;

            foreach (var candidate in FindObjectsByType<CarryableObject>(FindObjectsSortMode.None))
            {
                if (!IsInPickupCone(candidate.transform.position, out float dist)) continue;
                if (candidate.CarrierCount >= 2) continue;
                if (candidate.CarrierCount == 1 &&
                    (candidate.weightClass != CarryableObject.WeightClass.Heavy ||
                     Vector3.Distance(transform.position, candidate.transform.position) >= SecondCarrierJoinRange))
                    continue;

                if (dist < bestDist) { bestDist = dist; best = candidate; }
            }

            return best != null && TryPickUp(best);
        }

        public bool IsInPickupCone(Vector3 worldPos, out float distance)
        {
            Vector3 toTarget = worldPos - transform.position;
            distance = toTarget.magnitude;
            if (distance > PickupRange) return false;

            Vector3 flatFacing = transform.forward; flatFacing.y = 0f;
            Vector3 flatToTarget = toTarget; flatToTarget.y = 0f;
            if (flatToTarget.sqrMagnitude < 0.0001f) return true; // right on top of it

            float angle = Vector3.Angle(flatFacing.normalized, flatToTarget.normalized);
            return angle <= PickupHalfAngleDeg;
        }

        public bool TryPickUp(CarryableObject target)
        {
            if (target.CarrierCount == 0)
            {
                target.AttachPrimary(this);
                Held = target;
                IsPrimaryCarrier = true;

                if (target.weightClass == CarryableObject.WeightClass.Heavy)
                {
                    locomotion.SpeedMultiplier = heavySpeedFactor;
                    heavyCarryStartTime = Time.time;
                }
                else
                {
                    locomotion.SpeedMultiplier = 1f;
                }
                return true;
            }

            if (target.CarrierCount == 1 && target.weightClass == CarryableObject.WeightClass.Heavy
                && Vector3.Distance(transform.position, target.transform.position) < SecondCarrierJoinRange)
            {
                // Part 7.1: second carrier joins -> both speed penalties AND wind cancel immediately.
                target.AttachSecondary(this);
                Held = target;
                IsPrimaryCarrier = false;
                locomotion.SpeedMultiplier = 1f;

                var primary = target.PrimaryCarrier;
                primary.locomotion.SpeedMultiplier = 1f;
                primary.StopWind();
                return true;
            }

            return false;
        }

        public void Drop()
        {
            if (Held == null) return;

            bool wasPrimary = IsPrimaryCarrier;
            var obj = Held;
            Held = null;

            if (wasPrimary)
            {
                // A joint carry has no meaning with only one carrier left holding on -
                // dropping the primary releases the object entirely, so the secondary
                // carrier's own held-state must be cleared too (not just the object's).
                if (obj.SecondaryCarrier != null) obj.SecondaryCarrier.ForceReleaseHeld();
                obj.DetachAll();
                locomotion.SpeedMultiplier = 1f;
                StopWind();
            }
            else
            {
                // Secondary carrier simply lets go; the object stays with the primary
                // carrier, who returns to the solo heavy-carry state (and its speed
                // penalty/wind timer resume from a fresh start).
                var primary = obj.PrimaryCarrier;
                obj.AttachSecondary(null);
                if (primary != null)
                {
                    primary.locomotion.SpeedMultiplier = primary.heavySpeedFactor;
                    primary.heavyCarryStartTime = Time.time;
                }
            }
        }

        internal void ForceReleaseHeld()
        {
            Held = null;
            IsPrimaryCarrier = false;
        }

        // Test rigs have no PlayerInputReader (Stage 2/3's established pattern of
        // driving components directly instead of simulating device input), so the
        // pour long-press falls back to this settable flag when there's no reader.
        public bool SimulateInteractHeld { get; set; }
        private bool IsInteractCurrentlyHeld => inputReader != null ? inputReader.IsInteractHeld : SimulateInteractHeld;

        private void Update()
        {
            if (Held != null)
            {
                Held.transform.position = handAttach.position;
                Held.transform.rotation = handAttach.rotation;

                if (IsPrimaryCarrier && Held.weightClass == CarryableObject.WeightClass.Heavy && Held.CarrierCount == 1)
                {
                    if (!WindActive && Time.time - heavyCarryStartTime >= windStartDelay)
                        StartWind();
                }
            }

            if (WindActive)
            {
                locomotion.ExternalVelocity = transform.forward * (PlayerLocomotion.RunSpeed * WindPushFraction);
                CheckIgnition();
            }
            else
            {
                locomotion.ExternalVelocity = Vector3.zero;
            }

            UpdatePour(Time.deltaTime);
        }

        private void UpdatePour(float dt)
        {
            if (Held != null && Held.isPourable && IsInteractCurrentlyHeld)
            {
                pourHoldTimer += dt;
                if (pourHoldTimer >= PourHoldSeconds) DoPour();
            }
            else
            {
                pourHoldTimer = 0f;
            }
        }

        private void DoPour()
        {
            var obj = Held;

            // "خط استشعار نازل من موقع اللاعب لتحديد نقطة التلامس الدقيقة" - straight
            // downward raycast finds the exact surface (floor, stairs, balcony) underfoot.
            Vector3 origin = transform.position + Vector3.up * 0.1f;
            Vector3 contactPoint = transform.position;
            if (Physics.Raycast(origin, Vector3.down, out var hit, PourRaycastMaxDistance))
                contactPoint = hit.point;

            var zoneGo = new GameObject("SlipZone");
            zoneGo.transform.position = contactPoint + Vector3.up * 0.01f;
            var zone = zoneGo.AddComponent<SlipZone>(); // RequireComponent adds its SphereCollider
            var placerTeam = GetComponent<PlayerTeam>();
            zone.PlacerTeam = placerTeam != null ? placerTeam.Team : Team.None; // Part 9.1 scoring credit

            Held = null;
            IsPrimaryCarrier = false;
            locomotion.SpeedMultiplier = 1f; // pourables are always Light - safety net, not a real state change
            Destroy(obj.gameObject); // "الغرض القابل للسكب يُستهلك فوراً"
            pourHoldTimer = 0f;
            PourCount++;
        }

        private void StartWind()
        {
            WindActive = true;
            windParticles.Play();
            windAudio.clip = windLoopClip;
            windAudio.loop = true;
            windAudio.Play();
        }

        private void StopWind()
        {
            if (!WindActive) return;
            WindActive = false;
            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            windAudio.Stop();
            locomotion.ExternalVelocity = Vector3.zero;
        }

        private void CheckIgnition()
        {
            foreach (var fire in FireSource.Active)
            {
                if (Vector3.Distance(transform.position, fire.transform.position) <= FireSource.DetectionRadius)
                {
                    Ignite();
                    return;
                }
            }
        }

        private void Ignite()
        {
            IgniteCount++;
            StopWind();

            AudioService.PlayOneShotSfx(oneShotAudio, igniteClip);
            SpawnIgniteBurst();

            if (Held != null)
            {
                var obj = Held;
                Held = null;
                obj.DetachAll(); // "الغرض المحمول ... يسقط فوراً" - plain physics takes over
            }

            float horiz = RocketForce * launchHorizontalFraction;
            float vert = RocketForce * launchVerticalFraction;
            Vector3 launchVelocity = transform.forward * horiz + Vector3.up * vert;
            locomotion.BeginFreeFlight(launchVelocity);

            // Part 9.1 event 3: this mechanic is inherently self-inflicted (the
            // wind-active carrier is the one who walks into the fire), so there is no
            // second acting player to credit - per 9.1's own explicit simplification
            // note, the point goes to the launched player's OPPOSING team as a whole.
            var myTeam = GetComponent<PlayerTeam>();
            if (myTeam != null && myTeam.Team != Team.None)
                RoundManager.Instance?.RegisterPoint(PlayerTeam.Opponent(myTeam.Team));
        }

        // ---------------------------------------------------------------
        private void BuildHandAttachPoint()
        {
            var go = new GameObject("HandAttach");
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0.9f, 0.45f); // roughly chest-forward height
            go.transform.localRotation = Quaternion.identity;
            handAttach = go.transform;
        }

        private void BuildWindEffects()
        {
            var vfxGo = new GameObject("WindParticles");
            vfxGo.transform.SetParent(transform);
            vfxGo.transform.localPosition = new Vector3(0f, 0.5f, -0.15f); // "خلف حوض الشخصية"

            windParticles = vfxGo.AddComponent<ParticleSystem>();
            var main = windParticles.main;
            main.startLifetime = WindParticleLifetime;
            main.startSpeed = 0.5f;
            main.startSize = 0.15f;
            main.startColor = new Color(0.55f, 0.65f, 0.5f, 0.5f); // grey-green, translucent
            main.loop = true;
            main.playOnAwake = false;

            var emission = windParticles.emission;
            emission.rateOverTime = WindParticleRatePerSecond;

            var shape = windParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.05f;

            windParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            windAudio = vfxGo.AddComponent<AudioSource>();
            windAudio.playOnAwake = false;
            windAudio.spatialBlend = 1f;
            AudioService.RegisterSfx(windAudio, 0.25f); // "مستوى صوت منخفض نسبياً" - relative baseline, still scales with the SFX slider
            windLoopClip = PlaceholderAudio.GenerateTone("Placeholder_WindLoop", 90f, 0.5f, 0.15f);

            oneShotAudio = gameObject.AddComponent<AudioSource>();
            oneShotAudio.playOnAwake = false;
            oneShotAudio.spatialBlend = 1f;
            igniteClip = PlaceholderAudio.GenerateTone("Placeholder_IgniteExplosion", 140f, 0.35f, 0.4f);
        }

        private void SpawnIgniteBurst()
        {
            var go = new GameObject("IgniteBurst");
            go.transform.position = transform.position + Vector3.up * 0.6f;

            float lifetime = IgniteFlashSeconds * 3f; // sparks briefly outlive the flash itself

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = lifetime;
            main.startSpeed = 3f;
            main.startSize = 0.08f;
            main.startColor = new Color(1f, 0.65f, 0.15f); // yellow-orange flash/spark
            main.loop = false;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            int sparkCount = Random.Range(IgniteSparkParticleCountMin, IgniteSparkParticleCountMax + 1);
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, sparkCount) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            ps.Play();
            Destroy(go, lifetime + 0.2f);
        }
    }
}
