using NhemBootStrap.Editor.Core;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NhemBootstrap.Editor.Steps {
    /// <summary>Bootstrap step that creates the initial scene with Clean Architecture root GameObjects.</summary>
    public class SetupSceneStep : IBootstrapStep {
        /// <inheritdoc/>
        public string Name => "Setup Scene";

        /// <inheritdoc/>
        public bool CheckCompleted() {
            return true;
        }

        /// <inheritdoc/>
        public void Execute(BootstrapContext context) {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects);

            CreateRoot("[SYSTEMS]");
            CreateRoot("[UI]");
            CreateRoot("[MAP]");
            CreateRoot("[GAMEPLAY]");

            EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Main.unity");

            context.Log("Scene created with base structure");
        }

        private void CreateRoot(string name) {
            var go = new GameObject(name);
            go.transform.position = Vector3.zero;
        }
    }
}