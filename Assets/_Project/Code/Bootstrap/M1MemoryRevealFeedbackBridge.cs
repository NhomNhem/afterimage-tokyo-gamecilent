using GlassRefrain.Core;
using GlassRefrain.Memory;

namespace GlassRefrain.Bootstrap {
    public sealed class M1MemoryRevealFeedbackBridge {
        public bool TryPlayAcceptedInteraction(
            MemoryInteractionSnapshot interactionSnapshot,
            MemoryStateSnapshot memorySnapshot,
            M0MemoryVFXResponse memoryVfxResponse) {
            if (memoryVfxResponse == null) {
                return false;
            }

            if (interactionSnapshot.LastOutcome != MemoryInteractOutcome.Accepted) {
                return false;
            }

            if (!memorySnapshot.LastResult.Accepted) {
                memoryVfxResponse.OnRejectRequest(MemoryVFXResponseReasons.NotAcceptedByMemoryState);
                return false;
            }

            AcceptedMemoryRevealContext acceptedContext = new AcceptedMemoryRevealContext(
                memorySnapshot.MemoryId,
                memorySnapshot.LastRequest,
                memorySnapshot.LastResult,
                memorySnapshot.LastRequest.CombatResultSourceLabel,
                memorySnapshot.LastRequest.ContextLabel);

            memoryVfxResponse.OnAcceptedReveal(acceptedContext);
            if (memoryVfxResponse.State == MemoryVFXResponseState.Requested) {
                memoryVfxResponse.OnPlaybackStarted();
                return true;
            }

            return false;
        }
    }
}
