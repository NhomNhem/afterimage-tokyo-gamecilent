namespace NhemBootStrap.Editor.Core {
    /// <summary>Reports progress of a bootstrap operation step.</summary>
    public readonly struct BootstrapProgress {
        /// <summary>Zero-based index of the current step.</summary>
        public readonly int StepIndex;
        /// <summary>Total number of steps in the run.</summary>
        public readonly int TotalSteps;
        /// <summary>Display name of the current step.</summary>
        public readonly string StepName;
        /// <summary>Progress within the current package step (0–1).</summary>
        public readonly float PackageProgress;

        public BootstrapProgress(int stepIndex, int totalSteps, string stepName, float packageProgress = 0f) {
            StepIndex = stepIndex;
            TotalSteps = totalSteps;
            StepName = stepName;
            PackageProgress = packageProgress;
        }
    }
}
