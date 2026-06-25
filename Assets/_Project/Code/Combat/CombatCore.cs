#nullable enable
using System;
using GlassRefrain.Core;
using GlassRefrain.Targeting;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Combat {
    public interface ICombatCore {
        M0CombatSnapshot Snapshot { get; }
        void Tick(float deltaTime);
    }

    public sealed class CombatCore : ICombatCore {
        private static readonly M0CombatTimingSettings DefaultTimingSettings =
#if GR_M0_PROTOTYPE
            new M0CombatTimingSettings(0.12f, 0.18f, 0.25f, 0.08f, 0.18f, 0.25f, 0.08f, 0.16f, 0.25f, 3.0f, 0.25f);
#else
            new M0CombatTimingSettings(0.12f, 0.18f, 0.25f, 0.08f, 0.18f, 0.25f, 0.08f, 0.16f, 0.25f, 0.5f, 0.25f);
#endif

        private CombatCoreState currentState;
        private CombatActionRequestResult lastActionResult;
        private CombatResolutionResult lastResolutionResult;
        private CounterWindowState counterWindowState;
        private RevealRequestContext lastRevealRequestContext;
        private M0CombatSnapshot latestSnapshot;
        private M0TargetContext? targetContext;
        private bool parryWasEligible;
        private float actionStateElapsedSeconds;
        private readonly INhemLogger? logger;
        private readonly M0CombatTimingSettings timingSettings;

        public CombatCore(INhemLogger? logger = null)
            : this(DefaultTimingSettings, logger) {
        }

        public CombatCore(M0CombatTimingSettings timingSettings, INhemLogger? logger = null) {
            this.timingSettings = timingSettings;
            this.logger = logger;
            currentState = CombatCoreState.Neutral;
            lastActionResult = new CombatActionRequestResult(CombatActionResult.Ignored, "No action processed yet",
                currentState.ToString());
            lastResolutionResult = new CombatResolutionResult(CombatActionType.Unknown, false, false, false, false,
                string.Empty, "No resolution yet");
            counterWindowState = new CounterWindowState(false, string.Empty, 0f, 0f);
            lastRevealRequestContext = new RevealRequestContext(CombatRequestSourceType.Unknown, string.Empty,
                string.Empty, string.Empty, string.Empty);
            RefreshSnapshot();
        }

        public M0CombatSnapshot Snapshot => latestSnapshot;

        public RevealRequestContext LastRevealRequestContext => lastRevealRequestContext;

        public event Action<M0CombatSnapshot>? SnapshotChanged;
        public event Action<RevealRequestContext>? RevealRequestEmitted;

        public void SetTargetContext(M0TargetContext context) {
            targetContext = context;
        }

        // Story 1-6: Tick method for time-based state management.
        public void Tick(float deltaTime) {
            if (deltaTime <= 0f) return;

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

            float durationSeconds = GetCurrentStateDurationSeconds();
            if (durationSeconds <= 0f) return;

            actionStateElapsedSeconds += deltaTime;
            if (actionStateElapsedSeconds < durationSeconds) return;

            AdvanceState("Timed " + currentState + " complete");
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
#if GR_COMBAT_DEBUG
                logger?.Log("[M0Combat] " + request.ActionType + " ignored: combat core disabled");
#endif
                RefreshSnapshot();
                return lastActionResult;
            }

            if (currentState != CombatCoreState.Neutral) {
                lastActionResult = new CombatActionRequestResult(CombatActionResult.Rejected,
                    "Action rejected outside Neutral", currentState.ToString());
#if GR_COMBAT_DEBUG
                logger?.Log("[M0Combat] " + request.ActionType + " rejected: not in Neutral (current=" + currentState + ")");
#endif
                RefreshSnapshot();
                return lastActionResult;
            }

            lastResolutionResult = new CombatResolutionResult(
                request.ActionType,
                false,
                false,
                false,
                counterWindowState.IsOpen,
                request.Source,
                request.ActionType + " accepted; awaiting active resolution");

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
#if GR_COMBAT_DEBUG
                        logger?.Log("[M0Combat] Counter rejected: CounterWindow is not open");
#endif
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
#if GR_COMBAT_DEBUG
                    logger?.Log("[M0Combat] Unknown action type rejected");
#endif
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
#if GR_COMBAT_DEBUG
                        logger?.Log("[M0Combat] Parry success: CounterWindow opening");
#endif
                        OpenCounterWindow("ParrySuccess", timingSettings.CounterWindowDurationSeconds);
                    }
#if GR_COMBAT_DEBUG
                    else {
                        logger?.Log("[M0Combat] Parry fail: enemy intent not parry-eligible");
                    }
#endif
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

            RefreshSnapshot();
            return new CombatStepResult(previous != currentState, previous, currentState, reason);
        }

        public void OpenCounterWindow(string sourceTag, float durationSeconds) {
            counterWindowState = new CounterWindowState(true, sourceTag, 0f, durationSeconds);
#if GR_COMBAT_DEBUG
            logger?.Log("[M0Combat] CounterWindow opened duration=" + durationSeconds);
#endif
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
#if GR_COMBAT_DEBUG
            logger?.Log("[M0Combat] CounterWindow " + reason);
#endif
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

        public void ResetForEncounter(string reason) {
            currentState = CombatCoreState.Neutral;
            actionStateElapsedSeconds = 0f;
            parryWasEligible = false;

            lastActionResult = new CombatActionRequestResult(
                CombatActionResult.Ignored,
                "Encounter reset",
                CombatCoreState.Neutral.ToString());

            lastResolutionResult = new CombatResolutionResult(
                CombatActionType.Unknown,
                false,
                false,
                false,
                false,
                string.Empty,
                "Encounter reset");

            counterWindowState = new CounterWindowState(false, string.Empty, 0f, 0f);
            lastRevealRequestContext = new RevealRequestContext(
                CombatRequestSourceType.Unknown,
                "EncounterReset",
                "CombatCore",
                string.Empty,
                reason ?? "Encounter reset");

#if GR_COMBAT_DEBUG || GR_M0_PROTOTYPE
            logger?.Log("[M0Combat] ResetForEncounter -> Neutral");
#endif
            RefreshSnapshot();
        }

        private void TransitionTo(CombatCoreState nextState, string reason) {
            var previousState = currentState;
            currentState = nextState;
            if (previousState != nextState) {
                actionStateElapsedSeconds = 0f;
            }
#if GR_COMBAT_DEBUG
            if (previousState != nextState) {
                logger?.Log("[M0Combat] State changed: " + previousState + " -> " + nextState);
            }
#endif
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

#if GR_M0_PROTOTYPE || GR_MEMORY_DEBUG
        public void DebugEmitCounterRevealEvidence(string sourceLabel = "DebugCounterRevealEvidence") {
            EmitRevealRequest(sourceLabel);
        }
#endif

        private void RefreshSnapshot() {
            latestSnapshot = new M0CombatSnapshot(
                currentState,
                lastActionResult,
                lastResolutionResult,
                counterWindowState);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }

        private float GetCurrentStateDurationSeconds() {
            switch (currentState) {
                case CombatCoreState.AttackStartup:
                    return timingSettings.AttackStartupSeconds;
                case CombatCoreState.AttackActive:
                    return timingSettings.AttackActiveSeconds;
                case CombatCoreState.AttackRecovery:
                    return timingSettings.AttackRecoverySeconds;
                case CombatCoreState.DodgeStartup:
                    return timingSettings.DodgeStartupSeconds;
                case CombatCoreState.DodgeActive:
                    return timingSettings.DodgeActiveSeconds;
                case CombatCoreState.DodgeRecovery:
                    return timingSettings.DodgeRecoverySeconds;
                case CombatCoreState.ParryStartup:
                    return timingSettings.ParryStartupSeconds;
                case CombatCoreState.ParryActive:
                    return timingSettings.ParryActiveSeconds;
                case CombatCoreState.ParryRecovery:
                    return timingSettings.ParryRecoverySeconds;
                default:
                    return 0f;
            }
        }
    }
}
