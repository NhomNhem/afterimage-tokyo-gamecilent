using UnityEngine;
using VContainer;
using VContainer.Unity;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;
using GlassRefrain.Health;
using GlassRefrain.Enemy;
using GlassRefrain.Memory;
using NhemDangFugBixs.NhemLogging;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for M0 Gameplay.
    /// Resolves core gameplay skeleton services and wires runtime drivers.
    /// </summary>
    public sealed class GameplayLifetimeScope : LifetimeScope {
        [SerializeField] private M0GameplayTickHandler tickHandler;
        [SerializeField] private M0TargetableSceneAdapter targetableAdapter;
        [SerializeField] private M0EnemyIntentLoopDriver loopDriver;
        private INhemLogger logger;

        protected override void Configure(IContainerBuilder builder) {
            // Manual VContainer registration for M0.
            // ADR-0004: All registrations MUST be manual in composition roots.

            // Logging: register INhemLogger for gameplay-scoped debug logs
#if GR_M0_PROTOTYPE
            builder.Register<INhemLogger, NhemUnityLogger>(Lifetime.Singleton);
#else
            builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
#endif

            // Core Gameplay Skeletons (Pure C# Authority)
            builder.Register<M0CombatCore>(Lifetime.Singleton);
            builder.RegisterInstance(new M0LocomotionSettings());
            builder.Register<M0PlayerLocomotion>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0HealthDamageReactionModel>(Lifetime.Singleton);
            builder.RegisterInstance(new M0EnemyIntentModel());
            builder.Register<M0MemoryState>(Lifetime.Singleton);

            // Targeting: Manual DI per ADR-0004
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);

            // Input: Router service for routing intents to gameplay systems
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // Explicit manual composition: register scene components for M0 runtime wiring.
            // M0GameplayTickHandler receives the singleton M0PlayerLocomotion via [Inject]
            // and wires both the adapter and the input bridge.
            if (tickHandler != null) {
                builder.RegisterComponent(tickHandler);
            }

            if (targetableAdapter != null) {
                builder.RegisterComponent(targetableAdapter);
            }

            if (loopDriver != null) {
                builder.RegisterComponent(loopDriver);
            }
        }
    }
}
