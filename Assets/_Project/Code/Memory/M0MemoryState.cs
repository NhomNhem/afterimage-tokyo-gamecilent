using System;
using GlassRefrain.Core;

namespace GlassRefrain.Memory {
    public sealed class M0MemoryState {
        private const float DefaultCooldownSeconds = 0.25f;

        private readonly string defaultMemoryId;
        private MemoryRevealPhase currentPhase;
        private MemoryRevealPhase rejectedFallbackPhase;
        private RevealRequestContext lastRequest;
        private RevealRequestResult lastResult;
        private MemoryResponseContext responseContext;
        private MemoryCooldownContext cooldownContext;
        private MemoryStateSnapshot latestSnapshot;

        public M0MemoryState(string memoryId = "M0RevealCandidate") {
            defaultMemoryId = string.IsNullOrEmpty(memoryId) ? "M0RevealCandidate" : memoryId;
            currentPhase = MemoryRevealPhase.Dormant;
            rejectedFallbackPhase = MemoryRevealPhase.Dormant;
            lastRequest = new RevealRequestContext(string.Empty, defaultMemoryId, "Uninitialized");
            lastResult = new RevealRequestResult(
                RevealRequestDecision.Rejected,
                "No reveal request evaluated yet",
                "Initialization",
                RevealRequestClassification.Unknown,
                defaultMemoryId);
            responseContext = new MemoryResponseContext(defaultMemoryId, string.Empty, false, string.Empty);
            cooldownContext = new MemoryCooldownContext(false, 0f, string.Empty);
            RefreshSnapshot();
        }

        public MemoryStateSnapshot Snapshot {
            get { return latestSnapshot; }
        }

        public event Action<MemoryStateSnapshot> SnapshotChanged;

        public void IntakeRevealRequest(RevealRequestContext request) {
            rejectedFallbackPhase = currentPhase == MemoryRevealPhase.Cooldown
                ? MemoryRevealPhase.Cooldown
                : MemoryRevealPhase.Dormant;
            lastRequest = request;
            currentPhase = MemoryRevealPhase.Requested;
            RefreshSnapshot();
        }

        public RevealRequestResult EvaluateRequestedReveal() {
            if (currentPhase != MemoryRevealPhase.Requested) {
                lastResult = new RevealRequestResult(
                    RevealRequestDecision.Rejected,
                    "Reveal can only be evaluated in Requested phase",
                    currentPhase.ToString(),
                    lastRequest.Classification,
                    ResolveMemoryId(lastRequest));
                currentPhase = MemoryRevealPhase.Rejected;
                RefreshSnapshot();
                return lastResult;
            }

            string memoryId = ResolveMemoryId(lastRequest);
            if (ShouldReject(lastRequest, out string reason)) {
                lastResult = new RevealRequestResult(
                    RevealRequestDecision.Rejected,
                    reason,
                    lastRequest.ContextLabel,
                    lastRequest.Classification,
                    memoryId);
                currentPhase = MemoryRevealPhase.Rejected;
                RefreshSnapshot();
                return lastResult;
            }

            lastResult = new RevealRequestResult(
                RevealRequestDecision.Accepted,
                "Reveal request accepted",
                lastRequest.ContextLabel,
                lastRequest.Classification,
                memoryId);
            responseContext = new MemoryResponseContext(memoryId, "RevealResponsePending", false, "Awaiting response phase");
            currentPhase = MemoryRevealPhase.Accepted;
            RefreshSnapshot();
            return lastResult;
        }

        public MemoryStateSnapshot AdvancePhase(string reason, float cooldownSeconds = DefaultCooldownSeconds) {
            string memoryId = ResolveMemoryId(lastRequest);

            switch (currentPhase) {
                case MemoryRevealPhase.Accepted:
                    currentPhase = MemoryRevealPhase.Responding;
                    responseContext = new MemoryResponseContext(
                        memoryId,
                        "RevealResponding",
                        true,
                        string.IsNullOrEmpty(reason) ? "Responding" : reason);
                    break;

                case MemoryRevealPhase.Responding:
                    currentPhase = MemoryRevealPhase.Cooldown;
                    cooldownContext = new MemoryCooldownContext(
                        true,
                        cooldownSeconds > 0f ? cooldownSeconds : DefaultCooldownSeconds,
                        string.IsNullOrEmpty(reason) ? "Response complete" : reason);
                    responseContext = new MemoryResponseContext(memoryId, "RevealResponseComplete", false, "Cooldown entered");
                    break;

                case MemoryRevealPhase.Rejected:
                    currentPhase = rejectedFallbackPhase;
                    if (currentPhase != MemoryRevealPhase.Cooldown) {
                        cooldownContext = new MemoryCooldownContext(false, 0f, string.Empty);
                    }
                    responseContext = new MemoryResponseContext(memoryId, string.Empty, false, "Rejected");
                    break;

                case MemoryRevealPhase.Cooldown:
                    currentPhase = MemoryRevealPhase.Dormant;
                    cooldownContext = new MemoryCooldownContext(false, 0f, string.IsNullOrEmpty(reason) ? "Cooldown complete" : reason);
                    responseContext = new MemoryResponseContext(memoryId, string.Empty, false, "Dormant");
                    break;
            }

            RefreshSnapshot();
            return latestSnapshot;
        }

        private bool ShouldReject(RevealRequestContext request, out string reason) {
            if (request.Classification == RevealRequestClassification.GenericHit) {
                reason = "Generic hit requests cannot reveal memory";
                return true;
            }

            if (request.Classification == RevealRequestClassification.FailedDodge) {
                reason = "Failed dodge requests cannot reveal memory";
                return true;
            }

            if (request.Classification == RevealRequestClassification.FailedParry) {
                reason = "Failed parry requests cannot reveal memory";
                return true;
            }

            if (request.Classification == RevealRequestClassification.InvalidCounter) {
                reason = "Invalid counter requests cannot reveal memory";
                return true;
            }

            if (request.Classification == RevealRequestClassification.PresentationOnly) {
                reason = "Presentation-only events cannot reveal memory";
                return true;
            }

            reason = string.Empty;
            return false;
        }

        private string ResolveMemoryId(RevealRequestContext request) {
            if (!string.IsNullOrEmpty(request.MemoryId)) {
                return request.MemoryId;
            }

            return defaultMemoryId;
        }

        private void RefreshSnapshot() {
            latestSnapshot = new MemoryStateSnapshot(
                ResolveMemoryId(lastRequest),
                currentPhase,
                lastRequest,
                lastResult,
                responseContext,
                cooldownContext);

            Action<MemoryStateSnapshot> handler = SnapshotChanged;
            if (handler != null) {
                handler(latestSnapshot);
            }
        }
    }
}
