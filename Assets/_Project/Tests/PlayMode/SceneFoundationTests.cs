using System.IO;
using System.Collections;
using GlassRefrain.Bootstrap;
using GlassRefrain.Infrastructure;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GlassRefrain.Tests.PlayMode
{
    public class SceneFoundationTests
    {
        [UnityTest]
        public IEnumerator RequiredSceneAssetsExist()
        {
            Assert.That(File.Exists(ProjectScenePaths.Bootstrap), Is.True);
            Assert.That(File.Exists(ProjectScenePaths.Systems), Is.True);
            Assert.That(File.Exists(ProjectScenePaths.GameplayCombatPrototype), Is.True);
            Assert.That(File.Exists(ProjectScenePaths.CameraCombatPrototype), Is.True);
            Assert.That(File.Exists(ProjectScenePaths.UiDebugOverlay), Is.True);
            Assert.That(File.Exists(ProjectScenePaths.LevelTokyoStreetBlockout), Is.True);

            yield return null;
        }

        [UnityTest]
        public IEnumerator ScopeShellTypesExist()
        {
            Assert.That(typeof(ProjectRootLifetimeScope), Is.Not.Null);
            Assert.That(typeof(GameplayLifetimeScope), Is.Not.Null);
            Assert.That(typeof(CameraCombatPrototypeLifetimeScope), Is.Not.Null);
            Assert.That(typeof(UiDebugOverlayLifetimeScope), Is.Not.Null);

            yield return null;
        }
    }
}
