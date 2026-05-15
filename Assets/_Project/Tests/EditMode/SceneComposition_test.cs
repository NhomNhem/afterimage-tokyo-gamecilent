using NUnit.Framework;
using GlassRefrain.Infrastructure;
using UnityEngine;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Verifies the additive scene set and paths for the M0 foundation.
    /// </summary>
    public class SceneComposition_test {
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
    }
}
