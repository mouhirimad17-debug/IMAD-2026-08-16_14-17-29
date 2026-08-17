namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 9's Bedroom1/Bathroom1 + Bedroom2/Bathroom2
    /// prop tables (Part 4.5 + 4.6 + Law 0.3), shared between the Editor-only
    /// importer and the Play Mode test - same split as Stages 6/7/8's PropSpecs.
    ///
    /// Two items finally get their REAL assets after using placeholders earlier:
    /// Bathroom1_SoapBottle_01 is the actual Part 7.4 pourable prop (Stage 4 used a
    /// simple colored cube placeholder for testing); Bedroom2_CeilingFan_01 is the
    /// actual Part 7.5 fan-mount target (Stage 5 used a placeholder blade box).
    /// </summary>
    public static class Stage9BedroomsPropSpec
    {
        public const string Bed1Root = "Assets/_Project/Models/Bedroom1_Bathroom1/";
        public const string Bed2Root = "Assets/_Project/Models/Bedroom2_Bathroom2/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string BedroomsPropsRootName = "Stage9_BedroomsProps";

        // GrabbableDrag/InteractiveContainer added for the owner-specified Part 2/3
        // fix-up mechanics on Bedroom1_Blanket_01 / Bedroom1_Dresser_01 (previously
        // both DOCUMENT GAPs imported under the base PureStatic/CarryLight classes -
        // see Stage9_Decisions_Log points 3/4 and the new point 8).
        public enum PropClass { PureStatic, Fallable, CarryLight, Pushable, GrabbableDrag, InteractiveContainer }

        public struct Entry
        {
            public string unityName;
            public string sourcePath;
            public float expectedMaxDim;
            public PropClass cls;
            public bool isPourable;
            public bool isCeilingFan;
        }

        public static readonly Entry[] Table =
        {
            // ---- Part 4.5: Bedroom 1 + Bathroom 1 ----
            new Entry{ unityName="Bedroom1_BedKing_01", sourcePath=Bed1Root+"Static/Bedroom1_bedking_01.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Wardrobe_01", sourcePath=Bed1Root+"Static/bedroom1_wardrobe_01.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Pillow_01", sourcePath=Bed1Root+"Throwable/bedroom1_pillow_01.glb", expectedMaxDim=0.50f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_Pillow_02", sourcePath=Bed1Root+"Throwable/bedroom1_pillow_02.glb", expectedMaxDim=0.50f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_Blanket_01", sourcePath=Bed1Root+"Throwable/bedroom1_blanket_01.glb", expectedMaxDim=2.00f, cls=PropClass.GrabbableDrag },
            new Entry{ unityName="Bedroom1_BedsideTable_01", sourcePath=Bed1Root+"Static/bedroom1_bedsidetable_01.glb", expectedMaxDim=0.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_LampSide_01", sourcePath=Bed1Root+"Throwable/Bedroom1_lampside_01.glb", expectedMaxDim=0.35f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_Dresser_01", sourcePath=Bed1Root+"Static/bedroom1_dresser_01.glb", expectedMaxDim=0.90f, cls=PropClass.InteractiveContainer },
            new Entry{ unityName="Bedroom1_MirrorDresser_01", sourcePath=Bed1Root+"Static/bedroom1_mirrorDresser_01.glb", expectedMaxDim=0.70f, cls=PropClass.Fallable },
            new Entry{ unityName="Bedroom1_Perfume_01", sourcePath=Bed1Root+"Throwable/bedroom1_perfume_01.glb", expectedMaxDim=0.12f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_Perfume_02", sourcePath=Bed1Root+"Throwable/bedroom1_perfume_02.glb", expectedMaxDim=0.12f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_JewelryBox_01", sourcePath=Bed1Root+"Throwable/bedroom1_jewelrybox_01.glb", expectedMaxDim=0.20f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom1_ReadingChair_01", sourcePath=Bed1Root+"Throwable/bedroom1_readingchair_01.glb", expectedMaxDim=0.90f, cls=PropClass.Pushable },
            new Entry{ unityName="Bedroom1_Rug_01", sourcePath=Bed1Root+"Static/bedroom1_Rug_01.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Curtains_01", sourcePath=null, expectedMaxDim=2.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Window_01", sourcePath=null, expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Door_01", sourcePath=Bed1Root+"Static/bedroom1_door_01.glb", expectedMaxDim=2.10f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom1_Painting_01", sourcePath=Bed1Root+"Static/bedroom1_painting_01.glb", expectedMaxDim=0.80f, cls=PropClass.Fallable },
            new Entry{ unityName="Bedroom1_ShoeRack_01", sourcePath=Bed1Root+"Throwable/bedroom1_shoerack_01.glb", expectedMaxDim=0.60f, cls=PropClass.Pushable },
            new Entry{ unityName="Bedroom1_LaundryBasket_01", sourcePath=Bed1Root+"Throwable/bedroom1_laundrybasket_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom1_Bathtub_01", sourcePath=Bed1Root+"Static/bedroom1__bathtub_01.glb", expectedMaxDim=1.70f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_Toilet_01", sourcePath=Bed1Root+"Static/bedroom1_toilet_01.glb", expectedMaxDim=0.75f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_Sink_01", sourcePath=Bed1Root+"Static/bedroom1_sink_01.glb", expectedMaxDim=0.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_SoapBottle_01", sourcePath=Bed1Root+"Throwable/bedroom1_soapbottle_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight, isPourable=true },
            new Entry{ unityName="Bathroom1_Shampoo_01", sourcePath=Bed1Root+"Throwable/bedroom1_shampoo_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom1_BathMat_01", sourcePath=Bed1Root+"Throwable/bedroom1_bathmat_01.glb", expectedMaxDim=0.60f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom1_Plunger_01", sourcePath=Bed1Root+"Throwable/bedroom1_plunger_01.glb", expectedMaxDim=0.50f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom1_ToothbrushCup_01", sourcePath=Bed1Root+"Throwable/bedroom1_toothbrushcup_01.glb", expectedMaxDim=0.10f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom1_Mirror_01", sourcePath=Bed1Root+"Static/bedroom1_mirror_01.glb", expectedMaxDim=0.90f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_ShowerCurtain_01", sourcePath=Bed1Root+"Static/bedroom1_showercurtain_01.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_Door_01", sourcePath=Bed1Root+"Static/wooden_door.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom1_Window_01", sourcePath=null, expectedMaxDim=0.60f, cls=PropClass.PureStatic },

            // ---- Part 4.6: Bedroom 2 + Bathroom 2 ----
            new Entry{ unityName="Bedroom2_BedQueen_01", sourcePath=Bed2Root+"Static/bedroom2_bedQueen_01.glb", expectedMaxDim=1.80f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Wardrobe_01", sourcePath=Bed2Root+"Static/bedroom2_wardrobe_01.glb", expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Pillow_01", sourcePath=Bed2Root+"Throwable/bedroom2_pillow_01.glb", expectedMaxDim=0.50f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom2_CeilingFan_01", sourcePath=Bed2Root+"Static/bedroom2_ceilingfan_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic, isCeilingFan=true },
            new Entry{ unityName="Bedroom2_Clock_01", sourcePath=Bed2Root+"Throwable/bedroom2_clock_01.glb", expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom2_Desk_01", sourcePath=Bed2Root+"Static/bedroom2_desk_01.glb", expectedMaxDim=0.75f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Chair_01", sourcePath=Bed2Root+"Throwable/bedroom2_chair_01.glb", expectedMaxDim=0.90f, cls=PropClass.Pushable },
            new Entry{ unityName="Bedroom2_Bookshelf_01", sourcePath=Bed2Root+"Static/bedroom2_bookshelf_01.glb", expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Poster_01", sourcePath=null, expectedMaxDim=0.90f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Beanbag_01", sourcePath=Bed2Root+"Throwable/bedroom2_beanbag_01.glb", expectedMaxDim=0.70f, cls=PropClass.Pushable },
            new Entry{ unityName="Bedroom2_Guitar_01", sourcePath=Bed2Root+"Throwable/bedroom2_guiter_01.glb", expectedMaxDim=1.00f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bedroom2_Rug_01", sourcePath=Bed2Root+"Static/bedroom2_rug_01.glb", expectedMaxDim=1.80f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Curtains_01", sourcePath=null, expectedMaxDim=2.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Window_01", sourcePath=null, expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bedroom2_Door_01", sourcePath=Bed2Root+"Static/bedroom2_door_01.glb", expectedMaxDim=2.10f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_ShowerDoor_01", sourcePath=null, expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_Toilet_01", sourcePath=Bed2Root+"Static/bedroom2_toilet_01.glb", expectedMaxDim=0.75f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_Sink_01", sourcePath=Bed2Root+"Static/bedroom2_sink_01.glb", expectedMaxDim=0.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_TowelRack_01", sourcePath=Bed2Root+"Static/bedroom2_towelrack_01.glb", expectedMaxDim=0.60f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_Towel_01", sourcePath=null, expectedMaxDim=0.60f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom2_SoapDispenser_01", sourcePath=Bed2Root+"Throwable/bedroom2_soapdispenser_01.glb", expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom2_Door_01", sourcePath=null, expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_BathMat_01", sourcePath=Bed2Root+"Throwable/bedroom2_bathmat_01.glb", expectedMaxDim=0.60f, cls=PropClass.CarryLight },
            new Entry{ unityName="Bathroom2_TrashBin_01", sourcePath=null, expectedMaxDim=0.25f, cls=PropClass.Pushable },
            new Entry{ unityName="Bathroom2_ShowerHead_01", sourcePath=Bed2Root+"Static/bedroom2_Showerhead_01.glb", expectedMaxDim=0.15f, cls=PropClass.PureStatic },
            new Entry{ unityName="Bathroom2_Mirror_01", sourcePath=Bed2Root+"Static/bedroom2_mirror_01.glb", expectedMaxDim=0.90f, cls=PropClass.PureStatic },
        };
    }
}
