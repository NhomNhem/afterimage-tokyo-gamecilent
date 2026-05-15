using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0CombatCoreTests {
        [Test]
        public void NeutralAcceptsAllM0CombatRequests() {
            var core = new M0CombatCore();

            AssertAccepted(core, CombatActionType.LightAttack);
            core.AdvanceState("attack active");
            core.AdvanceState("attack recovery");
            core.AdvanceState("back to neutral");

            AssertAccepted(core, CombatActionType.HeavyAttack);
            core.AdvanceState("attack active");
            core.AdvanceState("attack recovery");
            core.AdvanceState("back to neutral");

            AssertAccepted(core, CombatActionType.Dodge);
            core.AdvanceState("dodge active");
            core.AdvanceState("dodge recovery");
            core.AdvanceState("back to neutral");

            AssertAccepted(core, CombatActionType.Parry);
            core.AdvanceState("parry active");
            core.AdvanceState("parry recovery");
            // Story 1-6: Parry via RequestAction does not set parryWasEligible, so CounterWindow stays closed.
            // Advance through CounterWindow state if open, otherwise just back to Neutral.
            if (core.Snapshot.CounterWindow.IsOpen) {
                core.AdvanceState("window");
                core.AdvanceState("back to neutral");
            }

            // Story 1-6: Counter now requires CounterWindow open. Open it manually then test.
            core.OpenCounterWindow("test", 0.5f);
            AssertAccepted(core, CombatActionType.Counter);
        }

        [Test]
        public void RequestsAreRejectedDuringCommittedStates() {
            var core = new M0CombatCore();
            AssertAccepted(core, CombatActionType.LightAttack);

            var duringStartup = core.RequestAction(CreateRequest(CombatActionType.Dodge));
            Assert.That(duringStartup.Result, Is.EqualTo(CombatActionResult.Rejected));
            Assert.That(duringStartup.Accepted, Is.False);
            Assert.That(duringStartup.StateLabel, Is.EqualTo(CombatCoreState.AttackStartup.ToString()));
        }

        [Test]
        public void AttackCycleTransitionsThroughExpectedStates() {
            var core = new M0CombatCore();
            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));

            core.AdvanceState("attack active");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackActive));

            core.AdvanceState("attack recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery));
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.True);

            core.AdvanceState("neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void DodgeCycleTransitionsThroughExpectedStates() {
            var core = new M0CombatCore();
            AssertAccepted(core, CombatActionType.Dodge);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));

            core.AdvanceState("dodge active");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));

            core.AdvanceState("dodge recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeRecovery));

            core.AdvanceState("neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void ParryCycleViaRequestActionDoesNotOpenCounterWindow() {
            // Story 1-6: RequestAction(Parry) no longer sets parryWasEligible — CounterWindow stays closed.
            // Valid parry via ConsumeDefensiveIntent is tested in M0DefensiveResolutionTests.
            var core = new M0CombatCore();
            AssertAccepted(core, CombatActionType.Parry);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryStartup));

            core.AdvanceState("parry active");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryActive));

            core.AdvanceState("parry recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryRecovery));
            // Window must NOT open when parryWasEligible was never set.
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);

            core.AdvanceState("back to neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void CounterPathEmitsRevealRequestContext() {
            var core = new M0CombatCore();
            RevealRequestContext emitted = default;
            var wasEmitted = false;
            core.RevealRequestEmitted += context => {
                emitted = context;
                wasEmitted = true;
            };

            // Story 1-6: Counter requires CounterWindow open. OpenCounterWindow now keeps state in Neutral.
            core.OpenCounterWindow("test", 0.5f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral), "OpenCounterWindow should not change state");
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True, "CounterWindow should be open");
            
            AssertAccepted(core, CombatActionType.Counter);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.CounterActive));

            core.AdvanceState("counter resolve");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.RevealBeat));
            Assert.That(wasEmitted, Is.True);
            Assert.That(emitted.RequestSourceType, Is.EqualTo(CombatRequestSourceType.CombatCore));
            Assert.That(emitted.CombatResultSourceLabel, Is.EqualTo("CounterToRevealPlaceholder"));
        }

        [Test]
        public void SnapshotRemainsConsistentWithInternalState() {
            var core = new M0CombatCore();

            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
            Assert.That(core.Snapshot.ActionLock.LockActive, Is.False);
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.False);

            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.ActionLock.LockActive, Is.True);
            Assert.That(core.Snapshot.ActionLock.RequestingState, Is.EqualTo(CombatCoreState.AttackStartup));
        }

        [Test]
        public void CommittedAndRecoveryStatesEmitLockAndRecoveryContexts() {
            var core = new M0CombatCore();

            AssertAccepted(core, CombatActionType.Dodge);
            Assert.That(core.Snapshot.ActionLock.LockActive, Is.True);
            Assert.That(core.Snapshot.ActionLock.RequestingState, Is.EqualTo(CombatCoreState.DodgeStartup));
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.False);

            core.AdvanceState("dodge active");
            core.AdvanceState("dodge recovery");
            Assert.That(core.Snapshot.Recovery.RecoveryActive, Is.True);
            Assert.That(core.Snapshot.Recovery.RequestingState, Is.EqualTo(CombatCoreState.DodgeRecovery));
        }

        [Test]
        public void CombatFilesDoNotReferenceLegacyInputManagerOrGeneratedDi() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Combat/M0CombatCore.cs",
                "Assets/_Project/Code/Combat/GlassRefrain.Combat.asmdef"
            };

            string[] forbiddenPatterns = {
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<",
                "NhemDangFugBixs.Attributes"
            };

            foreach (var file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                var contents = File.ReadAllText(file);
                foreach (var pattern in forbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
            }
        }

        private static void AssertAccepted(M0CombatCore core, CombatActionType actionType) {
            var result = core.RequestAction(CreateRequest(actionType));
            Assert.That(result.Accepted, Is.True, actionType + " should be accepted in Neutral");
            Assert.That(result.Result, Is.EqualTo(CombatActionResult.Accepted));
        }

        private static CombatActionRequest CreateRequest(CombatActionType actionType) {
            return new CombatActionRequest(
                actionType,
                1f,
                CombatRequestSourceType.TestHarness,
                "EditModeTests",
                "Test request");
        }
    }
}