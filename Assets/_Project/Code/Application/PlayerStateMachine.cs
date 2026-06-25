using R3;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;

namespace GlassRefrain.Application;

public sealed class PlayerStateMachine {
    private readonly StateMachine _locoSM;
    private readonly StateMachine _combatSM;
    private readonly LocomotionCore _locomotion;
    private readonly CombatCore _combat;
    private readonly PlayerInputHandler _inputHandler;
    private readonly INhemLogger _logger;
    private readonly ReactiveProperty<string> _fullState;
    private readonly ReactiveProperty<CombatCoreState> _combatPhase;
    private PlayerFrame _lastFrame;

    public string LocoState => _locoSM.CurrentKey;
    public string CombatState => _combatSM.CurrentKey;
    public PlayerFrame Frame => _lastFrame;
    public Observable<string> FullState => _fullState;
    public Observable<CombatCoreState> CombatPhaseChanged => _combatPhase;

    public PlayerStateMachine(LocomotionCore locomotion, CombatCore combat, INhemLogger logger, ImmunityLayer immunity) {
        _locomotion = locomotion;
        _combat = combat;
        _inputHandler = new PlayerInputHandler();
        _logger = logger;
        _lastFrame = default;
        _fullState = new ReactiveProperty<string>("Idle|Neutral");
        _combatPhase = new ReactiveProperty<CombatCoreState>(CombatCoreState.Neutral);

        _locoSM = new StateMachine("Loco", logger);
        _locoSM.AddState(LocoIdleState.Key, new LocoIdleState(locomotion));
        _locoSM.AddState(LocoMovingState.Key, new LocoMovingState(locomotion));
        _locoSM.TransitionTo(LocoIdleState.Key, default);

        _combatSM = new StateMachine("Combat", logger);
        _combatSM.AddState(CombatNeutralState.Key, new CombatNeutralState(combat));
        _combatSM.AddState(CombatAttackingState.Key, new CombatAttackingState(combat));
        _combatSM.AddState(CombatDodgingState.Key, new CombatDodgingState(combat));
        _combatSM.AddState(CombatParryingState.Key, new CombatParryingState(combat));
        _combatSM.AddState(CombatCounteringState.Key, new CombatCounteringState(combat));
        _combatSM.AddState(CombatStunnedState.Key, new CombatStunnedState(combat));
        _combatSM.AddState(IFrameState.Key, new IFrameState(immunity));
        _combatSM.TransitionTo(CombatNeutralState.Key, default);
    }

    public void Tick(InputIntentSnapshot intent, float deltaTime) {
        var combatSnapshot = _combat.Snapshot;
        var locoSnapshot = _locomotion.Snapshot;

        var ctx = new StateContext(
            deltaTime,
            combatSnapshot.State,
            locoSnapshot.State,
            _inputHandler.BuildSnapshot(intent));

        _locoSM.Update(ctx);
        _combatSM.Update(ctx);

        EvaluateAndTransition(_locoSM, ctx);
        EvaluateAndTransition(_combatSM, ctx);

        var control = ControlResolver.Resolve(combatSnapshot, locoSnapshot);
        _locomotion.Tick(intent, control, deltaTime);
        _combat.Tick(deltaTime);

        var updatedControl = ControlResolver.Resolve(_combat.Snapshot, _locomotion.Snapshot);
        var movement = _locomotion.GetMovementSnapshot();

        _lastFrame = new PlayerFrame(
            movement.Position, movement.Facing, movement.Velocity,
            movement.Velocity.magnitude, false,
            combatSnapshot.LastResolutionResult.ActionType,
            combatSnapshot.State,
            combatSnapshot.CounterWindow.IsOpen,
            100f, 100f,
            updatedControl.CanMove, updatedControl.CanAttack, updatedControl.CanRotate);

        var label = _locoSM.CurrentKey + "|" + _combatSM.CurrentKey;
        if (label != _fullState.Value) {
            _fullState.Value = label;
        }
        if (combatSnapshot.State != _combatPhase.Value) {
            _combatPhase.Value = combatSnapshot.State;
        }
    }

    private void EvaluateAndTransition(StateMachine sm, StateContext ctx) {
        var next = sm.CurrentState?.EvaluateTransition(ctx);
        if (next.HasValue) {
            if (sm == _combatSM) {
                var combatKey = MapPlayerStateTypeToCombatKey(next.Value);
                if (combatKey != null) sm.TransitionTo(combatKey, ctx);
            } else if (sm == _locoSM) {
                if (next.Value == PlayerStateType.Idle) sm.TransitionTo(LocoIdleState.Key, ctx);
                else if (next.Value == PlayerStateType.Moving) sm.TransitionTo(LocoMovingState.Key, ctx);
            }
        }
    }

    private static string MapPlayerStateTypeToCombatKey(PlayerStateType pst) {
        return pst switch {
            PlayerStateType.Idle => CombatNeutralState.Key,
            PlayerStateType.Moving => CombatNeutralState.Key,
            PlayerStateType.Attacking => CombatAttackingState.Key,
            PlayerStateType.Dodging => CombatDodgingState.Key,
            PlayerStateType.Parrying => CombatParryingState.Key,
            PlayerStateType.Countering => CombatCounteringState.Key,
            PlayerStateType.Stunned => CombatStunnedState.Key,
            PlayerStateType.Dead => CombatStunnedState.Key,
            _ => null
        };
    }

    public M0CombatSnapshot CombatSnapshot => _combat.Snapshot;
    public bool TryBeginDashDisplacement(Vector3 direction) => _locomotion.TryBeginDashDisplacement(direction);

    public void ConsumeAttackIntent(CombatActionType actionType) => _combat.ConsumeAttackIntent(actionType);
    public void ConsumeDefensiveIntent(CombatActionType actionType, EnemyIntentSnapshot enemy) => _combat.ConsumeDefensiveIntent(actionType, enemy);

    public static class ControlResolver {
        public static ControlState Resolve(M0CombatSnapshot combat, LocomotionStateSnapshot locomotion) {
            bool canMove = combat.State == CombatCoreState.Neutral;
            bool canAttack = combat.State == CombatCoreState.Neutral;
            bool canRotate = combat.State == CombatCoreState.Neutral;
            if (combat.State is CombatCoreState.AttackStartup or CombatCoreState.AttackActive
                or CombatCoreState.AttackRecovery or CombatCoreState.ParryStartup
                or CombatCoreState.ParryActive or CombatCoreState.ParryRecovery
                or CombatCoreState.CounterActive or CombatCoreState.RevealBeat) {
                canMove = false;
                canRotate = false;
            }
            return new ControlState(canMove, canAttack, canRotate);
        }
    }
}
