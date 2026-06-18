using GlassRefrain.Application;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode;

public class M0StateMachineDecompositionTests {
    [Test]
    public void CombatStateToPlayerState_EachCombatCoreState_ReturnsCorrectPlayerState() {
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.Neutral), Is.EqualTo(PlayerState.Idle));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.AttackStartup), Is.EqualTo(PlayerState.Attack));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.AttackActive), Is.EqualTo(PlayerState.Attack));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.AttackRecovery), Is.EqualTo(PlayerState.Attack));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.CounterWindow), Is.EqualTo(PlayerState.Attack));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.DodgeStartup), Is.EqualTo(PlayerState.Dodge));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.DodgeActive), Is.EqualTo(PlayerState.Dodge));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.DodgeRecovery), Is.EqualTo(PlayerState.Dodge));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.ParryStartup), Is.EqualTo(PlayerState.Parry));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.ParryActive), Is.EqualTo(PlayerState.Parry));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.ParryRecovery), Is.EqualTo(PlayerState.Parry));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.CounterActive), Is.EqualTo(PlayerState.CounterActive));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.RevealBeat), Is.EqualTo(PlayerState.RevealBeat));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.HitReact), Is.EqualTo(PlayerState.HitReaction));
        Assert.That(CombatStateMachine.CombatStateToPlayerState(CombatCoreState.Disabled), Is.EqualTo(PlayerState.Disabled));
    }

    [Test]
    public void LocomotionStateToGroundState_EachLocomotionState_ReturnsCorrectGroundState() {
        Assert.That(LocomotionStateMachine.LocomotionStateToGroundState(LocomotionState.Uninitialized), Is.EqualTo(GroundState.Idle));
        Assert.That(LocomotionStateMachine.LocomotionStateToGroundState(LocomotionState.Idle), Is.EqualTo(GroundState.Idle));
        Assert.That(LocomotionStateMachine.LocomotionStateToGroundState(LocomotionState.Moving), Is.EqualTo(GroundState.Moving));
        Assert.That(LocomotionStateMachine.LocomotionStateToGroundState(LocomotionState.Restricted), Is.EqualTo(GroundState.Restricted));
        Assert.That(LocomotionStateMachine.LocomotionStateToGroundState(LocomotionState.Recovering), Is.EqualTo(GroundState.Recovering));
    }

    [Test]
    public void PlayerStateResolver_CombatWithAttackPriority_WinsOverLocomotion() {
        var combat = CreateCombatCore();
        var locomotion = CreateLocomotionWithIntent();
        var resolver = CreateResolver(combat, locomotion);

        var beforeState = resolver.CurrentSnapshot.ResolvedState;
        Assert.That(beforeState, Is.EqualTo(PlayerState.Moving), "Baseline should be Moving (locomotion has intent, combat neutral)");

        combat.RequestAction(new CombatActionRequest(CombatActionType.LightAttack, 0f,
            CombatRequestSourceType.TestHarness, "Test", "Attack test"));

        var afterState = resolver.CurrentSnapshot.ResolvedState;
        Assert.That(afterState, Is.EqualTo(PlayerState.Attack),
            "Combat AttackStartup (priority 5) should override locomotion Moving (priority 1)");
    }

    [Test]
    public void PlayerStateResolver_CombatNeutral_LocomotionMoving_ResolvesToMoving() {
        var combat = CreateCombatCore();
        var locomotion = CreateLocomotionWithIntent();
        var resolver = CreateResolver(combat, locomotion);

        var state = resolver.CurrentSnapshot.ResolvedState;
        Assert.That(state, Is.EqualTo(PlayerState.Moving),
            "When combat is Neutral (priority 0) and locomotion is Moving (priority 1), should resolve to Moving");
    }

    [Test]
    public void PlayerStateResolver_NullDependencies_DegradeGracefully() {
        var resolverWithCombatOnly = new PlayerStateResolver(
            new CombatStateMachine(CreateCombatCore()), null);
        Assert.That(resolverWithCombatOnly.CurrentSnapshot.ResolvedState, Is.EqualTo(PlayerState.Idle));
        Assert.That(resolverWithCombatOnly.CurrentSnapshot.StateDetail, Does.Contain("Combat only (degraded)"));

        var resolverWithLocomotionOnly = new PlayerStateResolver(
            null, new LocomotionStateMachine(CreateLocomotionWithIntent()));
        Assert.That(resolverWithLocomotionOnly.CurrentSnapshot.ResolvedState, Is.EqualTo(PlayerState.Moving));
        Assert.That(resolverWithLocomotionOnly.CurrentSnapshot.StateDetail, Does.Contain("Locomotion only (degraded)"));

        var resolverWithBothNull = new PlayerStateResolver(null, null);
        Assert.That(resolverWithBothNull.CurrentSnapshot.ResolvedState, Is.EqualTo(PlayerState.Idle));
        Assert.That(resolverWithBothNull.CurrentSnapshot.StateDetail, Does.Contain("Locomotion only (degraded)"));
    }

    [Test]
    public void PlayerStateResolver_DebugSnapshot_FormatPreserved() {
        var combat = CreateCombatCore();
        var locomotion = CreateLocomotionWithIntent();
        var resolver = CreateResolver(combat, locomotion);

        var debug = resolver.CreateDebugSnapshot();

        Assert.That(debug.Summary, Is.EqualTo("M0 PlayerState"));
        Assert.That(debug.Details.Count, Is.EqualTo(7));
        Assert.That(debug.Details[0], Does.StartWith("ResolvedState:"));
        Assert.That(debug.Details[1], Does.StartWith("CombatState:"));
        Assert.That(debug.Details[2], Does.StartWith("LocomotionState:"));
        Assert.That(debug.Details[3], Does.StartWith("ActionLocked:"));
        Assert.That(debug.Details[4], Does.StartWith("Recovering:"));
        Assert.That(debug.Details[5], Does.StartWith("HasTargetFocus:"));
        Assert.That(debug.Details[6], Does.StartWith("Detail:"));
    }

    [Test]
    public void CombatStateMachine_NullCombatCore_EmitsDisabled() {
        var machine = new CombatStateMachine(null);
        Assert.That(machine.CurrentCombatState, Is.EqualTo(CombatCoreState.Disabled));
        Assert.That(machine.CurrentMappedState, Is.EqualTo(PlayerState.Disabled));
        Assert.That(machine.CurrentPriority, Is.EqualTo(9));
        Assert.That(machine.HasCore, Is.False);
    }

    [Test]
    public void LocomotionStateMachine_NullLocomotion_EmitsUninitialized() {
        var machine = new LocomotionStateMachine(null);
        Assert.That(machine.CurrentLocomotionState, Is.EqualTo(LocomotionState.Uninitialized));
        Assert.That(machine.CurrentGroundState, Is.EqualTo(GroundState.Idle));
        Assert.That(machine.CurrentPriority, Is.EqualTo(0));
        Assert.That(machine.HasLocomotion, Is.False);
    }

    private static M0CombatCore CreateCombatCore() {
        return new M0CombatCore(new M0CombatTimingSettings(
            attackStartupSeconds: 0.1f,
            attackActiveSeconds: 0.2f,
            attackRecoverySeconds: 0.3f,
            dodgeStartupSeconds: 0.1f,
            dodgeActiveSeconds: 0.2f,
            dodgeRecoverySeconds: 0.3f,
            parryStartupSeconds: 0.1f,
            parryActiveSeconds: 0.2f,
            parryRecoverySeconds: 0.3f,
            counterWindowDurationSeconds: 1f,
            recoveryDurationSeconds: 0.3f));
    }

    private static IM0PlayerLocomotion CreateLocomotionWithIntent() {
        var settings = new M0LocomotionSettings(
            moveSpeed: 5f,
            inputDeadzone: 0.1f,
            facingLerpSpeed: 8f,
            dodgeDistance: 1.5f,
            dodgeSpeed: 10f,
            dodgeDurationSeconds: 0.2f);
        var locomotion = new M0PlayerLocomotion(settings);
        locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
            new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Test"));
        locomotion.ConsumeInputIntent(new InputIntentSnapshot(
            new Axis2(0.5f, 0f), new Axis2(0f, 0f),
            false, false, false, false, false, false, false, false, true));
        return locomotion;
    }

    private static PlayerStateResolver CreateResolver(M0CombatCore combat, IM0PlayerLocomotion locomotion) {
        return new PlayerStateResolver(
            new CombatStateMachine(combat),
            new LocomotionStateMachine(locomotion));
    }
}
