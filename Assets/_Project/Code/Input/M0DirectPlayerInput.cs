using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Input {
    // TODO: Rename to M0InputIntentBridge once scene references are updated.
    // This is a temporary bridge that reads input from the Unity Input System
    // action map and feeds it to gameplay systems via ConsumeInputIntent.
    // No gamepad/keyboard/legacy Input Manager API is used — only InputActionAsset.
    // Story 1-3: Routes LockOn intent to M0TargetContext (toggle acquire/release).
    // Story 1-4: Routes LightAttack/HeavyAttack intents to M0CombatCore (raw intent only).
    // Story 1-6: Defensive intent reads — routing to CombatCore handled by M0GameplayTickHandler.
    public class M0DirectPlayerInput : MonoBehaviour {
        [SerializeField] private InputActionAsset inputAsset;
        private InputActionMap gameplayMap;
        private InputAction moveAction;
        private InputAction lockOnAction;
        private InputAction lightAttackAction;
        private InputAction heavyAttackAction;
        // Story 1-6: Defensive intent reads — no device polling, no validity decisions.
        private InputAction parryAction;
        private InputAction dodgeAction;
        private InputAction counterAction;
        private M0PlayerLocomotion locomotion;
        private M0TargetContext targetContext;
        private M0CombatCore combatCore;
        private INhemLogger logger;

        public void SetLocomotion(M0PlayerLocomotion loco) {
            locomotion = loco;
        }

        public void SetTargetContext(M0TargetContext context) {
            targetContext = context;
        }

        public void SetCombatCore(M0CombatCore combat) {
            combatCore = combat;
        }

        public void SetLogger(INhemLogger logger) {
            this.logger = logger;
        }

        private void OnEnable() {
            if (inputAsset == null) return;
            gameplayMap = inputAsset.FindActionMap("Gameplay");
            if (gameplayMap == null) return;
            moveAction = gameplayMap.FindAction("Move");
            lockOnAction = gameplayMap.FindAction("LockOn");
            lightAttackAction = gameplayMap.FindAction("LightAttack");
            heavyAttackAction = gameplayMap.FindAction("HeavyAttack");
            parryAction = gameplayMap.FindAction("Parry");
            dodgeAction = gameplayMap.FindAction("Dodge");
            counterAction = gameplayMap.FindAction("Counter");
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
            lightAttackAction = null;
            heavyAttackAction = null;
            parryAction = null;
            dodgeAction = null;
            counterAction = null;
        }

        public bool ParryPressedThisFrame => parryAction != null && parryAction.WasPressedThisFrame();
        public bool DodgePressedThisFrame => dodgeAction != null && dodgeAction.WasPressedThisFrame();
        public bool CounterPressedThisFrame => counterAction != null && counterAction.WasPressedThisFrame();

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
#if GR_INPUT_DEBUG
                    logger?.Log("[M0Input] LockOn pressed");
#endif
                    var intent = new InputIntentSnapshot(
                        new Axis2(0f, 0f),
                        new Axis2(0f, 0f),
                        false, false, false, false, false, true, false, false,
                        true);
                    targetContext.ConsumeInputIntent(intent);
                }
            }

            // Handle LightAttack input for combat (Story 1-4) — raw intent only
            if (combatCore != null && lightAttackAction != null) {
                if (lightAttackAction.WasPressedThisFrame()) {
#if GR_INPUT_DEBUG
                    logger?.Log("[M0Input] LightAttack pressed");
#endif
                    combatCore.ConsumeAttackIntent(CombatActionType.LightAttack);
                }
            }

            // Handle HeavyAttack input for combat (Story 1-4) — raw intent only
            if (combatCore != null && heavyAttackAction != null) {
                if (heavyAttackAction.WasPressedThisFrame()) {
#if GR_INPUT_DEBUG
                    logger?.Log("[M0Input] HeavyAttack pressed");
#endif
                    combatCore.ConsumeAttackIntent(CombatActionType.HeavyAttack);
                }
            }
        }
    }
}
