using System;
using System.Collections.Generic;
using UnityEngine;

namespace NhemBootstrap.Editor.Config {
    /// <summary>Defines an assembly definition file to generate.</summary>
    [Serializable]
    public class AsmdefEntry {
        /// <summary>Assembly name (e.g. MyProject.Domain).</summary>
        [Tooltip("Assembly name, e.g. MyProject.Domain")]
        public string name;
        /// <summary>Project-relative folder where the .asmdef file will be written.</summary>
        [Tooltip("Project-relative folder where the .asmdef file will be written")]
        public string targetFolder;
        /// <summary>List of assembly names this asmdef references.</summary>
        [Tooltip("List of assembly names this asmdef references; uninstalled assemblies are filtered out automatically")]
        public List<string> references = new();
        /// <summary>Whether this assembly is auto-referenced by all assemblies in the project.</summary>
        [Tooltip("Whether this assembly is auto-referenced by all assemblies in the project")]
        public bool autoReferenced = false;
        /// <summary>Whether this asmdef is included in the bootstrap run.</summary>
        [Tooltip("Uncheck to skip this asmdef during the bootstrap run")]
        public bool enabled = true;
    }
}
