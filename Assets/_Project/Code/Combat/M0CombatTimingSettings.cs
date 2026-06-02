using System;

namespace GlassRefrain.Combat {
    /// <summary>
    /// M0 tuning surface for Attack/Dodge/Parry readability timings.
    /// CombatCore remains the owner of timing truth; this only provides authored values.
    /// </summary>
    public readonly struct M0CombatTimingSettings {
        public float AttackStartupSeconds { get; }
        public float AttackActiveSeconds { get; }
        public float AttackRecoverySeconds { get; }
        public float DodgeStartupSeconds { get; }
        public float DodgeActiveSeconds { get; }
        public float DodgeRecoverySeconds { get; }
        public float ParryStartupSeconds { get; }
        public float ParryActiveSeconds { get; }
        public float ParryRecoverySeconds { get; }
        public float CounterWindowDurationSeconds { get; }
        public float RecoveryDurationSeconds { get; }

        public M0CombatTimingSettings(
            float attackStartupSeconds,
            float attackActiveSeconds,
            float attackRecoverySeconds,
            float dodgeStartupSeconds,
            float dodgeActiveSeconds,
            float dodgeRecoverySeconds,
            float parryStartupSeconds,
            float parryActiveSeconds,
            float parryRecoverySeconds,
            float counterWindowDurationSeconds,
            float recoveryDurationSeconds) {
            ValidatePositive(attackStartupSeconds, nameof(attackStartupSeconds));
            ValidatePositive(attackActiveSeconds, nameof(attackActiveSeconds));
            ValidatePositive(attackRecoverySeconds, nameof(attackRecoverySeconds));
            ValidatePositive(dodgeStartupSeconds, nameof(dodgeStartupSeconds));
            ValidatePositive(dodgeActiveSeconds, nameof(dodgeActiveSeconds));
            ValidatePositive(dodgeRecoverySeconds, nameof(dodgeRecoverySeconds));
            ValidatePositive(parryStartupSeconds, nameof(parryStartupSeconds));
            ValidatePositive(parryActiveSeconds, nameof(parryActiveSeconds));
            ValidatePositive(parryRecoverySeconds, nameof(parryRecoverySeconds));
            ValidatePositive(counterWindowDurationSeconds, nameof(counterWindowDurationSeconds));
            ValidatePositive(recoveryDurationSeconds, nameof(recoveryDurationSeconds));

            AttackStartupSeconds = attackStartupSeconds;
            AttackActiveSeconds = attackActiveSeconds;
            AttackRecoverySeconds = attackRecoverySeconds;
            DodgeStartupSeconds = dodgeStartupSeconds;
            DodgeActiveSeconds = dodgeActiveSeconds;
            DodgeRecoverySeconds = dodgeRecoverySeconds;
            ParryStartupSeconds = parryStartupSeconds;
            ParryActiveSeconds = parryActiveSeconds;
            ParryRecoverySeconds = parryRecoverySeconds;
            CounterWindowDurationSeconds = counterWindowDurationSeconds;
            RecoveryDurationSeconds = recoveryDurationSeconds;
        }

        private static void ValidatePositive(float value, string name) {
            if (value <= 0f) throw new ArgumentOutOfRangeException(name, value, name + " must be > 0.");
        }
    }
}
