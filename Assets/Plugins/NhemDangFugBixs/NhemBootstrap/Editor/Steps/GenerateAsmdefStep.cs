using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NhemBootStrap.Editor.Core;
using NhemBootstrap.Editor.Config;
using UnityEditor;
using UnityEditor.Compilation;

namespace NhemBootstrap.Editor.Steps {
    /// <summary>Bootstrap step that generates assembly definition files.</summary>
    public class GenerateAsmdefStep : IBootstrapStep {
        /// <summary>Asmdef files created during the last Execute call (used for rollback tracking).</summary>
        public List<string> CreatedAsmdefs { get; } = new();

        /// <inheritdoc/>
        public string Name => "Setup Clean Architecture Asmdefs";

        /// <inheritdoc/>
        public bool CheckCompleted() {
            // Minimal check without context — used by the window before a run
            return Directory.Exists("Assets/_Project/Domain") &&
                   Directory.GetFiles("Assets/_Project/Domain", "*.asmdef").Length > 0;
        }

        /// <summary>Checks completion against the asmdefs defined in the provided config.</summary>
        /// <param name="config">The bootstrap config whose asmdef list is checked. Falls back to the hardcoded check when null or empty.</param>
        /// <returns><c>true</c> when every enabled asmdef in the config (or the default set) already exists.</returns>
        public bool CheckCompleted(BootstrapConfig config) {
            if (config == null || config.asmdefs == null || config.asmdefs.Count == 0)
                return CheckCompleted();

            foreach (var entry in config.asmdefs) {
                if (!entry.enabled) continue;
                string path = Path.Combine(entry.targetFolder, $"{entry.name}.asmdef");
                if (!File.Exists(path)) return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public void Execute(BootstrapContext context) {
            CreatedAsmdefs.Clear();
            int created = 0;

            if (context.Config?.asmdefs != null && context.Config.asmdefs.Count > 0) {
                // Config-driven: iterate over enabled AsmdefEntry items
                foreach (var entry in context.Config.asmdefs) {
                    if (!entry.enabled) continue;

                    // Validate name and targetFolder
                    if (string.IsNullOrEmpty(entry.name)) {
                        context.Log($"⚠️ Asmdef entry has empty name — skipping.");
                        continue;
                    }
                    if (string.IsNullOrEmpty(entry.targetFolder) || !Directory.Exists(entry.targetFolder)) {
                        context.Log($"⚠️ Asmdef '{entry.name}' has invalid targetFolder '{entry.targetFolder}' — skipping.");
                        continue;
                    }

                    try {
                        if (WriteAsmdef(entry.targetFolder, entry.name, entry.references.ToArray(), entry.autoReferenced, context.ForceUpdateAsmdef)) {
                            created++;
                            string path = Path.Combine(entry.targetFolder, $"{entry.name}.asmdef");
                            CreatedAsmdefs.Add(path);
                        }
                    }
                    catch (Exception ex) {
                        context.Log($"⚠️ Error writing asmdef '{entry.name}': {ex.Message} — skipping.");
                    }
                }
            }
            else {
                // Fallback: use hardcoded defaults for backward compatibility
                string p = context.ProjectName;
                bool force = context.ForceUpdateAsmdef;

                string[] internalAsms = {
                    $"{p}.Domain",
                    $"{p}.Application",
                    $"{p}.Infrastructure",
                    $"{p}.Presentation",
                    $"{p}.Composition",
                    $"{p}.Shared"
                };

                created += WriteAsmdef("Assets/_Project/Domain", $"{p}.Domain", null, false, force) ? 1 : 0;
                created += WriteAsmdef("Assets/_Project/Application", $"{p}.Application",
                    Filter(internalAsms, $"{p}.Domain", "MessagePipe", "UniTask"), false, force) ? 1 : 0;
                created += WriteAsmdef("Assets/_Project/Infrastructure", $"{p}.Infrastructure",
                    Filter(internalAsms, $"{p}.Domain", $"{p}.Application", $"{p}.Shared", "FishNet.Runtime", "MessagePipe", "MessagePipe.VContainer", "UniTask", "ZLogger", "Unity.InputSystem"), false, force) ? 1 : 0;
                created += WriteAsmdef("Assets/_Project/Presentation", $"{p}.Presentation",
                    Filter(internalAsms, $"{p}.Domain", $"{p}.Application", $"{p}.Shared", "R3", "R3.Unity", "MessagePipe", "UniTask"), false, force) ? 1 : 0;
                created += WriteAsmdef("Assets/_Project/Composition", $"{p}.Composition",
                    Filter(internalAsms, $"{p}.Domain", $"{p}.Application", $"{p}.Infrastructure", $"{p}.Presentation", $"{p}.Shared", "VContainer", "VContainer.Unity", "FishNet.Runtime"), false, force) ? 1 : 0;
                created += WriteAsmdef("Assets/_Project/Shared", $"{p}.Shared",
                    Filter(internalAsms, "UniTask", "ZLogger", "R3", "R3.Unity"), false, force) ? 1 : 0;
            }

            AssetDatabase.Refresh();
            context.Log($"Asmdefs processed: {created} (Force: {context.ForceUpdateAsmdef})");
        }

        private bool WriteAsmdef(string folder, string name, string[] refs, bool autoReferenced, bool force) {
            string path = $"{folder}/{name}.asmdef";

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            refs ??= new string[] { };

            string refsJson = refs.Length == 0
                ? "[]"
                : "[\n" + string.Join(",\n", refs.Select(r => $"    \"{r}\"")) + "\n  ]";

            string autoRefJson = autoReferenced ? "true" : "false";

            string json =
                $@"{{
  ""name"": ""{name}"",
  ""rootNamespace"": ""{name}"",
  ""references"": {refsJson},
  ""autoReferenced"": {autoRefJson}
}}";

            if (File.Exists(path)) {
                if (!force) return false;

                // If force, check if content is different
                string existing = File.ReadAllText(path);
                if (existing.Replace("\r\n", "\n") == json.Replace("\r\n", "\n")) return false;
            }

            File.WriteAllText(path, json);
            return true;
        }

        private string[] Filter(string[] internalAsms, params string[] refs) {
            return refs.Where(r => IsInstalled(r, internalAsms)).ToArray();
        }

        private bool IsInstalled(string asm, string[] internalAsms) {
            // 1. If it's one of our internal asmdefs, assume it will exist
            if (internalAsms.Contains(asm)) return true;

            // 2. Check if it's already compiled by Unity
            if (CompilationPipeline.GetAssemblies().Any(a => a.name == asm)) return true;

            // 3. Fallback: search for any .asmdef file with this name in the project
            string[] guids = AssetDatabase.FindAssets(asm + " t:asmdef");
            foreach (var guid in guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == asm) return true;
            }

            return false;
        }
    }
}
