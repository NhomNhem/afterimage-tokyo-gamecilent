using Sirenix.OdinInspector;
using UnityEngine;

namespace GlassRefrain.Memory {
    [CreateAssetMenu(
        fileName = "M0MemoryRuntimeTuningConfig",
        menuName = "Glass Refrain/M0/Memory Runtime Tuning Config")]
    public sealed class M0MemoryRuntimeTuningConfig : ScriptableObject {
        [BoxGroup("Reveal Candidate")]
        [SerializeField, Required] private string defaultRevealCandidateId = "M0RevealCandidate";

        [BoxGroup("Reveal Feedback")]
        [SerializeField, MinValue(0f)] private float revealFeedbackDurationSeconds = 0.25f;
        [BoxGroup("Reveal Feedback")]
        [SerializeField, MinValue(0f)] private float revealFeedbackCooldownSeconds = 0f;
        [BoxGroup("Reveal Feedback")]
        [SerializeField, Required] private string revealFeedbackIntensityLabel = "standard";

        public string DefaultRevealCandidateId => defaultRevealCandidateId;
        public float RevealFeedbackDurationSeconds => revealFeedbackDurationSeconds;
        public float RevealFeedbackCooldownSeconds => revealFeedbackCooldownSeconds;
        public string RevealFeedbackIntensityLabel => revealFeedbackIntensityLabel;

        public M0MemoryRuntimeTuningSettings ToSettings() {
            return new M0MemoryRuntimeTuningSettings(
                defaultRevealCandidateId,
                revealFeedbackDurationSeconds,
                revealFeedbackCooldownSeconds,
                revealFeedbackIntensityLabel);
        }
    }
}
