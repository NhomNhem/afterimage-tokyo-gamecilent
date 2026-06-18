using System;
using GlassRefrain.Application;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using R3;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class M0AnimationPresentationAdapter : MonoBehaviour {
        private IPlayerAnimationService _playerAnimationService;
        private IEnemyAnimationService _enemyAnimationService;
        private INhemLogger _logger;
        private EnemyIntentState _lastEnemyIntentState = EnemyIntentState.Idle;
        private IDisposable _playerStateSubscription;

        [Inject]
        public void Construct(
            IPlayerAnimationService playerAnimationService,
            IEnemyAnimationService enemyAnimationService,
            INhemLogger logger) {
            _playerAnimationService = playerAnimationService;
            _enemyAnimationService = enemyAnimationService;
            _logger = logger;
        }

        public void ObservePlayerState(IPlayerStateMachine stateMachine) {
            _playerStateSubscription?.Dispose();
            _playerStateSubscription = stateMachine.StateChanges.Subscribe(OnPlayerStateChanged);
            OnPlayerStateChanged(stateMachine.CurrentSnapshot);
        }

        private void OnPlayerStateChanged(PlayerStateSnapshot snapshot) {
            if (_playerAnimationService == null) {
                _logger?.LogError("[M0Animation] Player animation service is not injected.", this);
                return;
            }

            var isCombatMode = snapshot.HasTargetFocus || snapshot.CombatState != CombatCoreState.Neutral;
            _playerAnimationService.SetCombatMode(isCombatMode);

            switch (snapshot.ResolvedState) {
                case PlayerState.Idle:
                case PlayerState.Moving:
                    _playerAnimationService.PlayLocomotion(snapshot.LocomotionState, snapshot);
                    break;
                case PlayerState.Dodge:
                    _playerAnimationService.PlayDodge(new DodgeAnimationRequest(
                        snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.Parry:
                    _playerAnimationService.PlayParry(new ParryAnimationRequest(
                        snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.Attack:
                    _playerAnimationService.PlayAttack(new AttackAnimationRequest(
                        ResolveAttackType(snapshot), snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.CounterActive:
                    _playerAnimationService.PlayCounter(new AttackAnimationRequest(
                        CombatActionType.Counter, snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.RevealBeat:
                    _playerAnimationService.PlayCounter(new AttackAnimationRequest(
                        CombatActionType.Counter, snapshot.CombatState, "RevealBeat"));
                    break;
                case PlayerState.HitReaction:
                    _playerAnimationService.PlayHitReaction(new AttackAnimationRequest(
                        CombatActionType.LightAttack, snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.Disabled:
                    _playerAnimationService.PlayNeutral();
                    break;
            }
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

        private static CombatActionType ResolveAttackType(PlayerStateSnapshot snapshot) {
            var actionType = snapshot.LastResolutionResult.ActionType;
            if (actionType == CombatActionType.LightAttack || actionType == CombatActionType.HeavyAttack) {
                return actionType;
            }

            return CombatActionType.LightAttack;
        }

        private void OnDestroy() {
            _playerStateSubscription?.Dispose();
            _playerStateSubscription = null;
        }
    }
}
