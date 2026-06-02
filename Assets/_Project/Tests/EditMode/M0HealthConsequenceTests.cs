using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.DebugOverlay;
using GlassRefrain.Enemy;
using GlassRefrain.Encounter;
using GlassRefrain.Health;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Targeting;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0HealthConsequenceTests {
        [Test]
        public void DamageConsequence_ReducesHealth_AfterResolvedValidCombatOutcome() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(
                new DamageApplicationContext("CombatCoreConfirmed", "Enemy", 12f, "Slash", "ConfirmedHit"));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(result.AppliedAmount, Is.EqualTo(12f));
            Assert.That(model.Snapshot.Current, Is.EqualTo(88f));
        }

        [Test]
        public void HitReaction_IsEmitted_AfterAcceptedDamageConsequence() {
            var model = new M0HealthDamageReactionModel(100f);

            model.ApplyDamage(
                new DamageApplicationContext("CombatCoreConfirmed", "Enemy", 5f, "Slash", "CounterConfirmed"));

            Assert.That(model.Snapshot.HitReaction.SourceId, Is.EqualTo("CombatCoreConfirmed"));
            Assert.That(model.Snapshot.HitReaction.ReactionLabel, Is.EqualTo("HitReactPlaceholder"));
            Assert.That(model.Snapshot.HitReaction.SuppressionSeconds, Is.GreaterThan(0f));
        }

        [Test]
        public void InvalidOrRejectedCombatOutcome_DoesNotReduceHealth() {
            var model = new M0HealthDamageReactionModel(100f);

            var rejectedResult = model.ApplyDamage(
                new DamageApplicationContext("CombatCore", "Enemy", 10f, "Slash", "Rejected"));

            Assert.That(rejectedResult.Result, Is.EqualTo(DamageApplicationResultType.Rejected));
            Assert.That(rejectedResult.AppliedAmount, Is.EqualTo(0f));
            Assert.That(model.Snapshot.Current, Is.EqualTo(100f));
        }

        [Test]
        public void DamageConsequence_DoesNotMutateCombatCoreDefensiveOwnership() {
            var model = new M0HealthDamageReactionModel(100f);
            var combatCore = new M0CombatCore();

            var before = combatCore.Snapshot;
            model.ApplyDamage(
                new DamageApplicationContext("CombatCoreConfirmed", "Enemy", 9f, "Slash", "ConfirmedHit"));
            var after = combatCore.Snapshot;

            Assert.That(after.State, Is.EqualTo(before.State));
            Assert.That(after.CounterWindow.IsOpen, Is.EqualTo(before.CounterWindow.IsOpen));
            Assert.That(after.LastActionResult.Reason, Is.EqualTo(before.LastActionResult.Reason));
        }

        [Test]
        public void HealthSnapshot_IsObservable_ThroughDebugReadOnlyAggregate_AfterDamage() {
            var health = new M0HealthDamageReactionModel(100f);
            health.ApplyDamage(new DamageApplicationContext("CombatCoreConfirmed", "Enemy", 7f, "Slash", "ConfirmedHit"));

            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var snapshot = aggregator.Capture(
                new M0InputRouter().Snapshot,
                null,
                new M0PlayerLocomotion().Snapshot,
                new M0TargetContext().Snapshot,
                new M0CombatCore().Snapshot,
                new M0EnemyIntentModel().Snapshot,
                health.Snapshot,
                new M0MemoryState().Snapshot,
                new M0MemoryVFXResponse().Snapshot,
                new M0EncounterFramework().Snapshot);

            var healthChannel = (HealthStateSnapshot)snapshot.Health.SourceSnapshot;
            Assert.That(snapshot.Health.ChannelId, Is.EqualTo(DebugOverlayChannelId.Health));
            Assert.That(snapshot.Health.IsVisible, Is.True);
            Assert.That(healthChannel.Current, Is.EqualTo(93f));
            Assert.That(healthChannel.Max, Is.EqualTo(100f));
            Assert.That(snapshot.Health.LastReason, Is.EqualTo("Damage applied"));
        }

        [Test]
        public void HealthConsequenceFiles_DoNotReferenceForbiddenDependencies() {
            string[] files = {
                "Assets/_Project/Code/Health/M0HealthDamageReactionModel.cs",
                "Assets/_Project/Code/Health/GlassRefrain.Health.asmdef",
                "Assets/_Project/Code/Core/M0Contracts.cs"
            };

            string[] forbiddenPatterns = {
                "FindObjectOfType",
                "FindAnyObjectByType",
                "GameObject.Find",
                "Resources.Load",
                "UnityEngine.Debug." + "Log(",
                "UnityEngine.Debug." + "LogWarning(",
                "UnityEngine.Debug." + "LogError(",
                "RegisterGeneratedFor<"
            };

            foreach (var file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);
                var contents = File.ReadAllText(file);
                foreach (var pattern in forbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
            }
        }
    }
}
