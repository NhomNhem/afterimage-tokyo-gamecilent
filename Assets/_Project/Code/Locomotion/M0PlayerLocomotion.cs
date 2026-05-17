using System;
using System.Collections.Generic;
using _Project.Code.Shared.DI;
using UnityEngine;
using GlassRefrain.Core;
using NhemDangFugBixs.Attributes;

namespace GlassRefrain.Locomotion {
    /// <summary>
    /// M0PlayerLocomotion — Pure C# gameplay truth owner for camera-relative movement.
    /// 
    /// Ownership:
    /// - Owns position, rotation (facing), velocity, and movement state
    /// - Processes input intents from Input System
    /// - Reads camera movement basis (read-only snapshot)
    /// - Expresses movement to adapters via read-only snapshot
    /// 
    /// Story 1-2 Scope:
    /// - Camera-relative movement and free-movement facing
    /// - No lock-on facing (deferred to Story 1-3)
    /// - No collision/ground detection (deferred to future)
    /// - No animator authority (FSM is Pure C# only, adapters observe)
    /// - No root motion (Locomotion owns movement truth)
    /// </summary>
    
    public interface IM0PlayerLocomotion {
        LocomotionStateSnapshot Snapshot { get; }
        LocomotionMovementSnapshot GetMovementSnapshot();
        void ConsumeInputIntent(InputIntentSnapshot inputIntent);
        void SetMovementRestriction(MovementRestrictionContext restriction);
        void SetRecoveryContext(RecoveryContext recovery);
        void SetCameraMovementBasis(CameraMovementBasisSnapshot cameraBasis);
        void ProcessMovementInput(float deltaTime);
        void UpdatePosition(float deltaTime);
        LocomotionDebugSnapshot CreateDebugSnapshot();
    }
    
    [AutoRegisterIn<IGameplayLifetimeScope>(Lifetime = NhemLifetime.Singleton)]
    public sealed class M0PlayerLocomotion : IM0PlayerLocomotion {
        private InputIntentSnapshot currentInput;
        private MovementRestrictionContext movementRestriction;
        private RecoveryContext recoveryContext;
        private CameraMovementBasisSnapshot cameraMovementBasis;
        private bool hasReceivedInput;
        private LocomotionStateSnapshot latestSnapshot;

        // Movement truth owned by M0PlayerLocomotion (Pure C#)
        private Vector3 position = Vector3.zero;  // World-space position
        private Vector3 facing = Vector3.forward; // World-space facing direction (normalized)
        private Vector3 velocity = Vector3.zero;  // Current movement velocity

        // Settings for tuning movement
        private M0LocomotionSettings settings;

        // Cached camera movement basis vectors (projected to world space)
        private Vector3 cachedCameraForward = Vector3.forward;
        private Vector3 cachedCameraRight = Vector3.right;

        public M0PlayerLocomotion() : this(new M0LocomotionSettings()) { }

        public M0PlayerLocomotion(M0LocomotionSettings settings) {
            this.settings = settings;

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

        public void SetMovementRestriction(MovementRestrictionContext restriction) {
            movementRestriction = restriction;
            RefreshSnapshot();
        }

        public void SetRecoveryContext(RecoveryContext recovery) {
            recoveryContext = recovery;
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

        /// <summary>
        /// Process movement input and update velocity based on camera movement basis.
        /// Should be called once per frame before UpdatePosition.
        /// </summary>
        public void ProcessMovementInput(float deltaTime) {
            if (!currentInput.InputEnabled || !movementRestriction.CanTranslate) {
                velocity = Vector3.zero;
                return;
            }

            // Apply deadzone to input
            Axis2 inputAxis = currentInput.Move;
            float inputMagnitude = Mathf.Sqrt(inputAxis.X * inputAxis.X + inputAxis.Y * inputAxis.Y);
            if (inputMagnitude < settings.InputDeadzone) {
                velocity = Vector3.zero;
                return;
            }

            // If camera basis is not valid, don't process movement
            if (!cameraMovementBasis.IsValid) {
                velocity = Vector3.zero;
                return;
            }

            // Use cached camera basis vectors (projected to world space)
            // Combine camera forward and right by input axis
            Vector3 desiredDirection = (cachedCameraForward * inputAxis.Y + cachedCameraRight * inputAxis.X).normalized;

            // Calculate velocity
            float speed = settings.MoveSpeed * inputMagnitude;
            velocity = desiredDirection * speed;

            // Update facing to follow movement direction
            if (inputMagnitude > settings.InputDeadzone) {
                facing = Vector3.Lerp(facing, desiredDirection, settings.FacingLerpSpeed * deltaTime);
                facing = facing.normalized;
            }
        }

        /// <summary>
        /// Integrate position based on current velocity.
        /// Should be called once per frame after ProcessMovementInput.
        /// </summary>
        public void UpdatePosition(float deltaTime) {
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
                stateDetail);

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

        private static bool HasMoveIntent(Axis2 move) {
            return move.X != 0f || move.Y != 0f;
        }
    }
}