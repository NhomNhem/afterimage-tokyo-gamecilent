namespace GlassRefrain.Memory {
    public readonly struct M0MemoryRuntimeTuningSettings {
        public M0MemoryRuntimeTuningSettings(
            string defaultRevealCandidateId,
            float revealFeedbackDurationSeconds,
            float revealFeedbackCooldownSeconds,
            string revealFeedbackIntensityLabel) {
            DefaultRevealCandidateId = string.IsNullOrEmpty(defaultRevealCandidateId)
                ? "M0RevealCandidate"
                : defaultRevealCandidateId;
            RevealFeedbackDurationSeconds = revealFeedbackDurationSeconds < 0f
                ? 0f
                : revealFeedbackDurationSeconds;
            RevealFeedbackCooldownSeconds = revealFeedbackCooldownSeconds < 0f
                ? 0f
                : revealFeedbackCooldownSeconds;
            RevealFeedbackIntensityLabel = string.IsNullOrEmpty(revealFeedbackIntensityLabel)
                ? "standard"
                : revealFeedbackIntensityLabel;
        }

        public string DefaultRevealCandidateId { get; }
        public float RevealFeedbackDurationSeconds { get; }
        public float RevealFeedbackCooldownSeconds { get; }
        public string RevealFeedbackIntensityLabel { get; }
    }
}
