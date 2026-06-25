using System;
using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class CombatCoreTests {
        [Test]
        public void NeutralAcceptsAllM0CombatRequests() {
            var core = new CombatCore();

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
            }
            core.AdvanceState("back to neutral");

            // Story 1-6: Counter now requires CounterWindow open. Open it manually then test.
            core.OpenCounterWindow("test", 0.5f);
            AssertAccepted(core, CombatActionType.Counter);
        }

        [Test]
        public void RequestsAreRejectedDuringCommittedStates() {
            var core = new CombatCore();
            AssertAccepted(core, CombatActionType.LightAttack);

            var duringStartup = core.RequestAction(CreateRequest(CombatActionType.Dodge));
            Assert.That(duringStartup.Result, Is.EqualTo(CombatActionResult.Rejected));
            Assert.That(duringStartup.Accepted, Is.False);
            Assert.That(duringStartup.StateLabel, Is.EqualTo(CombatCoreState.AttackStartup.ToString()));
        }

        [Test]
        public void AttackCycleTransitionsThroughExpectedStates() {
            var core = new CombatCore();
            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));

            core.AdvanceState("attack active");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackActive));

            core.AdvanceState("attack recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery));

            core.AdvanceState("neutral");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void LightAttackTickProgressionReturnsToNeutralAndAcceptsAgain() {
            var core = new CombatCore();

            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));

            core.Tick(0.2f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackActive));

            core.Tick(0.25f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery));

            core.Tick(0.35f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));

            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));
        }

        [Test]
        public void LockedStatesRejectHeavyAttackDodgeAndParryDuringTickProgression() {
            var core = new CombatCore();

            AssertAccepted(core, CombatActionType.LightAttack);
            AssertRejected(core, CombatActionType.HeavyAttack, CombatCoreState.AttackStartup);

            core.Tick(0.2f);
            AssertRejected(core, CombatActionType.Dodge, CombatCoreState.AttackActive);

            core.Tick(0.25f);
            AssertRejected(core, CombatActionType.Parry, CombatCoreState.AttackRecovery);
        }

        [Test]
        public void DodgeAndParryTickProgressionReturnToNeutral() {
            var core = new CombatCore();

            AssertAccepted(core, CombatActionType.Dodge);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));
            core.Tick(0.15f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));
            core.Tick(0.25f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeRecovery));
            core.Tick(0.35f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));

            AssertAccepted(core, CombatActionType.Parry);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryStartup));
            core.Tick(0.15f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryActive));
            core.Tick(0.25f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryRecovery));
            core.Tick(0.35f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void CounterWindowStillExpiresFromTick() {
            var core = new CombatCore();

            core.OpenCounterWindow("test", 0.5f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);

            core.Tick(0.25f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.True);

            core.Tick(0.25f);
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
        }

        [Test]
        public void DodgeCycleTransitionsThroughExpectedStates() {
            var core = new CombatCore();
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
            var core = new CombatCore();
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
            var core = new CombatCore();
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
        public void RejectedCounterDoesNotEmitRevealRequestContext() {
            var core = new CombatCore();
            var emittedCount = 0;
            core.RevealRequestEmitted += _ => emittedCount++;

            // Counter without an open window must reject and emit nothing.
            var result = core.RequestAction(CreateRequest(CombatActionType.Counter));
            Assert.That(result.Accepted, Is.False);
            Assert.That(emittedCount, Is.EqualTo(0));
        }

        [Test]
        public void SnapshotRemainsConsistentWithInternalState() {
            var core = new CombatCore();

            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));

            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));
        }

        [Test]
        public void CommittedAndRecoveryStatesEmitCorrectCombatStates() {
            var core = new CombatCore();

            AssertAccepted(core, CombatActionType.Dodge);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));

            core.AdvanceState("dodge active");
            core.AdvanceState("dodge recovery");
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeRecovery));
        }

        [Test]
        public void CombatFilesDoNotReferenceLegacyInputManagerOrGeneratedDi() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Combat/CombatCore.cs",
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

        [Test]
        public void ResetForEncounter_ForcesNeutralAndClearsTransients() {
            var core = new CombatCore();
            AssertAccepted(core, CombatActionType.Dodge);
            core.AdvanceState("to active");
            core.OpenCounterWindow("test", 0.5f);

            core.ResetForEncounter("Reset");

            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.Neutral));
            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False);
        }

        [Test]
        public void TickProgression_UsesProvidedTimingSettings() {
            var settings = new M0CombatTimingSettings(
                attackStartupSeconds: 0.30f,
                attackActiveSeconds: 0.40f,
                attackRecoverySeconds: 0.50f,
                dodgeStartupSeconds: 0.22f,
                dodgeActiveSeconds: 0.33f,
                dodgeRecoverySeconds: 0.44f,
                parryStartupSeconds: 0.25f,
                parryActiveSeconds: 0.35f,
                parryRecoverySeconds: 0.45f,
                counterWindowDurationSeconds: 1.0f,
                recoveryDurationSeconds: 0.44f);
            var core = new CombatCore(settings);

            AssertAccepted(core, CombatActionType.Dodge);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup));

            const float startupTick = 0.21f;
            const float startupBoundaryTick = 0.01f;
            const float activeTick = 0.32f;
            const float recoveryBoundaryTick = 0.05f;

            core.Tick(startupTick);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeStartup), "Should still be in startup before configured threshold");

            core.Tick(startupBoundaryTick);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive));

            core.Tick(activeTick);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeActive), "Should still be active before configured threshold");

            // Advance clearly beyond the configured active boundary to avoid edge rounding ambiguity.
            core.Tick(recoveryBoundaryTick);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.DodgeRecovery));
        }

        [Test]
        public void AttackTickProgression_UsesProvidedTimingSettings() {
            var settings = new M0CombatTimingSettings(
                attackStartupSeconds: 0.31f,
                attackActiveSeconds: 0.41f,
                attackRecoverySeconds: 0.51f,
                dodgeStartupSeconds: 0.10f,
                dodgeActiveSeconds: 0.20f,
                dodgeRecoverySeconds: 0.30f,
                parryStartupSeconds: 0.10f,
                parryActiveSeconds: 0.20f,
                parryRecoverySeconds: 0.30f,
                counterWindowDurationSeconds: 1.0f,
                recoveryDurationSeconds: 0.51f);
            var core = new CombatCore(settings);

            AssertAccepted(core, CombatActionType.LightAttack);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup));

            core.Tick(0.30f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackStartup), "Startup should hold until configured threshold");

            core.Tick(0.01f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackActive));

            core.Tick(0.40f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackActive), "Active should hold until configured threshold");

            core.Tick(0.01f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.AttackRecovery));
        }

        [Test]
        public void ParryTickProgression_UsesProvidedTimingSettings_AndPreservesCombatCoreCounterAuthority() {
            var settings = new M0CombatTimingSettings(
                attackStartupSeconds: 0.10f,
                attackActiveSeconds: 0.20f,
                attackRecoverySeconds: 0.30f,
                dodgeStartupSeconds: 0.10f,
                dodgeActiveSeconds: 0.20f,
                dodgeRecoverySeconds: 0.30f,
                parryStartupSeconds: 0.29f,
                parryActiveSeconds: 0.39f,
                parryRecoverySeconds: 0.49f,
                counterWindowDurationSeconds: 1.25f,
                recoveryDurationSeconds: 0.49f);
            var core = new CombatCore(settings);

            AssertAccepted(core, CombatActionType.Parry);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryStartup));

            core.Tick(0.28f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryStartup), "Startup should hold until configured threshold");

            core.Tick(0.01f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryActive));

            core.Tick(0.38f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryActive), "Active should hold until configured threshold");

            core.Tick(0.01f);
            Assert.That(core.Snapshot.State, Is.EqualTo(CombatCoreState.ParryRecovery));

            Assert.That(core.Snapshot.CounterWindow.IsOpen, Is.False, "CounterWindow must remain closed without CombatCore eligibility path");
        }

        [Test]
        public void CombatTimingSettings_RejectsNonPositiveValues() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new M0CombatTimingSettings(
                attackStartupSeconds: 0f,
                attackActiveSeconds: 0.2f,
                attackRecoverySeconds: 0.2f,
                dodgeStartupSeconds: 0.1f,
                dodgeActiveSeconds: 0.2f,
                dodgeRecoverySeconds: 0.2f,
                parryStartupSeconds: 0.1f,
                parryActiveSeconds: 0.2f,
                parryRecoverySeconds: 0.2f,
                counterWindowDurationSeconds: 1f,
                recoveryDurationSeconds: 0.2f));
        }

        private static void AssertAccepted(CombatCore core, CombatActionType actionType) {
            var result = core.RequestAction(CreateRequest(actionType));
            Assert.That(result.Accepted, Is.True, actionType + " should be accepted in Neutral");
            Assert.That(result.Result, Is.EqualTo(CombatActionResult.Accepted));
        }

        private static void AssertRejected(CombatCore core, CombatActionType actionType, CombatCoreState expectedState) {
            var result = core.RequestAction(CreateRequest(actionType));
            Assert.That(result.Accepted, Is.False, actionType + " should be rejected during " + expectedState);
            Assert.That(result.Result, Is.EqualTo(CombatActionResult.Rejected));
            Assert.That(core.Snapshot.State, Is.EqualTo(expectedState));
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
