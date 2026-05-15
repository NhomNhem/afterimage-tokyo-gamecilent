using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NhemBootstrap.Editor.Config {
    /// <summary>Generates sample BootstrapConfig assets in Assets/NhemBootstrap/Sample/.</summary>
    public static class SampleConfigGenerator {
        private const string SampleFolder = "Assets/NhemBootstrap/Sample";

        /// <summary>Creates the three sample config assets (Minimal, Full, Custom) under Assets/NhemBootstrap/Sample/.</summary>
        [MenuItem("Tools/Nhem Bootstrap/Generate Sample Configs")]
        public static void GenerateSampleConfigs() {
            EnsureFolder(SampleFolder);
            CreateMinimalConfig();
            CreateFullConfig();
            CreateCustomConfig();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NhemBootstrap] Sample configs generated in Assets/NhemBootstrap/Sample/");
        }

        private static void CreateMinimalConfig() {
            var config = ScriptableObject.CreateInstance<BootstrapConfig>();
            config.profile = ConfigProfile.Minimal;
            config.folders = new List<FolderEntry> {
                new FolderEntry { path = "Assets/_Project", enabled = true }
            };
            config.packages = new List<PackageEntry>();
            config.asmdefs = new List<AsmdefEntry>();
            AssetDatabase.CreateAsset(config, $"{SampleFolder}/MinimalBootstrapConfig.asset");
        }

        private static void CreateFullConfig() {
            var config = ScriptableObject.CreateInstance<BootstrapConfig>();
            config.profile = ConfigProfile.Full;
            config.folders = new List<FolderEntry> {
                new FolderEntry { path = "Assets/_Project", enabled = true },
                new FolderEntry { path = "Assets/_Project/Domain", enabled = true },
                new FolderEntry { path = "Assets/_Project/Domain/Entities", enabled = true },
                new FolderEntry { path = "Assets/_Project/Domain/ValueObjects", enabled = true },
                new FolderEntry { path = "Assets/_Project/Domain/Repositories", enabled = true },
                new FolderEntry { path = "Assets/_Project/Application", enabled = true },
                new FolderEntry { path = "Assets/_Project/Application/UseCases", enabled = true },
                new FolderEntry { path = "Assets/_Project/Application/Services", enabled = true },
                new FolderEntry { path = "Assets/_Project/Application/Messages", enabled = true },
                new FolderEntry { path = "Assets/_Project/Infrastructure", enabled = true },
                new FolderEntry { path = "Assets/_Project/Infrastructure/Network", enabled = true },
                new FolderEntry { path = "Assets/_Project/Infrastructure/Physics", enabled = true },
                new FolderEntry { path = "Assets/_Project/Infrastructure/Input", enabled = true },
                new FolderEntry { path = "Assets/_Project/Infrastructure/Logging", enabled = true },
                new FolderEntry { path = "Assets/_Project/Presentation", enabled = true },
                new FolderEntry { path = "Assets/_Project/Presentation/Player", enabled = true },
                new FolderEntry { path = "Assets/_Project/Presentation/HUD", enabled = true },
                new FolderEntry { path = "Assets/_Project/Composition", enabled = true },
                new FolderEntry { path = "Assets/_Project/Composition/Scopes", enabled = true },
                new FolderEntry { path = "Assets/_Project/Composition/Installers", enabled = true },
                new FolderEntry { path = "Assets/_Project/Shared", enabled = true },
                new FolderEntry { path = "Assets/_Project/Shared/Extensions", enabled = true },
                new FolderEntry { path = "Assets/_Project/Shared/Constants", enabled = true },
                new FolderEntry { path = "Assets/_Project/Shared/Logging", enabled = true },
                new FolderEntry { path = "Assets/_Art", enabled = true },
                new FolderEntry { path = "Assets/_Art/Characters", enabled = true },
                new FolderEntry { path = "Assets/_Art/UI", enabled = true },
                new FolderEntry { path = "Assets/_Art/VFX", enabled = true },
                new FolderEntry { path = "Assets/_Audio", enabled = true },
                new FolderEntry { path = "Assets/_Audio/SFX", enabled = true },
                new FolderEntry { path = "Assets/_Audio/BGM", enabled = true },
                new FolderEntry { path = "Assets/_Scenes", enabled = true },
                new FolderEntry { path = "Assets/_Scenes/Dev", enabled = true },
                new FolderEntry { path = "Assets/_Scenes/Gameplay", enabled = true }
            };
            config.packages = new List<PackageEntry> {
                new PackageEntry { displayName = "UniTask", gitUrl = "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask", enabled = true },
                new PackageEntry { displayName = "VContainer", gitUrl = "https://github.com/hadashiA/VContainer.git?path=VContainer/Assets/VContainer", enabled = true },
                new PackageEntry { displayName = "R3", gitUrl = "https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity", enabled = true },
                new PackageEntry { displayName = "MessagePipe", gitUrl = "https://github.com/Cysharp/MessagePipe.git?path=src/MessagePipe.Unity/Assets/Plugins/MessagePipe", enabled = true },
                new PackageEntry { displayName = "ZLogger", gitUrl = "https://github.com/Cysharp/ZLogger.git?path=src/ZLogger.Unity/Assets/ZLogger", enabled = true }
            };
            config.asmdefs = new List<AsmdefEntry>();
            AssetDatabase.CreateAsset(config, $"{SampleFolder}/FullBootstrapConfig.asset");
        }

        private static void CreateCustomConfig() {
            var config = ScriptableObject.CreateInstance<BootstrapConfig>();
            config.profile = ConfigProfile.Custom;
            config.folders = new List<FolderEntry>();
            config.packages = new List<PackageEntry>();
            config.asmdefs = new List<AsmdefEntry>();
            AssetDatabase.CreateAsset(config, $"{SampleFolder}/CustomBootstrapConfig.asset");
        }

        private static void EnsureFolder(string path) {
            if (!AssetDatabase.IsValidFolder(path)) {
                string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
                string name = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
