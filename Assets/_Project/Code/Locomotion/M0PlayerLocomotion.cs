using System;
using System.Collections.Generic;
using GlassRefrain.Core;

namespace GlassRefrain.Locomotion
{
    public sealed class M0PlayerLocomotion
    {
        private InputIntentSnapshot currentInput;
        private MovementRestrictionContext movementRestriction;
        private RecoveryContext recoveryContext;
        private CameraMovementBasisSnapshot cameraMovementBasis;
        private bool hasReceivedInput;
        private LocomotionStateSnapshot latestSnapshot;

        public M0PlayerLocomotion()
        {
            currentInput = new InputIntentSnapshot(
                new Axis2(0f, 0f),
                new Axis2(0f, 0f),
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true);

            movementRestriction = new MovementRestrictionContext(true, true, 0f, string.Empty);
            recoveryContext = new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty);
            cameraMovementBasis = new CameraMovementBasisSnapshot(new Axis2(0f, 1f), new Axis2(1f, 0f), false, "Deferred");
            hasReceivedInput = false;
            RefreshSnapshot();
        }

        public LocomotionStateSnapshot Snapshot
        {
            get { return latestSnapshot; }
        }

        public event Action<LocomotionStateSnapshot> SnapshotChanged;

        public void ConsumeInputIntent(InputIntentSnapshot inputIntent)
        {
            currentInput = inputIntent;
            hasReceivedInput = true;
            RefreshSnapshot();
        }

        public void SetMovementRestriction(MovementRestrictionContext restriction)
        {
            movementRestriction = restriction;
            RefreshSnapshot();
        }

        public void SetRecoveryContext(RecoveryContext recovery)
        {
            recoveryContext = recovery;
            RefreshSnapshot();
        }

        public void SetCameraMovementBasis(CameraMovementBasisSnapshot cameraBasis)
        {
            cameraMovementBasis = cameraBasis;
            RefreshSnapshot();
        }

        public LocomotionDebugSnapshot CreateDebugSnapshot()
        {
            string[] details = new string[]
            {
                "State: " + latestSnapshot.State,
                "StateDetail: " + latestSnapshot.StateDetail,
                "InputEnabled: " + latestSnapshot.InputEnabled,
                "MoveIntent: (" + latestSnapshot.MoveIntent.X + ", " + latestSnapshot.MoveIntent.Y + ")",
                "Restriction: " + latestSnapshot.MovementRestriction.CanTranslate + "/" + latestSnapshot.MovementRestriction.CanRotate + " | " + latestSnapshot.MovementRestriction.RestrictionStrength + " | " + latestSnapshot.MovementRestriction.Source,
                "Recovery: " + latestSnapshot.Recovery.IsRecovering + " | " + latestSnapshot.Recovery.RemainingSeconds + " | " + latestSnapshot.Recovery.Source + " | " + latestSnapshot.Recovery.Detail,
                "CameraBasis: " + latestSnapshot.CameraMovementBasis.IsValid + " | " + latestSnapshot.CameraMovementBasis.CameraModeLabel
            };

            return new LocomotionDebugSnapshot("M0 locomotion state", Array.AsReadOnly(details));
        }

        private void RefreshSnapshot()
        {
            LocomotionState state = ResolveState();
            string stateDetail = ResolveStateDetail(state);

            latestSnapshot = new LocomotionStateSnapshot(
                state,
                currentInput.Move,
                currentInput.InputEnabled,
                movementRestriction,
                recoveryContext,
                cameraMovementBasis,
                stateDetail);

            Action<LocomotionStateSnapshot> handler = SnapshotChanged;
            if (handler != null)
            {
                handler(latestSnapshot);
            }
        }

        private LocomotionState ResolveState()
        {
            if (recoveryContext.IsRecovering)
            {
                return LocomotionState.Recovering;
            }

            if (!currentInput.InputEnabled || !movementRestriction.CanTranslate)
            {
                return LocomotionState.Restricted;
            }

            if (hasReceivedInput && HasMoveIntent(currentInput.Move))
            {
                return LocomotionState.Moving;
            }

            if (hasReceivedInput)
            {
                return LocomotionState.Idle;
            }

            return LocomotionState.Uninitialized;
        }

        private string ResolveStateDetail(LocomotionState state)
        {
            switch (state)
            {
                case LocomotionState.Recovering:
                    if (!string.IsNullOrEmpty(recoveryContext.Detail))
                    {
                        return recoveryContext.Detail;
                    }

                    return "Recovering from " + recoveryContext.Source;
                case LocomotionState.Restricted:
                    if (!currentInput.InputEnabled)
                    {
                        return "Input disabled";
                    }

                    if (!movementRestriction.CanTranslate)
                    {
                        return string.IsNullOrEmpty(movementRestriction.Source) ? "Movement restricted" : movementRestriction.Source;
                    }

                    return "Movement restricted";
                case LocomotionState.Moving:
                    return "Raw move intent present";
                case LocomotionState.Idle:
                    return "No move intent";
                default:
                    return "Awaiting first movement intent";
            }
        }

        private static bool HasMoveIntent(Axis2 move)
        {
            return move.X != 0f || move.Y != 0f;
        }
    }
}
