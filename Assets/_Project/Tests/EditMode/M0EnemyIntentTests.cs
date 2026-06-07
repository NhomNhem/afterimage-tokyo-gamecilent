using System;
using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0EnemyIntentTests {
        [Test]
        public void IdleStateIsDefaultAndReadOnlySnapshotExposed() {
            var model = new M0EnemyIntentModel();

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Idle));
            Assert.That(model.Snapshot.EnemyId, Is.EqualTo("M0Enemy"));
            Assert.That(model.Snapshot.IsTelegraphing, Is.False);
            Assert.That(model.Snapshot.PunishWindow.IsOpen, Is.False);
        }

        [Test]
        public void TelegraphStateUpdatesSnapshot() {
            var model = new M0EnemyIntentModel();

            model.EnterTelegraph("SwingTelegraph", 0.75f, "Telegraphing swing");

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(model.Snapshot.IsTelegraphing, Is.True);
            Assert.That(model.Snapshot.Telegraph.IsActive, Is.True);
            Assert.That(model.Snapshot.Telegraph.TelegraphId, Is.EqualTo("SwingTelegraph"));
            Assert.That(model.Snapshot.RemainingSeconds, Is.EqualTo(0.75f));
        }

        [Test]
        public void ReadabilitySnapshot_ExposesPhaseLabelProgressAndDefensiveAnswer() {
            var model = new M0EnemyIntentModel();

            model.EnterTelegraph("SwingTelegraph", 1.0f, "Telegraph:SwingTelegraph (1.00s)");
            model.Tick(0.25f);

            var telegraphReadability = model.Snapshot.Readability;
            Assert.That(telegraphReadability.Phase, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(telegraphReadability.PhaseLabel, Is.EqualTo("Telegraph:SwingTelegraph (1.00s)"));
            Assert.That(telegraphReadability.RemainingSeconds, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(telegraphReadability.PhaseProgress01, Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(telegraphReadability.DefensiveAnswer, Is.EqualTo("Prepare"));
            Assert.That(telegraphReadability.PunishAvailable, Is.False);
        }

        [Test]
        public void CommitActiveRecoveryFlowMaintainsEnemyOwnership() {
            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible" }));

            model.EnterTelegraph("SlashTelegraph", 0.5f, "Telegraph");
            model.EnterCommit(intent, 0.2f, "Commit");
            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Commit));
            CollectionAssert.AreEquivalent(new[] { "DodgePunishable", "ParryEligible" },
                model.Snapshot.AttackIntent.AttackTags.Tags);

            model.EnterActive(0.1f, "Active");
            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Active));
            Assert.That(model.Snapshot.Readability.DefensiveAnswer, Is.EqualTo("DodgeOrParry"));
            CollectionAssert.AreEquivalent(new[] { "DodgePunishable", "ParryEligible" },
                model.Snapshot.Readability.AttackTags.Tags);

            model.EnterRecovery(0.4f, "Recovery", true, 0.3f, "WhiffPlaceholder");
            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Recovery));
            Assert.That(model.Snapshot.PunishWindow.IsOpen, Is.True);
            Assert.That(model.Snapshot.PunishWindow.Source, Is.EqualTo("WhiffPlaceholder"));
            Assert.That(model.Snapshot.Readability.PunishAvailable, Is.True);
            Assert.That(model.Snapshot.Readability.PunishSource, Is.EqualTo("WhiffPlaceholder"));
            Assert.That(model.Snapshot.Readability.DefensiveAnswer, Is.EqualTo("Counter"));
        }

        [Test]
        public void PunishWindowClosesAfterTickExpiry() {
            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "CounterOnWhiff" }));

            model.EnterCommit(intent, 0.2f, "Commit");
            model.EnterRecovery(0.4f, "Recovery", true, 0.25f, "RecoveryEnd");

            Assert.That(model.Snapshot.PunishWindow.IsOpen, Is.True);
            model.Tick(0.3f);
            Assert.That(model.Snapshot.PunishWindow.IsOpen, Is.False);
            Assert.That(model.Snapshot.PunishWindow.RemainingSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void IdleStateHasEmptyAttackIntent() {
            var model = new M0EnemyIntentModel();

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Idle));
            Assert.That(model.Snapshot.AttackIntent.AttackId, Is.EqualTo(string.Empty));
            Assert.That(model.Snapshot.AttackIntent.AttackTags.Tags, Is.Empty);
        }

        [Test]
        public void TelegraphDoesNotAdvanceStateOnTick() {
            var model = new M0EnemyIntentModel();

            model.EnterTelegraph("TelegraphA", 1.0f, "Test");
            model.Tick(0.4f);

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(model.Snapshot.RemainingSeconds, Is.EqualTo(0.6f).Within(0.001f));
            Assert.That(model.Snapshot.Telegraph.RemainingSeconds, Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void ActiveStatePreservesAttackIntentFromCommit() {
            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashB",
                "M0BasicSlash",
                0.15f,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible", "CounterOnWhiff" }));

            model.EnterCommit(intent, 0.2f, "Commit");
            model.EnterActive(0.15f, "Active");

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Active));
            Assert.That(model.Snapshot.AttackIntent.AttackId, Is.EqualTo("SlashB"));
            CollectionAssert.AreEquivalent(
                new[] { "DodgePunishable", "ParryEligible", "CounterOnWhiff" },
                model.Snapshot.AttackIntent.AttackTags.Tags);
        }

        [Test]
        public void ActiveStateFromIdleHasEmptyAttackIntent() {
            var model = new M0EnemyIntentModel();

            model.EnterActive(0.15f, "Active");

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Active));
            Assert.That(model.Snapshot.AttackIntent.AttackId, Is.EqualTo(string.Empty));
            Assert.That(model.Snapshot.AttackIntent.AttackTags.Tags, Is.Empty);
        }

        [Test]
        public void SnapshotIsReadOnlyValueCopy() {
            var model = new M0EnemyIntentModel();
            model.EnterTelegraph("TestTelegraph", 1.0f, "Test");

            var snapA = model.Snapshot;
            model.Tick(0.5f);
            var snapB = model.Snapshot;

            Assert.That(snapA.RemainingSeconds, Is.EqualTo(1.0f));
            Assert.That(snapB.RemainingSeconds, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(snapA.RemainingSeconds, Is.Not.EqualTo(snapB.RemainingSeconds));
            Assert.That(snapA.Readability.PhaseProgress01, Is.EqualTo(0f));
            Assert.That(snapB.Readability.PhaseProgress01, Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void AttackTagsAreCopiedIntoReadabilitySnapshot() {
            var tags = new[] { "ParryEligible" };
            var tagSet = new EnemyAttackTagSet(tags);
            tags[0] = "MutatedAfterConstruction";

            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashC",
                "M0BasicSlash",
                0.2f,
                tagSet);

            model.EnterCommit(intent, 0.2f, "Commit");
            model.EnterActive(0.2f, "Active");

            var snapshotTags = model.Snapshot.Readability.AttackTags.Tags;
            snapshotTags[0] = "MutatedFromSnapshot";

            Assert.That(model.Snapshot.Readability.AttackTags.Tags[0], Is.EqualTo("ParryEligible"));
            Assert.That(model.Snapshot.Readability.DefensiveAnswer, Is.EqualTo("Parry"));
        }

        [Test]
        public void IntentLabel_PreservesReadablePhaseCueAcrossTransitions() {
            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "ParryEligible" }));

            model.EnterTelegraph("TelegraphA", 0.9f, "Telegraph:TelegraphA (0.90s)");
            Assert.That(model.Snapshot.IntentLabel, Is.EqualTo("Telegraph:TelegraphA (0.90s)"));

            model.EnterCommit(intent, 0.25f, "Commit:SlashA (0.25s)");
            Assert.That(model.Snapshot.IntentLabel, Is.EqualTo("Commit:SlashA (0.25s)"));

            model.EnterActive(0.2f, "Active:SlashA (0.20s)");
            Assert.That(model.Snapshot.IntentLabel, Is.EqualTo("Active:SlashA (0.20s)"));

            model.EnterRecovery(0.65f, "Recovery:RecoveryEnd (0.65s)", true, 0.4f, "RecoveryEnd");
            Assert.That(model.Snapshot.IntentLabel, Is.EqualTo("Recovery:RecoveryEnd (0.65s)"));
        }

        [Test]
        public void EnemyIntentFilesDoNotReferenceForbiddenDependencies() {
            string[] coreAndEnemyFiles = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Enemy/M0EnemyIntentModel.cs",
                "Assets/_Project/Code/Enemy/GlassRefrain.Enemy.asmdef",
                "Assets/_Project/Code/Bootstrap/M0EnemyIntentLoopDriver.cs"
            };

            string[] sharedForbiddenPatterns = {
                "Debug.Log",
                "UnityEngine.Debug",
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<"
            };

            foreach (var file in coreAndEnemyFiles) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                var contents = File.ReadAllText(file);
                foreach (var pattern in sharedForbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
            }

            string[] enemyOnlyForbiddenPatterns = {
                "NavMesh",
                "Animator",
                "AnimationEvent",
                "OnTrigger",
                "OnCollision",
                "ApplyDamage",
                "FindWithTag",
                "GetComponent<",
                "AudioSource",
                "ParticleSystem",
                "Cinemachine"
            };

            string[] enemyOnlyFiles = {
                "Assets/_Project/Code/Enemy/M0EnemyIntentModel.cs",
                "Assets/_Project/Code/Enemy/GlassRefrain.Enemy.asmdef",
                "Assets/_Project/Code/Bootstrap/M0EnemyIntentLoopDriver.cs"
            };

            foreach (var file in enemyOnlyFiles) {
                var contents = File.ReadAllText(file);
                foreach (var pattern in enemyOnlyForbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
            }
        }

        [Test]
        public void ResetForEncounter_ReturnsModelToIdleAndClearsTelegraphAndPunishWindow() {
            var model = new M0EnemyIntentModel();
            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible" }));

            model.EnterCommit(intent, 0.2f, "Commit");
            model.EnterRecovery(0.4f, "Recovery", true, 0.25f, "RecoveryEnd");
            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Recovery));

            model.ResetForEncounter("EncounterReset");

            Assert.That(model.Snapshot.State, Is.EqualTo(EnemyIntentState.Idle));
            Assert.That(model.Snapshot.Telegraph.IsActive, Is.False);
            Assert.That(model.Snapshot.PunishWindow.IsOpen, Is.False);
        }
    }
}
