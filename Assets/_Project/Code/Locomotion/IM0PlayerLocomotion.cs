using System;
using GlassRefrain.Core;
using UnityEngine;

namespace GlassRefrain.Locomotion;

/// <summary>
/// M0PlayerLocomotion — Pure C# gameplay truth owner for camera-relative movement.
///
/// Ownership:
/// - Owns position, rotation (facing), velocity, and movement state
/// - Processes input intents from Input System
/// - Reads camera movement basis (read-only snapshot)
/// - Expresses movement to adapters via read-only snapshot
///
/// Story 1-2 Scope:
/// - Camera-relative movement and free-movement facing
/// - No lock-on facing (deferred to Story 1-3)
/// - No collision/ground detection (deferred to future)
/// - No animator authority (FSM is Pure C# only, adapters observe)
/// - No root motion (Locomotion owns movement truth)
/// </summary>

public interface IM0PlayerLocomotion {
    LocomotionStateSnapshot Snapshot { get; }
    event Action<LocomotionStateSnapshot> SnapshotChanged;
    LocomotionMovementSnapshot GetMovementSnapshot();
    void ConsumeInputIntent(InputIntentSnapshot inputIntent);
    void SetMovementRestriction(MovementRestrictionContext restriction);
    void SetRecoveryContext(RecoveryContext recovery);
    void SetCameraMovementBasis(CameraMovementBasisSnapshot cameraBasis);
    bool TryBeginDodgeDisplacement();
    void ResetForEncounter(Vector3 startPosition, Vector3 startFacing);
    void ProcessMovementInput(float deltaTime);
    void UpdatePosition(float deltaTime);
    LocomotionDebugSnapshot CreateDebugSnapshot();
}
