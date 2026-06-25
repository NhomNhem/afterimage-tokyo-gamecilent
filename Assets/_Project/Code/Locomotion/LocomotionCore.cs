        using System;
        using System.Collections.Generic;
        using UnityEngine;
        using GlassRefrain.Core;
        using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Locomotion {
    public sealed class LocomotionCore : ILocomotionCore {
        private InputIntentSnapshot currentInput;
        private MovementRestrictionContext movementRestriction;
        private RecoveryContext recoveryContext;
        private CameraMovementBasisSnapshot cameraMovementBasis;
        private bool hasReceivedInput;
        private LocomotionStateSnapshot latestSnapshot;

        // Movement truth owned by LocomotionCore (Pure C#)
        private Vector3 position = Vector3.zero;
        private Vector3 facing = Vector3.forward;
        private Vector3 velocity = Vector3.zero;

        // Settings for tuning movement
        private M0LocomotionSettings settings;

        // Cached camera movement basis vectors (projected to world space)
        private Vector3 cachedCameraForward = Vector3.forward;
        private Vector3 cachedCameraRight = Vector3.right;
        private bool dodgeDisplacementActive;
        private Vector3 dodgeDisplacementDirection = Vector3.forward;
        private float dodgeDisplacementRemainingDistance;
        private float dodgeDisplacementRemainingSeconds;
        private bool _strafeModeEnabled;
        private INhemLogger _logger;

        public LocomotionCore() : this(new M0LocomotionSettings(5.0f, 0.1f, 8.0f, 8.0f, 6.0f, 1.5f, 10.0f, 0.2f), null) { }

        public LocomotionCore(M0LocomotionSettings settings) : this(settings, null) { }

        public LocomotionCore(M0LocomotionSettings settings, INhemLogger logger) {
            if (settings.MoveSpeed <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.MoveSpeed,
                    "M0LocomotionSettings.MoveSpeed must be > 0. Check DI registration/wiring.");
            }

            if (settings.InputDeadzone < 0f || settings.InputDeadzone >= 1f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.InputDeadzone,
                    "M0LocomotionSettings.InputDeadzone must be in [0, 1).");
            }

            if (settings.FacingLerpSpeed <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.FacingLerpSpeed,
                    "M0LocomotionSettings.FacingLerpSpeed must be > 0.");
            }
            if (settings.DodgeDistance <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.DodgeDistance,
                    "M0LocomotionSettings.DodgeDistance must be > 0.");
            }
            if (settings.DodgeSpeed <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.DodgeSpeed,
                    "M0LocomotionSettings.DodgeSpeed must be > 0.");
            }
            if (settings.DodgeDurationSeconds <= 0f) {
                throw new ArgumentOutOfRangeException(
                    nameof(settings),
                    settings.DodgeDurationSeconds,
                    "M0LocomotionSettings.DodgeDurationSeconds must be > 0.");
            }

            this.settings = settings;
            _logger = logger;

            currentInput = new InputIntentSnapshot(
                new Axis2(0f, 0f),
                new Axis2(0f, 0f),
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true);

            movementRestriction = new MovementRestrictionContext(true, true, 0f, string.Empty);
            recoveryContext = new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty);
            cameraMovementBasis =
                new CameraMovementBasisSnapshot(new Axis2(0f, 1f), new Axis2(1f, 0f), false, "Deferred");
            hasReceivedInput = false;
            RefreshSnapshot();
        }

        public LocomotionStateSnapshot Snapshot => latestSnapshot;

        /// <summary>
        /// Returns a read-only snapshot of current movement state for adapters.
        /// Includes position, facing, velocity, and FSM state.
        /// </summary>
        public LocomotionMovementSnapshot GetMovementSnapshot() {
            return new LocomotionMovementSnapshot(
                position,
                facing,
                velocity,
                latestSnapshot.State,
                latestSnapshot.StateDetail);
        }

        public event Action<LocomotionStateSnapshot> SnapshotChanged;

        public void ConsumeInputIntent(InputIntentSnapshot inputIntent) {
            currentInput = inputIntent;
            hasReceivedInput = true;
            RefreshSnapshot();
        }

        public void SetCameraMovementBasis(CameraMovementBasisSnapshot cameraBasis) {
            cameraMovementBasis = cameraBasis;

            // Cache projected camera vectors to avoid repeated construction in ProcessMovementInput
            if (cameraBasis.IsValid) {
                cachedCameraForward = new Vector3(cameraBasis.Forward.X, 0f, cameraBasis.Forward.Y);
                cachedCameraRight = new Vector3(cameraBasis.Right.X, 0f, cameraBasis.Right.Y);
            } else {
                cachedCameraForward = Vector3.forward;
                cachedCameraRight = Vector3.right;
            }

            RefreshSnapshot();
        }

        public bool TryBeginDodgeDisplacement() {
            if (dodgeDisplacementActive) {
                return false;
            }

            dodgeDisplacementDirection = ResolveDodgeDirection();
            dodgeDisplacementRemainingDistance = settings.DodgeDistance;
            dodgeDisplacementRemainingSeconds = settings.DodgeDurationSeconds;
            dodgeDisplacementActive = true;
            return true;
        }

        public bool TryBeginDashDisplacement(Vector3 dashDirection) {
            if (dodgeDisplacementActive) {
                return false;
            }

            dodgeDisplacementDirection = dashDirection.normalized;
            dodgeDisplacementRemainingDistance = settings.DodgeDistance;
            dodgeDisplacementRemainingSeconds = settings.DodgeDurationSeconds;
            dodgeDisplacementActive = true;
            return true;
        }

        public void ResetForEncounter(Vector3 startPosition, Vector3 startFacing) {
            position = startPosition;

            if (startFacing.sqrMagnitude > 0.000001f) {
                facing = startFacing.normalized;
            } else {
                facing = Vector3.forward;
            }

            velocity = Vector3.zero;
            dodgeDisplacementActive = false;
            dodgeDisplacementDirection = facing;
            dodgeDisplacementRemainingDistance = 0f;
            dodgeDisplacementRemainingSeconds = 0f;

            currentInput = new InputIntentSnapshot(
                new Axis2(0f, 0f),
                new Axis2(0f, 0f),
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true);
            hasReceivedInput = true;

            RefreshSnapshot();
        }

        public void SetStrafeMode(bool enabled) {
            _strafeModeEnabled = enabled;
        }

        public void Tick(InputIntentSnapshot intent, ControlState control, float deltaTime) {
            currentInput = intent;
            hasReceivedInput = true;

            movementRestriction = new MovementRestrictionContext(
                control.CanMove, control.CanRotate, control.CanMove ? 0f : 1f,
                control.CanMove ? string.Empty : "ControlState");
            recoveryContext = new RecoveryContext(
                RecoverySource.Unknown, false, 0f, string.Empty);

            if (dodgeDisplacementActive) {
                ProcessDodgeDisplacement(deltaTime);
                RefreshSnapshot();
                return;
            }

            if (!currentInput.InputEnabled || !control.CanMove) {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, settings.Deceleration * deltaTime);
                IntegratePosition(deltaTime);
                RefreshSnapshot();
                return;
            }

            Axis2 inputAxis = currentInput.Move;
            float inputMagnitude = Mathf.Sqrt(inputAxis.X * inputAxis.X + inputAxis.Y * inputAxis.Y);
            if (inputMagnitude < settings.InputDeadzone) {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, settings.Deceleration * deltaTime);
                IntegratePosition(deltaTime);
                RefreshSnapshot();
                return;
            }

            if (!cameraMovementBasis.IsValid) {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, settings.Deceleration * deltaTime);
                IntegratePosition(deltaTime);
                RefreshSnapshot();
                return;
            }

            Vector3 desiredDirectionRaw = cachedCameraForward * inputAxis.Y + cachedCameraRight * inputAxis.X;
            if (desiredDirectionRaw.sqrMagnitude <= 0.000001f) {
                velocity = Vector3.MoveTowards(velocity, Vector3.zero, settings.Deceleration * deltaTime);
                IntegratePosition(deltaTime);
                RefreshSnapshot();
                return;
            }

            Vector3 desiredDirection = desiredDirectionRaw.normalized;
            float speed = settings.MoveSpeed * inputMagnitude;
            Vector3 desiredVelocity = desiredDirection * speed;
            velocity = Vector3.MoveTowards(velocity, desiredVelocity, settings.Acceleration * deltaTime);
            velocity = Vector3.ClampMagnitude(velocity, settings.MoveSpeed);

            if (inputMagnitude > settings.InputDeadzone && control.CanRotate) {
                Vector3 targetFacing = _strafeModeEnabled ? cachedCameraForward : desiredDirection;

                facing = Vector3.Lerp(facing, targetFacing, settings.FacingLerpSpeed * deltaTime);
                facing = facing.normalized;
            }

            IntegratePosition(deltaTime);
            RefreshSnapshot();
        }

        private void ProcessDodgeDisplacement(float deltaTime) {
            if (deltaTime <= 0f) return;

            float frameDistance = Mathf.Min(
                settings.DodgeSpeed * deltaTime,
                dodgeDisplacementRemainingDistance);

            position += dodgeDisplacementDirection * frameDistance;
            velocity = dodgeDisplacementDirection * (frameDistance / deltaTime);

            dodgeDisplacementRemainingDistance = Mathf.Max(0f, dodgeDisplacementRemainingDistance - frameDistance);
            dodgeDisplacementRemainingSeconds = Mathf.Max(0f, dodgeDisplacementRemainingSeconds - deltaTime);

            if (dodgeDisplacementRemainingDistance <= 0f || dodgeDisplacementRemainingSeconds <= 0f) {
                dodgeDisplacementActive = false;
                velocity = Vector3.zero;
            }
        }

        private void IntegratePosition(float deltaTime) {
            if (dodgeDisplacementActive) {
                if (deltaTime <= 0f) {
                    return;
                }

                float frameDistance = Mathf.Min(
                    settings.DodgeSpeed * deltaTime,
                    dodgeDisplacementRemainingDistance);

                position += dodgeDisplacementDirection * frameDistance;
                velocity = dodgeDisplacementDirection * (frameDistance / deltaTime);

                dodgeDisplacementRemainingDistance = Mathf.Max(0f, dodgeDisplacementRemainingDistance - frameDistance);
                dodgeDisplacementRemainingSeconds = Mathf.Max(0f, dodgeDisplacementRemainingSeconds - deltaTime);

                if (dodgeDisplacementRemainingDistance <= 0f || dodgeDisplacementRemainingSeconds <= 0f) {
                    dodgeDisplacementActive = false;
                    velocity = Vector3.zero;
                }

                return;
            }

            position += velocity * deltaTime;
        }

        public LocomotionDebugSnapshot CreateDebugSnapshot() {
            var details = new string[] {
                "State: " + latestSnapshot.State,
                "StateDetail: " + latestSnapshot.StateDetail,
                "InputEnabled: " + latestSnapshot.InputEnabled,
                "MoveIntent: (" + latestSnapshot.MoveIntent.X + ", " + latestSnapshot.MoveIntent.Y + ")",
                "Restriction: " + latestSnapshot.MovementRestriction.CanTranslate + "/" +
                latestSnapshot.MovementRestriction.CanRotate + " | " +
                latestSnapshot.MovementRestriction.RestrictionStrength + " | " +
                latestSnapshot.MovementRestriction.Source,
                "Recovery: " + latestSnapshot.Recovery.IsRecovering + " | " + latestSnapshot.Recovery.RemainingSeconds +
                " | " + latestSnapshot.Recovery.Source + " | " + latestSnapshot.Recovery.Detail,
                "CameraBasis: " + latestSnapshot.CameraMovementBasis.IsValid + " | " +
                latestSnapshot.CameraMovementBasis.CameraModeLabel,
                "Position: " + position.x + ", " + position.y + ", " + position.z,
                "Facing: " + facing.x + ", " + facing.y + ", " + facing.z,
                "Velocity: " + velocity.x + ", " + velocity.y + ", " + velocity.z
            };

            return new LocomotionDebugSnapshot("M0 locomotion state", Array.AsReadOnly(details));
        }

        private void RefreshSnapshot() {
            var state = ResolveState();
            var stateDetail = ResolveStateDetail(state);

            latestSnapshot = new LocomotionStateSnapshot(
                state,
                currentInput.Move,
                currentInput.InputEnabled,
                movementRestriction,
                recoveryContext,
                cameraMovementBasis,
                stateDetail,
                velocity);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }

        private LocomotionState ResolveState() {
            if (recoveryContext.IsRecovering) return LocomotionState.Recovering;

            if (!currentInput.InputEnabled || !movementRestriction.CanTranslate) return LocomotionState.Restricted;

            if (hasReceivedInput && HasMoveIntent(currentInput.Move)) return LocomotionState.Moving;

            if (hasReceivedInput) return LocomotionState.Idle;

            return LocomotionState.Uninitialized;
        }

        private string ResolveStateDetail(LocomotionState state) {
            switch (state) {
                case LocomotionState.Recovering:
                    if (!string.IsNullOrEmpty(recoveryContext.Detail)) return recoveryContext.Detail;

                    return "Recovering from " + recoveryContext.Source;
                case LocomotionState.Restricted:
                    if (!currentInput.InputEnabled) return "Input disabled";

                    if (!movementRestriction.CanTranslate)
                        return string.IsNullOrEmpty(movementRestriction.Source)
                            ? "Movement restricted"
                            : movementRestriction.Source;

                    return "Movement restricted";
                case LocomotionState.Moving:
                    return "Raw move intent present";
                case LocomotionState.Idle:
                    return "No move intent";
                default:
                    return "Awaiting first movement intent";
            }
        }

        private bool HasMoveIntent(Axis2 move) {
            float inputMagnitude = Mathf.Sqrt(move.X * move.X + move.Y * move.Y);
            return inputMagnitude >= settings.InputDeadzone;
        }

        private Vector3 ResolveDodgeDirection() {
            Axis2 inputAxis = currentInput.Move;
            float inputMagnitude = Mathf.Sqrt(inputAxis.X * inputAxis.X + inputAxis.Y * inputAxis.Y);
            if (inputMagnitude >= settings.InputDeadzone && cameraMovementBasis.IsValid) {
                Vector3 direction = cachedCameraForward * inputAxis.Y + cachedCameraRight * inputAxis.X;
                if (direction.sqrMagnitude > 0.000001f) {
                    return direction.normalized;
                }
            }

            if (facing.sqrMagnitude > 0.000001f) {
                return facing.normalized;
            }

            return Vector3.forward;
        }
    }
}
