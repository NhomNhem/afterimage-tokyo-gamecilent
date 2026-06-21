using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Pure C# combat camera service for M0.
    /// Orchestrates M0CombatCameraModel, holds target positions, exposes snapshot.
    /// No Unity component references.
    /// </summary>
    public sealed class M0CombatCameraService : IM0CombatCameraService {
        private readonly M0CombatCameraModel _model;

        private Vector3 _playerPosition;
        private Vector3? _enemyPosition;
        private bool _lockOn;

        public M0CombatCameraSnapshot Snapshot { get; private set; }
        public bool IsLockOn => _lockOn;

        public M0CombatCameraService(M0CombatCameraSettings settings) {
            _model = new M0CombatCameraModel(settings);
        }

        public void ApplyLook(Vector2 lookInput) {
            _model.ApplyLook(lookInput);
        }

        public void SetLockOn(bool enabled) {
            _lockOn = enabled;
        }

        public void ToggleLockOn() {
            _lockOn = !_lockOn;
        }

        public void SetTargets(Vector3 playerPosition, Vector3? enemyPosition) {
            _playerPosition = playerPosition;
            _enemyPosition = enemyPosition;
        }

        public void Tick(float playerSpeed, float deltaTime) {
            Snapshot = _model.Tick(
                _playerPosition,
                _enemyPosition,
                playerSpeed,
                _lockOn,
                deltaTime);
        }

        public void AddShake(float intensity) {
            _model.AddShake(intensity);
        }
    }
}
