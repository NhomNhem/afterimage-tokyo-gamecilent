using System;

namespace GlassRefrain.Application;

[Flags]
public enum ImmunityFlags {
    None = 0,
    AllDamage = 1 << 0,
    AllCC = 1 << 1,
    HardCCOnly = 1 << 2
}
