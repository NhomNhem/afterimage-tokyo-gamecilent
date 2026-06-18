using System;
using GlassRefrain.Core;

namespace GlassRefrain.Memory;

public interface IM0MemoryState {
    MemoryStateSnapshot Snapshot { get; }
    event Action<MemoryStateSnapshot> SnapshotChanged;
    void IntakeRevealRequest(RevealRequestContext request);
    RevealRequestResult EvaluateRequestedReveal();
    MemoryStateSnapshot AdvancePhase(string reason, float cooldownSeconds = 0.25f);
}
