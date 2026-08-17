using PrankMansion.Blockout;
using PrankMansion.Player;
using PrankMansion.Systems;
using UnityEngine;

namespace PrankMansion.Entities
{
    /// <summary>
    /// Part 6's stationary-cage parrot (Part 6.0's "الببغاء الثابت" rewrite - no
    /// travel between points, no fleeing from pushes). Lives on the cage/parrot
    /// prefab's root; visualRoot is the parrot model transform used for the
    /// procedural Look_Around sway (6.1) and the Law 0.5 comedic jump bounce that
    /// plays for as long as any mockery audio is running (6.3). The cage itself
    /// carries no Rigidbody at all (matches Stage 6's "ثابت صرف" pattern), so pushes
    /// and collisions produce zero reaction by construction - 6.4 needs no extra code.
    /// </summary>
    public class ParrotController : MonoBehaviour
    {
        public enum ParrotState { Idle, Mocking }

        public ParrotState State { get; private set; } = ParrotState.Idle;
        public int MockTriggerCount { get; private set; }
        public string LastMockCategory { get; private set; } = ""; // "laugh" or "sentence"
        public string LastMockLanguage { get; private set; } = ""; // "AR" / "EN" / "" (laugh has none)

        public Transform visualRoot;
        public AudioClip[] laughClips = new AudioClip[0];
        public AudioClip[] mockClipsEnglish = new AudioClip[0];
        public AudioClip[] mockClipsArabic = new AudioClip[0]; // empty until Part 6.3's Arabic set is supplied

        private AudioSource audioSource;
        private Vector3 visualRestLocalPos;
        private float detectionTimer;
        private float mockTimer;
        private float mockDuration;

        private void Awake()
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;

            if (visualRoot != null) visualRestLocalPos = visualRoot.localPosition;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            switch (State)
            {
                case ParrotState.Idle:
                    UpdateLookAround();
                    detectionTimer += dt;
                    if (detectionTimer >= Stage13ParrotSpec.DetectionCheckIntervalSeconds)
                    {
                        detectionTimer = 0f;
                        TryDetectAndMock();
                    }
                    break;

                case ParrotState.Mocking:
                    UpdateJumpBounce(dt);
                    mockTimer += dt;
                    if (mockTimer >= mockDuration) EndMocking();
                    break;
            }
        }

        // ---------------------------------------------------------------
        // 6.2 detection: nearest eligible (ragdolled OR unconscious) player within
        // DetectionRadius of the cage's own fixed position.
        private void TryDetectAndMock()
        {
            PlayerRagdoll best = null;
            float bestDist = float.MaxValue;

            foreach (var ragdoll in FindObjectsByType<PlayerRagdoll>(FindObjectsSortMode.None))
            {
                var capture = ragdoll.GetComponent<PlayerCapture>();
                bool eligible = ragdoll.IsRagdolled || (capture != null && capture.State == CaptureState.Unconscious);
                if (!eligible) continue;

                float dist = Vector3.Distance(ragdoll.transform.position, transform.position);
                if (dist <= Stage13ParrotSpec.DetectionRadius && dist < bestDist)
                {
                    bestDist = dist;
                    best = ragdoll;
                }
            }

            if (best != null) BeginMocking();
        }

        // ---------------------------------------------------------------
        // 6.3 mockery: no approach phase (the parrot never leaves the cage) - fires
        // immediately from its fixed position the instant a target is found.
        private void BeginMocking()
        {
            MockTriggerCount++;
            State = ParrotState.Mocking;
            mockTimer = 0f;

            AudioClip clip = PickMockClip();
            mockDuration = Stage13ParrotSpec.MinMockDurationSeconds;
            if (clip != null)
            {
                mockDuration = Mathf.Max(clip.length, Stage13ParrotSpec.MinMockDurationSeconds);
                AudioService.PlayOneShotSfx(audioSource, clip);
            }
        }

        private AudioClip PickMockClip()
        {
            if (Random.value < Stage13ParrotSpec.SimpleLaughChance)
            {
                LastMockCategory = "laugh";
                LastMockLanguage = "";
                return PickRandom(laughClips);
            }

            LastMockCategory = "sentence";
            bool wantArabic = Random.value < Stage13ParrotSpec.ArabicChance;
            // Arabic sentence set isn't in the project yet (logged to
            // Missing_Assets_Log.txt by the importer) - fall back to English rather
            // than staying silent, per Law 0.2's "don't block on one missing asset".
            AudioClip[] pool = wantArabic && mockClipsArabic.Length > 0 ? mockClipsArabic : mockClipsEnglish;
            LastMockLanguage = (wantArabic && mockClipsArabic.Length > 0) ? "AR" : "EN";
            return PickRandom(pool);
        }

        /// Test-only hook: samples the mockery pick (category/language roll) without
        /// entering the Mocking state, so Stage13ParrotTest can statistically verify
        /// the 60/40 and 50/50 splits without waiting through real mockery durations.
        public AudioClip DebugSampleMockPick() => PickMockClip();

        private static AudioClip PickRandom(AudioClip[] clips) =>
            clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : null;

        private void EndMocking()
        {
            State = ParrotState.Idle;
            detectionTimer = 0f;
            if (visualRoot != null) visualRoot.localPosition = visualRestLocalPos;
        }

        // ---------------------------------------------------------------
        private void UpdateLookAround()
        {
            if (visualRoot == null) return;
            float yaw = Mathf.Sin(Time.time * (2f * Mathf.PI / Stage13ParrotSpec.LookAroundPeriodSeconds)) * Stage13ParrotSpec.LookAroundMaxYawDeg;
            visualRoot.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private void UpdateJumpBounce(float dt)
        {
            if (visualRoot == null) return;
            float phase = Mathf.PingPong(mockTimer, Stage13ParrotSpec.JumpCycleSeconds * 0.5f) / (Stage13ParrotSpec.JumpCycleSeconds * 0.5f);
            float bounce = Mathf.Sin(phase * Mathf.PI) * Stage13ParrotSpec.JumpHeight;
            visualRoot.localPosition = visualRestLocalPos + Vector3.up * bounce;
        }
    }
}
