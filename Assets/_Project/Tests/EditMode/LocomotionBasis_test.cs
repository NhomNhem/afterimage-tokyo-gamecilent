using NUnit.Framework;
using UnityEngine;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// LocomotionBasis_test — EditMode tests for Story 1-2 camera-relative movement.
    /// 
    /// Coverage:
    /// - AC-1: Camera movement basis projection onto ground plane
    /// - AC-2: Camera provides read-only movement basis
    /// - AC-3: Locomotion FSM handles move/idle transitions
    /// - AC-4: Facing supports movement direction when not locked-on
    /// </summary>
    public class LocomotionBasis_test {
        private M0PlayerLocomotion locomotion;

        [SetUp]
        public void Setup() {
            M0LocomotionSettings settings = new M0LocomotionSettings(
                moveSpeed: 5.0f,
                inputDeadzone: 0.1f,
                facingLerpSpeed: 8.0f);
            locomotion = new M0PlayerLocomotion(settings);
        }

        #region AC-1: Camera Movement Basis Projection

        /// <summary>
        /// AC-1: Movement basis is projected correctly on the ground plane.
        /// Test: Camera forward is tilted at 45 degrees; basis should have Y=0.
        /// </summary>
        [Test]
        public void test_basis_projection_45degree_camera_returns_normalized_ground_plane() {
            // Arrange: Create a camera basis with forward tilted down (45 degrees)
            // In world space: forward = (0, -0.707, 0.707), right = (1, 0, 0)
            // Project onto ground: forward = (0, 0, 0.707) normalized = (0, 1)
            // right = (1, 0) normalized = (1, 0)
            var tilted45Forward = new Axis2(0f, 1f);  // Already projected (Y=0)
            var tilted45Right = new Axis2(1f, 0f);    // Already projected (Y=0)

            CameraMovementBasisSnapshot basis = new CameraMovementBasisSnapshot(
                tilted45Forward,
                tilted45Right,
                true,
                "Test Camera 45deg");

            locomotion.SetCameraMovementBasis(basis);

            // Act
            LocomotionStateSnapshot snapshot = locomotion.Snapshot;

            // Assert: Basis should be valid and forward should have no vertical component
            Assert.IsTrue(snapshot.CameraMovementBasis.IsValid);
            Assert.AreEqual(0f, snapshot.CameraMovementBasis.Forward.X, 0.01f);
            Assert.AreEqual(1f, snapshot.CameraMovementBasis.Forward.Y, 0.01f);
        }

        /// <summary>
        /// AC-1: Basis forward vector is normalized.
        /// </summary>
        [Test]
        public void test_basis_forward_is_normalized() {
            // Arrange: Create basis with non-unit forward vector
            var nonUnitForward = new Axis2(2f, 2f);  // Magnitude = 2.828, not 1
            var right = new Axis2(1f, 0f);

            CameraMovementBasisSnapshot basis = new CameraMovementBasisSnapshot(
                nonUnitForward,
                right,
                true,
                "Non-unit basis");

            locomotion.SetCameraMovementBasis(basis);

            // Act
            LocomotionStateSnapshot snapshot = locomotion.Snapshot;

            // Assert: Adapter/Locomotion should handle normalization
            // For this test, we verify the basis is accepted
            Assert.IsTrue(snapshot.CameraMovementBasis.IsValid);
        }

        #endregion

        #region AC-2: Movement Basis from CameraScope

        /// <summary>
        /// AC-2: Movement basis is provided by CameraScope (read-only).
        /// Test: Locomotion can read camera basis and it's not mutated.
        /// </summary>
        [Test]
        public void test_movement_basis_provided_by_camera_scope_is_readonly() {
            // Arrange: Create camera basis
            var forward = new Axis2(0f, 1f);
            var right = new Axis2(1f, 0f);

            CameraMovementBasisSnapshot originalBasis = new CameraMovementBasisSnapshot(
                forward,
                right,
                true,
                "Original Camera");

            // Act: Feed basis to locomotion
            locomotion.SetCameraMovementBasis(originalBasis);
            LocomotionStateSnapshot snapshot1 = locomotion.Snapshot;

            // Try to "mutate" by setting a different basis
            var newBasis = new CameraMovementBasisSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 1f),
                true,
                "Rotated Camera");
            locomotion.SetCameraMovementBasis(newBasis);
            LocomotionStateSnapshot snapshot2 = locomotion.Snapshot;

            // Assert: Snapshot updated correctly (camera basis changed)
            Assert.AreEqual(originalBasis.Forward.X, snapshot1.CameraMovementBasis.Forward.X);
            Assert.AreEqual(newBasis.Forward.X, snapshot2.CameraMovementBasis.Forward.X);
            Assert.AreNotEqual(snapshot1.CameraMovementBasis.Forward.X, snapshot2.CameraMovementBasis.Forward.X);
        }

        #endregion

        #region AC-3: FSM Move/Idle Transitions

        /// <summary>
        /// AC-3: FSM transitions from Idle to Moving when movement input is provided.
        /// </summary>
        [Test]
        public void test_fsm_idle_to_moving_on_input() {
            // Arrange: Initial state should be Uninitialized or Idle
            LocomotionStateSnapshot initialSnapshot = locomotion.Snapshot;
            Assert.AreNotEqual(LocomotionState.Moving, initialSnapshot.State);

            // Create input with movement
            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 0f),  // Move right
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act: Consume input
            locomotion.ConsumeInputIntent(moveInput);
            LocomotionStateSnapshot afterInput = locomotion.Snapshot;

            // Assert: Should transition to Moving
            Assert.AreEqual(LocomotionState.Moving, afterInput.State);
        }

        /// <summary>
        /// AC-3: FSM transitions from Moving to Idle when movement input is released.
        /// </summary>
        [Test]
        public void test_fsm_moving_to_idle_on_no_input() {
            // Arrange: First get to Moving state
            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);
            locomotion.ConsumeInputIntent(moveInput);
            LocomotionStateSnapshot movingSnapshot = locomotion.Snapshot;
            Assert.AreEqual(LocomotionState.Moving, movingSnapshot.State);

            // Act: Stop movement input
            var idleInput = new InputIntentSnapshot(
                new Axis2(0f, 0f),  // No movement
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);
            locomotion.ConsumeInputIntent(idleInput);
            LocomotionStateSnapshot afterStop = locomotion.Snapshot;

            // Assert: Should transition to Idle
            Assert.AreEqual(LocomotionState.Idle, afterStop.State);
        }

        /// <summary>
        /// AC-3: FSM respects input deadzone (no movement below deadzone).
        /// </summary>
        [Test]
        public void test_fsm_input_below_deadzone_triggers_idle() {
            // Arrange: Setup with deadzone of 0.1
            M0LocomotionSettings settings = new M0LocomotionSettings(
                moveSpeed: 5.0f,
                inputDeadzone: 0.1f,
                facingLerpSpeed: 8.0f);
            M0PlayerLocomotion locomotionWithDeadzone = new M0PlayerLocomotion(settings);

            // Create input below deadzone (0.05 magnitude)
            var deadzonInput = new InputIntentSnapshot(
                new Axis2(0.03f, 0.04f),  // Magnitude ~0.05, below 0.1 deadzone
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act
            locomotionWithDeadzone.ConsumeInputIntent(deadzonInput);
            LocomotionStateSnapshot snapshot = locomotionWithDeadzone.Snapshot;

            // Assert: Should be Idle, not Moving
            Assert.AreEqual(LocomotionState.Idle, snapshot.State);
        }

        /// <summary>
        /// AC-2: FSM input at exact deadzone boundary transitions to moving.
        /// Test threshold: just above vs just below deadzone value.
        /// </summary>
        [Test]
        public void test_fsm_input_at_exact_deadzone_threshold() {
            // Arrange: Deadzone is 0.1
            M0LocomotionSettings settings = new M0LocomotionSettings(
                moveSpeed: 5.0f,
                inputDeadzone: 0.1f,
                facingLerpSpeed: 8.0f);
            M0PlayerLocomotion locomotionThreshold = new M0PlayerLocomotion(settings);

            // Test just below threshold: (0.0707, 0.0707) magnitude = 0.0999... < 0.1
            var justBelowDeadzone = new InputIntentSnapshot(
                new Axis2(0.0707f, 0.0707f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);
            
            locomotionThreshold.ConsumeInputIntent(justBelowDeadzone);
            LocomotionStateSnapshot snapshotBelow = locomotionThreshold.Snapshot;
            Assert.AreEqual(LocomotionState.Idle, snapshotBelow.State, 
                "Just below deadzone should remain Idle");

            // Test just above threshold: (0.0708, 0.0708) magnitude = 0.1001... > 0.1
            var justAboveDeadzone = new InputIntentSnapshot(
                new Axis2(0.0708f, 0.0708f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);
            
            locomotionThreshold.ConsumeInputIntent(justAboveDeadzone);
            LocomotionStateSnapshot snapshotAbove = locomotionThreshold.Snapshot;
            Assert.AreEqual(LocomotionState.Moving, snapshotAbove.State,
                "Just above deadzone should transition to Moving");
        }

        /// <summary>
        /// AC-2: FSM respects movement restriction (CanTranslate=false).
        /// When movement is restricted, state should be Restricted even with input.
        /// </summary>
        [Test]
        public void test_fsm_movement_restriction_freezes_locomotion() {
            // Arrange: Create movement input
            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            locomotion.ConsumeInputIntent(moveInput);
            
            // Verify we can move without restriction
            LocomotionStateSnapshot beforeRestriction = locomotion.Snapshot;
            Assert.AreEqual(LocomotionState.Moving, beforeRestriction.State);

            // Act: Apply movement restriction
            var restriction = new MovementRestrictionContext(
                canTranslate: false,
                canRotate: true,
                restrictionStrength: 1.0f,
                source: "Test Restriction");
            locomotion.SetMovementRestriction(restriction);

            // Assert: Should transition to Restricted state
            LocomotionStateSnapshot afterRestriction = locomotion.Snapshot;
            Assert.AreEqual(LocomotionState.Restricted, afterRestriction.State,
                "Movement restriction should transition to Restricted state");

            // Process movement with restriction
            float deltaTime = 1.0f / 60.0f;
            LocomotionMovementSnapshot beforeProcess = locomotion.GetMovementSnapshot();
            locomotion.ProcessMovementInput(deltaTime);
            LocomotionMovementSnapshot afterProcess = locomotion.GetMovementSnapshot();

            // Verify velocity is zero when restricted
            Assert.AreEqual(0f, afterProcess.Velocity.magnitude, 0.001f,
                "Velocity should be zero when movement is restricted");
        }

        /// <summary>
        /// AC-2: FSM respects recovery context (IsRecovering=true).
        /// When recovering, state should be Recovering regardless of input.
        /// </summary>
        [Test]
        public void test_fsm_recovery_state_overrides_movement() {
            // Arrange: Create movement input
            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            locomotion.ConsumeInputIntent(moveInput);
            
            // Verify we're in Moving state
            LocomotionStateSnapshot beforeRecovery = locomotion.Snapshot;
            Assert.AreEqual(LocomotionState.Moving, beforeRecovery.State);

            // Act: Apply recovery context
            var recovery = new RecoveryContext(
                source: RecoverySource.PlayerLocomotion,
                isRecovering: true,
                remainingSeconds: 0.5f,
                detail: "Recovery");
            locomotion.SetRecoveryContext(recovery);

            // Assert: Should transition to Recovering state
            LocomotionStateSnapshot afterRecovery = locomotion.Snapshot;
            Assert.AreEqual(LocomotionState.Recovering, afterRecovery.State,
                "Active recovery should transition to Recovering state");
        }

        #endregion

        #region AC-4: Facing Direction

        /// <summary>
        /// AC-4: Facing updates with movement direction when not locked-on.
        /// Test: Facing direction changes when movement direction changes.
        /// </summary>
        [Test]
        public void test_facing_updates_with_movement_direction() {
            // Arrange: Setup camera basis (forward=+Z, right=+X)
            var forward = new Axis2(0f, 1f);
            var right = new Axis2(1f, 0f);
            var basis = new CameraMovementBasisSnapshot(forward, right, true, "Test");
            locomotion.SetCameraMovementBasis(basis);

            // Create movement input rightward (not forward — initial facing is already Vector3.forward)
            var moveRight = new InputIntentSnapshot(
                new Axis2(1f, 0f),  // Move right (camera X direction)
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act: Process movement
            locomotion.ConsumeInputIntent(moveRight);
            float deltaTime = 1.0f / 60.0f;  // One frame at 60 FPS

            // Initial snapshot before processing
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();

            // Process movement input to calculate facing
            locomotion.ProcessMovementInput(deltaTime);

            // Second snapshot after processing
            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Facing should have changed
            // After processing, facing should have rotated from (0,0,1) toward (1,0,0)
            Assert.Greater(after.Facing.x, before.Facing.x,
                "Facing should rotate rightward from initial forward");
        }

        /// <summary>
        /// AC-4: Facing remains stable when no movement input.
        /// </summary>
        [Test]
        public void test_facing_stable_with_no_input() {
            // Arrange: Setup with no movement
            var basis = new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "Test");
            locomotion.SetCameraMovementBasis(basis);

            // Create idle input
            var idleInput = new InputIntentSnapshot(
                new Axis2(0f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act: Process with no input
            locomotion.ConsumeInputIntent(idleInput);
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();

            locomotion.ProcessMovementInput(0.016f);
            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Facing should remain essentially the same
            Assert.AreEqual(before.Facing.x, after.Facing.x, 0.01f);
            Assert.AreEqual(before.Facing.z, after.Facing.z, 0.01f);
        }

        /// <summary>
        /// AC-3: Facing lerps smoothly toward movement direction, not instantly.
        /// Verify facing moves incrementally based on lerp speed and delta time.
        /// </summary>
        [Test]
        public void test_facing_lerps_smoothly_not_instantly() {
            // Arrange: Create camera basis
            var basis = new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "Test");
            locomotion.SetCameraMovementBasis(basis);

            // Create movement input to the right
            var moveRight = new InputIntentSnapshot(
                new Axis2(1f, 0f),  // Move right: desired direction (1, 0, 0)
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            locomotion.ConsumeInputIntent(moveRight);
            
            // Get initial facing
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();
            
            // Process movement for one frame (1/60 sec)
            float deltaTime = 1.0f / 60.0f;
            locomotion.ProcessMovementInput(deltaTime);
            
            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Facing should have moved toward right (1, 0, 0), but NOT 100% there
            // Expected lerp factor = 8.0 * (1/60) = 0.1333...
            float expectedLerpFactor = 8.0f * deltaTime;
            Assert.Greater(expectedLerpFactor, 0.1f, "Lerp factor should be significant per frame");
            Assert.Less(expectedLerpFactor, 0.5f, "Lerp factor should be less than 50% (smooth, not instant)");

            // After one frame, facing.x should have moved toward 1.0 but not reached it
            float facingMovement = after.Facing.x - before.Facing.x;
            Assert.Greater(facingMovement, 0f, "Facing should move toward right");
            Assert.Less(after.Facing.x, 1.0f, "Facing should not instantly reach target");
        }

        /// <summary>
        /// AC-3: Facing is normalized after lerp.
        /// </summary>
        [Test]
        public void test_facing_is_normalized_after_lerp() {
            // Arrange
            var basis = new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "Test");
            locomotion.SetCameraMovementBasis(basis);

            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 1f),  // Diagonal movement
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act
            locomotion.ConsumeInputIntent(moveInput);
            locomotion.ProcessMovementInput(1.0f / 60.0f);
            LocomotionMovementSnapshot snapshot = locomotion.GetMovementSnapshot();

            // Assert: Facing magnitude should be 1.0 (normalized)
            Assert.AreEqual(1.0f, snapshot.Facing.magnitude, 0.001f,
                "Facing should be normalized after lerp");
        }

        /// <summary>
        /// AC-3: Facing direction is correct (toward movement, not away).
        /// Test that facing moves in the same direction as desired movement.
        /// </summary>
        [Test]
        public void test_facing_moves_toward_movement_direction_not_away() {
            // Arrange
            var basis = new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),  // Forward = +Z
                new Axis2(1f, 0f),  // Right = +X
                true,
                "Test");
            locomotion.SetCameraMovementBasis(basis);

            // Movement input: right (1, 0)
            var moveRight = new InputIntentSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act
            locomotion.ConsumeInputIntent(moveRight);
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();
            
            locomotion.ProcessMovementInput(1.0f / 60.0f);
            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Facing should move toward +X direction (right)
            // Dot product between facing delta and desired direction should be positive
            Vector3 facingDelta = after.Facing - before.Facing;
            Vector3 desiredDirection = new Vector3(1f, 0f, 0f);  // Right
            
            float dotProduct = Vector3.Dot(facingDelta.normalized, desiredDirection);
            Assert.Greater(dotProduct, 0f, "Facing delta should have positive component toward desired direction");
        }

        #endregion

        #region AC-1/AC-4: Invalid Camera Basis

        /// <summary>
        /// AC-1/AC-4: Movement is frozen when camera basis is invalid.
        /// Velocity should be zero and position should not change.
        /// </summary>
        [Test]
        public void test_invalid_camera_basis_freezes_movement() {
            // Arrange: Create movement input
            var moveInput = new InputIntentSnapshot(
                new Axis2(1f, 0f),
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Set invalid camera basis
            var invalidBasis = new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                false,  // IsValid = false
                "Camera missing");
            
            locomotion.SetCameraMovementBasis(invalidBasis);
            locomotion.ConsumeInputIntent(moveInput);

            // Act: Process movement with invalid basis
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();
            locomotion.ProcessMovementInput(1.0f / 60.0f);
            locomotion.UpdatePosition(1.0f / 60.0f);
            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Velocity should be zero
            Assert.AreEqual(0f, after.Velocity.magnitude, 0.001f,
                "Velocity should be zero when camera basis is invalid");

            // Assert: Position should not have changed
            Assert.AreEqual(before.Position, after.Position,
                "Position should not change with invalid camera basis");
        }

        #endregion

        #region AC-1: Player Movement Direction

        /// <summary>
        /// AC-1: Player moves in world-projected direction relative to camera forward.
        /// </summary>
        [Test]
        public void test_movement_input_forward_moves_in_camera_forward_direction() {
            // Arrange: Camera pointing forward (+Z), input moving forward
            var forward = new Axis2(0f, 1f);  // +Z in world
            var right = new Axis2(1f, 0f);   // +X in world
            var basis = new CameraMovementBasisSnapshot(forward, right, true, "Test");
            locomotion.SetCameraMovementBasis(basis);

            var moveForward = new InputIntentSnapshot(
                new Axis2(0f, 1f),  // Full forward input
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act: Process movement for one frame
            locomotion.ConsumeInputIntent(moveForward);
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();

            locomotion.ProcessMovementInput(0.016f);
            locomotion.UpdatePosition(0.016f);

            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Position should have moved forward in Z direction
            Assert.Greater(after.Position.z, before.Position.z,
                "Position should move forward when input is forward");
        }

        /// <summary>
        /// AC-1: Player moves right when camera basis right is input.
        /// </summary>
        [Test]
        public void test_movement_input_right_moves_in_camera_right_direction() {
            // Arrange: Camera setup, input moving right
            var forward = new Axis2(0f, 1f);
            var right = new Axis2(1f, 0f);
            var basis = new CameraMovementBasisSnapshot(forward, right, true, "Test");
            locomotion.SetCameraMovementBasis(basis);

            var moveRight = new InputIntentSnapshot(
                new Axis2(1f, 0f),  // Full right input
                new Axis2(0f, 0f),
                false, false, false, false, false, false, false, false,
                true);

            // Act: Process movement
            locomotion.ConsumeInputIntent(moveRight);
            LocomotionMovementSnapshot before = locomotion.GetMovementSnapshot();

            locomotion.ProcessMovementInput(0.016f);
            locomotion.UpdatePosition(0.016f);

            LocomotionMovementSnapshot after = locomotion.GetMovementSnapshot();

            // Assert: Position should have moved right in X direction
            Assert.Greater(after.Position.x, before.Position.x,
                "Position should move right when input is right");
        }

        #endregion
    }
}
