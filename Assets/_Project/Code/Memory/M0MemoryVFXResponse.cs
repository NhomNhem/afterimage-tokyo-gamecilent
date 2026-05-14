using System;
using GlassRefrain.Core;

namespace GlassRefrain.Memory {
    public sealed class M0MemoryVFXResponse {
        private const string DefaultIntensityLabel = "standard";

        private readonly float responseDurationSeconds;
        private readonly float cooldownDurationSeconds;
        private readonly string intensityLabel;
        private MemoryVFXResponseState currentState;
        private AcceptedMemoryRevealContext? acceptedContext;
        private string rejectionReason;
        private float responseElapsedSeconds;
        private float cooldownElapsedSeconds;
        private MemoryVFXResponseSnapshot latestSnapshot;

        public M0MemoryVFXResponse(float responseDurationSeconds = 0.25f, float cooldownDurationSeconds = 0f, string intensityLabel = null) {
            this.responseDurationSeconds = responseDurationSeconds < 0f ? 0f : responseDurationSeconds;
            this.cooldownDurationSeconds = cooldownDurationSeconds < 0f ? 0f : cooldownDurationSeconds;
            this.intensityLabel = string.IsNullOrEmpty(intensityLabel) ? DefaultIntensityLabel : intensityLabel;
            currentState = MemoryVFXResponseState.Idle;
            acceptedContext = null;
            rejectionReason = string.Empty;
            responseElapsedSeconds = 0f;
            cooldownElapsedSeconds = 0f;
            RefreshSnapshot();
        }

        public MemoryVFXResponseState State {
            get { return currentState; }
        }

        public IMemoryVFXResponseSnapshot Snapshot {
            get { return latestSnapshot; }
        }

        public void OnAcceptedReveal(IAcceptedMemoryRevealContext context) {
            if (context == null) {
                OnRejectRequest(MemoryVFXResponseReasons.NotAcceptedByMemoryState);
                return;
            }

            if (currentState == MemoryVFXResponseState.Playing) {
                OnRejectRequest(MemoryVFXResponseReasons.AlreadyPlaying);
                return;
            }

            if (currentState == MemoryVFXResponseState.CoolingDown) {
                rejectionReason = MemoryVFXResponseReasons.InCooldown;
                RefreshSnapshot();
                return;
            }

            if (currentState != MemoryVFXResponseState.Idle) {
                return;
            }

            acceptedContext = new AcceptedMemoryRevealContext(context);
            rejectionReason = string.Empty;
            responseElapsedSeconds = 0f;
            cooldownElapsedSeconds = 0f;
            currentState = MemoryVFXResponseState.Requested;
            RefreshSnapshot();
        }

        public void OnPlaybackStarted() {
            if (currentState != MemoryVFXResponseState.Requested) {
                return;
            }

            currentState = MemoryVFXResponseState.Playing;
            responseElapsedSeconds = 0f;
            RefreshSnapshot();

            if (responseDurationSeconds <= 0f) {
                CompletePlayback();
            }
        }

        public void OnPlaybackComplete() {
            if (currentState != MemoryVFXResponseState.Playing) {
                return;
            }

            CompletePlayback();
        }

        public void OnRejectRequest(string reason) {
            currentState = MemoryVFXResponseState.Rejected;
            rejectionReason = reason ?? string.Empty;
            ClearAcceptedContext();
            RefreshSnapshot();
        }

        public void OnIgnoreRequest(string reason) {
            currentState = MemoryVFXResponseState.Ignored;
            rejectionReason = reason ?? string.Empty;
            ClearAcceptedContext();
            RefreshSnapshot();
        }

        public void Update(float deltaTime) {
            if (deltaTime <= 0f) {
                return;
            }

            if (currentState == MemoryVFXResponseState.Playing) {
                responseElapsedSeconds += deltaTime;
                if (responseElapsedSeconds >= responseDurationSeconds) {
                    CompletePlayback();
                    return;
                }

                RefreshSnapshot();
                return;
            }

            if (currentState == MemoryVFXResponseState.CoolingDown) {
                cooldownElapsedSeconds += deltaTime;
                if (cooldownElapsedSeconds >= cooldownDurationSeconds) {
                    ResetToIdle();
                    return;
                }

                RefreshSnapshot();
            }
        }

        public void OnReset() {
            ResetToIdle();
        }

        public IMemoryVFXResponseSnapshot GetSnapshot() {
            return latestSnapshot;
        }

        private void CompletePlayback() {
            if (cooldownDurationSeconds > 0f) {
                currentState = MemoryVFXResponseState.CoolingDown;
                cooldownElapsedSeconds = 0f;
                responseElapsedSeconds = 0f;
                acceptedContext = null;
                rejectionReason = string.Empty;
                RefreshSnapshot();
                return;
            }

            ResetToIdle();
        }

        private void ResetToIdle() {
            currentState = MemoryVFXResponseState.Idle;
            acceptedContext = null;
            rejectionReason = string.Empty;
            responseElapsedSeconds = 0f;
            cooldownElapsedSeconds = 0f;
            RefreshSnapshot();
        }

        private void ClearAcceptedContext() {
            acceptedContext = null;
            responseElapsedSeconds = 0f;
            cooldownElapsedSeconds = 0f;
        }

        private void RefreshSnapshot() {
            float cooldownProgress = 0f;
            if (currentState == MemoryVFXResponseState.CoolingDown && cooldownDurationSeconds > 0f) {
                cooldownProgress = Math.Min(1f, cooldownElapsedSeconds / cooldownDurationSeconds);
            }

            latestSnapshot = new MemoryVFXResponseSnapshot(
                currentState,
                acceptedContext,
                rejectionReason,
                cooldownProgress,
                intensityLabel);
        }
    }
}
