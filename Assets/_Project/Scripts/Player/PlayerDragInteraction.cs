using PrankMansion.Entities;
using UnityEngine;

namespace PrankMansion.Player
{
    /// <summary>
    /// Wires the real interact button to this stage's two new "grab and drag"
    /// mechanics - Entities.GrabbableDragProp (Bedroom1_Blanket_01, Part 4.5) and
    /// Entities.DrawerProp (Bedroom1_Dresser_01's drawers, same part). Kept
    /// entirely separate from PlayerCarry so neither that class nor
    /// PlayerInputReader needs any change - it only subscribes to
    /// PlayerInputReader's already-public OnInteractPressed/OnInteractReleased
    /// events, the same extension point PlayerCarry itself uses. Hold to drag,
    /// release to let go.
    /// </summary>
    public class PlayerDragInteraction : MonoBehaviour
    {
        private PlayerInputReader inputReader;
        private GrabbableDragProp grabbedBlanket;
        private DrawerProp grabbedDrawer;

        private void Awake()
        {
            inputReader = GetComponent<PlayerInputReader>();
            if (inputReader != null)
            {
                inputReader.OnInteractPressed += HandlePressed;
                inputReader.OnInteractReleased += HandleReleased;
            }
        }

        private void OnDestroy()
        {
            if (inputReader != null)
            {
                inputReader.OnInteractPressed -= HandlePressed;
                inputReader.OnInteractReleased -= HandleReleased;
            }
        }

        private void HandlePressed()
        {
            if (grabbedBlanket != null || grabbedDrawer != null) return;

            foreach (var prop in FindObjectsByType<GrabbableDragProp>(FindObjectsSortMode.None))
            {
                if (prop.TryGrab(transform))
                {
                    grabbedBlanket = prop;
                    return;
                }
            }

            foreach (var container in FindObjectsByType<InteractiveContainerProp>(FindObjectsSortMode.None))
            {
                foreach (var drawer in container.Drawers)
                {
                    if (drawer.TryGrab(transform))
                    {
                        grabbedDrawer = drawer;
                        return;
                    }
                }
            }
        }

        private void HandleReleased()
        {
            if (grabbedBlanket != null)
            {
                grabbedBlanket.ReleaseGrab();
                grabbedBlanket = null;
            }
            if (grabbedDrawer != null)
            {
                grabbedDrawer.ReleaseGrab();
                grabbedDrawer = null;
            }
        }
    }
}
