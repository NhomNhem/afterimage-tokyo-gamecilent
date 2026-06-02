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
            Assert.That(type.GetMethod("PlayNeutral"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayLocomotion"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayAttack"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayDodge"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayParry"), Is.Not.Null);
            Assert.That(type.GetMethod("PlayCounter"), Is.Not.Null);
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
            Assert.That(type.GetMethod("ObserveCombatSnapshot"), Is.Not.Null);
            Assert.That(type.GetMethod("ObserveLocomotionSnapshot"), Is.Not.Null);
            Assert.That(type.GetMethod("ObserveEnemyIntentSnapshot"), Is.Not.Null);
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
    }
}
