using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;

namespace GlassRefrain.Input {
    // TODO: Rename to M0InputIntentBridge once scene references are updated.
    // This is a temporary bridge that reads input from the Unity Input System
    // action map and feeds it to M0PlayerLocomotion via ConsumeInputIntent.
    // No gamepad/keyboard/legacy Input Manager API is used — only InputActionAsset.
    public class M0DirectPlayerInput : MonoBehaviour {
        [SerializeField] private InputActionAsset inputAsset;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private M0PlayerLocomotion locomotion;

        public void SetLocomotion(M0PlayerLocomotion loco) {
            locomotion = loco;
        }

        private void OnEnable() {
            if (inputAsset == null) return;
            gameplayMap = inputAsset.FindActionMap("Gameplay");
            if (gameplayMap == null) return;
            moveAction = gameplayMap.FindAction("Move");
            gameplayMap.Enable();
        }

        private void OnDisable() {
            gameplayMap?.Disable();
        }

        private void OnDestroy() {
            gameplayMap?.Disable();
            gameplayMap = null;
            moveAction = null;
        }

        private void Update() {
            if (locomotion == null || moveAction == null) return;

            Vector2 moveVec = moveAction.ReadValue<Vector2>();
            Axis2 moveAxis = new Axis2(moveVec.x, moveVec.y);

            var intent = new InputIntentSnapshot(
                moveAxis,
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            locomotion.ConsumeInputIntent(intent);
        }
    }
}
