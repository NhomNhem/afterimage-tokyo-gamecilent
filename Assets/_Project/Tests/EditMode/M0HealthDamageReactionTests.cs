using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Health;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0HealthDamageReactionTests {
        [Test]
        public void DamageRequestResultAcceptedForValidDamage() {
            var model = new M0HealthDamageReactionModel(100f);
            var result = model.ApplyDamage(
                new DamageApplicationContext("CombatCoreConfirmed", "Player", 25f, "Basic", "ConfirmedHit"));

            Assert.That(result.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(result.Accepted, Is.True);
            Assert.That(model.Snapshot.Current, Is.EqualTo(75f));
        }

        [Test]
        public void HealthSnapshotReflectsLatestDamageResult() {
            var model = new M0HealthDamageReactionModel(80f);
            model.ApplyDamage(new DamageApplicationContext("Enemy", "Player", 10f, "Basic", "Hit"));

            Assert.That(model.Snapshot.LastDamageResult.Result, Is.EqualTo(DamageApplicationResultType.Accepted));
            Assert.That(model.Snapshot.LastDamageResult.AppliedAmount, Is.EqualTo(10f));
            Assert.That(model.Snapshot.State, Is.EqualTo(HealthState.Damaged));
        }

        [Test]
        public void HitReactionPlaceholderIsSetAfterAcceptedDamage() {
            var model = new M0HealthDamageReactionModel();
            model.ApplyDamage(new DamageApplicationContext("EnemyStrike", "Player", 5f, "Basic", "Hit"));

            Assert.That(model.Snapshot.HitReaction.SourceId, Is.EqualTo("EnemyStrike"));
            Assert.That(model.Snapshot.HitReaction.ReactionLabel, Is.EqualTo("HitReactPlaceholder"));
            Assert.That(model.Snapshot.HitReaction.SuppressionSeconds, Is.EqualTo(0.2f));
        }

        [Test]
        public void DefeatedStateTriggeredWhenHealthReachesZero() {
            var model = new M0HealthDamageReactionModel(20f);
            model.ApplyDamage(new DamageApplicationContext("Enemy", "Player", 20f, "Basic", "Lethal"));

            Assert.That(model.Snapshot.Defeat.IsDefeated, Is.True);
            Assert.That(model.Snapshot.State, Is.EqualTo(HealthState.Disabled));
            Assert.That(model.Snapshot.IsAlive, Is.False);
        }

        [Test]
        public void HealthFilesDoNotReferenceForbiddenDependencies() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Health/M0HealthDamageReactionModel.cs",
                "Assets/_Project/Code/Health/GlassRefrain.Health.asmdef"
            };

            string[] forbiddenPatterns = {
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<",
                "NhemDangFugBixs.Attributes",
                "OnTrigger",
                "OnCollision",
                "Animator",
                "AnimationEvent",
                "Rigidbody",
                "AddForce",
                "Armor",
                "Resistance",
                "DamageNumber",
                "NavMesh",
                "FindWithTag",
                "GetComponent<"
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