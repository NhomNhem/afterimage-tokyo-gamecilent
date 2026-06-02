using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Input {
    // Unity adapter only:
    // - Binds InputAction callbacks.
    // - Captures raw intent and forwards it to M0InputRouter.
    // - Does NOT route to gameplay truth owners directly.
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
        private InputAction interactAction;
        private InputAction toggleDebugOverlayAction;
#if GR_M0_PROTOTYPE
        private InputAction debugForceParryEligibleActiveAction;
#endif
        private M0InputRouter inputRouter;
#if GR_M0_PROTOTYPE
        private IEnemyDebugHarness enemyDebugHarness;
        private int _lastDebugForceInvokeFrame = -1;
#endif
        private INhemLogger logger;

        private void Awake() {
#if GR_INPUT_DEBUG
            logger?.Log($"[M0Input] InputActionAsset loaded: {(inputAsset != null ? "success" : "failed")}");
#endif
        }

        public void SetInputRouter(M0InputRouter router) {
            inputRouter = router;
        }

#if GR_M0_PROTOTYPE
        public void SetEnemyDebugHarness(IEnemyDebugHarness harness) {
            enemyDebugHarness = harness;
        }
#endif

        public void SetLogger(INhemLogger logger) {
            this.logger = logger;
        }

        private void OnEnable() {
            if (inputAsset == null) {
#if GR_INPUT_DEBUG
                logger?.LogWarning("[M0Input] InputActionAsset missing");
#endif
                return;
            }
#if GR_INPUT_DEBUG
            logger?.Log($"[M0Input] InputActionAsset assigned: {inputAsset.name}");
#endif
            gameplayMap = inputAsset.FindActionMap("Gameplay");
            if (gameplayMap == null) {
#if GR_INPUT_DEBUG
                logger?.LogWarning("[M0Input] Gameplay action map missing");
#endif
                return;
            }
            moveAction = gameplayMap.FindAction("Move");
            lockOnAction = gameplayMap.FindAction("LockOn");
            lightAttackAction = gameplayMap.FindAction("LightAttack");
            heavyAttackAction = gameplayMap.FindAction("HeavyAttack");
            parryAction = gameplayMap.FindAction("Parry");
            dodgeAction = gameplayMap.FindAction("Dodge");
            counterAction = gameplayMap.FindAction("Counter");
            interactAction = gameplayMap.FindAction("Interact");
            toggleDebugOverlayAction = gameplayMap.FindAction("ToggleDebugOverlay");
#if GR_M0_PROTOTYPE
            debugForceParryEligibleActiveAction = gameplayMap.FindAction("DebugForceParryEligibleActive");
#endif
            gameplayMap.Enable();
            HookCallbacks();

#if GR_INPUT_DEBUG
            logger?.Log($"[M0Input] Gameplay action map enabled");
            if (moveAction == null) logger?.LogWarning("[M0Input] Move action missing");
            if (lockOnAction == null) logger?.LogWarning("[M0Input] LockOn action missing");
            if (lightAttackAction == null) logger?.LogWarning("[M0Input] LightAttack action missing");
            if (heavyAttackAction == null) logger?.LogWarning("[M0Input] HeavyAttack action missing");
            if (parryAction == null) logger?.LogWarning("[M0Input] Parry action missing");
            if (dodgeAction == null) logger?.LogWarning("[M0Input] Dodge action missing");
            if (counterAction == null) logger?.LogWarning("[M0Input] Counter action missing");
            if (interactAction == null) logger?.LogWarning("[M0Input] Interact action missing");
            logger?.Log($"[M0Input] Required Gameplay actions found: " +
                        $"{(moveAction != null ? "Move, " : string.Empty)}" +
                        $"{(lightAttackAction != null ? "LightAttack, " : string.Empty)}" +
                        $"{(heavyAttackAction != null ? "HeavyAttack, " : string.Empty)}" +
                        $"{(parryAction != null ? "Parry, " : string.Empty)}" +
                        $"{(dodgeAction != null ? "Dodge, " : string.Empty)}" +
                        $"{(counterAction != null ? "Counter, " : string.Empty)}" +
                        $"{(interactAction != null ? "Interact, " : string.Empty)}" +
                        $"{(lockOnAction != null ? "LockOn" : string.Empty)}");
#endif
        }

        private void OnDisable() {
            UnhookCallbacks();
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
            interactAction = null;
            toggleDebugOverlayAction = null;
#if GR_M0_PROTOTYPE
            debugForceParryEligibleActiveAction = null;
#endif
        }

        private void HookCallbacks() {
            if (moveAction != null) {
                moveAction.performed += OnMoveChanged;
                moveAction.canceled += OnMoveChanged;
            }

            if (lockOnAction != null) {
                lockOnAction.started += OnLockOnChanged;
                lockOnAction.canceled += OnLockOnChanged;
            }

            if (lightAttackAction != null) {
                lightAttackAction.started += OnLightAttackChanged;
                lightAttackAction.canceled += OnLightAttackChanged;
            }

            if (heavyAttackAction != null) {
                heavyAttackAction.started += OnHeavyAttackChanged;
                heavyAttackAction.canceled += OnHeavyAttackChanged;
            }

            if (parryAction != null) {
                parryAction.started += OnParryChanged;
                parryAction.canceled += OnParryChanged;
            }

            if (dodgeAction != null) {
                dodgeAction.started += OnDodgeChanged;
                dodgeAction.canceled += OnDodgeChanged;
            }

            if (counterAction != null) {
                counterAction.started += OnCounterChanged;
                counterAction.canceled += OnCounterChanged;
            }

            if (interactAction != null) {
                interactAction.started += OnInteractChanged;
                interactAction.canceled += OnInteractChanged;
            }

            if (toggleDebugOverlayAction != null) {
                toggleDebugOverlayAction.started += OnToggleDebugOverlayChanged;
                toggleDebugOverlayAction.canceled += OnToggleDebugOverlayChanged;
            }
#if GR_M0_PROTOTYPE
            if (debugForceParryEligibleActiveAction != null) {
                debugForceParryEligibleActiveAction.performed += OnDebugForceParryEligibleActivePerformed;
            }
#endif
        }

        private void UnhookCallbacks() {
            if (moveAction != null) {
                moveAction.performed -= OnMoveChanged;
                moveAction.canceled -= OnMoveChanged;
            }

            if (lockOnAction != null) {
                lockOnAction.started -= OnLockOnChanged;
                lockOnAction.canceled -= OnLockOnChanged;
            }

            if (lightAttackAction != null) {
                lightAttackAction.started -= OnLightAttackChanged;
                lightAttackAction.canceled -= OnLightAttackChanged;
            }

            if (heavyAttackAction != null) {
                heavyAttackAction.started -= OnHeavyAttackChanged;
                heavyAttackAction.canceled -= OnHeavyAttackChanged;
            }

            if (parryAction != null) {
                parryAction.started -= OnParryChanged;
                parryAction.canceled -= OnParryChanged;
            }

            if (dodgeAction != null) {
                dodgeAction.started -= OnDodgeChanged;
                dodgeAction.canceled -= OnDodgeChanged;
            }

            if (counterAction != null) {
                counterAction.started -= OnCounterChanged;
                counterAction.canceled -= OnCounterChanged;
            }

            if (interactAction != null) {
                interactAction.started -= OnInteractChanged;
                interactAction.canceled -= OnInteractChanged;
            }

            if (toggleDebugOverlayAction != null) {
                toggleDebugOverlayAction.started -= OnToggleDebugOverlayChanged;
                toggleDebugOverlayAction.canceled -= OnToggleDebugOverlayChanged;
            }
#if GR_M0_PROTOTYPE
            if (debugForceParryEligibleActiveAction != null) {
                debugForceParryEligibleActiveAction.performed -= OnDebugForceParryEligibleActivePerformed;
            }
#endif
        }

        private void OnMoveChanged(InputAction.CallbackContext context) {
            if (inputRouter == null || moveAction == null) return;

            var moveVec = moveAction.ReadValue<Vector2>();
            inputRouter.SetMove(new Axis2(moveVec.x, moveVec.y));
        }

        private void OnLockOnChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.LockOn, lockOnAction, context);
        }

        private void OnLightAttackChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.LightAttack, lightAttackAction, context);
        }

        private void OnHeavyAttackChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.HeavyAttack, heavyAttackAction, context);
        }

        private void OnParryChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.Parry, parryAction, context);
        }

        private void OnDodgeChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.Dodge, dodgeAction, context);
        }

        private void OnCounterChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.Counter, counterAction, context);
        }

        private void OnInteractChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.Interact, interactAction, context);
        }

        private void OnToggleDebugOverlayChanged(InputAction.CallbackContext context) {
            HandleButtonState(InputActionIntent.ToggleDebugOverlay, toggleDebugOverlayAction, context);
        }

        private void HandleButtonState(InputActionIntent actionIntent, InputAction action, InputAction.CallbackContext context) {
            if (inputRouter == null || action == null) return;

            var isPressed = action.IsPressed();
            inputRouter.SetActionPressed(actionIntent, isPressed);

            if (context.started) {
                inputRouter.RecordTriggeredAction(actionIntent);
            }
        }

#if GR_M0_PROTOTYPE
        private void OnDebugForceParryEligibleActivePerformed(InputAction.CallbackContext context) {
            if (!context.performed) return;
            TryInvokeDebugForceParryEligibleActiveHarness();
        }

        private void TryInvokeDebugForceParryEligibleActiveHarness() {
            int frame = Time.frameCount;
            if (_lastDebugForceInvokeFrame == frame)
                return;
            _lastDebugForceInvokeFrame = frame;

            if (enemyDebugHarness != null) {
                logger?.Log("[M0Debug] DebugForceParryEligibleActive input pressed");
                enemyDebugHarness.DebugForceParryEligibleActive();
            } else {
                logger?.LogWarning("[M0Debug] DebugForceParryEligibleActive rejected: enemy debug harness missing");
            }
        }
#endif
    }
}
