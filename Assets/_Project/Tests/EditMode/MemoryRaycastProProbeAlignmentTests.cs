using System.IO;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public sealed class MemoryRaycastProProbeAlignmentTests {
        private const string ProbePath = "Assets/_Project/Code/Memory/MemoryRaycastProProbe.cs";

        [Test]
        public void ProbeDebugOutput_ReportsServiceSnapshotAsTruth() {
            string source = File.ReadAllText(ProbePath);

            Assert.That(source, Does.Contain("MemoryInteractionService"));
            Assert.That(source, Does.Contain("_memoryInteractionService.Snapshot"));
            Assert.That(source, Does.Contain("serviceEligible="));
            Assert.That(source, Does.Contain("serviceFragmentId="));
            Assert.That(source, Does.Contain("serviceOutcome="));
            Assert.That(source, Does.Contain("serviceReason="));
        }

        [Test]
        public void ProbeDebugOutput_LabelsRaycastProDataAsSupplementalColliderEvidence() {
            string source = File.ReadAllText(ProbePath);

            Assert.That(source, Does.Contain("RangeDetector"));
            Assert.That(source, Does.Contain("MemoryProbeColliderSnapshot"));
            Assert.That(source, Does.Contain("colliderAvailable="));
            Assert.That(source, Does.Contain("colliderHitName="));
            Assert.That(source, Does.Contain("colliderDistance="));
            Assert.That(source, Does.Contain("colliderLayer="));
            Assert.That(source, Does.Contain("colliderWithinRadius="));
            Assert.That(source, Does.Contain("colliderReason="));
        }

        [Test]
        public void ProbeSource_DoesNotExecuteInteractOrMutateMemoryTruth() {
            string source = File.ReadAllText(ProbePath);

            Assert.That(source, Does.Not.Contain(".Tick("));
            Assert.That(source, Does.Not.Contain("IntakeRevealRequest"));
            Assert.That(source, Does.Not.Contain("EvaluateRequestedReveal"));
            Assert.That(source, Does.Not.Contain("AdvancePhase"));
            Assert.That(source, Does.Not.Contain("TryAppendAcceptedInteraction"));
            Assert.That(source, Does.Not.Contain("UpdateInteractionPrompt"));
            Assert.That(source, Does.Not.Contain("UpdateRuntimeMemoryLog"));
        }

        [Test]
        public void ProbeSource_AvoidsForbiddenLookupAndLoggingApis() {
            string source = File.ReadAllText(ProbePath);

            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
            Assert.That(source, Does.Not.Contain("UnityEngine.Debug"));
            Assert.That(source, Does.Not.Contain("Debug.Log"));
            Assert.That(source, Does.Contain("INhemLogger"));
        }
    }
}
