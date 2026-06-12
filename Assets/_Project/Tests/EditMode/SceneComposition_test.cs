using System.IO;
using GlassRefrain.Combat;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Infrastructure;
using NUnit.Framework;
using UnityEditor;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Verifies the additive scene set and paths for the M0 foundation.
    /// </summary>
    public class SceneComposition_test {
        private const string GameplayLifetimeScopePath = "Assets/_Project/Code/Bootstrap/GameplayLifetimeScope.cs";
        private const string M0RuntimeServiceCompositionRegistrarPath = "Assets/_Project/Code/Bootstrap/M0RuntimeServiceCompositionRegistrar.cs";
        private const string M0SceneCompositionRegistrarPath = "Assets/_Project/Code/Bootstrap/M0SceneCompositionRegistrar.cs";
        private const string GameplayLifetimeScopeEditorPath = "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.cs";
        private const string GameplayLifetimeScopeEditorUxmlPath = "Assets/_Project/Code/Bootstrap/Editor/GameplayLifetimeScopeEditor.uxml";
        private const string GameplayScenePath = "Assets/_Project/Content/Scenes/Gameplay/Gameplay_CombatPrototype.unity";
        private const string M0CombatTimingConfigAssetPath = "Assets/_Project/Content/Data/Combat/M0CombatTimingConfig.asset";
        private const string M0LocomotionConfigAssetPath = "Assets/_Project/Content/Data/Locomotion/M0LocomotionConfig.asset";
        private const string M0MemoryRuntimeTuningConfigPath = "Assets/_Project/Code/Memory/M0MemoryRuntimeTuningConfig.cs";
        private const string M0MemoryRuntimeTuningConfigAssetPath = "Assets/_Project/Content/Data/Memory/M0MemoryRuntimeTuningConfig.asset";

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
            string registrar = File.ReadAllText(M0SceneCompositionRegistrarPath);

            Assert.That(source, Does.Contain("MemoryRaycastProProbe memoryProbe"));
            Assert.That(source, Does.Contain("MemoryFragment[] memoryFragments"));
            Assert.That(source, Does.Contain("CreateSceneCompositionRegistrar().Register(builder)"));
            Assert.That(registrar, Does.Contain("container.Inject(_memoryProbe)"));
            Assert.That(registrar, Does.Contain("container.Inject(fragment)"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
            Assert.That(source, Does.Not.Contain("Debug.Log"));
            Assert.That(registrar, Does.Not.Contain("FindObjectOfType"));
            Assert.That(registrar, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(registrar, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(registrar, Does.Not.Contain("FindObjectsByType"));
            Assert.That(registrar, Does.Not.Contain("Resources.Load"));
            Assert.That(registrar, Does.Not.Contain("ServiceLocator"));
            Assert.That(registrar, Does.Not.Contain("Debug.Log"));
        }

        [Test]
        public void GameplayScene_AssignsExplicitMemoryCompositionReferences() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("- Name: memoryProbe"));
            Assert.That(scene, Does.Contain("Data: 9"));
            Assert.That(scene, Does.Contain("- {fileID: 1932872218}"));
            Assert.That(scene, Does.Contain("- Name: memoryFragments"));
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
            string runtimeRegistrar = File.ReadAllText(M0RuntimeServiceCompositionRegistrarPath);

            Assert.That(source, Does.Contain("RegisterGeneratedFor<IGameplayLifetimeScope>()"));
            Assert.That(source, Does.Contain("M0CombatTimingConfig combatTimingConfig"));
            Assert.That(source, Does.Contain("CreateRuntimeServiceCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Not.Contain("combatTimingConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("_combatTimingConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("M0RuntimeServiceCompositionRegistrar requires an assigned M0CombatTimingConfig."));
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
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("Resources.Load"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("FindObjectOfType"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("FindObjectsByType"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("ServiceLocator"));
            Assert.That(runtimeRegistrar, Does.Not.Contain("Debug.Log"));
        }

        [Test]
        public void GameplayScene_AssignsM0CombatTimingConfigReference() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("- Name: combatTimingConfig"));
            Assert.That(scene, Does.Contain("guid: b1e71d949c744cdf9a75c80e41a9f3ad, type: 2"));
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
            string runtimeRegistrar = File.ReadAllText(M0RuntimeServiceCompositionRegistrarPath);

            Assert.That(source, Does.Contain("CreateSceneCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Contain("CreateRuntimeServiceCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Contain("M0LocomotionConfig locomotionConfig"));
            Assert.That(source, Does.Not.Contain("locomotionConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("_locomotionConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("M0RuntimeServiceCompositionRegistrar requires an assigned M0LocomotionConfig."));
            Assert.That(source, Does.Not.Contain("new M0LocomotionSettings(5.0f"));
            Assert.That(source, Does.Not.Contain("new M0LocomotionSettings(5.0"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
        }

        [Test]
        public void GameplayScene_AssignsM0LocomotionConfigReference() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("- Name: locomotionConfig"));
            Assert.That(scene, Does.Contain("guid: c7391b1a9e8f4d92a00c14bdf8b8398e, type: 2"));
        }

        [Test]
        public void M0MemoryRuntimeTuningConfig_DefaultAssetMatchesCurrentM0Values() {
            var config = AssetDatabase.LoadAssetAtPath<M0MemoryRuntimeTuningConfig>(M0MemoryRuntimeTuningConfigAssetPath);

            Assert.That(config, Is.Not.Null, "Default M0 memory runtime tuning config asset should exist.");

            M0MemoryRuntimeTuningSettings settings = config.ToSettings();
            Assert.That(settings.DefaultRevealCandidateId, Is.EqualTo("M0RevealCandidate"));
            Assert.That(settings.RevealFeedbackDurationSeconds, Is.EqualTo(0.25f));
            Assert.That(settings.RevealFeedbackCooldownSeconds, Is.EqualTo(0f));
            Assert.That(settings.RevealFeedbackIntensityLabel, Is.EqualTo("standard"));
        }

        [Test]
        public void M0MemoryRuntimeTuningConfig_StoresStaticTuningOnly() {
            string source = File.ReadAllText(M0MemoryRuntimeTuningConfigPath);

            Assert.That(source, Does.Contain("defaultRevealCandidateId"));
            Assert.That(source, Does.Contain("revealFeedbackDurationSeconds"));
            Assert.That(source, Does.Contain("revealFeedbackCooldownSeconds"));
            Assert.That(source, Does.Contain("revealFeedbackIntensityLabel"));
            Assert.That(source, Does.Not.Contain("collected"));
            Assert.That(source, Does.Not.Contain("revealed"));
            Assert.That(source, Does.Not.Contain("accepted"));
            Assert.That(source, Does.Not.Contain("rejected"));
            Assert.That(source, Does.Not.Contain("duplicate"));
            Assert.That(source, Does.Not.Contain("playback"));
        }

        [Test]
        public void GameplayLifetimeScope_UsesExplicitMemoryRuntimeTuningConfigComposition() {
            string source = File.ReadAllText(GameplayLifetimeScopePath);
            string runtimeRegistrar = File.ReadAllText(M0RuntimeServiceCompositionRegistrarPath);

            Assert.That(source, Does.Contain("RegisterGeneratedFor<IGameplayLifetimeScope>()"));
            Assert.That(source, Does.Contain("M0MemoryRuntimeTuningConfig memoryRuntimeTuningConfig"));
            Assert.That(source, Does.Contain("CreateRuntimeServiceCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Not.Contain("CreateMemoryRuntimeTuningSettings()"));
            Assert.That(source, Does.Not.Contain("memoryRuntimeTuningConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("_memoryRuntimeTuningConfig.ToSettings()"));
            Assert.That(runtimeRegistrar, Does.Contain("memoryRuntimeTuningSettings.DefaultRevealCandidateId"));
            Assert.That(runtimeRegistrar, Does.Contain("memoryRuntimeTuningSettings.RevealFeedbackDurationSeconds"));
            Assert.That(runtimeRegistrar, Does.Contain("memoryRuntimeTuningSettings.RevealFeedbackCooldownSeconds"));
            Assert.That(runtimeRegistrar, Does.Contain("memoryRuntimeTuningSettings.RevealFeedbackIntensityLabel"));
            Assert.That(runtimeRegistrar, Does.Contain("M0RuntimeServiceCompositionRegistrar requires an assigned M0MemoryRuntimeTuningConfig."));
            Assert.That(source, Does.Not.Contain("new M0MemoryState(\"M0RevealCandidate\")"));
            Assert.That(source, Does.Not.Contain("new M0MemoryVFXResponse(0.25f, 0f, \"standard\")"));
            Assert.That(source, Does.Not.Contain("Resources.Load"));
            Assert.That(source, Does.Not.Contain("FindObjectOfType"));
            Assert.That(source, Does.Not.Contain("FindFirstObjectByType"));
            Assert.That(source, Does.Not.Contain("FindAnyObjectByType"));
            Assert.That(source, Does.Not.Contain("FindObjectsByType"));
            Assert.That(source, Does.Not.Contain("ServiceLocator"));
            Assert.That(source, Does.Not.Contain("Debug.Log"));
        }

        [Test]
        public void RuntimeServiceCompositionRegistrar_PreservesManualRegistrationParity() {
            string source = File.ReadAllText(M0RuntimeServiceCompositionRegistrarPath);

            Assert.That(source, Does.Contain("public sealed class M0RuntimeServiceCompositionRegistrar"));
            Assert.That(source, Does.Contain("public void Register(IContainerBuilder builder)"));
            Assert.That(source, Does.Contain("builder.Register(resolver => new M0CombatCore("));
            Assert.That(source, Does.Contain("resolver.Resolve<INhemLogger>()"));
            Assert.That(source, Does.Contain(".As<IM0CombatCore>()"));
            Assert.That(source, Does.Contain("builder.Register(_ => new M0PlayerLocomotion(locomotionSettings), Lifetime.Singleton)"));
            Assert.That(source, Does.Contain(".As<IM0PlayerLocomotion>()"));
            Assert.That(source, Does.Contain("builder.Register(_ => new M0MemoryState(memoryRuntimeTuningSettings.DefaultRevealCandidateId), Lifetime.Singleton)"));
            Assert.That(source, Does.Contain(".As<IM0MemoryState>()"));
            Assert.That(source, Does.Contain("builder.Register(_ => new M0MemoryVFXResponse("));
            Assert.That(source, Does.Contain("Lifetime.Singleton"));
            Assert.That(source, Does.Contain(".AsSelf()"));
        }

        [Test]
        public void RuntimeServiceCompositionRegistrar_ConstructsOnlyAndDoesNotOwnGameplayTruth() {
            string source = File.ReadAllText(M0RuntimeServiceCompositionRegistrarPath);

            Assert.That(source, Does.Not.Contain("RequestAction("));
            Assert.That(source, Does.Not.Contain("TryRequest"));
            Assert.That(source, Does.Not.Contain("ConsumeInputIntent("));
            Assert.That(source, Does.Not.Contain("TryInteract"));
            Assert.That(source, Does.Not.Contain("IntakeRevealRequest("));
            Assert.That(source, Does.Not.Contain("EvaluateRequestedReveal("));
            Assert.That(source, Does.Not.Contain("OnAcceptedReveal("));
            Assert.That(source, Does.Not.Contain("OnPlaybackStarted("));
            Assert.That(source, Does.Not.Contain("OnPlaybackComplete("));
        }

        [Test]
        public void GameplayScene_AssignsM0MemoryRuntimeTuningConfigReference() {
            string scene = File.ReadAllText(GameplayScenePath);

            Assert.That(scene, Does.Contain("- Name: memoryRuntimeTuningConfig"));
            Assert.That(scene, Does.Contain("guid: fa048c7d2e1bd4745a743423cc6f728a, type: 2"));
        }

        [Test]
        public void M0SceneCompositionRegistrar_HandlesSceneComponentRegistrationAndWiringOnly() {
            string source = File.ReadAllText(M0SceneCompositionRegistrarPath);

            Assert.That(source, Does.Contain("public sealed class M0SceneCompositionRegistrar"));
            Assert.That(source, Does.Contain("RegisterSceneComponents(builder)"));
            Assert.That(source, Does.Contain("RegisterBuildWiring(builder)"));
            Assert.That(source, Does.Contain("builder.UseComponents(components =>"));
            Assert.That(source, Does.Contain("components.AddInstance(_tickHandler)"));
            Assert.That(source, Does.Contain("components.AddInstance(_playerAnimationDriver).As<IPlayerAnimationService>()"));
            Assert.That(source, Does.Contain("components.AddInstance(_enemyAnimationDriver).As<IEnemyAnimationService>()"));
            Assert.That(source, Does.Contain("_tickHandler.SetVisualFeedbackAdapter(_visualFeedbackAdapter)"));
            Assert.That(source, Does.Contain("_loopDriver.Construct(enemyIntentModel, _logger)"));
            Assert.That(source, Does.Not.Contain("new M0CombatCore"));
            Assert.That(source, Does.Not.Contain("new M0PlayerLocomotion"));
            Assert.That(source, Does.Not.Contain("new M0MemoryState"));
            Assert.That(source, Does.Not.Contain("RequestAction("));
            Assert.That(source, Does.Not.Contain("ConsumeInputIntent("));
            Assert.That(source, Does.Not.Contain("TryInteract"));
        }

        [Test]
        public void GameplayLifetimeScope_ReadsAsHighLevelCompositionOrder() {
            string source = File.ReadAllText(GameplayLifetimeScopePath);

            Assert.That(source, Does.Contain("RegisterGeneratedFor<IGameplayLifetimeScope>()"));
            Assert.That(source, Does.Contain("Register<INhemLogger"));
            Assert.That(source, Does.Contain("CreateRuntimeServiceCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Contain("CreateSceneCompositionRegistrar().Register(builder)"));
            Assert.That(source, Does.Not.Contain("new M0CombatCore"));
            Assert.That(source, Does.Not.Contain("new M0PlayerLocomotion"));
            Assert.That(source, Does.Not.Contain("new M0MemoryState"));
            Assert.That(source, Does.Not.Contain("new M0MemoryVFXResponse"));
        }

        [Test]
        public void GameplayLifetimeScopeEditor_BindsSerializedFieldsWithoutDirectDebugLogging() {
            string editor = File.ReadAllText(GameplayLifetimeScopeEditorPath);
            string uxml = File.ReadAllText(GameplayLifetimeScopeEditorUxmlPath);

            Assert.That(editor, Does.Contain("rootElement.Bind(serializedObject)"));
            Assert.That(editor, Does.Not.Contain("Debug.Log"));
            Assert.That(uxml, Does.Contain("binding-path=\"tickHandler\""));
            Assert.That(uxml, Does.Contain("binding-path=\"combatTimingConfig\""));
            Assert.That(uxml, Does.Contain("binding-path=\"locomotionConfig\""));
            Assert.That(uxml, Does.Contain("binding-path=\"memoryRuntimeTuningConfig\""));
            Assert.That(uxml, Does.Contain("binding-path=\"memoryProbe\""));
            Assert.That(uxml, Does.Contain("binding-path=\"memoryFragments\""));
        }
    }
}
