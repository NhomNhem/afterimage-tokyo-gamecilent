using System.IO;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public sealed class M1InteractionPromptPlaceholderTests {
        private const string OverlayAdapterPath = "Assets/_Project/Code/Presentation/M0CombatDebugOverlayAdapter.cs";
        private const string TickHandlerPath = "Assets/_Project/Code/Bootstrap/M0GameplayTickHandler.cs";
        private const string MemoryBridgePath = "Assets/_Project/Code/Bootstrap/M0MemoryInteractionTickBridge.cs";
        private const string UxmlPath = "Assets/_Project/Content/UI/CombatDebugOverlay.uxml";
        private const string UssPath = "Assets/_Project/Content/UI/CombatDebugOverlay.uss";

        [Test]
        public void PromptPlaceholder_UxmlAndStyles_ArePresent() {
            var uxml = File.ReadAllText(UxmlPath);
            var uss = File.ReadAllText(UssPath);

            Assert.That(uxml, Does.Contain("interaction-prompt"));
            Assert.That(uxml, Does.Contain("Press F to Interact"));
            Assert.That(uss, Does.Contain(".interaction-prompt"));
            Assert.That(uss, Does.Contain(".hidden"));
        }

        [Test]
        public void PromptPresenter_UsesReadOnlySnapshotValues_FromMemoryBridge() {
            var memoryBridge = File.ReadAllText(MemoryBridgePath);
            var tickHandler = File.ReadAllText(TickHandlerPath);

            Assert.That(memoryBridge, Does.Contain("memoryInteractionService.Snapshot"));
            Assert.That(memoryBridge, Does.Contain("UpdateInteractionPrompt("));
            Assert.That(memoryBridge, Does.Contain("interactionSnapshot.HasEligibleFragment"));
            Assert.That(memoryBridge, Does.Contain("interactionSnapshot.NearbyFragmentId"));
            Assert.That(tickHandler, Does.Contain("_memoryInteractionTickBridge.TickInteraction"));
        }

        [Test]
        public void PromptPresenter_DoesNotOwnGameplayTruthOrInputCallbacks() {
            var adapter = File.ReadAllText(OverlayAdapterPath);

            Assert.That(adapter, Does.Contain("UpdateInteractionPrompt"));
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
        }
    }
}
