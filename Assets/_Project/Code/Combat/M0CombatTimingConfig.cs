using UnityEngine;
using Sirenix.OdinInspector;

namespace GlassRefrain.Combat {
    [CreateAssetMenu(
        fileName = "M0CombatTimingConfig",
        menuName = "Glass Refrain/M0/Combat Timing Config")]
    public sealed class M0CombatTimingConfig : ScriptableObject {
        [BoxGroup("Attack Timings")]
        [SerializeField, MinValue(0f)] private float attackStartupSeconds = 0.14f;
        [BoxGroup("Attack Timings")]
        [SerializeField, MinValue(0f)] private float attackActiveSeconds = 0.20f;
        [BoxGroup("Attack Timings")]
        [SerializeField, MinValue(0f)] private float attackRecoverySeconds = 0.26f;

        [BoxGroup("Dodge Timings")]
        [SerializeField, MinValue(0f)] private float dodgeStartupSeconds = 0.09f;
        [BoxGroup("Dodge Timings")]
        [SerializeField, MinValue(0f)] private float dodgeActiveSeconds = 0.20f;
        [BoxGroup("Dodge Timings")]
        [SerializeField, MinValue(0f)] private float dodgeRecoverySeconds = 0.24f;

        [BoxGroup("Parry Timings")]
        [SerializeField, MinValue(0f)] private float parryStartupSeconds = 0.10f;
        [BoxGroup("Parry Timings")]
        [SerializeField, MinValue(0f)] private float parryActiveSeconds = 0.18f;
        [BoxGroup("Parry Timings")]
        [SerializeField, MinValue(0f)] private float parryRecoverySeconds = 0.24f;

        [BoxGroup("Combat Windows")]
        [SerializeField, MinValue(0f)] private float counterWindowDurationSeconds = 3.0f;
        [BoxGroup("Combat Windows")]
        [SerializeField, MinValue(0f)] private float recoveryDurationSeconds = 0.24f;

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
