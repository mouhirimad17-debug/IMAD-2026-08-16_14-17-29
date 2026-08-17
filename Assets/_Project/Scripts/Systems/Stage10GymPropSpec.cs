namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 10's Gym prop table (Part 4.7 + Law 0.3),
    /// shared between the Editor-only importer and the Play Mode test - same split
    /// as Stages 6-9's own PropSpecs.
    /// </summary>
    public static class Stage10GymPropSpec
    {
        public const string ModelsRoot = "Assets/_Project/Models/Gym/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string GymPropsRootName = "Stage10_GymProps";

        public enum PropClass { PureStatic, CarryLight, CarryHeavy }

        public struct Entry
        {
            public string unityName;
            public string sourcePath;
            public float expectedMaxDim;
            public PropClass cls;
            public bool isPunchingBag;
        }

        public static readonly Entry[] Table =
        {
            new Entry{ unityName="Gym_Treadmill_01", sourcePath=ModelsRoot+"Static/Gym_treadmill_01.glb", expectedMaxDim=1.50f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_Bike_01", sourcePath=ModelsRoot+"Static/gym_bike_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_DumbbellLight_01", sourcePath=ModelsRoot+"Throwable/gym_dumbbelllight_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Gym_DumbbellHeavy_01", sourcePath=ModelsRoot+"Throwable/Gym_dumbbell_01.glb", expectedMaxDim=0.35f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Gym_WeightsRack_01", sourcePath=ModelsRoot+"Static/Gym_weightsracks_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_Barbell_01", sourcePath=ModelsRoot+"Throwable/gym_barbell_01.glb", expectedMaxDim=1.80f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Gym_YogaMat_01", sourcePath=ModelsRoot+"Throwable/Gym_yogamat_01.glb", expectedMaxDim=1.80f, cls=PropClass.CarryLight },
            new Entry{ unityName="Gym_PunchingBag_01", sourcePath=ModelsRoot+"Static/gym_punchingbag_01.glb", expectedMaxDim=1.50f, cls=PropClass.PureStatic, isPunchingBag=true },
            new Entry{ unityName="Gym_Mirror_01", sourcePath=null, expectedMaxDim=2.00f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_WaterBottle_01", sourcePath=ModelsRoot+"Throwable/gym_waterbottle_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Gym_Towel_01", sourcePath=ModelsRoot+"Throwable/gym_towel_01.glb", expectedMaxDim=0.60f, cls=PropClass.CarryLight },
            new Entry{ unityName="Gym_MedicineBall_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Gym_Bench_01", sourcePath=ModelsRoot+"Static/gym_bench_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_Door_01", sourcePath=null, expectedMaxDim=2.10f, cls=PropClass.PureStatic },
            new Entry{ unityName="Gym_Window_01", sourcePath=null, expectedMaxDim=1.00f, cls=PropClass.PureStatic },
        };
    }
}
