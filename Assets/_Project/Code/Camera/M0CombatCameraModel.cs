using UnityEngine;

namespace GlassRefrain.Camera {
    /// <summary>
    /// Pure C# combat camera model for M0.
    /// Owns orbit math, collision resolution, spring-arm smoothing, shake noise, and FOV expansion.
    /// No Unity component references — receives raw transforms, returns snapshot.
    /// </summary>
    public sealed class M0CombatCameraModel {
        private readonly M0CombatCameraSettings _settings;

        private float _yaw;
        private float _pitch;

        private Vector3 _positionVelocity;
        private float _rotationVelocityY;
        private float _rotationVelocityX;
        private float _distanceVelocity;
        private float _fovVelocity;
        private float _currentDistance;
        private float _currentFOV;

        private float _shakeIntensity;
        private float _shakeTime;

        private Vector3 _currentPosition;
        private Quaternion _currentRotation;
        private bool _initialized;

        public M0CombatCameraModel(M0CombatCameraSettings settings) {
            _settings = settings;
            _yaw = 0f;
            _pitch = 20f;
            _currentDistance = settings.Distance;
            _currentFOV = settings.BaseFOV;
        }

        /// <summary>
        /// Apply look input to yaw/pitch. Called before Tick.
        /// </summary>
        public void ApplyLook(Vector2 lookInput) {
            _yaw += lookInput.x * _settings.YawSensitivity;
            _pitch -= lookInput.y * _settings.PitchSensitivity;
            _pitch = Mathf.Clamp(_pitch, _settings.MinPitch, _settings.MaxPitch);
        }

        /// <summary>
        /// Tick the camera model. Returns the resolved snapshot.
        /// </summary>
        public M0CombatCameraSnapshot Tick(
            Vector3 playerPosition,
            Vector3? enemyPosition,
            float playerSpeed,
            bool lockOn,
            float deltaTime) {

            var pivot = playerPosition + Vector3.up * _settings.PivotHeight;

            Vector3 desiredPos;
            Quaternion desiredRot;

            if (lockOn && enemyPosition.HasValue) {
                ResolveLockOn(pivot, enemyPosition.Value, out desiredPos, out desiredRot);
            } else {
                ResolveFreeOrbit(pivot, out desiredPos, out desiredRot);
            }

            // Collision
            var collidedPos = ResolveCollision(pivot, desiredPos);

            // Spring arm smoothing
            if (!_initialized) {
                _currentPosition = collidedPos;
                _currentRotation = desiredRot;
                _initialized = true;
            } else {
                _currentPosition = Vector3.SmoothDamp(
                    _currentPosition, collidedPos, ref _positionVelocity,
                    _settings.PositionSmoothTime, Mathf.Infinity, deltaTime);

                var currentEuler = _currentRotation.eulerAngles;
                var desiredEuler = desiredRot.eulerAngles;

                var smoothedYaw = Mathf.SmoothDampAngle(
                    currentEuler.y, desiredEuler.y, ref _rotationVelocityY,
                    _settings.RotationSmoothTime, Mathf.Infinity, deltaTime);
                var smoothedPitch = Mathf.SmoothDampAngle(
                    currentEuler.x, desiredEuler.x, ref _rotationVelocityX,
                    _settings.RotationSmoothTime, Mathf.Infinity, deltaTime);

                _currentRotation = Quaternion.Euler(smoothedPitch, smoothedYaw, 0f);
            }

            // Shake
            var finalPos = ApplyShake(_currentPosition, deltaTime);

            // FOV
            var speedFactor = Mathf.Clamp01(playerSpeed / _settings.SpeedToFOVRatio);
            var targetFOV = _settings.BaseFOV + _settings.MaxFOVExpansion * speedFactor;
            _currentFOV = Mathf.SmoothDamp(
                _currentFOV, targetFOV, ref _fovVelocity,
                _settings.FOVSmoothTime, Mathf.Infinity, deltaTime);

            return new M0CombatCameraSnapshot(finalPos, _currentRotation, _currentFOV, lockOn);
        }

        public void AddShake(float intensity) {
            _shakeIntensity = Mathf.Max(_shakeIntensity, intensity);
        }

        private void ResolveFreeOrbit(Vector3 pivot, out Vector3 pos, out Quaternion rot) {
            var yawRad = _yaw * Mathf.Deg2Rad;
            var pitchRad = _pitch * Mathf.Deg2Rad;

            float horizontalDist = _settings.Distance * Mathf.Cos(pitchRad);
            float verticalDist = _settings.Distance * Mathf.Sin(pitchRad);

            var offset = new Vector3(
                -horizontalDist * Mathf.Sin(yawRad),
                verticalDist + _settings.Height,
                -horizontalDist * Mathf.Cos(yawRad)
            );

            pos = pivot + offset;
            rot = Quaternion.LookRotation(pivot - pos, Vector3.up);
        }

        private void ResolveLockOn(Vector3 pivot, Vector3 enemyPos, out Vector3 pos, out Quaternion rot) {
            var midpoint = (pivot + enemyPos) * 0.5f;
            var dir = enemyPos - pivot;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Vector3.forward;
            dir.Normalize();

            pos = midpoint - dir * _settings.LockOnDistance + Vector3.up * _settings.LockOnHeight;
            rot = Quaternion.LookRotation(midpoint - pos, Vector3.up);
        }

        private Vector3 ResolveCollision(Vector3 pivot, Vector3 desiredPos) {
            var direction = desiredPos - pivot;
            var targetDist = direction.magnitude;
            if (targetDist <= 0.01f) return desiredPos;

            direction.Normalize();

            if (Physics.SphereCast(
                pivot,
                _settings.CollisionRadius,
                direction,
                out var hit,
                targetDist + _settings.CollisionSafetyMargin,
                _settings.CollisionMask)) {
                var safeDist = Mathf.Max(_settings.MinDistance, hit.distance - _settings.CollisionSafetyMargin);
                _currentDistance = Mathf.SmoothDamp(
                    _currentDistance, safeDist, ref _distanceVelocity, 0.1f);
            } else {
                _currentDistance = Mathf.SmoothDamp(
                    _currentDistance, targetDist, ref _distanceVelocity, 0.1f);
            }

            return pivot + direction * _currentDistance;
        }

        private Vector3 ApplyShake(Vector3 basePos, float deltaTime) {
            if (_shakeIntensity <= 0.001f) {
                _shakeIntensity = 0f;
                return basePos;
            }

            _shakeTime += deltaTime * _settings.ShakeFrequency;
            var noiseX = Mathf.Sin(_shakeTime) * _shakeIntensity;
            var noiseY = Mathf.Cos(_shakeTime * 1.3f) * _shakeIntensity * 0.6f;
            var noiseZ = Mathf.Sin(_shakeTime * 0.7f) * _shakeIntensity * 0.3f;

            _shakeIntensity = Mathf.Lerp(_shakeIntensity, 0f, _settings.ShakeDecay * deltaTime);

            return basePos + new Vector3(noiseX, noiseY, noiseZ);
        }
    }
}
