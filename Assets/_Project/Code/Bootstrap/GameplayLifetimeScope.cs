using _Project.Code.Shared.DI;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Enemy;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using Sirenix.OdinInspector;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for M0 Gameplay.
    /// Resolves core gameplay skeleton services and wires runtime drivers.
    /// </summary>

    [LifetimeScopeFor<IGameplayLifetimeScope>()]
    public sealed class GameplayLifetimeScope : LifetimeScope {
        [SerializeField, Required] private M0GameplayTickHandler? tickHandler;
        [SerializeField] private M0TargetableSceneAdapter? targetableAdapter;
        [SerializeField] private M0EnemyIntentLoopDriver? loopDriver;
        private INhemLogger? _logger;

        protected override void Configure(IContainerBuilder builder) {
            // Manual VContainer registration for M0.
            // ADR-0004: All registrations MUST be manual in composition roots.

            // Auto-register all types marked with [AutoRegisterIn<IGameplayLifetimeScope>]
            // Source generator will generate registration code automatically

            // Logging: register INhemLogger for gameplay-scoped debug logs
#if GR_M0_PROTOTYPE
            builder.Register<INhemLogger, NhemUnityLogger>(Lifetime.Singleton);
#else
            builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
#endif

            builder.RegisterGeneratedFor<IGameplayLifetimeScope>();
            // Core Gameplay Skeletons (Pure C# Authority)
            // M0CombatCore is auto-registered via [AutoRegisterIn<IGameplayLifetimeScope>]
            builder.RegisterInstance(new M0LocomotionSettings());
            //builder.Register<M0PlayerLocomotion>(Lifetime.Singleton);
            //builder.Register<M0TargetContext>(Lifetime.Singleton);
            //builder.Register<M0HealthDamageReactionModel>(Lifetime.Singleton);
            builder.RegisterInstance(new M0EnemyIntentModel());
            //builder.Register<M0MemoryState>(Lifetime.Singleton);

            // Targeting: Manual DI per ADR-0004
            //builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);

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
