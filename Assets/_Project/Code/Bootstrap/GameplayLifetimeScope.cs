using System;
using _Project.Code.Shared.DI;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for M0 Gameplay.
    /// Resolves core gameplay skeleton services and wires runtime drivers.
    /// </summary>
    [LifetimeScopeFor<IGameplayLifetimeScope>]
    public sealed class GameplayLifetimeScope : LifetimeScope {
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0GameplayTickHandler tickHandler;
        [SerializeField, Required] private M0TargetableSceneAdapter targetableAdapter;
        [SerializeField, Required] private M0EnemyIntentLoopDriver loopDriver;
        [SerializeField, Required] private M0DirectPlayerInput playerInput;
        [SerializeField, Required] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [SerializeField, Required] private M0CombatDebugOverlayAdapter debugOverlayAdapter;
        [SerializeField, Required] private M0AnimationPresentationAdapter animationPresentationAdapter;

        [TabGroup("Gameplay Scope", "Animation Drivers")]
        [SerializeField, Required] private AnimancerPlayerAnimationDriver playerAnimationDriver;
        [SerializeField, Required] private AnimancerEnemyAnimationDriver enemyAnimationDriver;

        [TabGroup("Gameplay Scope", "Configs")]
        [SerializeField, Required] private M0CombatTimingConfig combatTimingConfig;
        [SerializeField, Required] private M0LocomotionConfig locomotionConfig;

        [TabGroup("Gameplay Scope", "Memory System")]
        [SerializeField] private MemoryRaycastProProbe memoryProbe;
        [SerializeField] private MemoryFragment[] memoryFragments = Array.Empty<MemoryFragment>();

        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterGeneratedFor<IGameplayLifetimeScope>();

#if GR_M0_PROTOTYPE
            builder.Register<INhemLogger, NhemUnityLogger>(Lifetime.Singleton);
#else
            builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
#endif

            builder.Register(resolver => new M0CombatCore(
                    CreateCombatTimingSettings(),
                    resolver.Resolve<INhemLogger>()),
                Lifetime.Singleton)
                .As<IM0CombatCore>()
                .AsSelf();

            builder.Register(_ => new M0PlayerLocomotion(CreateLocomotionSettings()), Lifetime.Singleton)
                .As<IM0PlayerLocomotion>()
                .AsSelf();

            builder.Register(_ => new M0MemoryState("M0RevealCandidate"), Lifetime.Singleton)
                .As<IM0MemoryState>()
                .AsSelf();
            builder.Register(_ => new M0MemoryVFXResponse(0.25f, 0f, "standard"), Lifetime.Singleton)
                .AsSelf();

            CreateSceneCompositionRegistrar().Register(builder);
        }

        private M0CombatTimingSettings CreateCombatTimingSettings() {
            if (combatTimingConfig == null) {
                throw new InvalidOperationException("GameplayLifetimeScope requires an assigned M0CombatTimingConfig.");
            }

            return combatTimingConfig.ToSettings();
        }

        private M0LocomotionSettings CreateLocomotionSettings() {
            if (locomotionConfig == null) {
                throw new InvalidOperationException("GameplayLifetimeScope requires an assigned M0LocomotionConfig.");
            }

            return locomotionConfig.ToSettings();
        }

        private M0SceneCompositionRegistrar CreateSceneCompositionRegistrar() {
            return new M0SceneCompositionRegistrar(
                tickHandler,
                targetableAdapter,
                loopDriver,
                playerInput,
                visualFeedbackAdapter,
                debugOverlayAdapter,
                animationPresentationAdapter,
                playerAnimationDriver,
                enemyAnimationDriver,
                memoryProbe,
                memoryFragments);
        }
    }
}
