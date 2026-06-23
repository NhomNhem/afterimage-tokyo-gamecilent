using System;
using GlassRefrain.Application;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using R3;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class M0AnimationPresentationAdapter : MonoBehaviour {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float maxMoveSpeed = 5.0f;

        private IPlayerAnimationService _playerAnimationService;
        private IEnemyAnimationService _enemyAnimationService;
        private ITurnDetectionSource _turnDetection;
        private INhemLogger _logger;
        private EnemyIntentState _lastEnemyIntentState = EnemyIntentState.Idle;
        private IDisposable _playerStateSubscription;
        private IDisposable _locomotionSubscription;
        private PlayerStateSnapshot _lastPlayerSnapshot;
        private bool _hasPlayerSnapshot;
        private Vector3 _previousEulerAngles = Vector3.zero;

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
            _playerStateSubscription = stateMachine.StateChanges.Subscribe(OnPlayerStateChanged);
            _locomotionSubscription = stateMachine.LocomotionChanges.Subscribe(OnLocomotionTick);
            OnPlayerStateChanged(stateMachine.CurrentSnapshot);
        }

        public void SubscribeToTurnDetection(ITurnDetectionSource turnDetection) {
            _turnDetection = turnDetection;
            turnDetection.SharpTurnDetected += OnSharpTurnDetected;
        }

        private void OnLocomotionTick(LocomotionStateSnapshot locomotionSnapshot) {
            if (_playerAnimationService == null) return;

            if (_hasPlayerSnapshot) {
                UpdateLocomotionParameters(locomotionSnapshot);
            }

            if (!_hasPlayerSnapshot) return;

            var snapshot = _lastPlayerSnapshot;
            if (snapshot.ResolvedState != PlayerState.Idle && snapshot.ResolvedState != PlayerState.Moving) {
                return;
            }

            var rawDirection = new Vector2(locomotionSnapshot.MoveIntent.X, locomotionSnapshot.MoveIntent.Y);
            _playerAnimationService.PlayLocomotion(locomotionSnapshot, rawDirection);
        }

        private void UpdateLocomotionParameters(LocomotionStateSnapshot locomotionSnapshot) {
            if (playerTransform == null) return;

            Vector3 velocity = locomotionSnapshot.WorldVelocity;
            Vector3 forward = playerTransform.forward;
            Vector3 right = playerTransform.right;

            float forwardSpeed = Vector3.Dot(velocity, forward);
            float strafeSpeed = Vector3.Dot(velocity, right);

            float moveAmount = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxMoveSpeed);
            float strafeAmount = Mathf.Clamp(strafeSpeed / maxMoveSpeed, -1f, 1f);

            // Compute rotationValue from angular delta (FS Melee HandleTurning pattern)
            Vector3 currentEuler = playerTransform.eulerAngles;
            Vector3 eulerDelta = currentEuler - _previousEulerAngles;
            float rotationValue = 0f;
            if (Mathf.Abs(eulerDelta.y) > 0.5f) {
                rotationValue = Mathf.Sign(eulerDelta.y) * 0.5f;
            }
            _previousEulerAngles = currentEuler;

            _playerAnimationService.SetLocomotionParameters(moveAmount, strafeAmount, rotationValue);
        }

        private void OnSharpTurnDetected(bool isRightTurn) {
            if (_playerAnimationService == null) return;
            _playerAnimationService.PlayTurn(isRightTurn ? TurnDirection.Right : TurnDirection.Left);
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

            var rawDirection = new Vector2(snapshot.MovementDirection.X, snapshot.MovementDirection.Y);

            switch (snapshot.ResolvedState) {
                case PlayerState.Idle:
                case PlayerState.Moving:
                    _playerAnimationService.PlayLocomotion(snapshot.LocomotionState, snapshot, rawDirection);
                    break;
                case PlayerState.Dodge:
                    var dodgeDirection = ResolveDashDirection(rawDirection);
#if GR_M0_PROTOTYPE
                    _logger?.Log("[M0Animation] Dodge: moveDir=(" + snapshot.MovementDirection.X + "," + snapshot.MovementDirection.Y
                        + ") faceDir=(" + snapshot.FacingDirection.X + "," + snapshot.FacingDirection.Y
                        + ") rawDir=(" + rawDirection.x + "," + rawDirection.y
                        + ") resolved=" + dodgeDirection);
#endif
                    _playerAnimationService.PlayDash(dodgeDirection);
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
                    _playerAnimationService.PlayCounter(new CounterAnimationRequest(
                        snapshot.CombatState, snapshot.StateDetail));
                    break;
                case PlayerState.RevealBeat:
                    _playerAnimationService.PlayCounter(new CounterAnimationRequest(
                        snapshot.CombatState, "RevealBeat"));
                    break;
                case PlayerState.HitReaction:
                    _playerAnimationService.PlayHitReaction(new HitReactionAnimationRequest(
                        snapshot.CombatState, snapshot.StateDetail));
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

        public void ObserveEnemyHitReaction() {
            if (_enemyAnimationService == null) return;

            _enemyAnimationService.PlayHitReaction(new HitReactionAnimationRequest(
                CombatCoreState.HitReact, "ConfirmedHit"));
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

        private void Start() {
            if (playerTransform != null) {
                _previousEulerAngles = playerTransform.eulerAngles;
            }
        }

        private void OnDestroy() {
            if (_turnDetection != null) {
                _turnDetection.SharpTurnDetected -= OnSharpTurnDetected;
                _turnDetection = null;
            }
            _playerStateSubscription?.Dispose();
            _playerStateSubscription = null;
            _locomotionSubscription?.Dispose();
            _locomotionSubscription = null;
        }
    }
}
