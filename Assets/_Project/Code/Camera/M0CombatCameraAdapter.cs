using UnityEngine;
using UnityEngine.InputSystem;
using GlassRefrain.Input;
using VContainer;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Thin MonoBehaviour adapter for M0 combat camera.
    /// Receives IM0CombatCameraService and IM0CameraTargetProvider via DI.
    /// Target positions are read from the cross-scene provider — no GameObject.Find.
    /// Presentation-only: owns no camera truth.
    /// </summary>
    public sealed class M0CombatCameraAdapter : MonoBehaviour, M0InputActions.IGameplayActions {
        [SerializeField] private bool lockCursorOnPlay = true;

        private IM0CombatCameraService _cameraService = null!;
        private IM0CameraTargetProvider _targetProvider = null!;
        private UnityEngine.Camera _unityCamera = null!;
        private M0InputActions _inputActions = null!;
        private Vector2 _lookInput;

        private Vector3 _lastPlayerPosition;
        private bool _hasLastPosition;

        [Inject]
        public void Construct(IM0CombatCameraService cameraService, IM0CameraTargetProvider targetProvider) {
            _cameraService = cameraService;
            _targetProvider = targetProvider;
        }

        private void Awake() {
            _unityCamera = GetComponent<UnityEngine.Camera>();
        }

        private void OnEnable() {
            _inputActions = new M0InputActions();
            _inputActions.Gameplay.SetCallbacks(this);
            _inputActions.Gameplay.Enable();

            if (lockCursorOnPlay) {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable() {
            _inputActions?.Gameplay.SetCallbacks(null);
            _inputActions?.Gameplay.Disable();
            _inputActions?.Dispose();
            _inputActions = null!;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void OnLook(InputAction.CallbackContext ctx) {
            if (ctx.performed) {
                _lookInput = ctx.ReadValue<Vector2>();
            } else if (ctx.canceled) {
                _lookInput = Vector2.zero;
            }
        }

        public void OnMove(InputAction.CallbackContext ctx) { }
        public void OnLockOn(InputAction.CallbackContext ctx) { }
        public void OnLightAttack(InputAction.CallbackContext ctx) { }
        public void OnHeavyAttack(InputAction.CallbackContext ctx) { }
        public void OnParry(InputAction.CallbackContext ctx) { }
        public void OnDodge(InputAction.CallbackContext ctx) { }
        public void OnCounter(InputAction.CallbackContext ctx) { }
        public void OnInteract(InputAction.CallbackContext ctx) { }
        public void OnToggleDebugOverlay(InputAction.CallbackContext ctx) { }
        public void OnResetEncounter(InputAction.CallbackContext ctx) { }
#if GR_M0_PROTOTYPE
        public void OnDebugForceParryEligibleActive(InputAction.CallbackContext ctx) { }
#endif

        private void LateUpdate() {
            if (_cameraService == null || _targetProvider == null) return;

            var playerPosition = _targetProvider.PlayerPosition;
            if (!_hasLastPosition) {
                _lastPlayerPosition = playerPosition;
                _hasLastPosition = true;
            }

            HandleKeyboard();

            _cameraService.ApplyLook(_lookInput);
            _cameraService.SetTargets(playerPosition, _targetProvider.EnemyPosition);

            var delta = playerPosition - _lastPlayerPosition;
            var speed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastPlayerPosition = playerPosition;

            _cameraService.Tick(speed, Time.deltaTime);

            var snapshot = _cameraService.Snapshot;
            transform.position = snapshot.Position;
            transform.rotation = snapshot.Rotation;
            if (_unityCamera != null) {
                _unityCamera.fieldOfView = snapshot.FOV;
            }
        }

        private void HandleKeyboard() {
            if (Keyboard.current == null) return;

            if (Keyboard.current.tabKey.wasPressedThisFrame) {
                _cameraService.ToggleLockOn();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame) {
                if (Cursor.lockState == CursorLockMode.Locked) {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                } else if (lockCursorOnPlay) {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }
}
