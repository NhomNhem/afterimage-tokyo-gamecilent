using System.Collections.Generic;

namespace NhemBootStrap.Editor.Core {
    /// <summary>Captures the project state before a bootstrap run for rollback support.</summary>
    public class ProjectSnapshot {
        /// <summary>Folders that existed before the run started.</summary>
        public List<string> ExistingFolders = new();
        /// <summary>Asmdef files that existed before the run started.</summary>
        public List<string> ExistingAsmdefs = new();
        /// <summary>Package IDs that were installed before the run started.</summary>
        public HashSet<string> InstalledPackages = new();
        /// <summary>Folders created during the current run (used for rollback).</summary>
        public List<string> CreatedFolders = new();
        /// <summary>Asmdef files created during the current run (used for rollback).</summary>
        public List<string> CreatedAsmdefs = new();
        /// <summary>Package IDs installed during the current run (used for rollback).</summary>
        public List<string> InstalledDuring = new();
    }
}
