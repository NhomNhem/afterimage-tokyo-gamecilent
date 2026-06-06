using System.Collections.Generic;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Bootstrap {
    public sealed class M0MemoryInteractionTickBridge {
        private readonly M1MemoryRevealFeedbackBridge _memoryRevealFeedbackBridge;
        private readonly M1RuntimeMemoryLogPlaceholder _runtimeMemoryLogPlaceholder;

        public M0MemoryInteractionTickBridge()
            : this(new M1MemoryRevealFeedbackBridge(), new M1RuntimeMemoryLogPlaceholder()) {
        }

        public M0MemoryInteractionTickBridge(
            M1MemoryRevealFeedbackBridge memoryRevealFeedbackBridge,
            M1RuntimeMemoryLogPlaceholder runtimeMemoryLogPlaceholder) {
            _memoryRevealFeedbackBridge = memoryRevealFeedbackBridge;
            _runtimeMemoryLogPlaceholder = runtimeMemoryLogPlaceholder;
        }

        public IReadOnlyList<string> RuntimeMemoryLogEntries => _runtimeMemoryLogPlaceholder.Entries;

        public void TickInteraction(
            M0PlayerLocomotion locomotion,
            MemoryInteractionService memoryInteractionService,
            IM0MemoryState memoryState,
            M0MemoryVFXResponse memoryVfxResponse,
            M0CombatDebugOverlayAdapter debugOverlayAdapter,
            bool interactTriggeredThisFrame) {
            if (memoryInteractionService == null || locomotion == null) {
                debugOverlayAdapter?.UpdateInteractionPrompt(false, string.Empty);
                debugOverlayAdapter?.UpdateRuntimeMemoryLog(_runtimeMemoryLogPlaceholder.Entries);
                return;
            }

            var playerPosition = locomotion.GetMovementSnapshot().Position;
            memoryInteractionService.Tick(playerPosition, interactTriggeredThisFrame);

            var interactionSnapshot = memoryInteractionService.Snapshot;
            debugOverlayAdapter?.UpdateInteractionPrompt(
                interactionSnapshot.HasEligibleFragment,
                interactionSnapshot.NearbyFragmentId);

            if (memoryState == null) {
                debugOverlayAdapter?.UpdateRuntimeMemoryLog(_runtimeMemoryLogPlaceholder.Entries);
                return;
            }

            var memorySnapshot = memoryState.Snapshot;
            _memoryRevealFeedbackBridge.TryPlayAcceptedInteraction(
                interactionSnapshot,
                memorySnapshot,
                memoryVfxResponse);
            _runtimeMemoryLogPlaceholder.TryAppendAcceptedInteraction(
                interactionSnapshot,
                memorySnapshot);
            debugOverlayAdapter?.UpdateRuntimeMemoryLog(_runtimeMemoryLogPlaceholder.Entries);
        }

        public void TickRevealFeedback(
            float deltaTime,
            IM0MemoryState memoryState,
            M0MemoryVFXResponse memoryVfxResponse,
            M0CombatDebugOverlayAdapter debugOverlayAdapter) {
            if (memoryVfxResponse != null) {
                memoryVfxResponse.Update(deltaTime);
                debugOverlayAdapter?.UpdateMemoryRevealFeedback(memoryVfxResponse.Snapshot);
            } else {
                debugOverlayAdapter?.UpdateMemoryRevealFeedback(null);
            }

            if (memoryState == null || memoryVfxResponse == null) {
                return;
            }

            var memorySnapshot = memoryState.Snapshot;
            var vfxState = memoryVfxResponse.State;
            var transitionedToCooldown = false;

            if (memorySnapshot.Phase == MemoryRevealPhase.Responding &&
                (vfxState == MemoryVFXResponseState.CoolingDown || vfxState == MemoryVFXResponseState.Idle)) {
                memoryState.AdvancePhase("Reveal playback complete");
                memorySnapshot = memoryState.Snapshot;
                transitionedToCooldown = true;
            }

            if (memorySnapshot.Phase == MemoryRevealPhase.Cooldown &&
                vfxState == MemoryVFXResponseState.Idle &&
                !transitionedToCooldown) {
                memoryState.AdvancePhase("Reveal cooldown complete");
            }
        }

        public void HandleRevealRequest(
            RevealRequestContext request,
            IM0MemoryState memoryState,
            M0MemoryVFXResponse memoryVfxResponse,
            INhemLogger logger) {
            if (memoryState == null || memoryVfxResponse == null) {
                return;
            }

            memoryState.IntakeRevealRequest(request);
            var evaluation = memoryState.EvaluateRequestedReveal();
            if (!evaluation.Accepted) {
                memoryVfxResponse.OnRejectRequest(MemoryVFXResponseReasons.NotAcceptedByMemoryState);
                return;
            }

            memoryState.AdvancePhase("Reveal response accepted");

            var acceptedContext = new AcceptedMemoryRevealContext(
                memoryState.Snapshot.MemoryId,
                request,
                evaluation,
                request.CombatResultSourceLabel,
                request.ContextLabel);

            memoryVfxResponse.OnAcceptedReveal(acceptedContext);
            memoryVfxResponse.OnPlaybackStarted();

#if GR_MEMORY_DEBUG || GR_M0_PROTOTYPE
            logger?.Log("[M0Memory] Reveal accepted: source=" + request.CombatResultSourceLabel + " memoryId=" + memoryState.Snapshot.MemoryId);
#endif
        }
    }
}
