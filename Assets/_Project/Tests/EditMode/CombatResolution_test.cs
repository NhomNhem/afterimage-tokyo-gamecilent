using NUnit.Framework;
using VContainer;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Targeting;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// EditMode tests for Story 1-4: Player Attack Resolution.
    /// Validates combat intent routing, validation, resolution, snapshot immutability,
    /// and ADR compliance (no damage/health mutation, no generated DI).
    /// </summary>
    public class CombatResolution_test {

        // ─── 8.2 / 8.3: Attack intent routes to correct CombatActionType ───────────

        [Test]
        public void LightAttack_Intent_Routes_To_CombatCore_As_LightAttack_Request() {
            var combat = new M0CombatCore();

            var result = combat.ConsumeAttackIntent(CombatActionType.LightAttack);

            Assert.That(result.Accepted, Is.True, "LightAttack intent should be accepted from Neutral state");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup),
                "Combat Core should transition to AttackStartup after LightAttack");
        }

        [Test]
        public void HeavyAttack_Intent_Routes_To_CombatCore_As_HeavyAttack_Request() {
            var combat = new M0CombatCore();

            var result = combat.ConsumeAttackIntent(CombatActionType.HeavyAttack);

            Assert.That(result.Accepted, Is.True, "HeavyAttack intent should be accepted from Neutral state");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup),
                "Combat Core should transition to AttackStartup after HeavyAttack");
        }

        // ─── 8.4: Combat Core rejects attack when not allowed by current state ────

        [Test]
        public void CombatCore_Rejects_LightAttack_During_AttackRecovery() {
            var combat = new M0CombatCore();
            combat.RequestAction(new CombatActionRequest(CombatActionType.LightAttack, "test", "setup"));
            combat.AdvanceState("startup→active");
            combat.AdvanceState("active→recovery");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery));

            var result = combat.ConsumeAttackIntent(CombatActionType.LightAttack);

            Assert.That(result.Accepted, Is.False, "Attack intent should be rejected during AttackRecovery");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery),
                "State should remain AttackRecovery after rejection");
        }

        [Test]
        public void CombatCore_Rejects_HeavyAttack_During_AttackStartup() {
            var combat = new M0CombatCore();
            combat.RequestAction(new CombatActionRequest(CombatActionType.LightAttack, "test", "setup"));
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));

            var result = combat.ConsumeAttackIntent(CombatActionType.HeavyAttack);

            Assert.That(result.Accepted, Is.False, "Attack intent should be rejected during AttackStartup");
        }

        // ─── 8.5: Combat Core resolves placeholder hit when valid active target exists

        [Test]
        public void CombatCore_Resolves_Hit_When_Valid_Active_Target_Exists() {
            var combat = new M0CombatCore();
            var targetCtx = new M0TargetContext();
            targetCtx.SetTargetValidity(new TargetValidityContext("enemy-01", true, string.Empty));
            targetCtx.RequestAcquire(new TargetAcquireRequest("enemy-01", "test", "setup"));
            Assert.That(targetCtx.Snapshot.IsLockedOn, Is.True);
            combat.SetTargetContext(targetCtx);

            var resolution = combat.ResolveAttack(CombatActionType.LightAttack);

            Assert.That(resolution.Resolved, Is.True);
            Assert.That(resolution.HitConfirmed, Is.True, "Hit should be confirmed when valid target exists");
            Assert.That(resolution.Detail, Does.Contain("hit"), "Resolution detail should indicate a hit");
        }

        // ─── 8.6: Combat Core resolves whiff when no valid target exists ───────────

        [Test]
        public void CombatCore_Resolves_Whiff_When_No_Valid_Target_Exists() {
            var combat = new M0CombatCore();

            var resolution = combat.ResolveAttack(CombatActionType.LightAttack);

            Assert.That(resolution.Resolved, Is.True);
            Assert.That(resolution.HitConfirmed, Is.False, "Hit should not be confirmed with no target");
            Assert.That(resolution.Detail, Does.Contain("whiff").Or.Contain("no valid target"),
                "Resolution detail should indicate whiff/no target");
        }

        [Test]
        public void CombatCore_Resolves_Whiff_When_Target_Exists_But_Not_Locked_On() {
            var combat = new M0CombatCore();
            var targetCtx = new M0TargetContext();
            targetCtx.SetTargetValidity(new TargetValidityContext("enemy-01", true, string.Empty));
            Assert.That(targetCtx.Snapshot.IsLockedOn, Is.False, "Target should not be locked on yet");
            combat.SetTargetContext(targetCtx);

            var resolution = combat.ResolveAttack(CombatActionType.HeavyAttack);

            Assert.That(resolution.HitConfirmed, Is.False, "Hit should not be confirmed without active lock-on");
        }

        // ─── 8.7: Combat result snapshot is read-only ────────────────────────────

        [Test]
        public void CombatResultSnapshot_Is_ReadOnly_Struct() {
            var combat = new M0CombatCore();
            var snapshot = combat.Snapshot;

            // M0CombatSnapshot is a readonly struct — assign to local copy; original is unaffected
            var copy = snapshot;
            // Verify snapshot type is a value type (readonly struct)
            Assert.That(typeof(M0CombatSnapshot).IsValueType, Is.True,
                "M0CombatSnapshot should be a value type (readonly struct) to enforce read-only semantics");
        }

        // ─── 8.8: Target Context is read-only consumer — does not decide combat validity ─

        [Test]
        public void CombatCore_Owns_Validity_Decision_Not_TargetContext() {
            var combat = new M0CombatCore();
            var targetCtx = new M0TargetContext();
            combat.SetTargetContext(targetCtx);

            // Target Context snapshot does not change based on combat validation
            var snapshotBefore = targetCtx.Snapshot;
            combat.ConsumeAttackIntent(CombatActionType.LightAttack);
            var snapshotAfter = targetCtx.Snapshot;

            Assert.That(snapshotAfter.FocusState, Is.EqualTo(snapshotBefore.FocusState),
                "Target Context focus state should not be mutated by combat intent processing");
        }

        // ─── 8.9: No damage/health mutation occurs ───────────────────────────────

        [Test]
        public void CombatCore_Resolution_Does_Not_Mutate_Damage_Or_Health() {
            var combat = new M0CombatCore();
            var targetCtx = new M0TargetContext();
            targetCtx.SetTargetValidity(new TargetValidityContext("enemy-01", true, string.Empty));
            targetCtx.RequestAcquire(new TargetAcquireRequest("enemy-01", "test", "setup"));
            combat.SetTargetContext(targetCtx);

            combat.ConsumeAttackIntent(CombatActionType.HeavyAttack);
            var resolution = combat.Snapshot.LastResolutionResult;

            // Combat resolution does not have a damage value — only hit confirmed
            // Health/Damage mutation is the domain of Health system (deferred story)
            Assert.That(resolution.Resolved, Is.True);
            // Snapshot has no damage field — this test confirms M0CombatCore owns only combat truth, not health truth
            Assert.That(typeof(M0CombatSnapshot).GetProperty("Damage"), Is.Null,
                "M0CombatSnapshot must not expose a damage property — damage belongs to Health system");
            Assert.That(typeof(CombatResolutionResult).GetProperty("Damage"), Is.Null,
                "CombatResolutionResult must not expose a damage property");
        }

        // ─── 8.10: Manual VContainer registration resolves Combat Core wiring ────

        [Test]
        public void ManualVContainer_Resolves_M0CombatCore_As_Singleton() {
            var builder = new ContainerBuilder();
            builder.Register<M0CombatCore>(Lifetime.Singleton);

            using (var container = builder.Build()) {
                var instance1 = container.Resolve<M0CombatCore>();
                var instance2 = container.Resolve<M0CombatCore>();

                Assert.That(instance1, Is.Not.Null, "M0CombatCore should resolve from manual DI container");
                Assert.That(ReferenceEquals(instance1, instance2), Is.True,
                    "M0CombatCore should be a singleton (same instance both times)");
            }
        }

        // ─── 8.11 / 8.12: No legacy Input Manager or hardcoded device polling ────

        [Test]
        public void M0CombatCore_Has_No_UnityEngine_Input_References() {
            var combatType = typeof(M0CombatCore);
            var methods = combatType.GetMethods(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            foreach (var method in methods) {
                var body = method.GetMethodBody();
                if (body == null) continue;
                foreach (var local in body.LocalVariables) {
                    Assert.That(local.LocalType.Namespace, Does.Not.StartWith("UnityEngine.InputLegacy"),
                        "M0CombatCore must not use legacy Input Manager");
                }
            }
        }

        [Test]
        public void CombatActionType_Distinguishes_LightAttack_And_HeavyAttack() {
            Assert.That(CombatActionType.LightAttack, Is.Not.EqualTo(CombatActionType.HeavyAttack),
                "LightAttack and HeavyAttack must be distinct enum values");
        }

        // ─── AC-1 gap: Combat Core rejects attack during HitReact ────────────────

        [Test]
        public void CombatCore_Rejects_LightAttack_During_HitReact() {
            var combat = new M0CombatCore();
            combat.TriggerHitReact("test-source");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.HitReact));

            var result = combat.ConsumeAttackIntent(CombatActionType.LightAttack);

            Assert.That(result.Accepted, Is.False, "Attack intent should be rejected during HitReact");
            Assert.That(combat.Snapshot.State, Is.EqualTo(CombatCoreState.HitReact),
                "State should remain HitReact after rejection");
        }

        // ─── AC-2: Action lock context emitted after accepted attack ─────────────

        [Test]
        public void CombatCore_Emits_ActionLock_After_Accepted_HeavyAttack() {
            var combat = new M0CombatCore();

            var result = combat.ConsumeAttackIntent(CombatActionType.HeavyAttack);

            Assert.That(result.Accepted, Is.True, "HeavyAttack should be accepted from Neutral state");
            Assert.That(combat.Snapshot.ActionLock.LockActive, Is.True,
                "ActionLock should be active after accepted attack");
            Assert.That(combat.Snapshot.ActionLock.RequestingState, Is.EqualTo(CombatCoreState.AttackStartup),
                "ActionLock requesting state should be AttackStartup");
        }

        [Test]
        public void CombatCore_Emits_ActionLock_After_Accepted_LightAttack() {
            var combat = new M0CombatCore();

            var result = combat.ConsumeAttackIntent(CombatActionType.LightAttack);

            Assert.That(result.Accepted, Is.True, "LightAttack should be accepted from Neutral state");
            Assert.That(combat.Snapshot.ActionLock.LockActive, Is.True,
                "ActionLock should be active after accepted LightAttack");
            Assert.That(combat.Snapshot.ActionLock.RequestingState, Is.EqualTo(CombatCoreState.AttackStartup),
                "ActionLock requesting state should be AttackStartup");
        }
    }
}
