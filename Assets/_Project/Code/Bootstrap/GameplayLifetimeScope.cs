using System;
using GlassRefrain.Code.Shared.DI;
using GlassRefrain.Code.Shared.Extentions;
using GlassRefrain.Combat;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using VContainer;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for M0 Gameplay.
    /// Resolves core gameplay skeleton services and wires runtime drivers.
    /// </summary>
    [LifetimeScopeFor<IGameplayLifetimeScope>]
    public sealed class GameplayLifetimeScope : SerializedLifetimeScope {
        #region Gameplay Scope / Core Adapters

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0GameplayTickHandler tickHandler;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0TargetableSceneAdapter targetableAdapter;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0EnemyIntentLoopDriver loopDriver;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0DirectPlayerInput playerInput;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0CombatDebugOverlayAdapter debugOverlayAdapter;

        [TabGroup("Gameplay Scope", "Core Adapters"), OdinSerialize, Required]
        private M0AnimationPresentationAdapter animationPresentationAdapter;

        #endregion

        #region Gameplay Scope / Animation Drivers

        [TabGroup("Gameplay Scope", "Animation Drivers"), OdinSerialize, Required]
        private AnimancerPlayerAnimationDriver playerAnimationDriver;

        [TabGroup("Gameplay Scope", "Animation Drivers"), OdinSerialize, Required]
        private AnimancerEnemyAnimationDriver enemyAnimationDriver;

        #endregion

        #region Gameplay Scope / Runtime Services

        [TabGroup("Gameplay Scope", "Configs"), Required]
        private M0CombatTimingConfig combatTimingConfig;

        [TabGroup("Gameplay Scope", "Configs"), Required]
        private M0LocomotionConfig locomotionConfig;

        [TabGroup("Gameplay Scope", "Configs"), Required]
        private M0MemoryRuntimeTuningConfig memoryRuntimeTuningConfig;

        #endregion

        #region Gameplay Scope / Memory System
        [TabGroup("Gameplay Scope", "Memory System"), OdinSerialize, Required]
        private MemoryRaycastProProbe memoryProbe;

        [TabGroup("Gameplay Scope", "Memory System"), OdinSerialize, Required]
        private MemoryFragment[] memoryFragments = Array.Empty<MemoryFragment>();
        #endregion

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
