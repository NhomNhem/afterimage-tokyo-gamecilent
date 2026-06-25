using System;
using GlassRefrain.Core;
using UnityEngine;

namespace GlassRefrain.Locomotion;

public interface ILocomotionCore {
    LocomotionStateSnapshot Snapshot { get; }
    event Action<LocomotionStateSnapshot> SnapshotChanged;
    LocomotionMovementSnapshot GetMovementSnapshot();
    void ConsumeInputIntent(InputIntentSnapshot inputIntent);
    void SetCameraMovementBasis(CameraMovementBasisSnapshot cameraBasis);
    bool TryBeginDodgeDisplacement();
    void ResetForEncounter(Vector3 startPosition, Vector3 startFacing);
    void Tick(InputIntentSnapshot intent, ControlState control, float deltaTime);
    LocomotionDebugSnapshot CreateDebugSnapshot();
}
