using System;
using GlassRefrain.Core;

namespace GlassRefrain.Combat {
    public sealed class M0CombatCore {
        private CombatCoreState currentState;
        private CombatActionRequestResult lastActionResult;
        private CombatResolutionResult lastResolutionResult;
        private CounterWindowState counterWindowState;
        private ActionLockContext actionLockContext;
        private RecoveryContext recoveryContext;
        private RevealRequestContext lastRevealRequestContext;
        private M0CombatSnapshot latestSnapshot;

        public M0CombatCore() {
            currentState = CombatCoreState.Neutral;
            lastActionResult = new CombatActionRequestResult(CombatActionResult.Ignored, "No action processed yet",
                currentState.ToString());
            lastResolutionResult = new CombatResolutionResult(CombatActionType.Unknown, false, false, false, false,
                string.Empty, "No resolution yet");
            counterWindowState = new CounterWindowState(false, string.Empty, 0f, 0f);
            actionLockContext = new ActionLockContext(false, string.Empty, CombatCoreState.Neutral);
            recoveryContext = new RecoveryContext(false, string.Empty, CombatCoreState.Neutral,
                RecoverySource.CombatCore, 0f);
            lastRevealRequestContext = new RevealRequestContext(CombatRequestSourceType.Unknown, string.Empty,
                string.Empty, string.Empty, string.Empty);
            RefreshSnapshot();
        }

        public M0CombatSnapshot Snapshot => latestSnapshot;

        public RevealRequestContext LastRevealRequestContext => lastRevealRequestContext;

        public event Action<M0CombatSnapshot> SnapshotChanged;
        public event Action<RevealRequestContext> RevealRequestEmitted;

        public CombatActionRequestResult RequestAction(CombatActionRequest request) {
            if (currentState == CombatCoreState.Disabled) {
                lastActionResult = new CombatActionRequestResult(CombatActionResult.Ignored, "Combat core is disabled",
                    currentState.ToString());
                RefreshSnapshot();
                return lastActionResult;
            }

            if (currentState != CombatCoreState.Neutral) {
                lastActionResult = new CombatActionRequestResult(CombatActionResult.Rejected,
                    "Action rejected outside Neutral", currentState.ToString());
                RefreshSnapshot();
                return lastActionResult;
            }

            switch (request.ActionType) {
                case CombatActionType.LightAttack:
                case CombatActionType.HeavyAttack:
                    TransitionTo(CombatCoreState.AttackStartup, request.ActionType + " accepted");
                    break;
                case CombatActionType.Dodge:
                    TransitionTo(CombatCoreState.DodgeStartup, "Dodge accepted");
                    break;
                case CombatActionType.Parry:
                    TransitionTo(CombatCoreState.ParryStartup, "Parry accepted");
                    break;
                case CombatActionType.Counter:
                    TransitionTo(CombatCoreState.CounterActive, "Counter accepted");
                    break;
                default:
                    lastActionResult = new CombatActionRequestResult(CombatActionResult.Rejected, "Unknown action type",
                        currentState.ToString());
                    RefreshSnapshot();
                    return lastActionResult;
            }

            lastActionResult = new CombatActionRequestResult(CombatActionResult.Accepted, "Action accepted",
                currentState.ToString());
            RefreshSnapshot();
            return lastActionResult;
        }

        public CombatStepResult AdvanceState(string reason) {
            var previous = currentState;

            switch (currentState) {
                case CombatCoreState.AttackStartup:
                    TransitionTo(CombatCoreState.AttackActive, reason);
                    break;
                case CombatCoreState.AttackActive:
                    TransitionTo(CombatCoreState.AttackRecovery, reason);
                    break;
                case CombatCoreState.AttackRecovery:
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
                case CombatCoreState.DodgeStartup:
                    TransitionTo(CombatCoreState.DodgeActive, reason);
                    break;
                case CombatCoreState.DodgeActive:
                    TransitionTo(CombatCoreState.DodgeRecovery, reason);
                    break;
                case CombatCoreState.DodgeRecovery:
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
                case CombatCoreState.ParryStartup:
                    TransitionTo(CombatCoreState.ParryActive, reason);
                    break;
                case CombatCoreState.ParryActive:
                    TransitionTo(CombatCoreState.ParryRecovery, reason);
                    OpenCounterWindow("ParrySuccessPlaceholder", 0.5f);
                    break;
                case CombatCoreState.ParryRecovery:
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
                case CombatCoreState.CounterWindow:
                    CloseCounterWindow("Counter window closed");
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
                case CombatCoreState.CounterActive:
                    TransitionTo(CombatCoreState.RevealBeat, reason);
                    EmitRevealRequest("CounterToRevealPlaceholder");
                    break;
                case CombatCoreState.RevealBeat:
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
                case CombatCoreState.HitReact:
                    TransitionTo(CombatCoreState.Neutral, reason);
                    break;
            }

            return new CombatStepResult(previous != currentState, previous, currentState, reason);
        }

        public void OpenCounterWindow(string sourceTag, float durationSeconds) {
            counterWindowState = new CounterWindowState(true, sourceTag, 0f, durationSeconds);
            TransitionTo(CombatCoreState.CounterWindow, "Counter window opened");
            lastResolutionResult = new CombatResolutionResult(
                CombatActionType.Parry,
                true,
                true,
                true,
                true,
                sourceTag,
                "Counter window opened");
            RefreshSnapshot();
        }

        public void CloseCounterWindow(string reason) {
            counterWindowState = new CounterWindowState(false, counterWindowState.SourceTag,
                counterWindowState.DurationSeconds, counterWindowState.DurationSeconds);
            lastResolutionResult = new CombatResolutionResult(
                CombatActionType.Parry,
                true,
                true,
                true,
                false,
                counterWindowState.SourceTag,
                reason);
            RefreshSnapshot();
        }

        public void TriggerHitReact(string sourceLabel) {
            TransitionTo(CombatCoreState.HitReact, sourceLabel);
            lastResolutionResult = new CombatResolutionResult(
                CombatActionType.Unknown,
                true,
                false,
                true,
                false,
                sourceLabel,
                "HitReact triggered");
            RefreshSnapshot();
        }

        public void SetDisabled(bool disabled, string reason) {
            if (disabled) {
                TransitionTo(CombatCoreState.Disabled, reason);
                lastActionResult = new CombatActionRequestResult(CombatActionResult.Ignored, "Combat disabled",
                    currentState.ToString());
            }
            else {
                TransitionTo(CombatCoreState.Neutral, reason);
            }

            RefreshSnapshot();
        }

        private void TransitionTo(CombatCoreState nextState, string reason) {
            currentState = nextState;
            actionLockContext = ResolveLockContext(nextState);
            recoveryContext = ResolveRecoveryContext(nextState);

            if (nextState != CombatCoreState.CounterWindow && counterWindowState.IsOpen)
                counterWindowState = new CounterWindowState(false, counterWindowState.SourceTag,
                    counterWindowState.DurationSeconds, counterWindowState.DurationSeconds);

            lastResolutionResult = new CombatResolutionResult(
                lastResolutionResult.ActionType,
                true,
                true,
                lastResolutionResult.HitConfirmed,
                counterWindowState.IsOpen,
                reason,
                "Transitioned to " + nextState);
        }

        private ActionLockContext ResolveLockContext(CombatCoreState state) {
            if (state == CombatCoreState.AttackStartup ||
                state == CombatCoreState.AttackActive ||
                state == CombatCoreState.DodgeStartup ||
                state == CombatCoreState.DodgeActive ||
                state == CombatCoreState.ParryStartup ||
                state == CombatCoreState.ParryActive ||
                state == CombatCoreState.CounterActive ||
                state == CombatCoreState.HitReact)
                return new ActionLockContext(true, state.ToString(), state);

            return new ActionLockContext(false, string.Empty, state);
        }

        private RecoveryContext ResolveRecoveryContext(CombatCoreState state) {
            if (state == CombatCoreState.AttackRecovery ||
                state == CombatCoreState.DodgeRecovery ||
                state == CombatCoreState.ParryRecovery)
                return new RecoveryContext(true, state.ToString(), state, RecoverySource.CombatCore, 0.25f);

            return new RecoveryContext(false, string.Empty, state, RecoverySource.CombatCore, 0f);
        }

        private void EmitRevealRequest(string sourceLabel) {
            lastRevealRequestContext = new RevealRequestContext(
                CombatRequestSourceType.CombatCore,
                sourceLabel,
                "CombatCore",
                "M0RevealCandidate",
                "CounterActive to RevealBeat",
                RevealRequestClassification.CounterConfirmed);

            var handler = RevealRequestEmitted;
            if (handler != null) handler(lastRevealRequestContext);
        }

        private void RefreshSnapshot() {
            latestSnapshot = new M0CombatSnapshot(
                currentState,
                lastActionResult,
                lastResolutionResult,
                counterWindowState,
                actionLockContext,
                recoveryContext);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }
    }
}
