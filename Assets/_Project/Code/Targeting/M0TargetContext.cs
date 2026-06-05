using System;
using System.Collections.Generic;
using _Project.Code.Shared.DI;
using GlassRefrain.Core;
using NhemDangFugBixs.Attributes;

namespace GlassRefrain.Targeting {
    public interface IM0TargetContext {
        TargetContextSnapshot Snapshot { get; }
        event Action<TargetContextSnapshot> SnapshotChanged;
        bool ConsumeInputIntent(InputIntentSnapshot inputIntent);
        TargetAcquireResult RequestAcquire(TargetAcquireRequest request);
        bool RequestRelease(TargetReleaseRequest request);
        void ResetForEncounter(string reason);
        void SetTargetValidity(TargetValidityContext validity);
        void SetTargetDirection(TargetDirectionContext direction);
        TargetDebugSnapshot CreateDebugSnapshot();
    }

    [AutoRegisterIn<IGameplayLifetimeScope>(Lifetime = NhemLifetime.Singleton), As<IM0TargetContext>]
    [AsSelf]
    public sealed class M0TargetContext : IM0TargetContext {
        private readonly ITargetableRegistry targetableRegistry;
        private TargetFocusState focusState;
        private string targetId;
        private bool targetValid;
        private TargetDirectionContext targetDirection;
        private string acquireReason;
        private string releaseReason;
        private string invalidReason;
        private TargetContextSnapshot latestSnapshot;

        public M0TargetContext(ITargetableRegistry registry = null) {
            targetableRegistry = registry;
            focusState = TargetFocusState.Inactive;
            targetId = string.Empty;
            targetValid = false;
            targetDirection = new TargetDirectionContext(new Axis2(0f, 0f), false, string.Empty);
            acquireReason = string.Empty;
            releaseReason = string.Empty;
            invalidReason = string.Empty;
            RefreshSnapshot();
        }

        public TargetContextSnapshot Snapshot => latestSnapshot;

        public event Action<TargetContextSnapshot> SnapshotChanged;

        public bool ConsumeInputIntent(InputIntentSnapshot inputIntent) {
            if (!inputIntent.LockOnPressed) return false;

            RefreshValidityFromRegistry();

            if (focusState == TargetFocusState.Focused) {
                RequestRelease(new TargetReleaseRequest(TargetReleaseReason.Manual, "InputMapping",
                    "LockOn toggled off"));
                return true;
            }

            if (targetValid && !string.IsNullOrEmpty(targetId)) {
                RequestAcquire(new TargetAcquireRequest(targetId, "InputMapping", "LockOn toggled on"));
                return true;
            }

            focusState = TargetFocusState.AcquireRequested;
            acquireReason = "LockOn request pending valid target";
            invalidReason = GetPendingAcquireReason();
            RefreshSnapshot();
            return true;
        }

        public TargetAcquireResult RequestAcquire(TargetAcquireRequest request) {
            if (string.IsNullOrEmpty(request.TargetId)) {
                focusState = TargetFocusState.AcquireRequested;
                acquireReason = request.Reason;
                targetValid = false;
                RefreshSnapshot();
                return new TargetAcquireResult(false, string.Empty, "No target id available");
            }

            targetId = request.TargetId;
            acquireReason = request.Reason;
            releaseReason = string.Empty;

            if (targetValid) {
                focusState = TargetFocusState.Focused;
                invalidReason = string.Empty;
                RefreshSnapshot();
                return new TargetAcquireResult(true, targetId, "Target focused");
            }

            focusState = TargetFocusState.AcquireRequested;
            invalidReason = "Target not yet valid";
            RefreshSnapshot();
            return new TargetAcquireResult(false, targetId, "Target not yet valid");
        }

        public bool RequestRelease(TargetReleaseRequest request) {
            var changed = focusState != TargetFocusState.Inactive || !string.IsNullOrEmpty(releaseReason);

            focusState = TargetFocusState.Inactive;
            releaseReason = request.Detail;
            acquireReason = string.Empty;
            invalidReason = request.Reason == TargetReleaseReason.Invalid ? request.Detail : string.Empty;
            RefreshSnapshot();
            return changed;
        }

        public void ResetForEncounter(string reason) {
            focusState = TargetFocusState.Inactive;
            targetId = string.Empty;
            targetValid = false;
            targetDirection = new TargetDirectionContext(new Axis2(0f, 0f), false, string.Empty);
            acquireReason = string.Empty;
            releaseReason = string.IsNullOrEmpty(reason) ? "Encounter reset release" : reason;
            invalidReason = string.Empty;
            RefreshSnapshot();
        }

        public void SetTargetValidity(TargetValidityContext validity) {
            if (!string.IsNullOrEmpty(validity.TargetId)) targetId = validity.TargetId;

            targetValid = validity.IsValid;
            invalidReason = validity.IsValid ? string.Empty : validity.Reason;

            if (validity.IsValid) {
                if (focusState == TargetFocusState.AcquireRequested && !string.IsNullOrEmpty(targetId)) {
                    focusState = TargetFocusState.Focused;
                    releaseReason = string.Empty;
                }
            }
            else if (focusState == TargetFocusState.Focused || focusState == TargetFocusState.AcquireRequested) {
                focusState = TargetFocusState.Invalid;
            }

            RefreshSnapshot();
        }

        public void SetTargetDirection(TargetDirectionContext direction) {
            targetDirection = direction;
            RefreshSnapshot();
        }

        public TargetDebugSnapshot CreateDebugSnapshot() {
            var details = new string[] {
                "FocusState: " + latestSnapshot.FocusState,
                "TargetId: " + latestSnapshot.TargetId,
                "IsLockedOn: " + latestSnapshot.IsLockedOn,
                "IsValid: " + latestSnapshot.IsValid,
                "Direction: " + latestSnapshot.Direction.Label + " | " + latestSnapshot.Direction.HasDirection +
                " | (" + latestSnapshot.Direction.Direction.X + ", " + latestSnapshot.Direction.Direction.Y + ")",
                "AcquireReason: " + latestSnapshot.AcquireReason,
                "ReleaseReason: " + latestSnapshot.ReleaseReason,
                "InvalidReason: " + latestSnapshot.InvalidReason
            };

            return new TargetDebugSnapshot("M0 target context", Array.AsReadOnly(details));
        }

        private void RefreshSnapshot() {
            latestSnapshot = new TargetContextSnapshot(
                focusState,
                targetId,
                targetValid,
                targetDirection,
                acquireReason,
                releaseReason,
                invalidReason);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }

        private void RefreshValidityFromRegistry() {
            if (targetableRegistry == null) return;

            var currentDuelEnemy = targetableRegistry.GetCurrentDuelEnemy();
            if (currentDuelEnemy == null) {
                targetValid = false;
                targetId = string.Empty;
                invalidReason = "No current duel enemy";
                return;
            }

            targetId = currentDuelEnemy.TargetId;
            if (string.IsNullOrEmpty(targetId)) {
                targetValid = false;
                invalidReason = "Target id missing";
                return;
            }

            if (!currentDuelEnemy.IsTargetable) {
                targetValid = false;
                invalidReason = "Target inactive";
                return;
            }

            if (!targetableRegistry.HasRegisteredTargetable(targetId)) {
                targetValid = false;
                invalidReason = "Target id not registered";
                return;
            }

            targetValid = true;
            invalidReason = string.Empty;
        }

        private string GetPendingAcquireReason() {
            if (targetableRegistry == null) return "Other validity fail";

            var currentDuelEnemy = targetableRegistry.GetCurrentDuelEnemy();
            if (currentDuelEnemy == null) return "No current duel enemy";
            if (string.IsNullOrEmpty(currentDuelEnemy.TargetId)) return "Target id missing";
            if (!currentDuelEnemy.IsTargetable) return "Target inactive";
            if (!targetableRegistry.HasRegisteredTargetable(currentDuelEnemy.TargetId)) return "Target id not registered";
            return "Other validity fail";
        }
    }
}
