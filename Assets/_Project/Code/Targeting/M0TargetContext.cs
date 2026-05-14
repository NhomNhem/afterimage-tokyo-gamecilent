using System;
using System.Collections.Generic;
using GlassRefrain.Core;

namespace GlassRefrain.Targeting
{
    public sealed class M0TargetContext
    {
        private TargetFocusState focusState;
        private string targetId;
        private bool targetValid;
        private TargetDirectionContext targetDirection;
        private string acquireReason;
        private string releaseReason;
        private string invalidReason;
        private TargetContextSnapshot latestSnapshot;

        public M0TargetContext()
        {
            focusState = TargetFocusState.Inactive;
            targetId = string.Empty;
            targetValid = false;
            targetDirection = new TargetDirectionContext(new Axis2(0f, 0f), false, string.Empty);
            acquireReason = string.Empty;
            releaseReason = string.Empty;
            invalidReason = string.Empty;
            RefreshSnapshot();
        }

        public TargetContextSnapshot Snapshot
        {
            get { return latestSnapshot; }
        }

        public event Action<TargetContextSnapshot> SnapshotChanged;

        public bool ConsumeInputIntent(InputIntentSnapshot inputIntent)
        {
            if (!inputIntent.LockOnPressed)
            {
                return false;
            }

            if (focusState == TargetFocusState.Focused)
            {
                RequestRelease(new TargetReleaseRequest(TargetReleaseReason.Manual, "InputMapping", "LockOn toggled off"));
                return true;
            }

            if (targetValid && !string.IsNullOrEmpty(targetId))
            {
                RequestAcquire(new TargetAcquireRequest(targetId, "InputMapping", "LockOn toggled on"));
                return true;
            }

            focusState = TargetFocusState.AcquireRequested;
            acquireReason = "LockOn request pending valid target";
            RefreshSnapshot();
            return true;
        }

        public TargetAcquireResult RequestAcquire(TargetAcquireRequest request)
        {
            if (string.IsNullOrEmpty(request.TargetId))
            {
                focusState = TargetFocusState.AcquireRequested;
                acquireReason = request.Reason;
                targetValid = false;
                RefreshSnapshot();
                return new TargetAcquireResult(false, string.Empty, "No target id available");
            }

            targetId = request.TargetId;
            acquireReason = request.Reason;
            releaseReason = string.Empty;

            if (targetValid)
            {
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

        public bool RequestRelease(TargetReleaseRequest request)
        {
            bool changed = focusState != TargetFocusState.Inactive || !string.IsNullOrEmpty(releaseReason);

            focusState = TargetFocusState.Inactive;
            releaseReason = request.Detail;
            acquireReason = string.Empty;
            invalidReason = request.Reason == TargetReleaseReason.Invalid ? request.Detail : string.Empty;
            RefreshSnapshot();
            return changed;
        }

        public void SetTargetValidity(TargetValidityContext validity)
        {
            if (!string.IsNullOrEmpty(validity.TargetId))
            {
                targetId = validity.TargetId;
            }

            targetValid = validity.IsValid;
            invalidReason = validity.IsValid ? string.Empty : validity.Reason;

            if (validity.IsValid)
            {
                if (focusState == TargetFocusState.AcquireRequested && !string.IsNullOrEmpty(targetId))
                {
                    focusState = TargetFocusState.Focused;
                    releaseReason = string.Empty;
                }
            }
            else if (focusState == TargetFocusState.Focused || focusState == TargetFocusState.AcquireRequested)
            {
                focusState = TargetFocusState.Invalid;
            }

            RefreshSnapshot();
        }

        public void SetTargetDirection(TargetDirectionContext direction)
        {
            targetDirection = direction;
            RefreshSnapshot();
        }

        public TargetDebugSnapshot CreateDebugSnapshot()
        {
            string[] details = new string[]
            {
                "FocusState: " + latestSnapshot.FocusState,
                "TargetId: " + latestSnapshot.TargetId,
                "IsLockedOn: " + latestSnapshot.IsLockedOn,
                "IsValid: " + latestSnapshot.IsValid,
                "Direction: " + latestSnapshot.Direction.Label + " | " + latestSnapshot.Direction.HasDirection + " | (" + latestSnapshot.Direction.Direction.X + ", " + latestSnapshot.Direction.Direction.Y + ")",
                "AcquireReason: " + latestSnapshot.AcquireReason,
                "ReleaseReason: " + latestSnapshot.ReleaseReason,
                "InvalidReason: " + latestSnapshot.InvalidReason
            };

            return new TargetDebugSnapshot("M0 target context", Array.AsReadOnly(details));
        }

        private void RefreshSnapshot()
        {
            latestSnapshot = new TargetContextSnapshot(
                focusState,
                targetId,
                targetValid,
                targetDirection,
                acquireReason,
                releaseReason,
                invalidReason);

            Action<TargetContextSnapshot> handler = SnapshotChanged;
            if (handler != null)
            {
                handler(latestSnapshot);
            }
        }
    }
}
