using System.IO;
using GlassRefrain.Infrastructure;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Verifies the additive scene set and paths for the M0 foundation.
    /// </summary>
    public class SceneComposition_test {
        private const string GameplayLifetimeScopePath = "Assets/_Project/Code/Bootstrap/GameplayLifetimeScope.cs";
        private const string GameplayScenePath = "Assets/_Project/Content/Scenes/Gameplay/Gameplay_CombatPrototype.unity";

        [Test]
        public void ScenePaths_AreDefined() {
            Assert.That(ProjectScenePaths.Bootstrap, Is.Not.Null.And.Not.Empty);
            Assert.That(ProjectScenePaths.Systems, Is.Not.Null.And.Not.Empty);
            Assert.That(ProjectScenePaths.GameplayCombatPrototype, Is.Not.Null.And.Not.Empty);
            Assert.That(ProjectScenePaths.CameraCombatPrototype, Is.Not.Null.And.Not.Empty);
            Assert.That(ProjectScenePaths.UiDebugOverlay, Is.Not.Null.And.Not.Empty);
            Assert.That(ProjectScenePaths.LevelTokyoStreetBlockout, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void ScenePaths_FollowNamingConventions() {
            Assert.That(ProjectScenePaths.Bootstrap, Does.Contain("Bootstrap"));
            Assert.That(ProjectScenePaths.Systems, Does.Contain("Systems"));
            Assert.That(ProjectScenePaths.GameplayCombatPrototype, Does.Contain("Gameplay"));
            Assert.That(ProjectScenePaths.CameraCombatPrototype, Does.Contain("Camera"));
            Assert.That(ProjectScenePaths.UiDebugOverlay, Does.Contain("UI"));
            Assert.That(ProjectScenePaths.LevelTokyoStreetBlockout, Does.Contain("Level"));
        }

        [Test]
        public void GameplayLifetimeScope_UsesExplicitMemorySceneComposition() {
            string source = File.ReadAllText(GameplayLifetimeScopePath);

            Assert.That(source, Does.Contain("MemoryRaycastProProbe memoryProbe"));
            Assert.That(source, Does.Contain("MemoryFragment[] memoryFragments"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
            Assert.That(source, Does.Not.Contain("Debug.Log"));
        }

        [Test]
        public void GameplayScene_AssignsExplicitMemoryCompositionReferences() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("memoryProbe: {fileID: 1932872218}"));
            Assert.That(scene, Does.Contain("memoryFragments:"));
            Assert.That(scene, Does.Contain("- {fileID: 1990882809}"));
        }
    }
}
