namespace PrankMansion.Systems
{
    /// <summary>
    /// Part 5.2's exact per-character table, one source of truth shared by the
    /// Editor-only importer (Stage12CharacterImporter) and the runtime selector
    /// (Player/CharacterSelector) - same PropSpec/Importer split every prop stage
    /// has used since Stage 6.
    /// </summary>
    public static class CharacterProfile
    {
        public struct Entry
        {
            public string unityName;
            public string modelPath;

            // Part 11.2's character-select card: proper name (never translated -
            // "أسماء علم خاصة") and a one/two-sentence ability description.
            public string displayName;
            public string description;

            // Part 5.1 standard values, overridden per-entry only where Part 5.2 gives
            // this character its own number (Bomba only - everyone else's base speed
            // is the plain Part 5.1 standard).
            public float speedMultiplier;   // multiplies PlayerLocomotion.WalkSpeed/RunSpeed

            // Part 7.1/5.2 heavy-carry solo speed factor + wind start delay.
            public float heavyCarrySpeedFactor;
            public float windStartDelaySeconds;

            // Part 4.1/5.2 push-force multiplier (Doran).
            public float pushForceMultiplier;

            // Part 7.2/5.2 wind-ignition launch horizontal/vertical split (Bouf).
            public float launchHorizontalFraction;
            public float launchVerticalFraction;

            // Part 5.1/5.2 sound detection: how far THIS character can hear others
            // (Reno), and how far AWAY others can hear THIS character's own
            // footsteps (Fifi). Standard for everyone else: 8m both ways.
            public float hearingRange;
            public float footstepAudibleRange;

            // Part 5.2 throw-power multiplier (Nouka) - Part 7.1 never gives a base
            // throw value, only this relative bonus; see PlayerCarry.BaseThrowSpeed.
            public float throwPowerMultiplier;

            public bool hasSoundDirectionIndicator; // Reno only (Part 11.5)
        }

        public const string ModelsRoot = "Assets/_Project/Models/Characters/Player/";

        // Part 5.1 standards, referenced directly so every entry below states its
        // value explicitly rather than relying on an implicit default elsewhere.
        public const float StandardSpeedMultiplier = 1f;
        public const float StandardHeavyCarrySpeedFactor = 0.30f; // Part 7.1: "ثلاثين بالمئة من السرعة الأساسية"
        public const float StandardWindStartDelay = 1f;
        public const float StandardPushForceMultiplier = 1f;
        public const float StandardLaunchHorizontalFraction = 0.20f;
        public const float StandardLaunchVerticalFraction = 0.80f;
        public const float StandardHearingRange = 8f;
        public const float StandardFootstepAudibleRange = 8f;
        public const float StandardThrowPowerMultiplier = 1f;

        public static readonly Entry[] Table =
        {
            new Entry
            {
                unityName = "Character_Slowpoke_01", // Bomba
                modelPath = ModelsRoot + "Character_Slowpoke_01/character_slowpoke_01.fbx",
                displayName = "Bomba",
                description = "Walks and runs 15% slower than everyone else, with no compensating bonus.",
                speedMultiplier = 0.85f, // "2.55 م/ث" / 3 = "5.10 م/ث" / 6 = 0.85 (both -15%)
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = StandardHearingRange,
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = StandardThrowPowerMultiplier,
            },
            new Entry
            {
                unityName = "Character_Strongman_01", // Zico
                modelPath = ModelsRoot + "Character_Strongman_01/character_strongman_01.fbx",
                displayName = "Zico",
                description = "Carries heavy objects solo with only a 30% speed penalty instead of 70%, and holds off the wind for a full 3 seconds instead of 1.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = 0.70f,   // "سبعين بالمئة من الأساسية" instead of standard 30%
                windStartDelaySeconds = 3f,       // "ثلاث ثوانٍ كاملة"
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = StandardHearingRange,
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = StandardThrowPowerMultiplier,
            },
            new Entry
            {
                unityName = "Character_StrongPush_01", // Doran
                modelPath = ModelsRoot + "Character_StrongPush_01/Character_strobgpush_01.fbx",
                displayName = "Doran",
                description = "Pushes objects 40% harder than the standard push force.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = 1.40f, // "أربعين بالمئة فوق القوة القياسية"
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = StandardHearingRange,
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = StandardThrowPowerMultiplier,
            },
            new Entry
            {
                unityName = "Character_BigEars_01", // Reno
                modelPath = ModelsRoot + "Character_BigEars_01/character_Bigears_01.fbx",
                displayName = "Reno",
                description = "Hears footsteps and doors from 15 meters away instead of 8, with a direction arrow only he can see.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = 15f, // "خمسة عشر متراً"
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = StandardThrowPowerMultiplier,
                hasSoundDirectionIndicator = true,
            },
            new Entry
            {
                unityName = "Character_QuietSteps_01", // Fifi
                modelPath = ModelsRoot + "Character_QuietSteps_01/character_QuietSteps_01.fbx",
                displayName = "Fifi",
                description = "Footsteps only carry 4 meters to other players instead of 8 - much harder to hear coming.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = StandardHearingRange,
                footstepAudibleRange = 4f, // "أربعة أمتار فقط"
                throwPowerMultiplier = StandardThrowPowerMultiplier,
            },
            new Entry
            {
                unityName = "Character_QuickPour_01", // Nouka
                modelPath = ModelsRoot + "Character_QuickPour_01/character_QuickPourr_01.fbx",
                displayName = "Nouka",
                description = "Throws objects 25% harder than the standard throw force.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = StandardLaunchHorizontalFraction,
                launchVerticalFraction = StandardLaunchVerticalFraction,
                hearingRange = StandardHearingRange,
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = 1.25f, // "خمسة وعشرين بالمئة فوق القوة القياسية"
            },
            new Entry
            {
                unityName = "Character_Featherweight_01", // Bouf
                modelPath = ModelsRoot + "Character_Featherweight_01/character_Featherweight_01.fbx",
                displayName = "Bouf",
                description = "Wind ignition launches him mostly forward (60% horizontal / 40% vertical) instead of mostly straight up.",
                speedMultiplier = StandardSpeedMultiplier,
                heavyCarrySpeedFactor = StandardHeavyCarrySpeedFactor,
                windStartDelaySeconds = StandardWindStartDelay,
                pushForceMultiplier = StandardPushForceMultiplier,
                launchHorizontalFraction = 0.60f, // instead of standard 0.20
                launchVerticalFraction = 0.40f,   // instead of standard 0.80
                hearingRange = StandardHearingRange,
                footstepAudibleRange = StandardFootstepAudibleRange,
                throwPowerMultiplier = StandardThrowPowerMultiplier,
            },
        };
    }
}
