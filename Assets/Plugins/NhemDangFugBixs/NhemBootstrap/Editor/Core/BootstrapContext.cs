using System;
using System.Collections.Generic;
using System.Threading;
using NhemBootstrap.Editor.Config;
using UnityEngine;

namespace NhemBootStrap.Editor.Core {
    /// <summary>Shared execution context passed to every bootstrap step during a run.</summary>
    public class BootstrapContext {
        /// <summary>Absolute path to the Unity project root.</summary>
        public string ProjectPath;
        /// <summary>Name of the Unity project, used as a prefix for generated assembly names.</summary>
        public string ProjectName;
        /// <summary>When <c>true</c>, existing asmdef files are overwritten if their content differs.</summary>
        public bool ForceUpdateAsmdef = false;
        /// <summary>The ScriptableObject configuration driving this run. May be <c>null</c> when using hardcoded defaults.</summary>
        public BootstrapConfig Config;
        /// <summary>Token used to cancel the run mid-execution.</summary>
        public CancellationToken CancellationToken;
        /// <summary>Optional progress sink updated by each step to drive the UI progress bar.</summary>
        public IProgress<BootstrapProgress> Progress;
        /// <summary>Accumulated log messages from all steps in the current run.</summary>
        public readonly List<string> Logs = new();

        /// <summary>Appends <paramref name="message"/> to <see cref="Logs"/> and writes it to the Unity console.</summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message) {
//            Debug.Log(message);
  //          Logs.Add(message);
        }
    }
}