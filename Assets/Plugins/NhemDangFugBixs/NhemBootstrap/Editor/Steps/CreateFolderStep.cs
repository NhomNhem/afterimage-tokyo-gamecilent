using System;
using System.Collections.Generic;
using System.IO;
using NhemBootStrap.Editor.Core;
using NhemBootstrap.Editor.Config;
using UnityEditor;
using UnityEngine;

namespace NhemBootstrap.Editor.Steps {
    /// <summary>Bootstrap step that creates the project folder structure.</summary>
    public class CreateFolderStep : IBootstrapStep {
        /// <summary>Folders created during the last Execute call (used for rollback tracking).</summary>
        public List<string> CreatedFolders { get; } = new();

        /// <inheritdoc/>
        public string Name => "Setup Clean Architecture Folders";

        // Fallback hardcoded folders used when no config is provided
        private static readonly string[] DefaultFolders = {
            "Assets/_Project",
            "Assets/_Project/Domain",
            "Assets/_Project/Domain/Entities",
            "Assets/_Project/Domain/ValueObjects",
            "Assets/_Project/Domain/Repositories",
            "Assets/_Project/Application",
            "Assets/_Project/Application/UseCases",
            "Assets/_Project/Application/Services",
            "Assets/_Project/Application/Messages",
            "Assets/_Project/Infrastructure",
            "Assets/_Project/Infrastructure/Network",
            "Assets/_Project/Infrastructure/Network/Fishnet",
            "Assets/_Project/Infrastructure/Physics",
            "Assets/_Project/Infrastructure/Input",
            "Assets/_Project/Infrastructure/Logging",
            "Assets/_Project/Presentation",
            "Assets/_Project/Presentation/Player",
            "Assets/_Project/Presentation/HUD",
            "Assets/_Project/Composition",
            "Assets/_Project/Composition/Scopes",
            "Assets/_Project/Composition/Installers",
            "Assets/_Project/Shared",
            "Assets/_Project/Shared/Extensions",
            "Assets/_Project/Shared/Constants",
            "Assets/_Project/Shared/Logging",
            "Assets/_Art",
            "Assets/_Art/Characters",
            "Assets/_Art/UI",
            "Assets/_Art/VFX",
            "Assets/_Audio",
            "Assets/_Audio/SFX",
            "Assets/_Audio/BGM",
            "Assets/_Scenes",
            "Assets/_Scenes/Dev",
            "Assets/_Scenes/Gameplay"
        };

        /// <inheritdoc/>
        public bool CheckCompleted() {
            // Minimal check without context — used by the window before a run
            return AssetDatabase.IsValidFolder("Assets/_Project/Domain") &&
                   AssetDatabase.IsValidFolder("Assets/_Project/Application") &&
                   AssetDatabase.IsValidFolder("Assets/_Project/Infrastructure");
        }

        /// <summary>Checks completion against the folders defined in the provided config.</summary>
        /// <param name="config">The bootstrap config whose folder list is checked. Falls back to the hardcoded check when null or empty.</param>
        /// <returns><c>true</c> when every enabled folder in the config (or the default set) already exists.</returns>
        public bool CheckCompleted(BootstrapConfig config) {
            if (config == null || config.folders == null || config.folders.Count == 0)
                return CheckCompleted();

            foreach (var entry in config.folders) {
                if (!entry.enabled) continue;
                if (!AssetDatabase.IsValidFolder(entry.path)) return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public void Execute(BootstrapContext context) {
            CreatedFolders.Clear();
            int created = 0;

            // Determine which folder paths to create
            IEnumerable<string> paths;
            if (context.Config?.folders != null && context.Config.folders.Count > 0) {
                // Config-driven: collect enabled FolderEntry paths
                var configPaths = new List<string>();
                foreach (var entry in context.Config.folders) {
                    if (entry.enabled) configPaths.Add(entry.path);
                }
                paths = configPaths;
            }
            else {
                // Fallback: use hardcoded defaults for backward compatibility
                paths = DefaultFolders;
            }

            foreach (var path in paths) {
                try {
                    if (MakeFolder(path)) {
                        created++;
                        CreatedFolders.Add(path);
                    }
                }
                catch (ArgumentException ex) {
                    context.Log($"⚠️ Invalid folder path '{path}': {ex.Message} — skipping.");
                }
                catch (IOException ex) {
                    context.Log($"⚠️ IO error creating folder '{path}': {ex.Message} — skipping.");
                }
            }

            AssetDatabase.Refresh();
            context.Log($"Created {created} folders.");
        }

        private bool MakeFolder(string path) {
            if (AssetDatabase.IsValidFolder(path)) return false;

            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && parent != "Assets" && !AssetDatabase.IsValidFolder(parent))
                MakeFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
            return true;
        }
    }
}
