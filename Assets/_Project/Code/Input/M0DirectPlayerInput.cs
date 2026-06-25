using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Input {
    public class M0DirectPlayerInput : MonoBehaviour {
        public System.Action<Vector2> DashRequested;

        private M0InputActions _inputActions;
        private M0InputActions.GameplayActions _gameplay;
        private M0InputRouter inputRouter;
#if GR_M0_PROTOTYPE
        private IEnemyDebugHarness enemyDebugHarness;
        private int _lastDebugForceInvokeFrame = -1;
#endif
        private INhemLogger logger;
        private float _lastSTapTime = -1f;
        private float _prevMoveY;
        private const float DoubleTapWindow = 0.3f;

        private void Awake() {
            _inputActions = new M0InputActions();
            _gameplay = _inputActions.Gameplay;
#if GR_INPUT_DEBUG
            logger?.Log($"[M0Input] M0InputActions created");
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
            _gameplay.Move.performed += OnMoveChanged;
            _gameplay.Move.canceled += OnMoveChanged;

            _gameplay.LockOn.started += OnLockOnChanged;
            _gameplay.LockOn.canceled += OnLockOnChanged;

            _gameplay.LightAttack.started += OnLightAttackChanged;
            _gameplay.LightAttack.canceled += OnLightAttackChanged;

            _gameplay.HeavyAttack.started += OnHeavyAttackChanged;
            _gameplay.HeavyAttack.canceled += OnHeavyAttackChanged;

            _gameplay.Parry.started += OnParryChanged;
            _gameplay.Parry.canceled += OnParryChanged;

            _gameplay.Dodge.started += OnDodgeChanged;
            _gameplay.Dodge.canceled += OnDodgeChanged;

            _gameplay.Counter.started += OnCounterChanged;
            _gameplay.Counter.canceled += OnCounterChanged;

            _gameplay.Interact.started += OnInteractChanged;
            _gameplay.Interact.canceled += OnInteractChanged;

            _gameplay.ToggleDebugOverlay.started += OnToggleDebugOverlayChanged;
            _gameplay.ToggleDebugOverlay.canceled += OnToggleDebugOverlayChanged;

            _gameplay.DashLeft.performed += OnDashLeft;
            _gameplay.DashRight.performed += OnDashRight;
#if GR_M0_PROTOTYPE
            _gameplay.DebugForceParryEligibleActive.performed += OnDebugForceParryEligibleActivePerformed;
#endif
            _gameplay.Enable();

#if GR_INPUT_DEBUG
            logger?.Log($"[M0Input] Gameplay action map enabled via M0InputActions");
#endif
        }

        private void OnDisable() {
            _gameplay.Move.performed -= OnMoveChanged;
            _gameplay.Move.canceled -= OnMoveChanged;

            _gameplay.LockOn.started -= OnLockOnChanged;
            _gameplay.LockOn.canceled -= OnLockOnChanged;

            _gameplay.LightAttack.started -= OnLightAttackChanged;
            _gameplay.LightAttack.canceled -= OnLightAttackChanged;

            _gameplay.HeavyAttack.started -= OnHeavyAttackChanged;
            _gameplay.HeavyAttack.canceled -= OnHeavyAttackChanged;

            _gameplay.Parry.started -= OnParryChanged;
            _gameplay.Parry.canceled -= OnParryChanged;

            _gameplay.Dodge.started -= OnDodgeChanged;
            _gameplay.Dodge.canceled -= OnDodgeChanged;

            _gameplay.Counter.started -= OnCounterChanged;
            _gameplay.Counter.canceled -= OnCounterChanged;

            _gameplay.Interact.started -= OnInteractChanged;
            _gameplay.Interact.canceled -= OnInteractChanged;

            _gameplay.ToggleDebugOverlay.started -= OnToggleDebugOverlayChanged;
            _gameplay.ToggleDebugOverlay.canceled -= OnToggleDebugOverlayChanged;

            _gameplay.DashLeft.performed -= OnDashLeft;
            _gameplay.DashRight.performed -= OnDashRight;
#if GR_M0_PROTOTYPE
            _gameplay.DebugForceParryEligibleActive.performed -= OnDebugForceParryEligibleActivePerformed;
#endif
            _gameplay.Disable();
        }

        private void OnDestroy() {
            _gameplay.Disable();
            _inputActions?.Dispose();
            _inputActions = null;
        }

        private void OnMoveChanged(InputAction.CallbackContext context) {
            if (inputRouter == null) return;

            var moveVec = _gameplay.Move.ReadValue<Vector2>();
            inputRouter.SetMove(new Axis2(moveVec.x, moveVec.y));

            float now = Time.time;
            bool sJustPressed = _prevMoveY >= -0.3f && moveVec.y < -0.5f;
            _prevMoveY = moveVec.y;

            if (sJustPressed && Mathf.Abs(moveVec.x) < 0.3f) {
                if (_lastSTapTime > 0f && (now - _lastSTapTime) < DoubleTapWindow) {
                    DashRequested?.Invoke(new Vector2(0f, -1f));
                    _lastSTapTime = -1f;
                } else {
                    _lastSTapTime = now;
                }
            }
        }

        private void OnLockOnChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.LockOn, _gameplay.LockOn, context);

        private void OnLightAttackChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.LightAttack, _gameplay.LightAttack, context);

        private void OnHeavyAttackChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.HeavyAttack, _gameplay.HeavyAttack, context);

        private void OnParryChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.Parry, _gameplay.Parry, context);

        private void OnDodgeChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.Dodge, _gameplay.Dodge, context);

        private void OnCounterChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.Counter, _gameplay.Counter, context);

        private void OnInteractChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.Interact, _gameplay.Interact, context);

        private void OnToggleDebugOverlayChanged(InputAction.CallbackContext context) =>
            HandleButtonState(InputActionIntent.ToggleDebugOverlay, _gameplay.ToggleDebugOverlay, context);

        private void OnDashLeft(InputAction.CallbackContext context) {
            if (context.performed) DashRequested?.Invoke(new Vector2(-1f, 0f));
        }

        private void OnDashRight(InputAction.CallbackContext context) {
            if (context.performed) DashRequested?.Invoke(new Vector2(1f, 0f));
        }

        private void HandleButtonState(InputActionIntent actionIntent, InputAction action, InputAction.CallbackContext context) {
            if (inputRouter == null || action == null) return;

            var isPressed = action.IsPressed();
            inputRouter.SetActionPressed(actionIntent, isPressed);

            if (context.started) inputRouter.RecordTriggeredAction(actionIntent);
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
