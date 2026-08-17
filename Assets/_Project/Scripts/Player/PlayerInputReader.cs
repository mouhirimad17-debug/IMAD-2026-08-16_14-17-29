using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PrankMansion.Player
{
    /// <summary>
    /// Thin glue layer: reads the real Input System actions (Part 14.4's control
    /// map - WASD move, Shift sprint, Space jump, mouse look, E interact, left-click
    /// throw) and feeds PlayerLocomotion/PlayerCameraRig. Kept deliberately thin and
    /// separate from those two components so the Stage 2 automated test can drive
    /// the actual movement/camera math directly with synthetic input, independent
    /// of real device simulation.
    ///
    /// Interact/Throw are exposed as events for later systems (carry in Stage 3,
    /// doors in Stage 11, etc.) to subscribe to - this stage only wires the buttons,
    /// per the roadmap ("نظام الحمل" and friends are separate stages).
    /// </summary>
    [RequireComponent(typeof(PlayerLocomotion))]
    public class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actionsAsset;
        [SerializeField] private PlayerCameraRig cameraRig;
        [SerializeField] private bool lockCursor = true;

        public event Action OnInteractPressed;
        public event Action OnInteractReleased;
        public event Action OnThrowPressed;

        // Part 7.4's pour mechanic needs to poll "is interact still held" every frame
        // (a 0.8s long-press) rather than react to a single press/release edge.
        public bool IsInteractHeld => interactAction != null && interactAction.IsPressed();

        private PlayerLocomotion locomotion;
        private InputAction moveAction, lookAction, sprintAction, jumpAction, interactAction, throwAction;

        private void Awake()
        {
            locomotion = GetComponent<PlayerLocomotion>();

            var map = actionsAsset.FindActionMap("Player");
            moveAction = map.FindAction("Move");
            lookAction = map.FindAction("Look");
            sprintAction = map.FindAction("Sprint");
            jumpAction = map.FindAction("Jump");
            interactAction = map.FindAction("Interact");
            throwAction = map.FindAction("Throw");
            map.Enable();

            jumpAction.performed += _ => locomotion.RequestJump();
            interactAction.performed += _ => OnInteractPressed?.Invoke();
            interactAction.canceled += _ => OnInteractReleased?.Invoke();
            throwAction.performed += _ => OnThrowPressed?.Invoke();
        }

        private void OnEnable()
        {
            if (!lockCursor) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (!lockCursor) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void OnDestroy()
        {
            actionsAsset.FindActionMap("Player")?.Disable();
        }

        private void Update()
        {
            locomotion.SetMoveInput(moveAction.ReadValue<Vector2>());
            locomotion.SetSprint(sprintAction.IsPressed());

            if (cameraRig != null)
            {
                cameraRig.SetLookDelta(lookAction.ReadValue<Vector2>());
                locomotion.SetCameraYaw(cameraRig.Yaw);
            }
        }
    }
}
