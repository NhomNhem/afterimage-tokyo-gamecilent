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
    /// Manual VContainer composition root for M0 Gameplay.
    /// Resolves core gameplay skeleton services and wires runtime drivers.
    /// </summary>

    [LifetimeScopeFor<IGameplayLifetimeScope>]
    public sealed class GameplayLifetimeScope : LifetimeScope {
        [SerializeField, Required] private M0GameplayTickHandler tickHandler;
        [SerializeField, Required] private M0TargetableSceneAdapter targetableAdapter;
        [SerializeField, Required] private M0EnemyIntentLoopDriver loopDriver;
        [SerializeField, Required] private M0DirectPlayerInput playerInput;
        [SerializeField, Required] private M0CombatVisualFeedbackAdapter visualFeedbackAdapter;
        [SerializeField, Required] private M0CombatDebugOverlayAdapter debugOverlayAdapter;
        [SerializeField, Required] private M0AnimationPresentationAdapter animationPresentationAdapter;
        [SerializeField, Required] private AnimancerPlayerAnimationDriver playerAnimationDriver;
        [SerializeField, Required] private AnimancerEnemyAnimationDriver enemyAnimationDriver;

        private INhemLogger _logger;

        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterGeneratedFor<IGameplayLifetimeScope>();

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
                    new M0CombatTimingSettings(
                        attackStartupSeconds: 0.14f,
                        attackActiveSeconds: 0.20f,
                        attackRecoverySeconds: 0.26f,
                        dodgeStartupSeconds: 0.09f,
                        dodgeActiveSeconds: 0.20f,
                        dodgeRecoverySeconds: 0.24f,
                        parryStartupSeconds: 0.10f,
                        parryActiveSeconds: 0.18f,
                        parryRecoverySeconds: 0.24f,
                        counterWindowDurationSeconds: 3.0f,
                        recoveryDurationSeconds: 0.24f),
                    resolver.Resolve<INhemLogger>()),
                Lifetime.Singleton)
                .As<IM0CombatCore>()
                .AsSelf();
            builder.Register(_ => new M0PlayerLocomotion(new M0LocomotionSettings(5.0f, 0.1f, 8.0f, 1.5f, 10.0f, 0.2f)), Lifetime.Singleton)
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

                var memoryProbe = FindFirstObjectByType<MemoryRaycastProProbe>(FindObjectsInactive.Include);
                if (memoryProbe != null) {
                    container.Inject(memoryProbe);
                } else {
                    _logger?.LogWarning("[M0Bootstrap] MemoryRaycastProProbe not found in scene; memory interact debug probe unavailable.");
                }

                var memoryFragments = FindObjectsByType<MemoryFragment>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (int i = 0; i < memoryFragments.Length; i++) {
                    container.Inject(memoryFragments[i]);
                }

                _logger?.Log("[M0Bootstrap] Memory DI injected: probe=" + (memoryProbe != null) + " fragments=" + memoryFragments.Length);

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
    }

}
