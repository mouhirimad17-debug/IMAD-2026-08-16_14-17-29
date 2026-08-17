using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Wires the real interact button to Part 8's door/drawer-slide toggles
    /// (Entities.DoorProp, Entities.DrawerSlideProp). Kept entirely separate from
    /// PlayerCarry/PlayerDragInteraction so no existing class needs any change - it
    /// only subscribes to PlayerInputReader's already-public OnInteractPressed
    /// event, the same extension point those components use. A single press
    /// toggles the nearest in-range door first, then the nearest in-range drawer.
    /// </summary>
    public class PlayerDoorInteraction : MonoBehaviour
    {
        private PlayerInputReader inputReader;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
            if (inputReader != null) inputReader.OnInteractPressed += HandlePressed;
        }

        private void OnDestroy()
        {
            if (inputReader != null) inputReader.OnInteractPressed -= HandlePressed;
        }

        private void HandlePressed()
        {
            DoorProp nearestDoor = null;
            float bestDoorDist = float.MaxValue;
            foreach (var door in FindObjectsByType<DoorProp>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(transform.position, door.transform.position);
                if (d <= door.interactRange && d < bestDoorDist) { bestDoorDist = d; nearestDoor = door; }
            }
            if (nearestDoor != null && nearestDoor.TryToggle(transform)) return;

            DrawerSlideProp nearestDrawer = null;
            float bestDrawerDist = float.MaxValue;
            foreach (var drawer in FindObjectsByType<DrawerSlideProp>(FindObjectsSortMode.None))
            {
                float d = Vector3.Distance(transform.position, drawer.transform.position);
                if (d <= DrawerSlideProp.InteractRange && d < bestDrawerDist) { bestDrawerDist = d; nearestDrawer = drawer; }
            }
            nearestDrawer?.TryToggle(transform);
        }
    }
}
