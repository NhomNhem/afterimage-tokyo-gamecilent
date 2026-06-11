using GlassRefrain.Bootstrap;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using NUnit.Framework;
namespace GlassRefrain.Tests.PlayMode {
    public class M0DodgeDisplacementIntegrationTests {
        [Test]
        public void CombatCoreDodgeActive_WhenBridgeIsArmed_ShouldMoveLocomotion() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(1f, 0f);
            var bridge = new M0DodgeDisplacementBridge();

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));
            bridge.HandleCombatTransition(CombatCoreState.Neutral, combat.Snapshot, locomotion);

            combat.Tick(0.11f);
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));
            Assert.That(bridge.HandleCombatTransition(CombatCoreState.DodgeStartup, combat.Snapshot, locomotion), Is.True);

            var before = locomotion.GetMovementSnapshot().Position;
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            var after = locomotion.GetMovementSnapshot().Position;

            Assert.That(after.x, Is.GreaterThan(before.x + 0.9f));
            Assert.That(after.z, Is.EqualTo(before.z).Within(0.001f));
        }

        [Test]
        public void NonDodgeCombatStates_WhenObservedByBridge_ShouldNotMoveLocomotion() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(0f, 0f);
            var bridge = new M0DodgeDisplacementBridge();

            combat.RequestAction(CreateRequest(CombatActionType.LightAttack));
            Assert.That(bridge.HandleCombatTransition(CombatCoreState.Neutral, combat.Snapshot, locomotion), Is.False);

            var before = locomotion.GetMovementSnapshot().Position;
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            var after = locomotion.GetMovementSnapshot().Position;

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void DodgeActive_WhenStartupWasNotObserved_ShouldNotStartDisplacement() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(0f, 0f);
            var bridge = new M0DodgeDisplacementBridge();

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));
            combat.Tick(0.11f);

            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));
            Assert.That(bridge.HandleCombatTransition(CombatCoreState.DodgeStartup, combat.Snapshot, locomotion), Is.False);

            var before = locomotion.GetMovementSnapshot().Position;
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            var after = locomotion.GetMovementSnapshot().Position;

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void DuplicateDodgeActiveObservation_WhenAlreadyStarted_ShouldNotStartSecondDisplacement() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(1f, 0f);
            var bridge = new M0DodgeDisplacementBridge();

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));
            bridge.HandleCombatTransition(CombatCoreState.Neutral, combat.Snapshot, locomotion);
            combat.Tick(0.11f);

            Assert.That(bridge.HandleCombatTransition(CombatCoreState.DodgeStartup, combat.Snapshot, locomotion), Is.True);
            Assert.That(bridge.HandleCombatTransition(CombatCoreState.DodgeActive, combat.Snapshot, locomotion), Is.False);
        }

        [Test]
        public void DodgeDisplacement_WhenStartedThroughBridge_ShouldPreserveAuthorityBoundaries() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(1f, 0f);
            var bridge = new M0DodgeDisplacementBridge();

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));
            var startupSnapshot = combat.Snapshot;
            bridge.HandleCombatTransition(CombatCoreState.Neutral, startupSnapshot, locomotion);
            combat.Tick(0.11f);
            var activeSnapshot = combat.Snapshot;

            Assert.That(bridge.HandleCombatTransition(startupSnapshot.State, activeSnapshot, locomotion), Is.True);
            Assert.That(activeSnapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));
            Assert.That(activeSnapshot.ActionLock.RequestingState, Is.EqualTo(CombatCoreState.DodgeActive));
            Assert.That(locomotion.GetMovementSnapshot().State, Is.EqualTo(LocomotionState.Moving));
        }

        private static M0CombatCore CreateCombatCore() {
            return new M0CombatCore(new M0CombatTimingSettings(
                attackStartupSeconds: 0.1f,
                attackActiveSeconds: 0.2f,
                attackRecoverySeconds: 0.3f,
                dodgeStartupSeconds: 0.1f,
                dodgeActiveSeconds: 0.2f,
                dodgeRecoverySeconds: 0.3f,
                parryStartupSeconds: 0.1f,
                parryActiveSeconds: 0.2f,
                parryRecoverySeconds: 0.3f,
                counterWindowDurationSeconds: 1f,
                recoveryDurationSeconds: 0.3f));
        }

        private static M0PlayerLocomotion CreateLocomotion(float moveX, float moveY) {
            var locomotion = new M0PlayerLocomotion(new M0LocomotionSettings(
                moveSpeed: 5f,
                inputDeadzone: 0.1f,
                facingLerpSpeed: 8f,
                dodgeDistance: 1.5f,
                dodgeSpeed: 10f,
                dodgeDurationSeconds: 0.2f));

            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "Test"));
            locomotion.ConsumeInputIntent(CreateInputSnapshot(moveX, moveY));
            return locomotion;
        }

        private static CombatActionRequest CreateRequest(CombatActionType actionType) {
            return new CombatActionRequest(
                actionType,
                0f,
                CombatRequestSourceType.TestHarness,
                "M0DodgeDisplacementIntegrationTests",
                "Test request");
        }

        private static InputIntentSnapshot CreateInputSnapshot(float x, float y) {
            return new InputIntentSnapshot(
                new Axis2(x, y),
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
        }
    }
}
