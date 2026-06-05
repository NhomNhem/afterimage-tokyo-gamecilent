using System.IO;
using GlassRefrain.Bootstrap;
using GlassRefrain.Core;
using GlassRefrain.Memory;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M1MemoryRevealFeedbackBridgeTests {
        [Test]
        public void TryPlayAcceptedInteraction_WhenMemoryStateAccepted_ShouldStartVfxResponse() {
            M0MemoryState memoryState = CreateAcceptedMemoryState("memory-fragment");
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.25f, 0f, "standard");
            M1MemoryRevealFeedbackBridge bridge = new M1MemoryRevealFeedbackBridge();
            MemoryInteractionSnapshot interactionSnapshot = new MemoryInteractionSnapshot(
                "memory-fragment",
                true,
                true,
                MemoryInteractOutcome.Accepted,
                "Reveal accepted by MemoryState");

            bool played = bridge.TryPlayAcceptedInteraction(interactionSnapshot, memoryState.Snapshot, response);

            Assert.That(played, Is.True);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Playing));
            Assert.That(response.Snapshot.SourceAcceptedContext, Is.Not.Null);
            Assert.That(response.Snapshot.SourceAcceptedContext.MemoryId, Is.EqualTo("memory-fragment"));
        }

        [Test]
        public void TryPlayAcceptedInteraction_WhenInteractionNotAccepted_ShouldNotStartVfxResponse() {
            M0MemoryState memoryState = CreateAcceptedMemoryState("memory-fragment");
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.25f, 0f, "standard");
            M1MemoryRevealFeedbackBridge bridge = new M1MemoryRevealFeedbackBridge();
            MemoryInteractionSnapshot interactionSnapshot = new MemoryInteractionSnapshot(
                "memory-fragment",
                true,
                true,
                MemoryInteractOutcome.DuplicateIgnored,
                "Fragment already collected");

            bool played = bridge.TryPlayAcceptedInteraction(interactionSnapshot, memoryState.Snapshot, response);

            Assert.That(played, Is.False);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Idle));
            Assert.That(response.Snapshot.SourceAcceptedContext, Is.Null);

            MemoryInteractionSnapshot noEligibleSnapshot = new MemoryInteractionSnapshot(
                string.Empty,
                false,
                true,
                MemoryInteractOutcome.NoEligibleFragment,
                "No eligible fragment in range");

            played = bridge.TryPlayAcceptedInteraction(noEligibleSnapshot, memoryState.Snapshot, response);

            Assert.That(played, Is.False);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Idle));
            Assert.That(response.Snapshot.SourceAcceptedContext, Is.Null);
        }

        [Test]
        public void TryPlayAcceptedInteraction_WhenMemoryStateRejected_ShouldRejectWithoutPlayback() {
            M0MemoryState memoryState = new M0MemoryState("memory-fragment");
            RevealRequestContext request = new RevealRequestContext(
                CombatRequestSourceType.TestHarness,
                "Rejected",
                "TestHarness",
                "memory-fragment",
                "Presentation-only request",
                RevealRequestClassification.PresentationOnly);
            memoryState.IntakeRevealRequest(request);
            memoryState.EvaluateRequestedReveal();

            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.25f, 0f, "standard");
            M1MemoryRevealFeedbackBridge bridge = new M1MemoryRevealFeedbackBridge();
            MemoryInteractionSnapshot interactionSnapshot = new MemoryInteractionSnapshot(
                "memory-fragment",
                true,
                true,
                MemoryInteractOutcome.Accepted,
                "Forced accepted interaction snapshot");

            bool played = bridge.TryPlayAcceptedInteraction(interactionSnapshot, memoryState.Snapshot, response);

            Assert.That(played, Is.False);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Rejected));
            Assert.That(response.Snapshot.RejectionReason, Is.EqualTo(MemoryVFXResponseReasons.NotAcceptedByMemoryState));
            Assert.That(response.Snapshot.SourceAcceptedContext, Is.Null);
        }

        [Test]
        public void TryPlayAcceptedInteraction_WhenResponseInCooldown_ShouldNotReplayAcceptedFeedback() {
            M0MemoryState memoryState = CreateAcceptedMemoryState("memory-fragment");
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.1f, 0.25f, "standard");
            M1MemoryRevealFeedbackBridge bridge = new M1MemoryRevealFeedbackBridge();
            MemoryInteractionSnapshot interactionSnapshot = new MemoryInteractionSnapshot(
                "memory-fragment",
                true,
                true,
                MemoryInteractOutcome.Accepted,
                "Reveal accepted by MemoryState");

            bool played = bridge.TryPlayAcceptedInteraction(interactionSnapshot, memoryState.Snapshot, response);
            response.Update(0.1f);

            Assert.That(played, Is.True);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.CoolingDown));

            played = bridge.TryPlayAcceptedInteraction(interactionSnapshot, memoryState.Snapshot, response);

            Assert.That(played, Is.False);
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.CoolingDown));
            Assert.That(response.Snapshot.RejectionReason, Is.EqualTo(MemoryVFXResponseReasons.InCooldown));
        }

        [Test]
        public void FeedbackBridgeSourceStaysWithinOwnershipGuardrails() {
            string source = File.ReadAllText("Assets/_Project/Code/Bootstrap/M1MemoryRevealFeedbackBridge.cs");

            Assert.That(source.Contains("IntakeRevealRequest"), Is.False);
            Assert.That(source.Contains("EvaluateRequestedReveal"), Is.False);
            Assert.That(source.Contains("AdvancePhase"), Is.False);
            Assert.That(source.Contains("MemoryInteractionService"), Is.False);
            Assert.That(source.Contains("CombatCore"), Is.False);
            Assert.That(source.Contains("TargetContext"), Is.False);
            Assert.That(source.Contains("InputAction"), Is.False);
            Assert.That(source.Contains("FindObjectOfType"), Is.False);
            Assert.That(source.Contains("Resources.Load"), Is.False);
            Assert.That(source.Contains("Debug.Log"), Is.False);
        }

        [Test]
        public void CombatDebugOverlayContainsMemoryRevealPlaceholderOnly() {
            string adapterSource = File.ReadAllText("Assets/_Project/Code/Presentation/M0CombatDebugOverlayAdapter.cs");
            string uxml = File.ReadAllText("Assets/_Project/Content/UI/CombatDebugOverlay.uxml");
            string uss = File.ReadAllText("Assets/_Project/Content/UI/CombatDebugOverlay.uss");

            Assert.That(adapterSource.Contains("UpdateMemoryRevealFeedback"), Is.True);
            Assert.That(adapterSource.Contains("IMemoryVFXResponseSnapshot"), Is.True);
            Assert.That(adapterSource.Contains("MemoryInteractionService"), Is.False);
            Assert.That(adapterSource.Contains("MemoryState"), Is.False);
            Assert.That(adapterSource.Contains("UnityEngine.InputSystem"), Is.False);
            Assert.That(adapterSource.Contains("InputAction."), Is.False);
            Assert.That(adapterSource.Contains("InputAction "), Is.False);
            Assert.That(adapterSource.Contains("CallbackContext"), Is.False);
            Assert.That(uxml.Contains("memory-reveal-feedback"), Is.True);
            Assert.That(uss.Contains(".memory-reveal-feedback"), Is.True);
        }

        private static M0MemoryState CreateAcceptedMemoryState(string memoryId) {
            M0MemoryState memoryState = new M0MemoryState(memoryId);
            RevealRequestContext request = new RevealRequestContext(
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
    }
}
