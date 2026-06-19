using GlassRefrain.Application;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using GlassRefrain.Presentation;
using NUnit.Framework;

namespace GlassRefrain.Tests.PlayMode {
    public class PlayerStateMachineDodgeTests {
        [Test]
        public void PlayerStateMachine_OnDodgeEntry_CallsTryBeginDodgeDisplacement() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(1f, 0f);
            var machine = CreateResolver(combat, locomotion);

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));

            var before = locomotion.GetMovementSnapshot().Position;
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            var after = locomotion.GetMovementSnapshot().Position;

            Assert.That(after.x, Is.GreaterThan(before.x + 0.9f));
        }

        [Test]
        public void PlayerStateMachine_NonDodgeState_DoesNotTriggerDisplacement() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(0f, 0f);
            var machine = CreateResolver(combat, locomotion);

            combat.RequestAction(CreateRequest(CombatActionType.LightAttack));

            var before = locomotion.GetMovementSnapshot().Position;
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            var after = locomotion.GetMovementSnapshot().Position;

            Assert.That(after, Is.EqualTo(before));
        }

        [Test]
        public void PlayerStateMachine_DodgeDisplacement_OnlyStartsOncePerTransition() {
            var combat = CreateCombatCore();
            var locomotion = CreateLocomotion(1f, 0f);
            var machine = CreateResolver(combat, locomotion);

            combat.RequestAction(CreateRequest(CombatActionType.Dodge));

            Assert.That(locomotion.TryBeginDodgeDisplacement(), Is.False,
                "Second TryBeginDodgeDisplacement should be rejected because first one from machine is active");
        }

        private static PlayerStateResolver CreateResolver(M0CombatCore combat, M0PlayerLocomotion locomotion) {
            return new PlayerStateResolver(
                new CombatStateMachine(combat),
                new LocomotionStateMachine(locomotion));
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
                "PlayerStateMachineDodgeTests",
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

        private sealed class MockAnimationService : IPlayerAnimationService {
            public bool IsTurnActive => false;
            public System.Action<bool> TurnActiveChanged { get; set; }
            public void SetCombatMode(bool isCombatMode) { }
            public void PlayNeutral() { }
            public void PlayLocomotion(LocomotionStateSnapshot snapshot, UnityEngine.Vector2 relativeMovementDirection) { }
            public void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot, UnityEngine.Vector2 relativeMovementDirection) { }
            public void PlayTurn(TurnDirection direction) { }
            public void PlayAttack(AttackAnimationRequest request) { }
            public void PlayDodge(DodgeAnimationRequest request) { }
            public void PlayParry(ParryAnimationRequest request) { }
            public void PlayCounter(AttackAnimationRequest request) { }
            public void PlayDash(DashDirection direction) { }
            public void PlayHitReaction(AttackAnimationRequest request) { }
            public void PlayStun() { }
        }
    }
}
