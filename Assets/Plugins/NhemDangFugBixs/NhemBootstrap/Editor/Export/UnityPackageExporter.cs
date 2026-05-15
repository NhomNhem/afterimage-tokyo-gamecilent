using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NhemBootstrap.Editor.Export {
    /// <summary>Exports the NhemBootstrap tool as a distributable .unitypackage file.</summary>
    public static class UnityPackageExporter {
        private const string SourceFolder = "Assets/NhemBootstrap";

        // Patterns for files/folders to exclude from the export
        private static readonly string[] ExcludePatterns = {
            ".vs/",
            "Temp/",
            "Library/",
            "UserSettings/",
            ".idea/",
            ".DS_Store",
            "Thumbs.db",
            "*.user",
            "*.suo"
        };

        /// <summary>Exports the NhemBootstrap tool as a .unitypackage file.</summary>
        [MenuItem("Tools/Nhem Bootstrap/Export as UnityPackage")]
        public static void Export() {
            // Determine output path
            string outputDir = Path.GetFullPath("Exports");
            Directory.CreateDirectory(outputDir);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string outputPath = Path.Combine(outputDir, $"NhemBootstrap_{timestamp}.unitypackage");

            // Collect all asset paths under Assets/NhemBootstrap, filtered
            string[] allGuids = AssetDatabase.FindAssets("", new[] { SourceFolder });
            var assetPaths = new List<string>();

            foreach (var guid in allGuids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!ShouldExclude(path)) {
                    assetPaths.Add(path);
                }
            }

            if (assetPaths.Count == 0) {
                Debug.LogError("[NhemBootstrap] No assets found to export.");
                return;
            }

            Debug.Log($"[NhemBootstrap] Exporting {assetPaths.Count} assets to {outputPath}...");

            AssetDatabase.ExportPackage(
                assetPaths.ToArray(),
                outputPath,
                ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

            Debug.Log($"[NhemBootstrap] \u2705 Export complete: {outputPath}");

            // 10.4: Reveal the output folder in the OS file browser
            EditorUtility.RevealInFinder(outputPath);
        }

        /// <summary>Returns true if the given asset path matches any exclusion pattern.</summary>
        private static bool ShouldExclude(string assetPath) {
            foreach (var pattern in ExcludePatterns) {
                if (pattern.EndsWith("/")) {
                    // Directory pattern
                    if (assetPath.Contains(pattern)) return true;
                }
                else if (pattern.StartsWith("*.")) {
                    // Extension pattern
                    string ext = pattern.Substring(1); // e.g. ".user"
                    if (assetPath.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
                }
                else {
                    // Exact filename match
                    if (Path.GetFileName(assetPath).Equals(pattern, StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            return false;
        }
    }
}
