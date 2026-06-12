using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.DebugOverlay;
using GlassRefrain.Enemy;
using GlassRefrain.Health;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Encounter;
using GlassRefrain.Targeting;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    [TestFixture]
    public sealed class M0DebugOverlaySnapshotIntegrationTests {
        [Test]
        public void AggregateSnapshotIncludesAllRequiredChannelsInOrder() {
            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var snapshot = CreateAggregateSnapshot(aggregator);

            Assert.That(snapshot.ChannelCount, Is.EqualTo(9));
            Assert.That(snapshot.Channels[0].ChannelId, Is.EqualTo(DebugOverlayChannelId.Input));
            Assert.That(snapshot.Channels[1].ChannelId, Is.EqualTo(DebugOverlayChannelId.Locomotion));
            Assert.That(snapshot.Channels[2].ChannelId, Is.EqualTo(DebugOverlayChannelId.TargetContext));
            Assert.That(snapshot.Channels[3].ChannelId, Is.EqualTo(DebugOverlayChannelId.CombatCore));
            Assert.That(snapshot.Channels[4].ChannelId, Is.EqualTo(DebugOverlayChannelId.EnemyIntent));
            Assert.That(snapshot.Channels[5].ChannelId, Is.EqualTo(DebugOverlayChannelId.Health));
            Assert.That(snapshot.Channels[6].ChannelId, Is.EqualTo(DebugOverlayChannelId.MemoryState));
            Assert.That(snapshot.Channels[7].ChannelId, Is.EqualTo(DebugOverlayChannelId.MemoryVFXResponse));
            Assert.That(snapshot.Channels[8].ChannelId, Is.EqualTo(DebugOverlayChannelId.EncounterFramework));
        }

        [Test]
        public void AggregateSnapshotPassesThroughReadOnlyStateAndReasons() {
            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var input = new M0InputRouter();
            input.RecordRoutingOutcome(InputActionIntent.Dodge, InputRoutingDisposition.Rejected, "CombatCore", "Dodge blocked");

            var locomotion = new M0PlayerLocomotion();
            locomotion.SetMovementRestriction(new MovementRestrictionContext(false, true, 1f, "Movement locked"));

            var target = new M0TargetContext();
            target.RequestAcquire(new TargetAcquireRequest("EnemyA", "TestHarness", "Acquire target"));

            var combat = new M0CombatCore();
            combat.RequestAction(new CombatActionRequest(CombatActionType.Dodge, "TestHarness", "Overlay"));

            var enemy = new M0EnemyIntentModel();
            enemy.EnterTelegraph("EnemyAttack01", 1f, "Telegraphing slash");

            var health = new M0HealthDamageReactionModel();
            health.ApplyDamage(new DamageApplicationContext("EnemyA", "Player", 5f, "Slash", "Damage applied",
                DamageApplicationCombatOutcome.ConfirmedHit));

            var memory = new M0MemoryState();
            memory.IntakeRevealRequest(new RevealRequestContext(
                CombatRequestSourceType.CombatCore,
                "CounterToRevealPlaceholder",
                "CombatCore",
                "M0RevealCandidate",
                "Overlay reveal",
                RevealRequestClassification.GenericHit));
            memory.EvaluateRequestedReveal();

            var memoryVfx = new M0MemoryVFXResponse();
            memoryVfx.OnRejectRequest("not_accepted_by_memory_state");

            var encounter = new M0EncounterFramework();
            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerA", "TestHarness", "Player registered"));
            encounter.RegisterEnemy(new EncounterParticipantRegistration("EnemyA", "TestHarness", "Enemy registered"));
            encounter.Prepare("Prepare duel");

            var snapshot = aggregator.Capture(
                input.Snapshot,
                input.RoutingHistory.Count > 0 ? input.RoutingHistory[input.RoutingHistory.Count - 1] : (InputRoutingResult?)null,
                locomotion.Snapshot,
                target.Snapshot,
                combat.Snapshot,
                enemy.Snapshot,
                health.Snapshot,
                memory.Snapshot,
                memoryVfx.Snapshot,
                encounter.Snapshot);

            Assert.That(snapshot.Input.LastReason, Is.EqualTo("Dodge blocked"));
            Assert.That(snapshot.Locomotion.LastReason, Is.EqualTo("Movement locked"));
            Assert.That(snapshot.TargetContext.LastReason, Is.EqualTo("Target not yet valid"));
            Assert.That(snapshot.CombatCore.LastReason, Is.EqualTo("Action accepted"));
            Assert.That(snapshot.EnemyIntent.LastReason, Is.EqualTo("Telegraphing slash"));
            Assert.That(snapshot.EnemyIntent.Snapshot.Readability.Phase, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(snapshot.EnemyIntent.Snapshot.Readability.DefensiveAnswer, Is.EqualTo("Prepare"));
            Assert.That(snapshot.Health.LastReason, Is.EqualTo("Damage applied"));
            Assert.That(snapshot.MemoryState.LastReason, Is.EqualTo("Generic hit requests cannot reveal memory"));
            Assert.That(snapshot.MemoryVFXResponse.LastReason, Is.EqualTo("not_accepted_by_memory_state"));
            Assert.That(snapshot.EncounterFramework.LastReason, Is.EqualTo("Prepare duel"));

            Assert.That(((InputIntentSnapshot)snapshot.Input.SourceSnapshot).InputEnabled, Is.True);
            Assert.That(((LocomotionStateSnapshot)snapshot.Locomotion.SourceSnapshot).MovementRestriction.Source, Is.EqualTo("Movement locked"));
            Assert.That(((TargetContextSnapshot)snapshot.TargetContext.SourceSnapshot).AcquireReason, Is.EqualTo("Acquire target"));
            Assert.That(((M0CombatSnapshot)snapshot.CombatCore.SourceSnapshot).State, Is.EqualTo(CombatCoreState.DodgeStartup));
            Assert.That(((EnemyIntentSnapshot)snapshot.EnemyIntent.SourceSnapshot).State, Is.EqualTo(EnemyIntentState.Telegraph));
            Assert.That(((HealthStateSnapshot)snapshot.Health.SourceSnapshot).Current, Is.EqualTo(95f));
            Assert.That(((MemoryStateSnapshot)snapshot.MemoryState.SourceSnapshot).Phase, Is.EqualTo(MemoryRevealPhase.Rejected));
            Assert.That(((IMemoryVFXResponseSnapshot)snapshot.MemoryVFXResponse.SourceSnapshot).State, Is.EqualTo(MemoryVFXResponseState.Rejected));
            Assert.That(((EncounterLifecycleSnapshot)snapshot.EncounterFramework.SourceSnapshot).State, Is.EqualTo(EncounterLifecycleState.Ready));
        }

        [Test]
        public void ChannelVisibilityTogglesWithoutMutatingSourceSystems() {
            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var input = new M0InputRouter();
            var locomotion = new M0PlayerLocomotion();
            var target = new M0TargetContext();
            var combat = new M0CombatCore();
            var enemy = new M0EnemyIntentModel();
            var health = new M0HealthDamageReactionModel();
            var memory = new M0MemoryState();
            var memoryVfx = new M0MemoryVFXResponse();
            var encounter = new M0EncounterFramework();

            var beforeInput = input.Snapshot;
            var beforeLocomotion = locomotion.Snapshot;
            var beforeTarget = target.Snapshot;
            var beforeCombat = combat.Snapshot;
            var beforeEnemy = enemy.Snapshot;
            var beforeHealth = health.Snapshot;
            var beforeMemory = memory.Snapshot;
            var beforeMemoryVfx = memoryVfx.Snapshot;
            var beforeEncounter = encounter.Snapshot;

            aggregator.SetChannelVisible(DebugOverlayChannelId.MemoryState, false);
            aggregator.ToggleChannelVisibility(DebugOverlayChannelId.TargetContext);

            var snapshot = aggregator.Capture(
                input.Snapshot,
                null,
                locomotion.Snapshot,
                target.Snapshot,
                combat.Snapshot,
                enemy.Snapshot,
                health.Snapshot,
                memory.Snapshot,
                memoryVfx.Snapshot,
                encounter.Snapshot);

            Assert.That(snapshot.MemoryState.IsVisible, Is.False);
            Assert.That(snapshot.TargetContext.IsVisible, Is.False);
            Assert.That(snapshot.Input.IsVisible, Is.True);

            Assert.That(input.Snapshot.Move.X, Is.EqualTo(beforeInput.Move.X));
            Assert.That(locomotion.Snapshot.State, Is.EqualTo(beforeLocomotion.State));
            Assert.That(target.Snapshot.FocusState, Is.EqualTo(beforeTarget.FocusState));
            Assert.That(combat.Snapshot.State, Is.EqualTo(beforeCombat.State));
            Assert.That(enemy.Snapshot.State, Is.EqualTo(beforeEnemy.State));
            Assert.That(health.Snapshot.Current, Is.EqualTo(beforeHealth.Current));
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(beforeMemory.Phase));
            Assert.That(memoryVfx.Snapshot.State, Is.EqualTo(beforeMemoryVfx.State));
            Assert.That(encounter.Snapshot.State, Is.EqualTo(beforeEncounter.State));
        }

        [Test]
        public void EnemyReadabilityPassesThroughWithoutMutatingSourceModel() {
            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var input = new M0InputRouter();
            var locomotion = new M0PlayerLocomotion();
            var target = new M0TargetContext();
            var combat = new M0CombatCore();
            var enemy = new M0EnemyIntentModel();
            var health = new M0HealthDamageReactionModel();
            var memory = new M0MemoryState();
            var memoryVfx = new M0MemoryVFXResponse();
            var encounter = new M0EncounterFramework();

            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible" }));
            enemy.EnterCommit(intent, 0.25f, "Commit:SlashA (0.25s)");
            enemy.EnterActive(0.2f, "Active:SlashA (0.20s)");

            var before = enemy.Snapshot;
            var snapshot = aggregator.Capture(
                input.Snapshot,
                null,
                locomotion.Snapshot,
                target.Snapshot,
                combat.Snapshot,
                enemy.Snapshot,
                health.Snapshot,
                memory.Snapshot,
                memoryVfx.Snapshot,
                encounter.Snapshot);

            Assert.That(snapshot.EnemyIntent.LastReason, Is.EqualTo("Active:SlashA (0.20s)"));
            Assert.That(snapshot.EnemyIntent.Snapshot.Readability.Phase, Is.EqualTo(EnemyIntentState.Active));
            Assert.That(snapshot.EnemyIntent.Snapshot.Readability.DefensiveAnswer, Is.EqualTo("DodgeOrParry"));

            Assert.That(enemy.Snapshot.State, Is.EqualTo(before.State));
            Assert.That(enemy.Snapshot.RemainingSeconds, Is.EqualTo(before.RemainingSeconds));
            CollectionAssert.AreEquivalent(before.Readability.AttackTags.Tags, enemy.Snapshot.Readability.AttackTags.Tags);
        }

        [Test]
        public void EnemyIntentReason_UsesIntentLabelThenPunishSourceThenTelegraphId() {
            var aggregator = new M0DebugOverlaySnapshotAggregator();
            var input = new M0InputRouter();
            var locomotion = new M0PlayerLocomotion();
            var target = new M0TargetContext();
            var combat = new M0CombatCore();
            var health = new M0HealthDamageReactionModel();
            var memory = new M0MemoryState();
            var memoryVfx = new M0MemoryVFXResponse();
            var encounter = new M0EncounterFramework();

            var enemyWithIntentLabel = new M0EnemyIntentModel();
            enemyWithIntentLabel.EnterTelegraph("EnemyAttack01", 1f, "Telegraph:EnemyAttack01 (1.00s)");
            var snapshotWithIntentLabel = aggregator.Capture(
                input.Snapshot, null, locomotion.Snapshot, target.Snapshot, combat.Snapshot,
                enemyWithIntentLabel.Snapshot, health.Snapshot, memory.Snapshot, memoryVfx.Snapshot, encounter.Snapshot);
            Assert.That(snapshotWithIntentLabel.EnemyIntent.LastReason, Is.EqualTo("Telegraph:EnemyAttack01 (1.00s)"));

            var intent = new EnemyAttackIntentContext(
                "SlashA",
                "BasicSlash",
                0.2f,
                new EnemyAttackTagSet(new[] { "CounterOnWhiff" }));
            var enemyWithPunishOnly = new EnemyIntentSnapshot(
                EnemyIntentState.Recovery,
                "M0Enemy",
                string.Empty,
                false,
                0.4f,
                new TelegraphStateSnapshot(string.Empty, false, 0f),
                intent,
                new EnemyPunishWindowContext(true, 0.3f, "RecoveryEnd"));
            var snapshotWithPunishOnly = aggregator.Capture(
                input.Snapshot, null, locomotion.Snapshot, target.Snapshot, combat.Snapshot,
                enemyWithPunishOnly, health.Snapshot, memory.Snapshot, memoryVfx.Snapshot, encounter.Snapshot);
            Assert.That(snapshotWithPunishOnly.EnemyIntent.LastReason, Is.EqualTo("RecoveryEnd"));

            var enemyWithTelegraphOnly = new EnemyIntentSnapshot(
                EnemyIntentState.Telegraph,
                "M0Enemy",
                string.Empty,
                true,
                1f,
                new TelegraphStateSnapshot("EnemyAttack02", true, 1f),
                new EnemyAttackIntentContext(string.Empty, string.Empty, 0f, new EnemyAttackTagSet(new string[0])),
                new EnemyPunishWindowContext(false, 0f, string.Empty));
            var snapshotWithTelegraphOnly = aggregator.Capture(
                input.Snapshot, null, locomotion.Snapshot, target.Snapshot, combat.Snapshot,
                enemyWithTelegraphOnly, health.Snapshot, memory.Snapshot, memoryVfx.Snapshot, encounter.Snapshot);
            Assert.That(snapshotWithTelegraphOnly.EnemyIntent.LastReason, Is.EqualTo("EnemyAttack02"));
        }

        [Test]
        public void DebugOverlaySource_RemainsReadOnlyAndDoesNotLogDirectly() {
            const string sourcePath = "Assets/_Project/Code/DebugOverlay/M0DebugOverlaySnapshotAggregator.cs";
            Assert.That(File.Exists(sourcePath), Is.True);

            var source = File.ReadAllText(sourcePath);
            Assert.That(source, Does.Not.Contain("Debug.Log"));
            Assert.That(source, Does.Not.Contain("UnityEngine.Debug"));
            Assert.That(source, Does.Not.Contain("EnterTelegraph("));
            Assert.That(source, Does.Not.Contain("EnterCommit("));
            Assert.That(source, Does.Not.Contain("EnterActive("));
            Assert.That(source, Does.Not.Contain("EnterRecovery("));
            Assert.That(source, Does.Not.Contain("ClosePunishWindow("));
        }

        private static DebugOverlayAggregateSnapshot CreateAggregateSnapshot(M0DebugOverlaySnapshotAggregator aggregator) {
            var input = new M0InputRouter();
            var locomotion = new M0PlayerLocomotion();
            var target = new M0TargetContext();
            var combat = new M0CombatCore();
            var enemy = new M0EnemyIntentModel();
            var health = new M0HealthDamageReactionModel();
            var memory = new M0MemoryState();
            var memoryVfx = new M0MemoryVFXResponse();
            var encounter = new M0EncounterFramework();

            return aggregator.Capture(
                input.Snapshot,
                null,
                locomotion.Snapshot,
                target.Snapshot,
                combat.Snapshot,
                enemy.Snapshot,
                health.Snapshot,
                memory.Snapshot,
                memoryVfx.Snapshot,
                encounter.Snapshot);
        }
    }
}
