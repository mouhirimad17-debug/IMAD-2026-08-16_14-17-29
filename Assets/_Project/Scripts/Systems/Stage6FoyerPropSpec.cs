namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 6's Foyer prop table (Part 4.2 + Law 0.3),
    /// shared between the Editor-only importer (Stage6FoyerImporter, which builds
    /// the corrected prefabs) and the Play Mode test (Stage6FoyerPropsTest, which
    /// can't reference Editor-assembly types). Same split as MansionSpec (runtime)
    /// vs MansionBlockoutBuilder (Editor).
    /// </summary>
    public static class Stage6FoyerPropSpec
    {
        public const string ModelsRoot = "Assets/_Project/Models/Foyer/";
        public const string ArchModelsRoot = "Assets/_Project/Models/Architecture/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string FoyerPropsRootName = "Stage6_FoyerProps";

        public enum PropClass { PureStatic, Fallable, CarryLight, FireSourceFallable, Pushable }

        public struct Entry
        {
            public string unityName;
            public string sourcePath;
            public float expectedMaxDim;
            public PropClass cls;
        }

        public static readonly Entry[] Table =
        {
            new Entry{ unityName="Foyer_SofaLuxury_01", sourcePath=ModelsRoot+"Static/Foyer_sofa__01.glb", expectedMaxDim=2.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_Armchair_01", sourcePath=ModelsRoot+"Static/Foyer_armchair_01.glb", expectedMaxDim=0.90f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_CoffeeTable_01", sourcePath=ModelsRoot+"Static/Foyer_coffeetable_01.glb", expectedMaxDim=1.20f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_Rug_01", sourcePath=ModelsRoot+"Static/Foyer_rug_01.glb", expectedMaxDim=3.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_Chandelier_01", sourcePath=ModelsRoot+"Static/Foyer_chandelier_01.glb", expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_Painting_01", sourcePath=ModelsRoot+"Static/Foyer_painting_01.glb", expectedMaxDim=1.20f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_VaseLarge_01", sourcePath=ModelsRoot+"Throwable/Foyer_vaselarge_01.glb", expectedMaxDim=0.60f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_VaseSmall_01", sourcePath=ModelsRoot+"Throwable/Foyer_vaseSmall_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_ClockGrand_01", sourcePath=ModelsRoot+"Static/Foyer_clockgrand_01.glb", expectedMaxDim=2.20f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_SideTable_01", sourcePath=ModelsRoot+"Static/Foyer_sidetable_01.glb", expectedMaxDim=0.60f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_Curtains_01", sourcePath=null, expectedMaxDim=3.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_Statue_01", sourcePath=ModelsRoot+"Static/Foyer_statue_01.glb", expectedMaxDim=1.60f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_FruitBowl_01", sourcePath=ModelsRoot+"Throwable/Foyer_fruitbowl_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_CandelabraStanding_01", sourcePath=ModelsRoot+"Static/Foyer_candelabraStanding_01.glb", expectedMaxDim=1.50f, cls=PropClass.FireSourceFallable },
            new Entry{ unityName="Foyer_Ottoman_01", sourcePath=ModelsRoot+"Throwable/Foyer_ottoman_01.glb", expectedMaxDim=0.50f, cls=PropClass.Pushable },
            new Entry{ unityName="Foyer_BookshelfTall_01", sourcePath=ModelsRoot+"Static/Foyer_bookshelftall_01.glb", expectedMaxDim=2.50f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_UmbrellaStand_01", sourcePath=null, expectedMaxDim=0.70f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_MirrorOrnate_01", sourcePath=ModelsRoot+"Static/Foyer_mirrorornate_01.glb", expectedMaxDim=1.50f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_ChampagneBottle_01", sourcePath=ModelsRoot+"Throwable/Foyer_champagnebottles_01.glb", expectedMaxDim=0.35f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_ChampagneGlass_01", sourcePath=ModelsRoot+"Throwable/Foyer_champagneglass_01.glb", expectedMaxDim=0.20f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_Ashtray_01", sourcePath=ModelsRoot+"Throwable/Foyer_ashtray_01.glb", expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_ChessTable_01", sourcePath=null, expectedMaxDim=0.70f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_ChessPiece_01", sourcePath=null, expectedMaxDim=0.08f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_Sconce_01", sourcePath=ModelsRoot+"Static/Foyer_sconce_01.glb", expectedMaxDim=0.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_PianoGrand_01", sourcePath=ModelsRoot+"Throwable/Foyer_pianogrand_01.glb", expectedMaxDim=1.50f, cls=PropClass.Pushable },
            new Entry{ unityName="Foyer_PianoBench_01", sourcePath=ModelsRoot+"Throwable/Foyer_pianobench_01.glb", expectedMaxDim=0.90f, cls=PropClass.Pushable },
            new Entry{ unityName="Foyer_FloorLamp_01", sourcePath=null, expectedMaxDim=1.60f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_CoatRack_01", sourcePath=ModelsRoot+"Static/Foyer_coatrack_01.glb", expectedMaxDim=1.80f, cls=PropClass.Fallable },
            new Entry{ unityName="Foyer_Pillow_01", sourcePath=ModelsRoot+"Throwable/Foyer_pillow_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_WineRack_01", sourcePath=ModelsRoot+"Static/Foyer_winerack_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Foyer_WineBottle_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Foyer_MainDoor_01", sourcePath=ArchModelsRoot+"arch_maindoor_01.glb", expectedMaxDim=2.40f, cls=PropClass.PureStatic },
        };
    }
}
