namespace PrankMansion.Blockout
{
    /// <summary>
    /// Runtime-accessible half of Stage 8's Office prop table (Part 4.4 + Law 0.3),
    /// shared between the Editor-only importer and the Play Mode test - same split
    /// as Stage6FoyerPropSpec/Stage7KitchenPropSpec.
    ///
    /// Several Office source files use different words than Part 4.4's Unity names
    /// (e.g. office_desk_01 for Office_DeskExec_01, office_meetingchair_01 for
    /// Office_ChairGuest_01) - matched here on the same judgment basis as Stage 6's
    /// sofa/champagne/MainDoor matches (single plausible candidate, no ambiguity).
    /// Where two candidates existed or none did, the item is left missing rather
    /// than forced - see Stage8_Decisions_Log.txt for the specific reasoning per item.
    /// </summary>
    public static class Stage8OfficePropSpec
    {
        public const string ModelsRoot = "Assets/_Project/Models/Office/";
        public const string PrefabDir = "Assets/_Project/Prefabs/Props/";
        public const string OfficePropsRootName = "Stage8_OfficeProps";

        public enum PropClass { PureStatic, Fallable, CarryLight, CarryHeavy, Pushable }

        public struct Entry
        {
            public string unityName;
            public string sourcePath;
            public float expectedMaxDim;
            public PropClass cls;
        }

        public static readonly Entry[] Table =
        {
            new Entry{ unityName="Office_DeskExec_01", sourcePath=ModelsRoot+"Static/office_desk_01.glb", expectedMaxDim=1.80f, cls=PropClass.PureStatic },
            new Entry{ unityName="Office_ChairWheels_01", sourcePath=ModelsRoot+"Throwable/office_chairwheels_01.glb", expectedMaxDim=1.10f, cls=PropClass.Pushable },
            new Entry{ unityName="Office_ChairGuest_01", sourcePath=ModelsRoot+"Throwable/office_meetingchair_01.glb", expectedMaxDim=0.90f, cls=PropClass.Pushable },
            new Entry{ unityName="Office_BookshelfLarge_01", sourcePath=ModelsRoot+"Static/Office_Bookschelf_01.glb", expectedMaxDim=2.20f, cls=PropClass.Fallable },
            new Entry{ unityName="Office_Book_01", sourcePath=ModelsRoot+"Throwable/Office_book_01.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Book_02", sourcePath=ModelsRoot+"Throwable/Office_book_02.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Book_03", sourcePath=ModelsRoot+"Throwable/Office_book_03.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Book_04", sourcePath=ModelsRoot+"Throwable/Office_book_04.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Book_05", sourcePath=ModelsRoot+"Throwable/Office_book_05.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Book_06", sourcePath=ModelsRoot+"Throwable/office_book_06.glb", expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_BookStack_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Printer_01", sourcePath=ModelsRoot+"Throwable/Office_printer_01.glb", expectedMaxDim=0.45f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Office_LampDesk_01", sourcePath=ModelsRoot+"Throwable/office_lamp_01.glb", expectedMaxDim=0.40f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_LampFloor_01", sourcePath=null, expectedMaxDim=1.60f, cls=PropClass.Fallable },
            new Entry{ unityName="Office_Stapler_01", sourcePath=ModelsRoot+"Throwable/office_stapler_01.glb", expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_HolePuncher_01", sourcePath=null, expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_PaperStack_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_ClockWall_01", sourcePath=ModelsRoot+"Static/Office_clock_01.glb", expectedMaxDim=0.30f, cls=PropClass.PureStatic },
            new Entry{ unityName="Office_Globe_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Trophy_01", sourcePath=null, expectedMaxDim=0.30f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_FilingCabinet_01", sourcePath=null, expectedMaxDim=1.30f, cls=PropClass.Pushable },
            new Entry{ unityName="Office_TrashBin_01", sourcePath=ModelsRoot+"Throwable/office_wastebasketmetal_01.glb", expectedMaxDim=0.50f, cls=PropClass.Pushable },
            new Entry{ unityName="Office_Whiteboard_01", sourcePath=ModelsRoot+"Static/office_bulletinboard_01.glb", expectedMaxDim=1.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Office_Marker_01", sourcePath=null, expectedMaxDim=0.15f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_CoffeeMug_01", sourcePath=null, expectedMaxDim=0.12f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_CoffeeMachine_01", sourcePath=null, expectedMaxDim=0.40f, cls=PropClass.CarryHeavy },
            new Entry{ unityName="Office_Telephone_01", sourcePath=null, expectedMaxDim=0.25f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_DeskOrganizer_01", sourcePath=ModelsRoot+"Throwable/office_penholder_01.glb", expectedMaxDim=0.20f, cls=PropClass.CarryLight },
            new Entry{ unityName="Office_Painting_01", sourcePath=ModelsRoot+"Static/office_worldmap_01.glb", expectedMaxDim=1.00f, cls=PropClass.Fallable },
            new Entry{ unityName="Office_Curtains_01", sourcePath=null, expectedMaxDim=2.20f, cls=PropClass.PureStatic },
            new Entry{ unityName="Office_Door_01", sourcePath=null, expectedMaxDim=2.10f, cls=PropClass.PureStatic },
        };
    }
}
