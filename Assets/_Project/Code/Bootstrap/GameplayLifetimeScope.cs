using System;
using _Project.Code.Shared.DI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Health;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Enemy;
using GlassRefrain.Memory;
using GlassRefrain.Targeting;
using GlassRefrain.Presentation;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace GlassRefrain.Bootstrap {
    /// <summary>
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Health;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Enemy;
using GlassRefrain.Memory;
using GlassRefrain.Targeting;
using GlassRefrain.Presentation;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

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
        [SerializeField] private MemoryFragment[] memoryFragments = new MemoryFragment[0];

            // Manual VContainer registration for M0.
            // ADR-0004: All registrations MUST be manual in composition roots.

            // Logging: register INhemLogger for gameplay-scoped debug logs
#if GR_M0_PROTOTYPE
            builder.Register<INhemLogger, NhemUnityLogger>(Lifetime.Singleton);
#else
            builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
#endif

            // Core Gameplay Skeletons (Pure C# Authority)
            builder.Register(resolver => new M0CombatCore(
                    CreateCombatTimingSettings(),
                    resolver.Resolve<INhemLogger>()),
                Lifetime.Singleton)
                .As<IM0CombatCore>()
                .AsSelf();
            builder.Register(_ => new M0PlayerLocomotion(CreateLocomotionSettings()), Lifetime.Singleton)
                .As<IM0PlayerLocomotion>()
                .AsSelf();
            // builder.Register<M0TargetContext>(Lifetime.Singleton).As<IM0TargetContext>().AsSelf();
            // builder.Register<M0HealthDamageReactionModel>(Lifetime.Singleton).As<IM0HealthDamageReactionModel>().AsSelf();
            //builder.Register<M0EnemyIntentModel>(Lifetime.Singleton).AsSelf();
            builder.Register(_ => new M0MemoryState("M0RevealCandidate"), Lifetime.Singleton)
                .As<IM0MemoryState>()
                .AsSelf();
            builder.Register(_ => new M0MemoryVFXResponse(0.25f, 0f, "standard"), Lifetime.Singleton)
                .AsSelf();


            // Explicit manual composition: register scene components for M0 runtime wiring.
            // M0GameplayTickHandler receives the singleton M0PlayerLocomotion via [Inject]
            // and wires both the adapter and the input bridge.
            if (tickHandler != null) builder.RegisterComponent(tickHandler);

            if (targetableAdapter != null) builder.RegisterComponent(targetableAdapter);
            builder.RegisterComponent(visualFeedbackAdapter);
            builder.RegisterComponent(debugOverlayAdapter);
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

            // Post-build wiring: resolve dependencies and wire to components that need manual setup
            builder.RegisterBuildCallback(container => {
                _logger = container.Resolve<INhemLogger>();
                playerInput?.SetLogger(_logger);

                InjectMemorySceneParticipants(container);

                // Wire presentation adapters to tickHandler
                if (tickHandler != null) {
                    tickHandler.SetVisualFeedbackAdapter(visualFeedbackAdapter);
                    tickHandler.SetDebugOverlayAdapter(debugOverlayAdapter);
                    tickHandler.SetAnimationPresentationAdapter(animationPresentationAdapter);
                }

                if (animationPresentationAdapter == null || playerAnimationDriver == null || enemyAnimationDriver == null) {
                    _logger?.LogWarning("[M0Bootstrap] Animation presentation not assigned; animation playback disabled for this M0 smoke run.");
                }

                // Manual dependency injection for enemy loop driver
                if (loopDriver != null) {
                    var enemyIntentModel = container.Resolve<M0EnemyIntentModel>();
                    loopDriver.Construct(enemyIntentModel, _logger);
                    playerInput?.SetEnemyDebugHarness(loopDriver);
                } else {
                    _logger?.LogWarning("[GameplayLifetimeScope] loopDriver is null in build callback");
                }
            });
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
