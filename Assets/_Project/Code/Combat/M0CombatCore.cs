using System;
using GlassRefrain.Core;
using GlassRefrain.Targeting;

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
        private M0TargetContext targetContext;
        private bool parryWasEligible;

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

        public void SetTargetContext(M0TargetContext context) {
            targetContext = context;
        }

        // Story 1-6: Tick method for time-based state management (CounterWindow duration expiry).
        public void Tick(float deltaTime) {
            if (counterWindowState.IsOpen) {
                float newElapsed = counterWindowState.ElapsedSeconds + deltaTime;
                if (newElapsed >= counterWindowState.DurationSeconds) {
                    CloseCounterWindow("Duration expired");
                } else {
                    counterWindowState = new CounterWindowState(
                        counterWindowState.IsOpen,
                        counterWindowState.SourceTag,
                        newElapsed,
                        counterWindowState.DurationSeconds);
                    RefreshSnapshot();
                }
            }
        }

        // Story 1-6: Defensive intent — EnemyIntentSnapshot passed as value struct by M0GameplayTickHandler.
        // Combat Core owns all validation; no reference to GlassRefrain.Enemy is needed.
        public CombatActionRequestResult ConsumeDefensiveIntent(CombatActionType actionType, EnemyIntentSnapshot enemySnapshot) {
            if (actionType == CombatActionType.Parry) {
                bool stateValid = enemySnapshot.State == EnemyIntentState.Active;
                string[] tags = enemySnapshot.AttackIntent.AttackTags.Tags;
                bool tagsValid = tags == null || tags.Length == 0 || System.Array.IndexOf(tags, "ParryEligible") >= 0;
                parryWasEligible = stateValid && tagsValid;
                var request = new CombatActionRequest(
                    CombatActionType.Parry, 0f,
                    CombatRequestSourceType.InputMapping, "M0GameplayTickHandler", "Parry intent from Input");
                return RequestAction(request);
            }
            if (actionType == CombatActionType.Dodge) {
                var request = new CombatActionRequest(
                    CombatActionType.Dodge, 0f,
                    CombatRequestSourceType.InputMapping, "M0GameplayTickHandler", "Dodge intent from Input");
                return RequestAction(request);
            }
            if (actionType == CombatActionType.Counter) {
                if (!counterWindowState.IsOpen) {
                    lastActionResult = new CombatActionRequestResult(
                        CombatActionResult.Rejected, "Counter rejected: CounterWindow is not open",
                        currentState.ToString());
                    RefreshSnapshot();
                    return lastActionResult;
                }
                var request = new CombatActionRequest(
                    CombatActionType.Counter, 0f,
                    CombatRequestSourceType.InputMapping, "M0GameplayTickHandler", "Counter intent from Input");
                return RequestAction(request);
            }
            lastActionResult = new CombatActionRequestResult(
                CombatActionResult.Rejected, "ConsumeDefensiveIntent called with non-defensive action",
                currentState.ToString());
            RefreshSnapshot();
            return lastActionResult;
        }

        public CombatActionRequestResult ConsumeAttackIntent(CombatActionType attackType) {
            var request = new CombatActionRequest(
                attackType,
                0f,
                CombatRequestSourceType.InputMapping,
                "M0DirectPlayerInput",
                attackType.ToString() + " intent from Input");
            var result = RequestAction(request);
            if (result.Accepted) {
                ResolveAttack(attackType);
            }
            return result;
        }

        public CombatResolutionResult ResolveAttack(CombatActionType attackType) {
            var targetSnapshot = targetContext != null ? targetContext.Snapshot : default(TargetContextSnapshot);
            bool hasValidTarget = targetSnapshot.IsLockedOn && targetSnapshot.IsValid;

            lastResolutionResult = new CombatResolutionResult(
                attackType,
                true,
                hasValidTarget,
                hasValidTarget,
                false,
                hasValidTarget ? targetSnapshot.TargetId : string.Empty,
                hasValidTarget ? attackType + " hit (placeholder)" : attackType + " whiff — no valid target");

            RefreshSnapshot();
            return lastResolutionResult;
        }

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
                    if (!counterWindowState.IsOpen) {
                        lastActionResult = new CombatActionRequestResult(
                            CombatActionResult.Rejected, "Counter rejected: CounterWindow is not open",
                            currentState.ToString());
                        RefreshSnapshot();
                        return lastActionResult;
                    }
                    // Story 1-6: Close CounterWindow immediately when Counter is consumed.
                    CloseCounterWindow("Counter consumed");
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
                    // Story 1-6: CounterWindow opens only on a valid parry (Active + ParryEligible).
                    if (parryWasEligible) {
                        OpenCounterWindow("ParrySuccess", 0.5f);
                    }
                    parryWasEligible = false;
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
            // Story 1-6: Do NOT transition to CounterWindow state here.
            // CounterWindow is a transient cleanup state used when the window times out.
            // The "window is open" condition is tracked by counterWindowState.IsOpen only.
            // State remains in the caller's state (ParryRecovery) so player can press Counter from Neutral later.
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

            // Story 1-6: CounterWindow is duration-based, not state-based.
            // Do NOT auto-close on state transitions. Only CloseCounterWindow or duration expiry should close it.
            // The CounterWindow state is a transient cleanup state for when the window times out,
            // but the open flag (counterWindowState.IsOpen) should persist across normal state changes.

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
