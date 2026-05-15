using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    // Story 1-6: Defensive combat resolution tests.
    // Covers: input routing, parry validation against EnemyIntentSnapshot, dodge recovery context,
    // counter guard, no forbidden dependencies. All tests use pure C# — no MonoBehaviour required.
    public class M0DefensiveResolutionTests {

        // ──────────────────────────────────────────────────────────────────
        // Snapshot helpers
        // ──────────────────────────────────────────────────────────────────

        private static EnemyIntentSnapshot MakeSnapshot(
            EnemyIntentState state,
            string[] tags = null) {
            var tagSet = new EnemyAttackTagSet(tags);
            var attackIntent = new EnemyAttackIntentContext("atk-test", "TestAttack", 0.15f, tagSet);
            var telegraph = new TelegraphStateSnapshot(string.Empty, false, 0f);
            var punishWindow = new EnemyPunishWindowContext(false, 0f, string.Empty);
            return new EnemyIntentSnapshot(state, "enemy-0", state.ToString(), false, 0f, telegraph, attackIntent, punishWindow);
        }

        private static EnemyIntentSnapshot ActiveParryEligible() =>
            MakeSnapshot(EnemyIntentState.Active, new[] { "ParryEligible" });

        private static EnemyIntentSnapshot ActiveEmptyTags() =>
            MakeSnapshot(EnemyIntentState.Active, null);

        private static EnemyIntentSnapshot ActiveNonEligible() =>
            MakeSnapshot(EnemyIntentState.Active, new[] { "DodgePunishable" });

        private static EnemyIntentSnapshot IdleSnapshot() =>
            MakeSnapshot(EnemyIntentState.Idle, null);

        // ──────────────────────────────────────────────────────────────────
        // Input routing tests (tasks 5.2 – 5.5)
        // ──────────────────────────────────────────────────────────────────

        [Test]
        public void ParryIntentRoutesToCombatCoreAsParryAction() {
            var core = new M0CombatCore();
            var result = core.ConsumeDefensiveIntent(CombatActionType.Parry, ActiveParryEligible());
            Assert.That(result.Accepted, Is.True, "Parry intent should be accepted in Neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryStartup));
        }

        [Test]
        public void DodgeIntentRoutesToCombatCoreAsDodgeAction() {
            var core = new M0CombatCore();
            var result = core.ConsumeDefensiveIntent(CombatActionType.Dodge, IdleSnapshot());
            Assert.That(result.Accepted, Is.True, "Dodge intent should be accepted in Neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));
        }

        [Test]
        public void CounterIntentRoutesToCombatCoreAsCounterAction() {
            var core = new M0CombatCore();
            core.OpenCounterWindow("test", 0.5f);
            var result = core.ConsumeDefensiveIntent(CombatActionType.Counter, IdleSnapshot());
            Assert.That(result.Accepted, Is.True, "Counter intent should be accepted when CounterWindow is open");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.CounterActive));
        }

        [Test]
        public void InputDoesNotDecideParryValidity() {
            // Same action type, same caller — result differs based on snapshot state only.
            var core1 = new M0CombatCore();
            var r1 = core1.ConsumeDefensiveIntent(CombatActionType.Parry, ActiveParryEligible());
            Assert.That(r1.Accepted, Is.True);
            core1.AdvanceState("parry active");
            core1.AdvanceState("parry recovery");
            Assert.That(core1.Snapshot.CounterWindow.IsOpen, Is.True, "Valid parry should open window");

            var core2 = new M0CombatCore();
            core2.ConsumeDefensiveIntent(CombatActionType.Parry, IdleSnapshot());
            core2.AdvanceState("parry active");
            core2.AdvanceState("parry recovery");
            Assert.That(core2.Snapshot.CounterWindow.IsOpen, Is.False, "Invalid parry must not open window");
        }

        // ──────────────────────────────────────────────────────────────────
        // Parry validation tests (tasks 5.6 – 5.12)
        // ──────────────────────────────────────────────────────────────────

        [Test]
        public void ParrySucceedsAndOpensCounterWindowWhenEnemyActiveAndParryEligible() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, ActiveParryEligible());
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.CounterWindow));
        }

        [Test]
        public void ParryDoesNotOpenCounterWindowWhenEnemyInTelegraph() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, MakeSnapshot(EnemyIntentState.Telegraph, new[] { "ParryEligible" }));
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryRecovery));
        }

        [Test]
        public void ParryDoesNotOpenCounterWindowWhenEnemyInCommit() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, MakeSnapshot(EnemyIntentState.Commit, new[] { "ParryEligible" }));
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
        }

        [Test]
        public void ParryDoesNotOpenCounterWindowWhenEnemyInRecovery() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, MakeSnapshot(EnemyIntentState.Recovery, new[] { "ParryEligible" }));
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
        }

        [Test]
        public void ParryDoesNotOpenCounterWindowWhenEnemyInIdle() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, IdleSnapshot());
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
        }

        [Test]
        public void ParryAgainstActiveButNonParryEligibleTagsDoesNotOpenCounterWindow() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, ActiveNonEligible());
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
        }

        [Test]
        public void ParryAgainstActiveWithEmptyTagsOpensCounterWindow() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, ActiveEmptyTags());
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);
        }

        // ──────────────────────────────────────────────────────────────────
        // Dodge tests (tasks 5.13 – 5.16)
        // ──────────────────────────────────────────────────────────────────

        [Test]
        public void DodgeTransitionsThroughExpectedStatesViaConsumeDefensiveIntent() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Dodge, IdleSnapshot());
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));

            core.AdvanceState("dodge active");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));

            core.AdvanceState("dodge recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeRecovery));

            core.AdvanceState("neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void DodgeRecoveryContextIsActiveWhenInDodgeRecoveryState() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Dodge, IdleSnapshot());
            core.AdvanceState("dodge active");
            core.AdvanceState("dodge recovery");
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.True);
            Assert.That(core.Snapshot.Recovery.RequestingState, Is.EqualTo(CombatCoreState.DodgeRecovery));
        }

        [Test]
        public void DodgeRecoveryContextIsFalseWhenNeutral() {
            var core = new M0CombatCore();
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.False);
        }

        [Test]
        public void DodgeDoesNotMutateEnemyIntentSnapshot() {
            var snapshot = IdleSnapshot();
            var stateBefore = snapshot.State;
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Dodge, snapshot);
            Assert.That(snapshot.State, Is.EqualTo(stateBefore), "EnemyIntentSnapshot must not be mutated by Combat Core");
        }

        // ──────────────────────────────────────────────────────────────────
        // Counter tests (tasks 5.17 – 5.18)
        // ──────────────────────────────────────────────────────────────────

        [Test]
        public void CounterRejectedWhenCounterWindowIsClosed() {
            var core = new M0CombatCore();
            var result = core.ConsumeDefensiveIntent(CombatActionType.Counter, IdleSnapshot());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Result, Is.EqualTo(CombatActionResult.Rejected));
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void CounterAcceptedWhenCounterWindowIsOpen() {
            var core = new M0CombatCore();
            core.OpenCounterWindow("test", 0.5f);
            var result = core.ConsumeDefensiveIntent(CombatActionType.Counter, IdleSnapshot());
            Assert.That(result.Accepted, Is.True);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.CounterActive));
            // Story 1-6: CounterWindow should close immediately when Counter is consumed.
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False, "CounterWindow should close after Counter consumed");
        }

        [Test]
        public void CounterWindowExpiresAfterDurationWhenTicked() {
            var core = new M0CombatCore();
            core.OpenCounterWindow("test", 0.5f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);
            core.Tick(0.3f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True, "Window should still be open before duration");
            core.Tick(0.3f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False, "Window should close after duration expires");
        }

        [Test]
        public void CounterWindowDoesNotExpireBeforeDuration() {
            var core = new M0CombatCore();
            core.OpenCounterWindow("test", 1.0f);
            core.Tick(0.5f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True, "Window should still be open at 0.5s");
            core.Tick(0.4f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True, "Window should still be open at 0.9s");
        }

        [Test]
        public void CounterRejectedAfterWindowExpired() {
            var core = new M0CombatCore();
            core.OpenCounterWindow("test", 0.1f);
            core.Tick(0.15f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
            var result = core.ConsumeDefensiveIntent(CombatActionType.Counter, IdleSnapshot());
            Assert.That(result.Accepted, Is.False);
            Assert.That(result.Result, Is.EqualTo(CombatActionResult.Rejected));
        }

        [Test]
        public void OpenCounterWindowDoesNotChangeCurrentState() {
            var core = new M0CombatCore();
            core.ConsumeDefensiveIntent(CombatActionType.Parry, MakeSnapshot(EnemyIntentState.Active, new[] { "ParryEligible" }));
            core.AdvanceState("active");
            var stateBefore = core.Snapshot.State;
            core.OpenCounterWindow("test", 0.5f);
            Assert.That(core.Snapshot.State, Is.EqualTo(stateBefore), "OpenCounterWindow should not change state");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);
        }

        // ──────────────────────────────────────────────────────────────────
        // Scope exclusion test (task 5.19)
        // ──────────────────────────────────────────────────────────────────

        [Test]
        public void DefensiveWiringFilesDoNotReferenceForbiddenDependencies() {
            string[] files = {
                "Assets/_Project/Code/Combat/M0CombatCore.cs",
                "Assets/_Project/Code/Input/M0DirectPlayerInput.cs",
                "Assets/_Project/Code/Bootstrap/M0GameplayTickHandler.cs"
            };

            string[] forbiddenPatterns = {
                "NavMesh",
                "Animator",
                "AnimationEvent",
                "ApplyDamage",
                "AudioSource",
                "ParticleSystem",
                "Cinemachine",
                "FindObjectOfType",
                "FindFirstObjectByType",
                "GameObject.Find",
                "Resources.Load",
                "RegisterGeneratedFor",
                "UnityEngine.Input;",
                "Keyboard.current",
                "Mouse.current",
                "Gamepad.current"
            };

            foreach (var file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);
                var contents = File.ReadAllText(file);
                foreach (var pattern in forbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False,
                        file + " contains forbidden pattern: " + pattern);
            }
        }
    }
}
