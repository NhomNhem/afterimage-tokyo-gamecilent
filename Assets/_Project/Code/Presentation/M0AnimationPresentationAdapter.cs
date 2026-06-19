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
        private IDisposable _locomotionSubscription;
        private PlayerStateSnapshot _lastPlayerSnapshot;
        private bool _hasPlayerSnapshot;
        private IPlayerStateMachine _activeStateMachine;

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
            _locomotionSubscription?.Dispose();
            _activeStateMachine = stateMachine;
            _playerStateSubscription = stateMachine.StateChanges.Subscribe(OnPlayerStateChanged);
            _locomotionSubscription = stateMachine.LocomotionChanges.Subscribe(OnLocomotionTick);
            OnPlayerStateChanged(stateMachine.CurrentSnapshot);
            SubscribeToTurnSignal();
        }

        private void SubscribeToTurnSignal() {
            _playerAnimationService.TurnActiveChanged -= OnTurnActiveChanged;
            _playerAnimationService.TurnActiveChanged += OnTurnActiveChanged;
        }

        private void OnTurnActiveChanged(bool isTurnActive) {
            if (_activeStateMachine == null) return;
            _activeStateMachine.SetMovementLockedForTurn(isTurnActive, "TurnInPlace");
        }

        private void OnLocomotionTick(LocomotionStateSnapshot locomotionSnapshot) {
            if (_playerAnimationService == null) return;
            if (!_hasPlayerSnapshot) return;

            var snapshot = _lastPlayerSnapshot;
            if (snapshot.ResolvedState != PlayerState.Idle && snapshot.ResolvedState != PlayerState.Moving) {
                return;
            }

            var relativeDirection = ComputeRelativeDirection(snapshot);
            _playerAnimationService.PlayLocomotion(locomotionSnapshot, relativeDirection);
        }

        private void OnPlayerStateChanged(PlayerStateSnapshot snapshot) {
            _lastPlayerSnapshot = snapshot;
            _hasPlayerSnapshot = true;

            if (_playerAnimationService == null) {
                _logger?.LogError("[M0Animation] Player animation service is not injected.", this);
                return;
            }

            var isCombatMode = snapshot.HasTargetFocus || snapshot.CombatState != CombatCoreState.Neutral;
            _playerAnimationService.SetCombatMode(isCombatMode);

            var relativeDirection = ComputeRelativeDirection(snapshot);

            switch (snapshot.ResolvedState) {
                case PlayerState.Idle:
                case PlayerState.Moving:
                    _playerAnimationService.PlayLocomotion(snapshot.LocomotionState, snapshot, relativeDirection);
                    break;
                case PlayerState.Dodge:
                    var dashDir = ResolveDashDirection(relativeDirection);
                    _playerAnimationService.PlayDash(dashDir);
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

        private static Vector2 ComputeRelativeDirection(PlayerStateSnapshot snapshot) {
            var moveX = snapshot.MovementDirection.X;
            var moveY = snapshot.MovementDirection.Y;
            var faceX = snapshot.FacingDirection.X;
            var faceY = snapshot.FacingDirection.Y;

            var facingMag = Mathf.Sqrt(faceX * faceX + faceY * faceY);
            if (facingMag < 0.001f) return Vector2.zero;

            var normFaceX = faceX / facingMag;
            var normFaceY = faceY / facingMag;

            var forward = moveX * normFaceX + moveY * normFaceY;
            var right = moveX * normFaceY - moveY * normFaceX;

            return new Vector2(right, forward);
        }

        private static DashDirection ResolveDashDirection(Vector2 relativeDirection) {
            var absX = Mathf.Abs(relativeDirection.x);
            var absY = Mathf.Abs(relativeDirection.y);

            if (absX < 0.1f && absY < 0.1f) return DashDirection.Forward;

            if (absY >= absX) {
                return relativeDirection.y >= 0f ? DashDirection.Forward : DashDirection.Back;
            }

            return relativeDirection.x >= 0f ? DashDirection.Right : DashDirection.Left;
        }

        private void OnDestroy() {
            if (_playerAnimationService != null) {
                _playerAnimationService.TurnActiveChanged -= OnTurnActiveChanged;
            }
            _playerStateSubscription?.Dispose();
            _playerStateSubscription = null;
            _locomotionSubscription?.Dispose();
            _locomotionSubscription = null;
        }
    }
}
