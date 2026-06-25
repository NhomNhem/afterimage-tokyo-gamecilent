using UnityEngine;

namespace GlassRefrain.Application;

public sealed class SkillSlotResolver {
    private readonly float[] _cooldowns = new float[4];
    private const float DashCooldown = 2f;

    public const int SlotDashLeft = 0;
    public const int SlotDashRight = 1;
    public const int SlotDashBack = 2;

    public SkillSlotResolver() {
        _cooldowns[0] = -99f;
        _cooldowns[1] = -99f;
        _cooldowns[2] = -99f;
    }

    public bool CanActivate(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= _cooldowns.Length) return false;
        return Time.time - _cooldowns[slotIndex] >= DashCooldown;
    }

    public void MarkUsed(int slotIndex) {
        if (slotIndex >= 0 && slotIndex < _cooldowns.Length)
            _cooldowns[slotIndex] = Time.time;
    }

    public float CooldownRemaining(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= _cooldowns.Length) return 0f;
        return Mathf.Max(0f, DashCooldown - (Time.time - _cooldowns[slotIndex]));
    }
}
