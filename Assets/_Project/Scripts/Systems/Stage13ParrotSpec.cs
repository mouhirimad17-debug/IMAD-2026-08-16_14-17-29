namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 13's parrot/cage setup (Part 6, stationary-
    /// cage version), shared between the Editor-only importer (Stage13ParrotImporter)
    /// and the Play Mode test (Stage13ParrotTest) - same PropSpec/Importer/Test split
    /// every prop-import stage since Stage 6 has used.
    /// </summary>
    public static class Stage13ParrotSpec
    {
        public const string CageModelPath = "Assets/_Project/Models/Characters/Parrot/Parrot_Cage_01/paroott_cage_01.glb";
        public const string ParrotModelPath = "Assets/_Project/Models/Characters/Parrot/Parrot_Model_01/parrot_modelt_01.glb";
        public const string LaughAudioDir = "Assets/_Project/Audio/Voice/Parrot/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string PrefabName = "Parrot_Cage_01";
        public const string SceneRootName = "Stage13_Parrot";

        // DECISION: Part 4's prop table has no row for the cage or the parrot itself
        // (only Part 1's folder structure and Part 6's behaviour reference them), so
        // Law 0.3's scale-correction has no expected-dimension number to compare
        // against. Chosen against Law 0.1's 1.000m character reference: a comedic-
        // proportioned parrot small enough to look bird-sized next to a 1m character,
        // and a cage roomy enough to read clearly as "a cage" without dwarfing it.
        public const float ExpectedParrotMaxDim = 0.35f;
        public const float ExpectedCageMaxDim = 0.90f;

        // DECISION: Part 6.0 asks for a mount height "بين متر ونصف ومترين" (1.5-2m)
        // near the foyer center, but Part 3.3 reserves the exact foyer center for the
        // future seating cluster, and Part 3.3's relocated staircase footprint
        // (MansionSpec.StaircaseFootprint: x[36,54] z[24,36]) also occupies part of
        // that same central area. Midpoint of the given height range, offset toward
        // the foyer's open front-left quadrant so it clears BOTH the reserved seating
        // center point and the staircase footprint while staying close enough to
        // center for wide room coverage (Part 6.2's 10m detection radius).
        public const float CageMountHeight = 1.75f;
        public const float CageCenterOffsetX = -10f;
        public const float CageCenterOffsetZ = -10f;

        // ---- Part 6.2 detection ----
        public const float DetectionCheckIntervalSeconds = 0.5f; // "يتكرر كل نصف ثانية"
        public const float DetectionRadius = 10f;                // "عشرة أمتار"

        // ---- Part 6.3 mockery ----
        public const float SimpleLaughChance = 0.60f;   // "ستين بالمئة للحركة البسيطة"
        public const float FullSentenceChance = 0.40f;  // "أربعين بالمئة للجملة الكاملة"
        public const float ArabicChance = 0.50f;         // "خمسين بالمئة لكل من المجموعة العربية والإنجليزية"
        public const float MinMockDurationSeconds = 1.5f; // "بحد أدنى ثانية ونصف"

        // ---- Law 0.5 comedic jump while any parrot audio plays ----
        public const float JumpHeight = 0.1f;         // "ارتفاع تقريبي 0.1 متر"
        public const float JumpCycleSeconds = 0.3f;   // "مدة 0.3 ثانية"

        // DECISION: Part 6.1's "Look_Around" has no real animation clip (Animations/
        // Parrot is empty - logged to Missing_Assets_Log.txt) and no numeric turn
        // rate is given anywhere. Built procedurally instead of blocking on the clip:
        // a slow back-and-forth yaw sway, small enough to read as "looking around"
        // from a stationary bird rather than a spin.
        public const float LookAroundPeriodSeconds = 4f;
        public const float LookAroundMaxYawDeg = 25f;
    }
}
