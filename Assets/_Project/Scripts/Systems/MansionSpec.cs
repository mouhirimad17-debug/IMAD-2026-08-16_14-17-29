using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Simple axis-aligned footprint on the X/Z ground plane.
    /// x/z = min corner, sizeX/sizeZ = extent along each axis.
    /// </summary>
    public struct XZRect
    {
        public float x;
        public float z;
        public float sizeX;
        public float sizeZ;

        public XZRect(float x, float z, float sizeX, float sizeZ)
        {
            this.x = x;
            this.z = z;
            this.sizeX = sizeX;
            this.sizeZ = sizeZ;
        }

        public float xMax => x + sizeX;
        public float zMax => z + sizeZ;
        public float centerX => x + sizeX * 0.5f;
        public float centerZ => z + sizeZ * 0.5f;
    }

    /// <summary>
    /// Single source of truth for every numeric value used to build the Stage 1
    /// mansion blockout. Values are taken directly from PrankMansion_MasterDocument.md,
    /// Part 3 (architecture) and Law 0.4 (closed mansion). Referenced by both the
    /// editor-time builder (MansionBlockoutBuilder) and the runtime wall-closure test
    /// (StageOneWallClosureTest) so both always agree on exact coordinates.
    ///
    /// REVISION (post owner review of the first Stage 1 result): Part 3 was corrected
    /// with 3 changes - (1) the whole footprint is scaled x3 (30x20 -> 90x60),
    /// (2) the double staircase moved off the foyer center to a side wall so the
    /// foyer center is free for seating furniture, (3) every interior wall must carry
    /// a door gap from this stage onward (new law 3.6). All values below reflect the
    /// corrected document. Values NOT given an explicit number in the master document
    /// (staircase step count/rise-run, exact wing-corner assignment, roof/ground slab
    /// existence, railing thickness, door gap width/height) are marked DECISION below
    /// and are logged to Assets/_ProjectLogs/Stage1_Decisions_Log.txt per document law 21.2.
    /// </summary>
    public static class MansionSpec
    {
        // ---- 3.1 global envelope (x3 revision: 30x20 -> 90x60) ----
        public const float LenX = 90f;                // longitudinal footprint (X axis)
        public const float WidZ = 60f;                 // transverse footprint (Z axis)
        public const float ExtWallT = 0.30f;          // law 0.4 (unchanged - only horizontal footprint scaled)
        public const float IntWallT = 0.20f;          // 3.1 interior room walls (unchanged)
        public const float BathWallT = 0.15f;         // 3.4 wing-internal bathroom split (unchanged)
        public const float FloorClearH = 3.20f;       // 3.1 floor-to-ceiling-underside, each floor (unchanged - heights did not scale)
        public const float SlabT = 0.30f;             // 3.1 inter-floor slab thickness (unchanged)

        // DECISION: floor-to-floor rise = clear height + slab thickness (3.50m), taking
        // 3.1's explicit two-value formula as authoritative over 3.3's parenthetical
        // "(3.20 m)" shorthand for the staircase, which reads as a loose restatement of
        // the floor-height constant rather than a distinct, separately-computed rise value.
        public const float Floor2Level = FloorClearH + SlabT;        // 3.50
        public const float WallTop = FloorClearH * 2f + SlabT;       // 6.70 (both floors, no gap)

        // DECISION: roof slab and ground slab thickness/existence are not given explicit
        // numbers in Part 3 (only "wall runs floor to ceiling, no interruption" is stated).
        // Reusing the 0.30m inter-floor slab thickness for both is a reasonable, logged,
        // easily-adjustable technical choice.
        public const float RoofT = 0.30f;
        public const float GroundSlabT = 0.30f;

        public const float GroundY = 0f;
        public const float Floor1CeilingY = FloorClearH;                 // 3.20
        public const float Floor2FloorY = Floor2Level;                   // 3.50 (top of inter-floor slab)
        public const float Floor2CeilingY = Floor2Level + FloorClearH;   // 6.70
        public const float RoofTopY = WallTop + RoofT;                   // 7.00

        // ---- 3.2 ground floor room zones (x3 revision; front strip; remaining back space = corridor) ----
        public static readonly XZRect Gym = new XZRect(0f, 0f, 24f, 18f);
        public static readonly XZRect Foyer = new XZRect(24f, 0f, 42f, 36f);
        public static readonly XZRect Kitchen = new XZRect(66f, 0f, 24f, 24f);

        // ---- 3.3 ceiling opening (REVISED: stays centered on the foyer footprint,
        // independent of the staircase's new side location - "موقع الفتحة العلوية لا
        // يتحرك مع الدرج"). 24m x 18m per the corrected document, centered on the foyer.
        public static readonly XZRect Opening = new XZRect(33f, 9f, 24f, 18f);

        // ---- 3.3 double staircase (REVISED: relocated off the foyer center to the
        // foyer's long wall opposite the main entrance, i.e. the wall shared with the
        // back service corridor at z = Foyer.zMax, freeing the foyer center for seating
        // furniture per the owner's explicit decision).
        //
        // DECISION: "the long side wall" is read as the wall running along the foyer's
        // longer dimension (42m, front/back) rather than the shorter one (36m, left/right).
        // The wall opposite the main entrance (assumed on the exterior front wall,
        // z=0) is the interior wall at z=Foyer.zMax=36 shared with the back corridor -
        // that is the wall chosen. Footprint: 18m along the wall (X) x 12m deep into the
        // room (Z), flush against that wall (zMax = 36).
        //
        // DECISION: exact step count / rise / run are not numerically specified in the
        // document (only overall footprint 18x12m and total rise are). Built as literal
        // stacked cube steps (matching this stage's own name: "blockout WITH CUBES"),
        // horizontal step dimensions scaled x3 with the rest of the footprint; vertical
        // rise/step-count left unchanged since floor height did not scale:
        // two parallel 5.4m-wide sub-flights (1.2m decorative gap between them) rising
        // from a shared bottom start to a shared mid-landing at half height, then two
        // more parallel sub-flights continuing separately to the shared top landing,
        // flush with the floor-2 slab edge (no fall-through gap at arrival).
        //
        // The Opening and this Shaft are independent, non-moving rectangles that happen
        // to overlap slightly where the foyer's depth forces them together (foyer is
        // 36m deep; the centered 18m-deep opening occupies z:[9,27], the wall-flush 12m-
        // deep stair occupies z:[24,36] - a 3m overlap band). The floor-2 slab and its
        // balcony railing are both built by a generic rectilinear "cut around N holes"
        // algorithm (see MansionBlockoutBuilder) that correctly resolves this overlap
        // into one merged void with no double-walled seam and no unguarded floor edge,
        // without hand-tuning a manual gap - see decisions log point 6.
        public static readonly XZRect StaircaseFootprint = new XZRect(36f, 24f, 18f, 12f);
        public const float StairFlightStartX = 37.5f;      // stair.x + 1.5m bottom landing
        public const float StairMidLandingStartX = 45f;    // end of flight 1 (10 steps * 0.75m)
        public const float StairMidLandingEndX = 46.5f;     // + 1.5m mid landing
        public const float StairStepRun = 0.75f;            // horizontal, scaled x3 with footprint
        public const float StairStepRise = 0.175f;          // vertical, unchanged (1.75m half-height / 10 steps)
        public const int StairStepsPerFlight = 10;
        public const float StairSubFlightWidth = 5.4f;       // scaled x3
        public const float StairGapZMin = 29.4f;             // decorative center gap between sub-flights
        public const float StairGapZMax = 30.6f;

        // ---- 3.4 floor 2 wings (x3 revision: each wing now ~30m x 24m, matching the
        // document's explicit new numbers exactly) ----
        // DECISION: the document places wings at 3 of the upper floor's 4 corners
        // ("الزاوية الأولى/المقابلة/الثالثة من الطابق العلوي") without giving X/Z
        // coordinates. Corner assignment and the choice to leave the 4th corner open
        // as a hallway hub are carried over unchanged from the original Stage 1 build
        // (only the coordinates are rescaled x3), since the document's revision did not
        // touch this decision.
        public static readonly XZRect OfficeWing = new XZRect(0f, 36f, 30f, 24f);     // back-west
        public static readonly XZRect Bedroom2Wing = new XZRect(0f, 0f, 30f, 24f);    // front-west
        public static readonly XZRect Bedroom1Wing = new XZRect(60f, 36f, 30f, 24f);  // back-east
        // front-east (60,0,30,24) intentionally left open: stair-arrival hallway hub

        public const float OfficeBathSplitX = 21f;    // office: main [0,21] (70%, exterior side), bath [21,30] (30%, hub side)
        public const float Bedroom2BathSplitX = 21f;  // bedroom2: main [0,21], bath [21,30] (same pattern)
        public const float Bedroom1BathSplitX = 69f;  // bedroom1: bath [60,69] (30%, hub side), main [69,90] (70%, exterior side)

        // ---- 3.3 balcony railing ----
        public const float RailingHeight = 1.0f;
        // DECISION: railing post/panel thickness not specified numerically; 0.10m chosen
        // as a reasonable, logged value for a blockout-stage placeholder railing.
        public const float RailingThickness = 0.10f;

        // ---- 3.6 (NEW) interior door gaps - mandatory in every interior wall from the
        // Blockout stage itself. DECISION: exact width/height not pinned to a decimal in
        // the document beyond "~1m wide, ~2.1m tall" (3.6's own words), so those
        // approximate figures are used directly as exact blockout values.
        public const float DoorWidth = 1.0f;
        public const float DoorHeight = 2.1f;
    }
}
