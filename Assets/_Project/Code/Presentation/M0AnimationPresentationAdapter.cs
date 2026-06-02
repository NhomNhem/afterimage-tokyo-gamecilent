using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class M0AnimationPresentationAdapter : MonoBehaviour {
        private IPlayerAnimationService _playerAnimationService;
        private IEnemyAnimationService _enemyAnimationService;
        private INhemLogger _logger;
        private CombatCoreState _lastCombatState = CombatCoreState.Neutral;
        private CombatActionType _lastAttackType = CombatActionType.LightAttack;
        private LocomotionState _lastLocomotionState = LocomotionState.Uninitialized;
        private EnemyIntentState _lastEnemyIntentState = EnemyIntentState.Idle;

        [Inject]
        public void Construct(
            IPlayerAnimationService playerAnimationService,
            IEnemyAnimationService enemyAnimationService,
            INhemLogger logger) {
            _playerAnimationService = playerAnimationService;
            _enemyAnimationService = enemyAnimationService;
            _logger = logger;
        }

        public void ObserveCombatSnapshot(M0CombatSnapshot snapshot) {
            if (_playerAnimationService == null) {
                _logger?.LogError("[M0Animation] Player animation service is not injected.", this);
                return;
            }

            if (_lastCombatState == snapshot.State) {
                return;
            }

            _lastCombatState = snapshot.State;

            switch (snapshot.State) {
                case CombatCoreState.Neutral:
                    _playerAnimationService.PlayNeutral();
                    break;
                case CombatCoreState.AttackStartup:
                    _lastAttackType = ResolveAttackType(snapshot);
                    _playerAnimationService.PlayAttack(new AttackAnimationRequest(_lastAttackType, snapshot.State, snapshot.LastResolutionResult.Detail));
                    break;
                case CombatCoreState.AttackActive:
                case CombatCoreState.AttackRecovery:
                    _playerAnimationService.PlayAttack(new AttackAnimationRequest(_lastAttackType, snapshot.State, snapshot.LastResolutionResult.Detail));
                    break;
                case CombatCoreState.DodgeStartup:
                case CombatCoreState.DodgeActive:
                case CombatCoreState.DodgeRecovery:
                    _playerAnimationService.PlayDodge(new DodgeAnimationRequest(snapshot.State, snapshot.LastActionResult.Reason));
                    break;
                case CombatCoreState.ParryStartup:
                case CombatCoreState.ParryActive:
                case CombatCoreState.ParryRecovery:
                    _playerAnimationService.PlayParry(new ParryAnimationRequest(snapshot.State, snapshot.LastActionResult.Reason));
                    break;
                case CombatCoreState.CounterActive:
                case CombatCoreState.RevealBeat:
                    _playerAnimationService.PlayCounter(new AttackAnimationRequest(CombatActionType.Counter, snapshot.State, snapshot.LastResolutionResult.Detail));
                    break;
            }
        }

        public void ObserveLocomotionSnapshot(LocomotionStateSnapshot snapshot) {
            if (_playerAnimationService == null) {
                _logger?.LogError("[M0Animation] Player animation service is not injected.", this);
                return;
            }

            if (_lastCombatState != CombatCoreState.Neutral && _lastCombatState != CombatCoreState.Disabled) {
                return;
            }

            if (_lastLocomotionState == snapshot.State) {
                return;
            }

            _lastLocomotionState = snapshot.State;
            _playerAnimationService.PlayLocomotion(snapshot);
        }

        public void ObserveEnemyIntentSnapshot(EnemyIntentSnapshot snapshot) {
            if (_enemyAnimationService == null) {
                _logger?.LogError("[M0Animation] Enemy animation service is not injected.", this);
                return;
            }

            if (_lastEnemyIntentState == snapshot.State) {
                return;
            }

            _lastEnemyIntentState = snapshot.State;
            _enemyAnimationService.PlayIntent(new EnemyIntentAnimationRequest(
                snapshot.State,
                snapshot.EnemyId,
                snapshot.IntentLabel,
                snapshot.Telegraph.TelegraphId));
        }

        private static CombatActionType ResolveAttackType(M0CombatSnapshot snapshot) {
            var actionType = snapshot.LastResolutionResult.ActionType;
            if (actionType == CombatActionType.LightAttack || actionType == CombatActionType.HeavyAttack) {
                return actionType;
            }

            return CombatActionType.LightAttack;
        }
    }
}
