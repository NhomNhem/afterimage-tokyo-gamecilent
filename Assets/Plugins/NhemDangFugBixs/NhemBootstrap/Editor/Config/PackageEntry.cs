using System;
using UnityEngine;

namespace NhemBootstrap.Editor.Config {
    /// <summary>Defines a Unity package to install via UPM.</summary>
    [Serializable]
    public class PackageEntry {
        /// <summary>Human-readable display name shown in the UI.</summary>
        [Tooltip("Human-readable display name shown in the Bootstrap Window")]
        public string displayName;
        /// <summary>Git URL for the package (e.g. https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask).</summary>
        [Tooltip("Git URL for the package, e.g. https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask")]
        public string gitUrl;
        /// <summary>Optional branch, tag, or commit hash appended as #version to the Git URL.</summary>
        [Tooltip("Optional: branch, tag, or commit hash appended as #version to the Git URL")]
        public string version;
        /// <summary>Whether this package is included in the bootstrap run.</summary>
        [Tooltip("Uncheck to skip this package during the bootstrap run")]
        public bool enabled = true;
    }
}
