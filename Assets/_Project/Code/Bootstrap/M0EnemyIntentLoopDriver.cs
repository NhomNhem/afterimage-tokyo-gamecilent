using UnityEngine;
using VContainer;
using NhemDangFugBixs.NhemLogging;
using GlassRefrain.Core;
using GlassRefrain.Enemy;

namespace GlassRefrain.Bootstrap {
    public sealed class M0EnemyIntentLoopDriver : MonoBehaviour, IEnemyDebugHarness {
        [SerializeField] private float idleDuration = 1.2f;
        [SerializeField] private float telegraphDuration = 0.9f;
        [SerializeField] private float commitDuration = 0.25f;
        [SerializeField] private float activeDuration = 0.2f;
        [SerializeField] private float recoveryDuration = 0.65f;
        [SerializeField] private float punishWindowDuration = 0.4f;
        [SerializeField] private string telegraphId = "BasicSlashTelegraph";
        [SerializeField] private string attackId = "BasicSlash";
        [SerializeField] private string attackLabel = "M0BasicSlash";

#if GR_M0_PROTOTYPE
        [SerializeField] private float debugParryEligibleActiveDuration = 3.0f;
#endif

        private M0EnemyIntentModel _model;
        private INhemLogger _logger;

        private float _loopTimer;
        private int _loopPhase; // 0=Idle, 1=Telegraph, 2=Commit, 3=Active, 4=Recovery
        private bool _isForcedParryEligibleActive;
        private float _forcedActiveRemainingSeconds;

        private const float MinIdleDuration = 0.5f;
        private const float MinTelegraphDuration = 0.5f;
        private const float MinCommitDuration = 0.1f;
        private const float MinActiveDuration = 0.1f;
        private const float MinRecoveryDuration = 0.3f;
        private const float MinPunishWindowDuration = 0.2f;
#if GR_ENEMY_DEBUG
        private float _debugTickLogTimer;
#endif

        [Inject]
        public void Construct(M0EnemyIntentModel enemyIntentModel, INhemLogger logger) {
            _model = enemyIntentModel;
            _logger = logger;
        }

        private void Start() {
            _logger?.Log("[M0EnemyLoop] Driver initialized");
            if (_model == null) {
                _logger?.LogWarning("[M0EnemyIntentLoopDriver] M0EnemyIntentModel not injected. Loop will not run.");
                return;
            }

            SanitizeTimingConfig();
            _model?.EnterIdle(BuildPhaseLabel("Idle", "LoopIdle", idleDuration));
            _loopTimer = 0f;
            _loopPhase = 0;
        }

        public void Tick(float deltaTime) {
            if (_model == null) return;

            if (_isForcedParryEligibleActive) {
                _forcedActiveRemainingSeconds = Mathf.Max(0f, _forcedActiveRemainingSeconds - deltaTime);
                _loopTimer = _forcedActiveRemainingSeconds;

#if GR_ENEMY_DEBUG
                _debugTickLogTimer += deltaTime;
                if (_debugTickLogTimer >= 1f) {
                    _logger?.Log($"[M0EnemyLoop] Tick phase=ForcedActive timer={_forcedActiveRemainingSeconds:F2} dt={deltaTime:F3}");
                    _debugTickLogTimer = 0f;
                }
#endif

                // During forced window, natural loop transitions are suspended.
                if (_forcedActiveRemainingSeconds > 0f) {
                    return;
                }

                _isForcedParryEligibleActive = false;
                _model.EnterRecovery(recoveryDuration, "ForcedActiveRecovery", true, punishWindowDuration, "RecoveryEnd");
                _loopPhase = 4;
                _loopTimer = 0f;
                return;
            }

            _loopTimer += deltaTime;

#if GR_ENEMY_DEBUG
            _debugTickLogTimer += deltaTime;
            if (_debugTickLogTimer >= 1f) {
                _logger?.Log($"[M0EnemyLoop] Tick phase={GetPhaseName(_loopPhase)} timer={_loopTimer:F2} dt={deltaTime:F3}");
                _debugTickLogTimer = 0f;
            }
#endif

            switch (_loopPhase) {
                case 0: // Idle
                    if (_loopTimer >= idleDuration) {
                        _logger?.Log("[M0EnemyLoop] Transition Idle -> Telegraph");
                        _model?.EnterTelegraph(
                            telegraphId,
                            telegraphDuration,
                            BuildPhaseLabel("Telegraph", telegraphId, telegraphDuration));
                        _loopTimer = 0f;
                        _loopPhase = 1;
                    }
                    break;
                case 1: // Telegraph
                    if (_loopTimer >= telegraphDuration) {
                        var attackIntent = new EnemyAttackIntentContext(
                            attackId,
                            attackLabel,
                            activeDuration,
                            new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible", "CounterOnWhiff" })
                        );
                        _logger?.Log("[M0EnemyLoop] Transition Telegraph -> Commit");
                        _model?.EnterCommit(
                            attackIntent,
                            commitDuration,
                            BuildPhaseLabel("Commit", attackId, commitDuration));
                        _loopTimer = 0f;
                        _loopPhase = 2;
                    }
                    break;
                case 2: // Commit
                    if (_loopTimer >= commitDuration) {
                        _logger?.Log("[M0EnemyLoop] Transition Commit -> Active");
                        _model?.EnterActive(activeDuration, BuildPhaseLabel("Active", attackId, activeDuration));
                        _loopTimer = 0f;
                        _loopPhase = 3;
                    }
                    break;
                case 3: // Active
                    if (_loopTimer >= activeDuration) {
                        _logger?.Log("[M0EnemyLoop] Transition Active -> Recovery");
                        _model?.EnterRecovery(
                            recoveryDuration,
                            BuildPhaseLabel("Recovery", "RecoveryEnd", recoveryDuration),
                            true,
                            punishWindowDuration,
                            "RecoveryEnd");
                        _loopTimer = 0f;
                        _loopPhase = 4;
                    }
                    break;
                case 4: // Recovery
                    if (_loopTimer >= recoveryDuration) {
                        _logger?.Log("[M0EnemyLoop] Transition Recovery -> Idle");
                        _model?.EnterIdle(BuildPhaseLabel("Idle", "LoopIdle", idleDuration));
                        _loopTimer = 0f;
                        _loopPhase = 0;
                    }
                    break;
            }
        }

        private void SanitizeTimingConfig() {
            idleDuration = Mathf.Max(MinIdleDuration, idleDuration);
            telegraphDuration = Mathf.Max(MinTelegraphDuration, telegraphDuration);
            commitDuration = Mathf.Max(MinCommitDuration, commitDuration);
            activeDuration = Mathf.Max(MinActiveDuration, activeDuration);
            recoveryDuration = Mathf.Max(MinRecoveryDuration, recoveryDuration);
            punishWindowDuration = Mathf.Max(MinPunishWindowDuration, punishWindowDuration);
        }

        private static string BuildPhaseLabel(string phase, string cue, float durationSeconds) {
            return phase + ":" + cue + " (" + durationSeconds.ToString("F2") + "s)";
        }

        public void ResetForEncounter(string reason) {
            _loopTimer = 0f;
            _loopPhase = 0;
            _isForcedParryEligibleActive = false;
            _forcedActiveRemainingSeconds = 0f;
#if GR_ENEMY_DEBUG
            _debugTickLogTimer = 0f;
#endif

            if (_model != null) {
                _model.ResetForEncounter(string.IsNullOrEmpty(reason) ? "EncounterReset" : reason);
            }

#if GR_M0_PROTOTYPE || GR_ENEMY_DEBUG
            _logger?.Log("[M0EnemyLoop] ResetForEncounter -> Idle");
#endif
        }

#if GR_ENEMY_DEBUG
        private string GetPhaseName(int phase) {
            return phase switch {
                0 => "Idle",
                1 => "Telegraph",
                2 => "Commit",
                3 => "Active",
                4 => "Recovery",
                _ => "Unknown"
            };
        }
#endif

#if GR_M0_PROTOTYPE
        [ContextMenu("Debug: Force ParryEligible Active (CounterWindow Verification)")]
        public void DebugForceParryEligibleActive() {
            if (_model == null) {
                _logger?.LogWarning("[M0Debug] M0EnemyIntentModel not injected. Cannot force debug state.");
                return;
            }

            var previousState = _model.Snapshot.State;
            var attackIntent = new EnemyAttackIntentContext(
                attackId,
                attackLabel,
                debugParryEligibleActiveDuration,
                new EnemyAttackTagSet(new[] { "DodgePunishable", "ParryEligible", "CounterOnWhiff" })
            );

            _model.EnterCommit(attackIntent, debugParryEligibleActiveDuration, "DebugForceParryEligible");
            _model.EnterActive(debugParryEligibleActiveDuration, "DebugParryEligibleActive");

            // Align loop driver internals with forced active window so natural loop does not overwrite it.
            _isForcedParryEligibleActive = true;
            _forcedActiveRemainingSeconds = debugParryEligibleActiveDuration;
            _loopPhase = 3; // Active phase
            _loopTimer = debugParryEligibleActiveDuration;

            var newState = _model.Snapshot.State;
            _logger?.Log($"[M0Debug] Forced enemy ParryEligible Active for {debugParryEligibleActiveDuration}s. Previous state: {previousState} -> Current state: {newState}. Press Q to Parry.");
        }
#endif
    }
}
