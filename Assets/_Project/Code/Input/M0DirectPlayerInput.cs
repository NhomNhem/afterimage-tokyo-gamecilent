using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;

namespace GlassRefrain.Input {
    // TODO: Rename to M0InputIntentBridge once scene references are updated.
    // This is a temporary bridge that reads input from the Unity Input System
    // action map and feeds it to gameplay systems via ConsumeInputIntent.
    // No gamepad/keyboard/legacy Input Manager API is used — only InputActionAsset.
    // Story 1-3: Routes LockOn intent to M0TargetContext (toggle acquire/release).
    public class M0DirectPlayerInput : MonoBehaviour {
        [SerializeField] private InputActionAsset inputAsset;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction lockOnAction;
        private M0PlayerLocomotion locomotion;
        private M0TargetContext targetContext;

        public void SetLocomotion(M0PlayerLocomotion loco) {
            locomotion = loco;
        }

        public void SetTargetContext(M0TargetContext context) {
            targetContext = context;
        }

        private void OnEnable() {
            if (inputAsset == null) return;
            gameplayMap = inputAsset.FindActionMap("Gameplay");
            if (gameplayMap == null) return;
            moveAction = gameplayMap.FindAction("Move");
            lockOnAction = gameplayMap.FindAction("LockOn");
            gameplayMap.Enable();
        }

        private void OnDisable() {
            gameplayMap?.Disable();
        }

        private void OnDestroy() {
            gameplayMap?.Disable();
            gameplayMap = null;
            moveAction = null;
            lockOnAction = null;
        }

        private void Update() {
            // Handle movement input for locomotion
            if (locomotion != null && moveAction != null) {
                Vector2 moveVec = moveAction.ReadValue<Vector2>();
                Axis2 moveAxis = new Axis2(moveVec.x, moveVec.y);

                var intent = new InputIntentSnapshot(
                    moveAxis,
                    new Axis2(0f, 0f),
                    false, false, false, false, false, false, false, false,
                    true);

                locomotion.ConsumeInputIntent(intent);
            }

            // Handle LockOn input for targeting (Story 1-3)
            if (targetContext != null && lockOnAction != null) {
                bool lockOnPressed = lockOnAction.WasPressedThisFrame();
                if (lockOnPressed) {
                    var intent = new InputIntentSnapshot(
                        new Axis2(0f, 0f),
                        new Axis2(0f, 0f),
                        false, false, false, false, false, true, false, false,
                        true);
                    targetContext.ConsumeInputIntent(intent);
                }
            }
        }
    }
}
