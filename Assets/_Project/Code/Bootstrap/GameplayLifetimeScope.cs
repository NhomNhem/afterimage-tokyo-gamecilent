using System;
using GlassRefrain.Code.Shared.DI;
using GlassRefrain.Combat;
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
        [SerializeField, Required] private M0MemoryRuntimeTuningConfig memoryRuntimeTuningConfig;

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

            CreateRuntimeServiceCompositionRegistrar().Register(builder);
            CreateSceneCompositionRegistrar().Register(builder);
        }

        private M0RuntimeServiceCompositionRegistrar CreateRuntimeServiceCompositionRegistrar() {
            return new M0RuntimeServiceCompositionRegistrar(
                combatTimingConfig,
                locomotionConfig,
                memoryRuntimeTuningConfig);
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
