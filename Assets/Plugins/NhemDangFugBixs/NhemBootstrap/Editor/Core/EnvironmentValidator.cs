using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace NhemBootStrap.Editor.Core {
    /// <summary>Validates the Unity environment when the tool is imported or the Editor reloads.</summary>
    [InitializeOnLoad]
    public static class EnvironmentValidator {
        private static readonly Version MinUnityVersion = new Version(2021, 3);
        private static readonly string[] OptionalPackages = {
            "com.unity.nuget.newtonsoft-json"
        };

        /// <summary>The result of the last validation run. Updated on every domain reload.</summary>
        public static ValidationResult LastResult { get; private set; }

        static EnvironmentValidator() {
            LastResult = Validate();
            if (!LastResult.IsValid) {
                foreach (var err in LastResult.Errors)
                    Debug.LogError($"[NhemBootstrap] ❌ {err}");
            }
            foreach (var warn in LastResult.Warnings)
                Debug.LogWarning($"[NhemBootstrap] ⚠️ {warn}");
        }

        /// <summary>Runs all environment checks and returns a <see cref="ValidationResult"/>.</summary>
        public static ValidationResult Validate() {
            var result = new ValidationResult();

            // 9.2: Unity version check
            CheckUnityVersion(result);

            // 9.3: Optional package presence check
            CheckOptionalPackages(result);

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private static void CheckUnityVersion(ValidationResult result) {
            try {
                // Application.unityVersion format: "2021.3.0f1"
                string raw = Application.unityVersion;
                // Extract major.minor
                string[] parts = raw.Split('.');
                if (parts.Length >= 2 &&
                    int.TryParse(parts[0], out int major) &&
                    int.TryParse(parts[1], out int minor)) {
                    var current = new Version(major, minor);
                    if (current < MinUnityVersion) {
                        result.Errors.Add(
                            $"Unity {raw} is below the minimum required version {MinUnityVersion.Major}.{MinUnityVersion.Minor}. " +
                            "Please upgrade to Unity 2021.3 or higher.");
                    }
                }
                else {
                    result.Warnings.Add($"Could not parse Unity version string: '{raw}'");
                }
            }
            catch (Exception ex) {
                result.Warnings.Add($"Unity version check failed: {ex.Message}");
            }
        }

        private static void CheckOptionalPackages(ValidationResult result) {
            try {
                var listRequest = Client.List(true);
                // Use a short synchronous wait — this runs at domain reload, not on the main game loop
                float timeout = 5f;
                var sw = System.Diagnostics.Stopwatch.StartNew();
                while (!listRequest.IsCompleted) {
                    if (sw.Elapsed.TotalSeconds > timeout) {
                        result.Warnings.Add("Package list timed out during environment validation.");
                        return;
                    }
                    System.Threading.Thread.Sleep(10);
                }

                if (listRequest.Status != StatusCode.Success) {
                    result.Warnings.Add("Could not retrieve installed package list during environment validation.");
                    return;
                }

                var installedNames = new HashSet<string>(listRequest.Result.Select(p => p.name));
                foreach (var pkg in OptionalPackages) {
                    if (!installedNames.Contains(pkg)) {
                        result.Warnings.Add($"Optional package '{pkg}' is not installed. Some features may be unavailable.");
                    }
                }
            }
            catch (Exception ex) {
                result.Warnings.Add($"Optional package check failed: {ex.Message}");
            }
        }
    }

    /// <summary>Holds the result of an environment validation run.</summary>
    public class ValidationResult {
        /// <summary>Whether the environment meets all hard requirements (no errors).</summary>
        public bool IsValid { get; set; } = true;

        /// <summary>Critical issues that prevent the tool from running correctly.</summary>
        public List<string> Errors { get; } = new List<string>();

        /// <summary>Non-critical issues that may affect optional features.</summary>
        public List<string> Warnings { get; } = new List<string>();
    }
}
