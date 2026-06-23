using System.Linq;
using GlassRefrain.Core;
using GlassRefrain.Presentation;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class AnimatorPresentationOnlyTests {
        [Test]
        public void AnimationClipTransition_IsAssigned_ReturnsFalseForNullClip() {
            var transition = new M0AnimationClipTransition();
            Assert.That(transition.IsAssigned, Is.False);
        }

        [Test]
        public void AnimationClipTransition_FadeDuration_DefaultsToZero() {
            var transition = new M0AnimationClipTransition();
            Assert.That(transition.FadeDuration, Is.EqualTo(0.1f));
        }

        [Test]
        public void AttackAnimationRequest_StoresValuesImmutably() {
            var request = new AttackAnimationRequest(
                CombatActionType.LightAttack,
                CombatCoreState.AttackStartup,
                "test detail");

            Assert.That(request.AttackType, Is.EqualTo(CombatActionType.LightAttack));
            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.AttackStartup));
            Assert.That(request.SourceLabel, Is.EqualTo("test detail"));
        }

        [Test]
        public void DodgeAnimationRequest_StoresValuesImmutably() {
            var request = new DodgeAnimationRequest(
                CombatCoreState.DodgeStartup,
                "dodge reason");

            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.DodgeStartup));
            Assert.That(request.SourceLabel, Is.EqualTo("dodge reason"));
        }

        [Test]
        public void ParryAnimationRequest_StoresValuesImmutably() {
            var request = new ParryAnimationRequest(
                CombatCoreState.ParryActive,
                "parry reason");

            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.ParryActive));
            Assert.That(request.SourceLabel, Is.EqualTo("parry reason"));
        }

        [Test]
        public void EnemyIntentAnimationRequest_StoresValuesImmutably() {
            var request = new EnemyIntentAnimationRequest(
                EnemyIntentState.Telegraph,
                "enemy-01",
                "slash",
                "telegraph-01");

            Assert.That(request.IntentState, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(request.EnemyId, Is.EqualTo("enemy-01"));
            Assert.That(request.IntentLabel, Is.EqualTo("slash"));
            Assert.That(request.TelegraphId, Is.EqualTo("telegraph-01"));
        }

        [Test]
        public void EnemyIntentAnimationRequest_HandlesNullStrings() {
            var request = new EnemyIntentAnimationRequest(
                EnemyIntentState.Idle,
                null!,
                null!,
                null!);

            Assert.That(request.EnemyId, Is.EqualTo(string.Empty));
            Assert.That(request.IntentLabel, Is.EqualTo(string.Empty));
            Assert.That(request.TelegraphId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void IPlayerAnimationService_InterfaceExists() {
            var type = typeof(IPlayerAnimationService);
            Assert.That(type.IsInterface, Is.True);
            Assert.That(type.GetMethod("SetCombatMode"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayNeutral"), Is.Not.Null);
            Assert.That(type.GetMethods().Any(m => m.Name == "PlayLocomotion"), Is.True);
            Assert.That(type.GetMethod("PlayAttack"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayDodge"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayParry"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayCounter"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayDash"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayHitReaction"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayStun"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayTurn"), Is.Not.Null);
            Assert.That(type.GetMethod("SetLocomotionParameters"), Is.Not.Null);
        }

        [Test]
        public void DashDirection_EnumHasFourDirections() {
            var values = System.Enum.GetValues(typeof(DashDirection));
            Assert.That(values.Length, Is.EqualTo(4));
            Assert.That(System.Enum.IsDefined(typeof(DashDirection), DashDirection.Forward), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(DashDirection), DashDirection.Back), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(DashDirection), DashDirection.Left), Is.True);
            Assert.That(System.Enum.IsDefined(typeof(DashDirection), DashDirection.Right), Is.True);
        }

        [Test]
        public void PlayerStateSnapshot_HasMovementAndFacingDirection() {
            var snapshot = new PlayerStateSnapshot(
                PlayerState.Moving,
                CombatCoreState.Neutral,
                LocomotionState.Moving,
                new ActionLockContext(false, string.Empty, CombatCoreState.Neutral),
                new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty),
                false,
                "test",
                new CombatResolutionResult(false, false, "test"),
                new Axis2(1f, 0f),
                new Axis2(0f, 1f));

            Assert.That(snapshot.MovementDirection.X, Is.EqualTo(1f));
            Assert.That(snapshot.MovementDirection.Y, Is.EqualTo(0f));
            Assert.That(snapshot.FacingDirection.X, Is.EqualTo(0f));
            Assert.That(snapshot.FacingDirection.Y, Is.EqualTo(1f));
        }

        [Test]
        public void PlayerStateSnapshot_OldConstructor_DefaultsMovementToZero() {
            var snapshot = new PlayerStateSnapshot(
                PlayerState.Idle,
                CombatCoreState.Neutral,
                LocomotionState.Idle,
                new ActionLockContext(false, string.Empty, CombatCoreState.Neutral),
                new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty),
                false,
                "test",
                new CombatResolutionResult(false, false, "test"));

            Assert.That(snapshot.MovementDirection.X, Is.EqualTo(0f));
            Assert.That(snapshot.MovementDirection.Y, Is.EqualTo(0f));
            Assert.That(snapshot.FacingDirection.X, Is.EqualTo(0f));
            Assert.That(snapshot.FacingDirection.Y, Is.EqualTo(1f));
        }

        [Test]
        public void IEnemyAnimationService_InterfaceExists() {
            var type = typeof(IEnemyAnimationService);
            Assert.That(type.IsInterface, Is.True);
            Assert.That(type.GetMethod("PlayIdle"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayIntent"), Is.Not.Null);
        }

        [Test]
        public void M0AnimationPresentationAdapter_ExistsInPresentationNamespace() {
            var type = typeof(M0AnimationPresentationAdapter);
            Assert.That(type.Namespace, Is.EqualTo("GlassRefrain.Presentation"));
        }

        [Test]
        public void AnimancerPlayerAnimationDriver_ImplementsIPlayerAnimationService() {
            var type = typeof(AnimancerPlayerAnimationDriver);
            Assert.That(typeof(IPlayerAnimationService).IsAssignableFrom(type), Is.True);
        }

        [Test]
        public void AnimancerEnemyAnimationDriver_ImplementsIEnemyAnimationService() {
            var type = typeof(AnimancerEnemyAnimationDriver);
            Assert.That(typeof(IEnemyAnimationService).IsAssignableFrom(type), Is.True);
        }

        [Test]
        public void AnimationDrivers_DoNotReferenceDomainLayerDirectly() {
            var playerDriverType = typeof(AnimancerPlayerAnimationDriver);
            var enemyDriverType = typeof(AnimancerEnemyAnimationDriver);

            var playerFields = playerDriverType.GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            foreach (var field in playerFields) {
                var fieldType = field.FieldType;
                var ns = fieldType.Namespace ?? string.Empty;
                Assert.That(ns, Does.Not.Contain("GlassRefrain.Domain"),
                    $"AnimancerPlayerAnimationDriver field '{field.Name}' references Domain type '{fieldType.FullName}'");
            }

            var enemyFields = enemyDriverType.GetFields(
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            foreach (var field in enemyFields) {
                var fieldType = field.FieldType;
                var ns = fieldType.Namespace ?? string.Empty;
                Assert.That(ns, Does.Not.Contain("GlassRefrain.Domain"),
                    $"AnimancerEnemyAnimationDriver field '{field.Name}' references Domain type '{fieldType.FullName}'");
            }
        }

        [Test]
        public void M0AnimationPresentationAdapter_ObserveMethodsExist() {
            var type = typeof(M0AnimationPresentationAdapter);
            Assert.That(type.GetMethod("ObservePlayerState"), Is.Not.Null);
            Assert.That(type.GetMethod("ObserveEnemyIntentSnapshot"), Is.Not.Null);
        }

        [Test]
        public void M0AnimationPresentationAdapter_ObservePlayerState_AcceptsIPlayerStateMachine() {
            var method = typeof(M0AnimationPresentationAdapter).GetMethod("ObservePlayerState");
            Assert.That(method, Is.Not.Null);
            var paramType = method.GetParameters()[0].ParameterType;
            Assert.That(paramType.Name, Is.EqualTo("IPlayerStateMachine"));
        }

        [Test]
        public void M0CombatVisualFeedbackAdapter_CounterAvailabilityHookExists() {
            var type = typeof(M0CombatVisualFeedbackAdapter);

            Assert.That(type.GetMethod("TriggerCounterAvailableFeedback"), Is.Not.Null);
        }

        [Test]
        public void M0PlayerAnimationSet_IsScriptableObject() {
            var type = typeof(M0PlayerAnimationSet);
            Assert.That(type.BaseType, Is.EqualTo(typeof(UnityEngine.ScriptableObject)));
        }

        [Test]
        public void M0EnemyAnimationSet_IsScriptableObject() {
            var type = typeof(M0EnemyAnimationSet);
            Assert.That(type.BaseType, Is.EqualTo(typeof(UnityEngine.ScriptableObject)));
        }

        [Test]
        public void M0PlayerAnimationSet_HasAttackWindupProperty() {
            var type = typeof(M0PlayerAnimationSet);
            var property = type.GetProperty("AttackWindup");
            Assert.That(property, Is.Not.Null, "M0PlayerAnimationSet should expose AttackWindup property");
            Assert.That(property.PropertyType, Is.EqualTo(typeof(M0AnimationClipTransition)));
        }

        [Test]
        public void M0PlayerAnimationSet_HasAttackRecoveryProperty() {
            var type = typeof(M0PlayerAnimationSet);
            var property = type.GetProperty("AttackRecovery");
            Assert.That(property, Is.Not.Null, "M0PlayerAnimationSet should expose AttackRecovery property");
            Assert.That(property.PropertyType, Is.EqualTo(typeof(M0AnimationClipTransition)));
        }

        [Test]
        public void AttackAnimationRequest_StoresCombatPhaseForWindup() {
            var request = new AttackAnimationRequest(
                CombatActionType.LightAttack,
                CombatCoreState.AttackStartup,
                "windup phase");

            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.AttackStartup));
            Assert.That(request.AttackType, Is.EqualTo(CombatActionType.LightAttack));
        }

        [Test]
        public void AttackAnimationRequest_StoresCombatPhaseForRecovery() {
            var request = new AttackAnimationRequest(
                CombatActionType.HeavyAttack,
                CombatCoreState.AttackRecovery,
                "recovery phase");

            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.AttackRecovery));
            Assert.That(request.AttackType, Is.EqualTo(CombatActionType.HeavyAttack));
        }

        [Test]
        public void AttackAnimationRequest_StoresCombatPhaseForActive() {
            var request = new AttackAnimationRequest(
                CombatActionType.LightAttack,
                CombatCoreState.AttackActive,
                "active phase");

            Assert.That(request.CombatState, Is.EqualTo(CombatCoreState.AttackActive));
        }
    }
}
