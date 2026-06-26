using System;
using GlassRefrain.Application;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using R3;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class AnimationFacade : MonoBehaviour {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float maxMoveSpeed = 5.0f;

        private PlayerStateMachine _stateMachine;
        private CombatModeTracker _combatModeTracker;
        private IPlayerAnimationService _playerAnimationService;
        private IEnemyAnimationService _enemyAnimationService;
        private INhemLogger _logger;
        private IDisposable _stateSubscription;
        private IDisposable _combatSubscription;
        private IDisposable _phaseSubscription;

        private EnemyIntentState _lastEnemyIntentState = EnemyIntentState.Idle;
        private float _suppressLocomotionUntil;

        [Inject]
        public void Construct(
            PlayerStateMachine stateMachine,
            CombatModeTracker combatModeTracker,
            IPlayerAnimationService playerAnimationService,
            IEnemyAnimationService enemyAnimationService,
            INhemLogger logger) {
            _stateMachine = stateMachine;
            _combatModeTracker = combatModeTracker;
            _playerAnimationService = playerAnimationService;
            _enemyAnimationService = enemyAnimationService;
            _logger = logger;
        }

        private void Start() {
            _stateSubscription = _stateMachine.FullState
                .DistinctUntilChanged()
                .Subscribe(_ => OnFullStateChanged());

            _combatSubscription = _combatModeTracker.IsInCombat
                .DistinctUntilChanged()
                .Skip(1)
                .Subscribe(isCombat => HandleCombatModeChanged(isCombat));

            _phaseSubscription = _stateMachine.CombatPhaseChanged
                .DistinctUntilChanged()
                .Subscribe(phase => HandleSubPhaseTransition(phase));
        }

        private void HandleCombatModeChanged(bool isCombat) {
            if (isCombat)
                _playerAnimationService?.PlayEnterCombat();
            else
                _playerAnimationService?.PlayExitCombat();
            _playerAnimationService?.SetCombatMode(isCombat);
        }

        private void OnDestroy() {
            _stateSubscription?.Dispose();
            _stateSubscription = null;
            _combatSubscription?.Dispose();
            _combatSubscription = null;
            _phaseSubscription?.Dispose();
            _phaseSubscription = null;
        }

        public void ObserveEnemyIntentSnapshot(EnemyIntentSnapshot snapshot) {
            if (_enemyAnimationService == null) return;
            if (_lastEnemyIntentState == snapshot.State) return;
            _lastEnemyIntentState = snapshot.State;
            _enemyAnimationService.PlayIntent(new EnemyIntentAnimationRequest(
                snapshot.State, snapshot.EnemyId, snapshot.IntentLabel, snapshot.Telegraph.TelegraphId));
        }

        public void ObserveEnemyHitReaction() {
            if (_enemyAnimationService == null) return;
            _enemyAnimationService.PlayHitReaction(new HitReactionAnimationRequest(
                CombatCoreState.HitReact, "ConfirmedHit"));
        }

        public void TriggerDashLeft() {
            if (_playerAnimationService == null) return;
            _suppressLocomotionUntil = Time.time + 1.0f;
            _playerAnimationService.PlayDash(DashDirection.Left);
        }

        public void TriggerDashRight() {
            if (_playerAnimationService == null) return;
            _suppressLocomotionUntil = Time.time + 1.0f;
            _playerAnimationService.PlayDash(DashDirection.Right);
        }

        public void TriggerDashBack() {
            if (_playerAnimationService == null) return;
            _suppressLocomotionUntil = Time.time + 1.0f;
            _playerAnimationService.PlayDash(DashDirection.Back);
        }

        public void PlayJump() {
            if (_playerAnimationService == null) return;
            _suppressLocomotionUntil = Time.time + 1.0f;
            _playerAnimationService.PlayJump();
        }

        private void OnFullStateChanged() {
            if (_playerAnimationService == null) return;
            if (Time.time < _suppressLocomotionUntil) return;

            var frame = _stateMachine.Frame;

            var combatKey = _stateMachine.CombatState;
            var isCombatAction = combatKey is "Attacking" or "Dodging" or "Parrying"
                or "Countering" or "Stunned";

            if (isCombatAction) {
                _combatModeTracker.NotifyCombatActivity();
            }

            switch (combatKey) {
                case "Neutral":
                    var locoKey = _stateMachine.LocoState;
                    PlayLocomotion(frame, locoKey == "Moving");
                    break;
                case "Attacking":
                    _playerAnimationService.PlayAttack(new AttackAnimationRequest(
                        frame.CurrentCombatAction, frame.CurrentCombatPhase, string.Empty));
                    break;
                case "Dodging":
                    _playerAnimationService.PlayDash(DashDirection.Forward);
                    break;
                case "Parrying":
                    _playerAnimationService.PlayParry(new ParryAnimationRequest(
                        frame.CurrentCombatPhase, string.Empty));
                    break;
                case "Countering":
                    _playerAnimationService.PlayCounter(new CounterAnimationRequest(
                        frame.CurrentCombatPhase, string.Empty));
                    break;
                case "Stunned":
                    _playerAnimationService.PlayHitReaction(new HitReactionAnimationRequest(
                        frame.CurrentCombatPhase, string.Empty));
                    break;
            }
        }

        private void HandleSubPhaseTransition(CombatCoreState state) {
            var frame = _stateMachine.Frame;
            switch (state) {
                case CombatCoreState.AttackStartup:
                case CombatCoreState.AttackActive:
                case CombatCoreState.AttackRecovery:
                    _combatModeTracker.NotifyCombatActivity();
                    _playerAnimationService.PlayAttack(new AttackAnimationRequest(
                        frame.CurrentCombatAction, frame.CurrentCombatPhase, string.Empty));
                    break;
                case CombatCoreState.DodgeStartup:
                case CombatCoreState.DodgeActive:
                case CombatCoreState.DodgeRecovery:
                    _combatModeTracker.NotifyCombatActivity();
                    _playerAnimationService.PlayDash(DashDirection.Forward);
                    break;
                case CombatCoreState.ParryStartup:
                case CombatCoreState.ParryActive:
                case CombatCoreState.ParryRecovery:
                    _combatModeTracker.NotifyCombatActivity();
                    _playerAnimationService.PlayParry(new ParryAnimationRequest(
                        frame.CurrentCombatPhase, string.Empty));
                    break;
                case CombatCoreState.CounterWindow:
                case CombatCoreState.CounterActive:
                case CombatCoreState.RevealBeat:
                    _combatModeTracker.NotifyCombatActivity();
                    _playerAnimationService.PlayCounter(new CounterAnimationRequest(
                        frame.CurrentCombatPhase, string.Empty));
                    break;
            }
        }

        private void PlayLocomotion(PlayerFrame frame, bool isMoving) {
            Transform t = playerTransform != null ? playerTransform : transform;
            Vector3 forward = t.forward;
            Vector3 right = t.right;
            float forwardSpeed = Vector3.Dot(frame.MoveVelocity, forward);
            float strafeSpeed = Vector3.Dot(frame.MoveVelocity, right);
            float moveAmount = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxMoveSpeed);
            float strafeAmount = Mathf.Clamp(strafeSpeed / maxMoveSpeed, -1f, 1f);

            var locoState = isMoving ? LocomotionState.Moving : LocomotionState.Idle;
            var locoSnapshot = new LocomotionStateSnapshot(
                locoState, new Axis2(strafeAmount, moveAmount), true,
                new MovementRestrictionContext(frame.CanMove, frame.CanRotate, 0f, string.Empty),
                new RecoveryContext(RecoverySource.Unknown, false, 0f, string.Empty),
                new CameraMovementBasisSnapshot(new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Deferred"),
                string.Empty);
            _playerAnimationService.PlayLocomotion(locoSnapshot, new Vector2(strafeAmount, moveAmount));
        }
    }
}
