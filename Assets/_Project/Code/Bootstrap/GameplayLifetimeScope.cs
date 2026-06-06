using System;
using _Project.Code.Shared.DI;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using GlassRefrain.Health;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using GlassRefrain.Targeting;
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
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0TargetableSceneAdapter targetableAdapter;
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0EnemyIntentLoopDriver loopDriver;
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0DirectPlayerInput playerInput;
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0CombatDebugOverlayAdapter debugOverlayAdapter;
        [TabGroup("Gameplay Scope", "Core Adapters")]
        [SerializeField, Required] private M0AnimationPresentationAdapter animationPresentationAdapter;

        [TabGroup("Gameplay Scope", "Animation Drivers")]
        [SerializeField, Required] private AnimancerPlayerAnimationDriver playerAnimationDriver;
        [TabGroup("Gameplay Scope", "Animation Drivers")]
        [SerializeField, Required] private AnimancerEnemyAnimationDriver enemyAnimationDriver;

        [TabGroup("Gameplay Scope", "Configs")]
        [SerializeField, Required] private M0CombatTimingConfig combatTimingConfig;
        [TabGroup("Gameplay Scope", "Configs")]
        [SerializeField, Required] private M0LocomotionConfig locomotionConfig;

        [TabGroup("Gameplay Scope", "Memory System")]
        [SerializeField] private MemoryRaycastProProbe memoryProbe;
        [TabGroup("Gameplay Scope", "Memory System")]
        [SerializeField] private MemoryFragment[] memoryFragments = Array.Empty<MemoryFragment>();

        private INhemLogger _logger;

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

            RegisterSceneComponents(builder);
            RegisterBuildWiring(builder);
        }

        private void RegisterSceneComponents(IContainerBuilder builder) {
            if (tickHandler != null) {
                builder.RegisterComponent(tickHandler);
            }

            if (targetableAdapter != null) {
                builder.RegisterComponent(targetableAdapter);
            }

            if (visualFeedbackAdapter != null) {
                builder.RegisterComponent(visualFeedbackAdapter);
            }

            if (debugOverlayAdapter != null) {
                builder.RegisterComponent(debugOverlayAdapter);
            }

            if (animationPresentationAdapter != null) {
                builder.RegisterComponent(animationPresentationAdapter);
            }

            if (playerAnimationDriver != null) {
                builder.RegisterComponent(playerAnimationDriver).As<IPlayerAnimationService>();
            }

            if (enemyAnimationDriver != null) {
                builder.RegisterComponent(enemyAnimationDriver).As<IEnemyAnimationService>();
            }

            if (loopDriver != null) {
                builder.RegisterComponent(loopDriver);
            }

            if (playerInput != null) {
                builder.RegisterComponent(playerInput);
            }
        }

        private void RegisterBuildWiring(IContainerBuilder builder) {
            builder.RegisterBuildCallback(container => {
                _logger = container.Resolve<INhemLogger>();
                playerInput?.SetLogger(_logger);

                InjectMemorySceneParticipants(container);
                WirePresentationAdapters();
                WireEnemyLoop(container);
            });
        }

        private void WirePresentationAdapters() {
            if (tickHandler != null) {
                tickHandler.SetVisualFeedbackAdapter(visualFeedbackAdapter);
                tickHandler.SetDebugOverlayAdapter(debugOverlayAdapter);
                tickHandler.SetAnimationPresentationAdapter(animationPresentationAdapter);
            }

            if (animationPresentationAdapter == null || playerAnimationDriver == null || enemyAnimationDriver == null) {
                _logger?.LogWarning("[M0Bootstrap] Animation presentation not assigned; animation playback disabled for this M0 smoke run.");
            }
        }

        private void WireEnemyLoop(IObjectResolver container) {
            if (loopDriver == null) {
                _logger?.LogWarning("[GameplayLifetimeScope] loopDriver is null in build callback");
                return;
            }

            var enemyIntentModel = container.Resolve<M0EnemyIntentModel>();
            loopDriver.Construct(enemyIntentModel, _logger);
            playerInput?.SetEnemyDebugHarness(loopDriver);
        }

        private int InjectMemorySceneParticipants(IObjectResolver container) {
            if (memoryProbe != null) {
                container.Inject(memoryProbe);
            } else {
                _logger?.LogWarning("[M0Bootstrap] MemoryRaycastProProbe reference is not assigned; memory interact debug probe unavailable.");
            }

            int injectedFragmentCount = 0;
            if (memoryFragments != null) {
                for (int i = 0; i < memoryFragments.Length; i++) {
                    var fragment = memoryFragments[i];
                    if (fragment == null) {
                        continue;
                    }

                    container.Inject(fragment);
                    injectedFragmentCount++;
                }
            }

            _logger?.Log("[M0Bootstrap] Memory DI injected: probe=" + (memoryProbe != null) + " fragments=" + injectedFragmentCount);
            if (injectedFragmentCount == 0) {
                _logger?.LogWarning("[M0Bootstrap] No MemoryFragment references assigned; memory interaction fragments will not register.");
            }

            return injectedFragmentCount;
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
    }
}
