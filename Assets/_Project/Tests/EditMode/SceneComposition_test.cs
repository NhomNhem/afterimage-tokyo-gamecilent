using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Locomotion;
using GlassRefrain.Infrastructure;
using NUnit.Framework;
using UnityEditor;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Verifies the additive scene set and paths for the M0 foundation.
    /// </summary>
    public class SceneComposition_test {
        private const string GameplayLifetimeScopePath = "Assets/_Project/Code/Bootstrap/GameplayLifetimeScope.cs";
        private const string GameplayScenePath = "Assets/_Project/Content/Scenes/Gameplay/Gameplay_CombatPrototype.unity";
        private const string M0CombatTimingConfigAssetPath = "Assets/_Project/Content/Data/Combat/M0CombatTimingConfig.asset";
        private const string M0LocomotionConfigAssetPath = "Assets/_Project/Content/Data/Locomotion/M0LocomotionConfig.asset";

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

        [Test]
        public void M0CombatTimingConfig_DefaultAssetMatchesCurrentM0Values() {
            var config = AssetDatabase.LoadAssetAtPath<M0CombatTimingConfig>(M0CombatTimingConfigAssetPath);

            Assert.That(config, Is.Not.Null, "Default M0 combat timing config asset should exist.");

            M0CombatTimingSettings settings = config.ToSettings();
            Assert.That(settings.AttackStartupSeconds, Is.EqualTo(0.14f));
            Assert.That(settings.AttackActiveSeconds, Is.EqualTo(0.20f));
            Assert.That(settings.AttackRecoverySeconds, Is.EqualTo(0.26f));
            Assert.That(settings.DodgeStartupSeconds, Is.EqualTo(0.09f));
            Assert.That(settings.DodgeActiveSeconds, Is.EqualTo(0.20f));
            Assert.That(settings.DodgeRecoverySeconds, Is.EqualTo(0.24f));
            Assert.That(settings.ParryStartupSeconds, Is.EqualTo(0.10f));
            Assert.That(settings.ParryActiveSeconds, Is.EqualTo(0.18f));
            Assert.That(settings.ParryRecoverySeconds, Is.EqualTo(0.24f));
            Assert.That(settings.CounterWindowDurationSeconds, Is.EqualTo(3.0f));
            Assert.That(settings.RecoveryDurationSeconds, Is.EqualTo(0.24f));
        }

        [Test]
        public void GameplayLifetimeScope_UsesExplicitCombatTimingConfigComposition() {
            string source = File.ReadAllText(GameplayLifetimeScopePath);

            Assert.That(source, Does.Contain("M0CombatTimingConfig combatTimingConfig"));
            Assert.That(source, Does.Contain("combatTimingConfig.ToSettings()"));
            Assert.That(source, Does.Not.Contain("attackStartupSeconds: 0.14f"));
            Assert.That(source, Does.Not.Contain("attackActiveSeconds: 0.20f"));
            Assert.That(source, Does.Not.Contain("attackRecoverySeconds: 0.26f"));
            Assert.That(source, Does.Not.Contain("dodgeStartupSeconds: 0.09f"));
            Assert.That(source, Does.Not.Contain("dodgeActiveSeconds: 0.20f"));
            Assert.That(source, Does.Not.Contain("dodgeRecoverySeconds: 0.24f"));
            Assert.That(source, Does.Not.Contain("parryStartupSeconds: 0.10f"));
            Assert.That(source, Does.Not.Contain("parryActiveSeconds: 0.18f"));
            Assert.That(source, Does.Not.Contain("parryRecoverySeconds: 0.24f"));
            Assert.That(source, Does.Not.Contain("counterWindowDurationSeconds: 3.0f"));
            Assert.That(source, Does.Not.Contain("recoveryDurationSeconds: 0.24f"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
        }

        [Test]
        public void GameplayScene_AssignsM0CombatTimingConfigReference() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("combatTimingConfig: {fileID: 11400000, guid: b1e71d949c744cdf9a75c80e41a9f3ad, type: 2}"));
        }

        [Test]
        public void M0LocomotionConfig_DefaultAssetMatchesCurrentM0Values() {
            var config = AssetDatabase.LoadAssetAtPath<M0LocomotionConfig>(M0LocomotionConfigAssetPath);

            Assert.That(config, Is.Not.Null, "Default M0 locomotion config asset should exist.");

            M0LocomotionSettings settings = config.ToSettings();
            Assert.That(settings.MoveSpeed, Is.EqualTo(5.0f));
            Assert.That(settings.InputDeadzone, Is.EqualTo(0.1f));
            Assert.That(settings.FacingLerpSpeed, Is.EqualTo(8.0f));
            Assert.That(settings.DodgeDistance, Is.EqualTo(1.5f));
            Assert.That(settings.DodgeSpeed, Is.EqualTo(10.0f));
            Assert.That(settings.DodgeDurationSeconds, Is.EqualTo(0.2f));
        }

        [Test]
        public void GameplayLifetimeScope_UsesExplicitLocomotionConfigComposition() {
            string source = File.ReadAllText(GameplayLifetimeScopePath);

            Assert.That(source, Does.Contain("M0LocomotionConfig locomotionConfig"));
            Assert.That(source, Does.Contain("locomotionConfig.ToSettings()"));
            Assert.That(source, Does.Not.Contain("new M0LocomotionSettings(5.0f"));
            Assert.That(source, Does.Not.Contain("new M0LocomotionSettings(5.0"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
        }

        [Test]
        public void GameplayScene_AssignsM0LocomotionConfigReference() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("locomotionConfig: {fileID: 11400000, guid: c7391b1a9e8f4d92a00c14bdf8b8398e, type: 2}"));
        }
    }
}
