using System.Collections.Generic;
using UnityEngine;

namespace NhemBootstrap.Editor.Config {
    /// <summary>ScriptableObject configuration for the NhemBootstrap tool.</summary>
    [CreateAssetMenu(menuName = "Nhem/Bootstrap Config")]
    public class BootstrapConfig : ScriptableObject {
        /// <summary>The configuration profile (Minimal, Full, or Custom).</summary>
        [Tooltip("Select a predefined profile or use Custom for manual configuration")]
        public ConfigProfile profile = ConfigProfile.Custom;

        /// <summary>Packages to install via UPM.</summary>
        [Header("Packages")]
        [Tooltip("List of Unity packages to install via UPM during the bootstrap run")]
        public List<PackageEntry> packages = new();

        /// <summary>Folders to create in the project structure.</summary>
        [Header("Folders")]
        [Tooltip("List of project-relative folder paths to create during the bootstrap run")]
        public List<FolderEntry> folders = new();

        /// <summary>Assembly definition files to generate.</summary>
        [Header("Asmdef Definitions")]
        [Tooltip("List of assembly definition files to generate during the bootstrap run")]
        public List<AsmdefEntry> asmdefs = new();
    }
}
