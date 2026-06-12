using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Health;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0HealthCombatContractTests {
        [Test]
        public void ApplyDamage_WhenTypedConfirmedHit_ShouldAcceptDamage() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(new DamageApplicationContext(
                "CombatCoreConfirmed",
                "Enemy",
                15f,
                "Slash",
                "DebugOnlyLabel",
                DamageApplicationCombatOutcome.ConfirmedHit));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(result.AppliedAmount, Is.EqualTo(15f));
            Assert.That(model.Snapshot.Current, Is.EqualTo(85f));
            Assert.That(model.Snapshot.HitReaction.SourceId, Is.EqualTo("CombatCoreConfirmed"));
        }

        [Test]
        public void ApplyDamage_WhenTypedConfirmedCounterHit_ShouldAcceptDamage() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(new DamageApplicationContext(
                "CombatCoreCounter",
                "Enemy",
                20f,
                "CounterSlash",
                "CounterConfirmed",
                DamageApplicationCombatOutcome.ConfirmedCounterHit));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(model.Snapshot.Current, Is.EqualTo(80f));
        }

        [TestCase(DamageApplicationCombatOutcome.Unknown)]
        [TestCase(DamageApplicationCombatOutcome.Blocked)]
        [TestCase(DamageApplicationCombatOutcome.Parried)]
        [TestCase(DamageApplicationCombatOutcome.Whiffed)]
        [TestCase(DamageApplicationCombatOutcome.Rejected)]
        [TestCase(DamageApplicationCombatOutcome.Invalid)]
        public void ApplyDamage_WhenTypedOutcomeIsNotConfirmedHit_ShouldRejectDamage(
            DamageApplicationCombatOutcome combatOutcome) {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(new DamageApplicationContext(
                "CombatCoreOutcome",
                "Enemy",
                15f,
                "Slash",
                "ConfirmedHit",
                combatOutcome));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Rejected));
            Assert.That(result.AppliedAmount, Is.EqualTo(0f));
            Assert.That(model.Snapshot.Current, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyDamage_WhenLabelLooksRejectedButTypedOutcomeIsConfirmed_ShouldAcceptDamage() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(new DamageApplicationContext(
                "CombatCoreConfirmed",
                "Enemy",
                10f,
                "Slash",
                "Rejected",
                DamageApplicationCombatOutcome.ConfirmedHit));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(model.Snapshot.Current, Is.EqualTo(90f));
        }

        [Test]
        public void ApplyDamage_WhenDefaultContextIsUninitialized_ShouldRejectDamage() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(default);

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Rejected));
            Assert.That(model.Snapshot.Current, Is.EqualTo(100f));
        }

        [Test]
        public void ApplyDamage_WhenSourceIdIsMissingDespiteConfirmedOutcome_ShouldRejectDamage() {
            var model = new M0HealthDamageReactionModel(100f);

            var result = model.ApplyDamage(new DamageApplicationContext(
                string.Empty,
                "Enemy",
                10f,
                "Slash",
                "ConfirmedHit",
                DamageApplicationCombatOutcome.ConfirmedHit));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Rejected));
            Assert.That(model.Snapshot.Current, Is.EqualTo(100f));
        }

        [Test]
        public void HealthModelSource_DoesNotUseContextLabelStringMatchingForCombatOutcomeTruth() {
            var source = File.ReadAllText("Assets/_Project/Code/Health/M0HealthDamageReactionModel.cs");

            Assert.That(source.Contains("ContextLabel.Trim"), Is.False);
            Assert.That(source.Contains("StringComparison.OrdinalIgnoreCase"), Is.False);
            Assert.That(source.Contains("IndexOf(\""), Is.False);
            Assert.That(source.Contains(".Equals(\"Rejected\""), Is.False);
            Assert.That(source.Contains(".Equals(\"Invalid\""), Is.False);
            Assert.That(source.Contains(".Equals(\"Blocked\""), Is.False);
        }
    }
}
