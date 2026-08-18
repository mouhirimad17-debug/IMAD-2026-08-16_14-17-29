using UnityEditor;
using UnityEngine;

namespace PrankMansion.Blockout
{
    /// <summary>
    /// One-shot batch entry point for the FurnitureGridPlacement fix-up: re-runs
    /// only each Stage 6-10 room importer's placement-only entry point
    /// (RegeneratePlacementOnly - loads the already-built prefabs straight off
    /// disk and re-runs just PlaceInto*) rather than the full ImportAndCorrect,
    /// which would needlessly re-run model correction/prefab-saving on every
    /// prop. Then re-captures the verification screenshots in the same run.
    /// </summary>
    public static class RegenerateFurniturePlacementTool
    {
        [MenuItem("PrankMansion/Tools - Regenerate Furniture Placement (All Rooms)")]
        public static void RegenerateAll()
        {
            Stage6FoyerImporter.RegeneratePlacementOnly();
            Stage7KitchenImporter.RegeneratePlacementOnly();
            Stage8OfficeImporter.RegeneratePlacementOnly();
            Stage9BedroomsImporter.RegeneratePlacementOnly();
            Stage10GymImporter.RegeneratePlacementOnly();

            Debug.Log("[RegenerateFurniturePlacementTool] All 5 rooms re-placed with the full-area grid.");

            SceneScreenshotTool.CaptureAllRooms();
        }
    }
}
