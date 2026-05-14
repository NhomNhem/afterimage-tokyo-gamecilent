using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Health;
using GlassRefrain.Memory;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0MemoryStateTests {
        [Test]
        public void RevealFlowTransitionsFromDormantToRequestedAcceptedRespondingCooldown() {
            M0MemoryState memory = new M0MemoryState("M0RevealCandidate");
            memory.IntakeRevealRequest(CreateValidRequest());
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Requested));

            RevealRequestResult evaluation = memory.EvaluateRequestedReveal();
            Assert.That(evaluation.Decision, Is.EqualTo(RevealRequestDecision.Accepted));
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Accepted));

            memory.AdvancePhase("Begin response");
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Responding));
            Assert.That(memory.Snapshot.Response.ResponseActive, Is.True);

            memory.AdvancePhase("Enter cooldown", 0.5f);
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Cooldown));
            Assert.That(memory.Snapshot.Cooldown.CooldownActive, Is.True);
            Assert.That(memory.Snapshot.Cooldown.RemainingSeconds, Is.EqualTo(0.5f));

            memory.AdvancePhase("Cooldown complete");
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Dormant));
            Assert.That(memory.Snapshot.Cooldown.CooldownActive, Is.False);
        }

        [Test]
        public void RejectedRequestRetainsExplicitResultContextThenReturnsToStableState() {
            M0MemoryState memory = new M0MemoryState("M0RevealCandidate");
            memory.IntakeRevealRequest(CreateRequest(RevealRequestClassification.GenericHit, "Generic hit"));

            RevealRequestResult evaluation = memory.EvaluateRequestedReveal();
            Assert.That(evaluation.Decision, Is.EqualTo(RevealRequestDecision.Rejected));
            Assert.That(evaluation.Reason, Does.Contain("Generic hit"));
            Assert.That(evaluation.ResultContext, Is.EqualTo("Generic hit"));
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Rejected));

            memory.AdvancePhase("Rejected settled");
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Dormant));
        }

        [TestCase(RevealRequestClassification.GenericHit)]
        [TestCase(RevealRequestClassification.FailedDodge)]
        [TestCase(RevealRequestClassification.FailedParry)]
        [TestCase(RevealRequestClassification.InvalidCounter)]
        [TestCase(RevealRequestClassification.PresentationOnly)]
        public void InvalidRequestClassificationsAreRejected(RevealRequestClassification classification) {
            M0MemoryState memory = new M0MemoryState("M0RevealCandidate");
            memory.IntakeRevealRequest(CreateRequest(classification, classification.ToString()));

            RevealRequestResult evaluation = memory.EvaluateRequestedReveal();
            Assert.That(evaluation.Decision, Is.EqualTo(RevealRequestDecision.Rejected));
            Assert.That(memory.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Rejected));
        }

        [Test]
        public void SnapshotIsReadOnlyValueAndTracksPhaseAndResultConsistency() {
            M0MemoryState memory = new M0MemoryState("M0RevealCandidate");
            MemoryStateSnapshot initial = memory.Snapshot;

            memory.IntakeRevealRequest(CreateValidRequest());
            RevealRequestResult evaluation = memory.EvaluateRequestedReveal();
            MemoryStateSnapshot current = memory.Snapshot;

            Assert.That(initial.Phase, Is.EqualTo(MemoryRevealPhase.Dormant));
            Assert.That(current.Phase, Is.EqualTo(MemoryRevealPhase.Accepted));
            Assert.That(current.LastResult.Decision, Is.EqualTo(RevealRequestDecision.Accepted));
            Assert.That(evaluation.Accepted, Is.True);
        }

        [Test]
        public void CombatCoreAndHealthRemainRevealContextProvidersOnly() {
            M0CombatCore combatCore = new M0CombatCore();
            M0HealthDamageReactionModel health = new M0HealthDamageReactionModel();

            Assert.That(combatCore.LastRevealRequestContext.RequestSourceType, Is.EqualTo(CombatRequestSourceType.Unknown));
            Assert.That(health.Snapshot.LastDamageResult.Accepted, Is.False);

            string combatSource = File.ReadAllText("Assets/_Project/Code/Combat/M0CombatCore.cs");
            string healthSource = File.ReadAllText("Assets/_Project/Code/Health/M0HealthDamageReactionModel.cs");
            Assert.That(combatSource.Contains("EvaluateRequestedReveal"), Is.False);
            Assert.That(combatSource.Contains("RevealRequestDecision"), Is.False);
            Assert.That(healthSource.Contains("EvaluateRequestedReveal"), Is.False);
            Assert.That(healthSource.Contains("RevealRequestDecision"), Is.False);
        }

        [Test]
        public void MemorySkeletonFilesDoNotReferenceLegacyInputOrGeneratedDi() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Memory/M0MemoryState.cs",
                "Assets/_Project/Code/Memory/GlassRefrain.Memory.asmdef"
            };
            string[] forbiddenPatterns = {
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<",
                "NhemDangFugBixs.Attributes"
            };

            AssertFilesDoNotContain(files, forbiddenPatterns);
        }

        [Test]
        public void MemorySkeletonFilesDoNotContainOutOfScopeSubsystems() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Memory/M0MemoryState.cs",
                "Assets/_Project/Code/Memory/GlassRefrain.Memory.asmdef",
                "Assets/_Project/Code/Combat/M0CombatCore.cs",
                "Assets/_Project/Code/Health/M0HealthDamageReactionModel.cs"
            };
            string[] forbiddenPatterns = {
                "MemoryVfx",
                "NarrativeGraph",
                "ClueDatabase",
                "BranchingMemory",
                "DistrictReinterpretation",
                "Persistence",
                "SaveData",
                "Cutscene",
                "ApplyDamageValidation",
                "SceneManager.LoadScene",
                "Instantiate(",
                "GetComponent<"
            };

            AssertFilesDoNotContain(files, forbiddenPatterns);
        }

        [Test]
        public void VContainerScopeRemainsManualWiring() {
            string scopeSource = File.ReadAllText("Assets/_Project/Code/Bootstrap/ProjectRootLifetimeScope.cs");
            Assert.That(scopeSource.Contains("Manual VContainer skeleton"), Is.True);
            Assert.That(scopeSource.Contains("RegisterGeneratedFor<"), Is.False);
            Assert.That(scopeSource.Contains("NhemDangFugBixs.Attributes"), Is.False);
        }

        private static RevealRequestContext CreateValidRequest() {
            return CreateRequest(RevealRequestClassification.CounterConfirmed, "Counter confirmed");
        }

        private static RevealRequestContext CreateRequest(RevealRequestClassification classification, string contextLabel) {
            return new RevealRequestContext(
                CombatRequestSourceType.TestHarness,
                classification.ToString(),
                "EditModeTests",
                "M0RevealCandidate",
                contextLabel,
                classification);
        }

        private static void AssertFilesDoNotContain(string[] files, string[] forbiddenPatterns) {
            foreach (string file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);
                string contents = File.ReadAllText(file);
                foreach (string pattern in forbiddenPatterns) {
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
                }
            }
        }
    }
}
