using UnityEngine;

namespace GlassRefrain.Combat {
    [CreateAssetMenu(
        fileName = "M0CombatTimingConfig",
        menuName = "Glass Refrain/M0/Combat Timing Config")]
    public sealed class M0CombatTimingConfig : ScriptableObject {
        [SerializeField] private float attackStartupSeconds = 0.14f;
        [SerializeField] private float attackActiveSeconds = 0.20f;
        [SerializeField] private float attackRecoverySeconds = 0.26f;
        [SerializeField] private float dodgeStartupSeconds = 0.09f;
        [SerializeField] private float dodgeActiveSeconds = 0.20f;
        [SerializeField] private float dodgeRecoverySeconds = 0.24f;
        [SerializeField] private float parryStartupSeconds = 0.10f;
        [SerializeField] private float parryActiveSeconds = 0.18f;
        [SerializeField] private float parryRecoverySeconds = 0.24f;
        [SerializeField] private float counterWindowDurationSeconds = 3.0f;
        [SerializeField] private float recoveryDurationSeconds = 0.24f;

        public float AttackStartupSeconds => attackStartupSeconds;
        public float AttackActiveSeconds => attackActiveSeconds;
        public float AttackRecoverySeconds => attackRecoverySeconds;
        public float DodgeStartupSeconds => dodgeStartupSeconds;
        public float DodgeActiveSeconds => dodgeActiveSeconds;
        public float DodgeRecoverySeconds => dodgeRecoverySeconds;
        public float ParryStartupSeconds => parryStartupSeconds;
        public float ParryActiveSeconds => parryActiveSeconds;
        public float ParryRecoverySeconds => parryRecoverySeconds;
        public float CounterWindowDurationSeconds => counterWindowDurationSeconds;
        public float RecoveryDurationSeconds => recoveryDurationSeconds;

        public M0CombatTimingSettings ToSettings() {
            return new M0CombatTimingSettings(
                attackStartupSeconds,
                attackActiveSeconds,
                attackRecoverySeconds,
                dodgeStartupSeconds,
                dodgeActiveSeconds,
                dodgeRecoverySeconds,
                parryStartupSeconds,
                parryActiveSeconds,
                parryRecoverySeconds,
                counterWindowDurationSeconds,
                recoveryDurationSeconds);
        }
    }
}
