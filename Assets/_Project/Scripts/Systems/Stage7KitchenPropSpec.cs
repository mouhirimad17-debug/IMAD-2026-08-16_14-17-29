using PrankMansion.Entities;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 7's Kitchen prop table (Part 4.3 + Law 0.3),
    /// shared between the Editor-only importer and the Play Mode test - same split
    /// as Stage6FoyerPropSpec vs Stage6FoyerImporter.
    /// </summary>
    public static class Stage7KitchenPropSpec
    {
        public const string ModelsRoot = "Assets/_Project/Models/Kitchen/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string KitchenPropsRootName = "Stage7_KitchenProps";

        public enum PropClass { PureStatic, CarryLight, CarryHeavy, Pushable }

        public struct Entry
        {
            public string unityName;
            public string sourcePath;
            public float expectedMaxDim;
            public PropClass cls;
            public ImpactReactionProp.ReactionType? reaction; // Part 4.3's extra impact notes, null = none
        }

        public static readonly Entry[] Table =
        {
            new Entry{ unityName="Kitchen_FullCombo_01", sourcePath=ModelsRoot+"Static/kitchen_Fullcombo_01.glb", expectedMaxDim=4.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Kitchen_Table_01", sourcePath=ModelsRoot+"Static/Kitchen_table_01.glb", expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Kitchen_Chair_01", sourcePath=ModelsRoot+"Throwable/Kitchen_chair_01.glb", expectedMaxDim=0.90f, cls=PropClass.Pushable },
            new Entry{ unityName="Kitchen_Apple_01", sourcePath=ModelsRoot+"Throwable/Kitchen_apple_01.glb", expectedMaxDim=0.08f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Banana_01", sourcePath=ModelsRoot+"Throwable/Kitchen_banana_01.glb", expectedMaxDim=0.18f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Banana_02", sourcePath=ModelsRoot+"Throwable/Kitchen_bananas_02.glb", expectedMaxDim=0.18f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Orange_01", sourcePath=ModelsRoot+"Throwable/Kitchen_orange_01.glb", expectedMaxDim=0.08f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Tomato_01", sourcePath=ModelsRoot+"Throwable/Kitchen_tomato_01.glb", expectedMaxDim=0.07f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Egg_01", sourcePath=ModelsRoot+"Throwable/Kitchen_egg_01.glb", expectedMaxDim=0.06f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Egg_02", sourcePath=ModelsRoot+"Throwable/Kitchen_eggs_02.glb", expectedMaxDim=0.06f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_EggCarton_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Watermelon_01", sourcePath=ModelsRoot+"Throwable/Kitchen_watermelon_01.glb", expectedMaxDim=0.35f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Kitchen_FlourBag_01", sourcePath=ModelsRoot+"Throwable/Kitchen_flourbag_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight, reaction=ImpactReactionProp.ReactionType.Explode },
            new Entry{ unityName="Kitchen_SugarBag_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Pan_01", sourcePath=ModelsRoot+"Throwable/Kitchen_pan_01.glb", expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Knife_01", sourcePath=ModelsRoot+"Throwable/kitchen_knife_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Knife_02", sourcePath=ModelsRoot+"Throwable/kitchen_knife_02.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_RollingPin_01", sourcePath=ModelsRoot+"Throwable/Kitchen_rollingpin_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Plate_01", sourcePath=ModelsRoot+"Throwable/Kitchen_plate_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight, reaction=ImpactReactionProp.ReactionType.Shatter },
            new Entry{ unityName="Kitchen_Bowl_01", sourcePath=ModelsRoot+"Throwable/kitchen_bowl_01.glb", expectedMaxDim=0.20f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_WineBottle_01", sourcePath=ModelsRoot+"Throwable/Kitchen_winebottle_01.glb", expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Pot_01", sourcePath=ModelsRoot+"Throwable/kitchen_pot_01.glb", expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_MilkCarton_01", sourcePath=ModelsRoot+"Throwable/kitchen_milkcarton_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight, reaction=ImpactReactionProp.ReactionType.Leak },
            new Entry{ unityName="Kitchen_KetchupBottle_01", sourcePath=ModelsRoot+"Throwable/kitchen_ketchupbottle_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_CuttingBoard_01", sourcePath=ModelsRoot+"Throwable/kitchen_cuttingboard_01.glb", expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Blender_01", sourcePath=ModelsRoot+"Throwable/kitchen_blender_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Toaster_01", sourcePath=ModelsRoot+"Throwable/kitchen_toaster_01.glb", expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_Kettle_01", sourcePath=ModelsRoot+"Throwable/kitchen_kettle_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_SpiceRack_01", sourcePath=ModelsRoot+"Static/kitchen_spicerack_01.glb", expectedMaxDim=0.40f, cls=PropClass.PureStatic },
            new Entry{ unityName="Kitchen_SpiceJar_01", sourcePath=null, expectedMaxDim=0.10f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_CookieJar_01", sourcePath=ModelsRoot+"Throwable/kitchen_cookiejar_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_FruitBasket_01", sourcePath=ModelsRoot+"Throwable/kitchen_fruitbasket_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Kitchen_TrashBin_01", sourcePath=ModelsRoot+"Throwable/kitchen_trashbin_01.glb", expectedMaxDim=0.60f, cls=PropClass.Pushable },
            new Entry{ unityName="Kitchen_Window_01", sourcePath=null, expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Kitchen_Door_01", sourcePath=ModelsRoot+"Static/kitchen_door_01.glb", expectedMaxDim=2.10f, cls=PropClass.PureStatic },
        };
    }
}
