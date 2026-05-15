using System.Collections.Generic;
using System.Threading.Tasks;
using GlassRefrain.Infrastructure;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Orchestrates the additive loading sequence for the M0 First Playable Duel.
    /// Strictly follows the order defined in ADR-0001.
    /// </summary>
    public sealed class M0BootstrapOrchestrator : MonoBehaviour {
        [SerializeField] private bool loadOnStart = true;

        private async void Start() {
            if (loadOnStart) {
                await LoadM0SceneSetAsync();
            }
        }

        public async Task LoadM0SceneSetAsync() {
            Debug.Log("[Bootstrap] Starting M0 Additive Scene Load...");

            // Strict order per ADR-0001: Bootstrap (current) -> Systems -> Level -> Gameplay -> Camera -> UI
            var scenesToLoad = new List<string> {
                ProjectScenePaths.Systems,
                ProjectScenePaths.LevelTokyoStreetBlockout,
                ProjectScenePaths.GameplayCombatPrototype,
                ProjectScenePaths.CameraCombatPrototype,
                ProjectScenePaths.UiDebugOverlay
            };

            foreach (var scenePath in scenesToLoad) {
                if (string.IsNullOrEmpty(scenePath)) continue;

                Debug.Log($"[Bootstrap] Loading scene additive: {scenePath}");
                
                // Using scene path directly. Ensure scenes are in Build Settings.
                var operation = SceneManager.LoadSceneAsync(scenePath, LoadSceneMode.Additive);
                
                if (operation == null) {
                    Debug.LogError($"[Bootstrap] Failed to start load for scene: {scenePath}. Is it in Build Settings?");
                    continue;
                }

                while (!operation.isDone) {
                    await Task.Yield();
                }
            }

            Debug.Log("[Bootstrap] M0 Scene Set Load Complete.");
        }
    }
}
