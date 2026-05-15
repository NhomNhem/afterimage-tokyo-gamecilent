using System.Collections.Generic;

namespace NhemBootStrap.Editor.Core {
    /// <summary>Summarizes the outcome of a bootstrap run.</summary>
    public class BootstrapSummary {
        /// <summary>Total number of steps in the run.</summary>
        public int TotalSteps;
        /// <summary>Number of steps that completed successfully.</summary>
        public int SucceededSteps;
        /// <summary>Number of steps that threw an exception.</summary>
        public int FailedSteps;
        /// <summary>Number of steps skipped due to cancellation or already-completed state.</summary>
        public int SkippedSteps;
        /// <summary>Whether the run was cancelled before all steps completed.</summary>
        public bool WasCancelled;
        /// <summary>List of (step name, error message) pairs for failed steps.</summary>
        public List<(string StepName, string Error)> Failures = new();
    }
}
