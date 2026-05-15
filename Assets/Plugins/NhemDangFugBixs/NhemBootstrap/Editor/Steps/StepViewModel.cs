using NhemBootStrap.Editor.Core;

namespace NhemBootstrap.Editor.Steps {
    /// <summary>View model that pairs a bootstrap step with its UI state (enabled/completed).</summary>
    public class StepViewModel {
        /// <summary>The bootstrap step this view model represents.</summary>
        public IBootstrapStep Step;
        /// <summary>Whether the step is selected by the user to run in the next bootstrap execution.</summary>
        public bool Enabled;
        /// <summary>Whether the step has already been completed (its side effects are present in the project).</summary>
        public bool Completed;
    }
}