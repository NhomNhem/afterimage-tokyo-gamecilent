using System;
using GlassRefrain.Core;

namespace GlassRefrain.Health;

public interface IM0HealthDamageReactionModel {
    HealthStateSnapshot Snapshot { get; }
    event Action<HealthStateSnapshot> SnapshotChanged;
    DamageApplicationResult ApplyDamage(DamageApplicationContext request);
    void EnterRecovery(string reason, float suppressionSeconds);
    void EnterLiving(string reason);
}
