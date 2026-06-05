using System.IO;
using GlassRefrain.Bootstrap;
using GlassRefrain.Core;
using GlassRefrain.Memory;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public sealed class M1RuntimeMemoryLogPlaceholderTests {
        private const string RuntimeLogPath = "Assets/_Project/Code/Bootstrap/M1RuntimeMemoryLogPlaceholder.cs";
        private const string OverlayAdapterPath = "Assets/_Project/Code/Presentation/M0CombatDebugOverlayAdapter.cs";
        private const string TickHandlerPath = "Assets/_Project/Code/Bootstrap/M0GameplayTickHandler.cs";
        private const string UxmlPath = "Assets/_Project/Content/UI/CombatDebugOverlay.uxml";
        private const string UssPath = "Assets/_Project/Content/UI/CombatDebugOverlay.uss";

        [Test]
        public void TryAppendAcceptedInteraction_WhenAccepted_ShouldAppendExactlyOneEntry() {
            var log = new M1RuntimeMemoryLogPlaceholder();
            var interactionSnapshot = CreateInteractionSnapshot(MemoryInteractOutcome.Accepted);
            var memorySnapshot = CreateAcceptedMemoryState("memory-fragment").Snapshot;

            bool appended = log.TryAppendAcceptedInteraction(interactionSnapshot, memorySnapshot);

            Assert.That(appended, Is.True);
            Assert.That(log.Entries, Has.Count.EqualTo(1));
            Assert.That(log.Entries[0], Is.EqualTo("memory-fragment: Revealed"));
        }

        [Test]
        public void TryAppendAcceptedInteraction_WhenNotAcceptedOrRejectedByMemoryState_ShouldAppendNoEntries() {
            var log = new M1RuntimeMemoryLogPlaceholder();
            var acceptedMemorySnapshot = CreateAcceptedMemoryState("memory-fragment").Snapshot;

            Assert.That(log.TryAppendAcceptedInteraction(
                CreateInteractionSnapshot(MemoryInteractOutcome.NoEligibleFragment),
                acceptedMemorySnapshot), Is.False);
            Assert.That(log.TryAppendAcceptedInteraction(
                CreateInteractionSnapshot(MemoryInteractOutcome.DuplicateIgnored),
                acceptedMemorySnapshot), Is.False);

            var rejectedMemorySnapshot = CreateRejectedMemoryState("memory-fragment").Snapshot;
            Assert.That(log.TryAppendAcceptedInteraction(
                CreateInteractionSnapshot(MemoryInteractOutcome.Accepted),
                rejectedMemorySnapshot), Is.False);

            Assert.That(log.Entries, Is.Empty);
        }

        [Test]
        public void TryAppendAcceptedInteraction_WhenSameAcceptedOutcomeRepeats_ShouldSuppressDuplicateEntry() {
            var log = new M1RuntimeMemoryLogPlaceholder();
            var interactionSnapshot = CreateInteractionSnapshot(MemoryInteractOutcome.Accepted);
            var memorySnapshot = CreateAcceptedMemoryState("memory-fragment").Snapshot;

            bool firstAppend = log.TryAppendAcceptedInteraction(interactionSnapshot, memorySnapshot);
            bool secondAppend = log.TryAppendAcceptedInteraction(interactionSnapshot, memorySnapshot);

            Assert.That(firstAppend, Is.True);
            Assert.That(secondAppend, Is.False);
            Assert.That(log.Entries, Has.Count.EqualTo(1));
        }

        [Test]
        public void TryAppendAcceptedInteraction_WhenDisplayDataMissing_ShouldUseFallbackLabel() {
            var log = new M1RuntimeMemoryLogPlaceholder();
            var interactionSnapshot = new MemoryInteractionSnapshot(
                string.Empty,
                true,
                true,
                MemoryInteractOutcome.Accepted,
                "Reveal accepted by MemoryState");
            var memorySnapshot = new MemoryStateSnapshot(
                string.Empty,
                MemoryRevealPhase.Responding,
                new RevealRequestContext(
                    CombatRequestSourceType.InputMapping,
                    "MemoryFragmentInteract",
                    string.Empty,
                    string.Empty,
                    "M1 Fragment Interact"),
                new RevealRequestResult(
                    RevealRequestDecision.Accepted,
                    "Reveal request accepted",
                    "M1 Fragment Interact",
                    RevealRequestClassification.Unknown,
                    string.Empty),
                new MemoryResponseContext(string.Empty, "RevealResponding", true, "Accepted"),
                new MemoryCooldownContext(false, 0f, string.Empty));

            bool appended = log.TryAppendAcceptedInteraction(interactionSnapshot, memorySnapshot);

            Assert.That(appended, Is.True);
            Assert.That(log.Entries, Has.Count.EqualTo(1));
            Assert.That(log.Entries[0], Is.EqualTo("Memory Fragment: Revealed"));
        }

        [Test]
        public void RuntimeMemoryLogPlaceholderSourceStaysWithinOwnershipGuardrails() {
            string source = File.ReadAllText(RuntimeLogPath);
            string adapter = File.ReadAllText(OverlayAdapterPath);
            string tickHandler = File.ReadAllText(TickHandlerPath);

            Assert.That(source, Does.Not.Contain("IntakeRevealRequest"));
            Assert.That(source, Does.Not.Contain("EvaluateRequestedReveal"));
            Assert.That(source, Does.Not.Contain("AdvancePhase"));
            Assert.That(source, Does.Not.Contain("MemoryInteractionService"));
            Assert.That(source, Does.Not.Contain("InputAction"));
            Assert.That(source, Does.Not.Contain("CombatCore"));
            Assert.That(source, Does.Not.Contain("TargetContext"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("Debug.Log"));

            Assert.That(adapter, Does.Contain("UpdateRuntimeMemoryLog"));
            Assert.That(adapter, Does.Not.Contain("MemoryState"));
            Assert.That(adapter, Does.Not.Contain("MemoryInteractionService"));
            Assert.That(adapter, Does.Not.Contain("UnityEngine.InputSystem"));
            Assert.That(adapter, Does.Not.Contain("InputAction."));
            Assert.That(adapter, Does.Not.Contain("InputAction "));
            Assert.That(adapter, Does.Not.Contain("CallbackContext"));
            Assert.That(adapter, Does.Not.Contain("FindObjectOfType"));
            Assert.That(adapter, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(adapter, Does.Not.Contain("Resources.Load"));
            Assert.That(adapter, Does.Not.Contain("Debug.Log"));

            Assert.That(tickHandler, Does.Contain("_runtimeMemoryLogPlaceholder.TryAppendAcceptedInteraction"));
            Assert.That(tickHandler, Does.Contain("UpdateRuntimeMemoryLog"));
        }

        [Test]
        public void RuntimeMemoryLogPlaceholder_UxmlAndStyles_ArePresent() {
            string uxml = File.ReadAllText(UxmlPath);
            string uss = File.ReadAllText(UssPath);

            Assert.That(uxml, Does.Contain("runtime-memory-log"));
            Assert.That(uxml, Does.Contain("runtime-memory-log-latest-label"));
            Assert.That(uss, Does.Contain(".runtime-memory-log"));
            Assert.That(uss, Does.Contain(".runtime-memory-log-entry"));
        }

        private static MemoryInteractionSnapshot CreateInteractionSnapshot(MemoryInteractOutcome outcome) {
            return new MemoryInteractionSnapshot(
                "memory-fragment",
                true,
                true,
                outcome,
                outcome == MemoryInteractOutcome.Accepted
                    ? "Reveal accepted by MemoryState"
                    : "Not accepted");
        }

        private static M0MemoryState CreateAcceptedMemoryState(string memoryId) {
            var memoryState = new M0MemoryState(memoryId);
            var request = new RevealRequestContext(
                CombatRequestSourceType.InputMapping,
                "MemoryFragmentInteract",
                memoryId,
                memoryId,
                "M1 Fragment Interact");
            memoryState.IntakeRevealRequest(request);
            memoryState.EvaluateRequestedReveal();
            memoryState.AdvancePhase("Memory fragment interaction accepted");
            return memoryState;
        }

        private static M0MemoryState CreateRejectedMemoryState(string memoryId) {
            var memoryState = new M0MemoryState(memoryId);
            var request = new RevealRequestContext(
                CombatRequestSourceType.TestHarness,
                "Rejected",
                "TestHarness",
                memoryId,
                "Presentation-only request",
                RevealRequestClassification.PresentationOnly);
            memoryState.IntakeRevealRequest(request);
            memoryState.EvaluateRequestedReveal();
            return memoryState;
        }
    }
}
