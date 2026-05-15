using System;
using UnityEngine;

namespace NhemBootstrap.Editor.Config {
    /// <summary>Defines a folder path to create in the project structure.</summary>
    [Serializable]
    public class FolderEntry {
        /// <summary>Project-relative path to create (e.g. Assets/_Project/Domain).</summary>
        [Tooltip("Project-relative path to create, e.g. Assets/_Project/Domain")]
        public string path;
        /// <summary>Whether this folder is included in the bootstrap run.</summary>
        [Tooltip("Uncheck to skip this folder during the bootstrap run")]
        public bool enabled = true;
    }
}
