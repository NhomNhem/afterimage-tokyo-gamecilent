using NUnit.Framework;
using UnityEngine;
using VContainer;
using GlassRefrain.Bootstrap;
using GlassRefrain.Core;
using GlassRefrain.Combat;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;
using GlassRefrain.Health;
using GlassRefrain.Enemy;
using GlassRefrain.Memory;
using NhemDangFugBixs.NhemLogging;

#pragma warning disable ND005 // Test-only containers intentionally mirror isolated manual VContainer wiring.

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Verifies that all M0 technical skeleton services resolve correctly in a manual VContainer builder.
    /// This validates the wiring logic used in GameplayLifetimeScope.
    /// </summary>
    public class VContainerRegistry_test {
        [Test]
        public void GameplayScope_CanResolveM0Skeletons() {
            // Given
            var builder = new ContainerBuilder();
            var runtimeServiceRegistrar = new M0RuntimeServiceCompositionRegistrar(
                ScriptableObject.CreateInstance<M0CombatTimingConfig>(),
                ScriptableObject.CreateInstance<M0LocomotionConfig>(),
                ScriptableObject.CreateInstance<M0MemoryRuntimeTuningConfig>());

            // When (Manual registration as seen in GameplayLifetimeScope)
            builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
            runtimeServiceRegistrar.Register(builder);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register(_ => new M0HealthDamageReactionModel(), Lifetime.Singleton);
            builder.Register<M0EnemyIntentModel>(Lifetime.Singleton);
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // Then
            using (var container = builder.Build()) {
                Assert.That(container.Resolve<CombatCore>(), Is.Not.Null, "CombatCore failed to resolve");
                Assert.That(container.Resolve<LocomotionCore>(), Is.Not.Null, "LocomotionCore failed to resolve");
                Assert.That(container.Resolve<M0TargetContext>(), Is.Not.Null, "M0TargetContext failed to resolve");
                Assert.That(container.Resolve<M0HealthDamageReactionModel>(), Is.Not.Null, "M0HealthDamageReactionModel failed to resolve");
                Assert.That(container.Resolve<M0EnemyIntentModel>(), Is.Not.Null, "M0EnemyIntentModel failed to resolve");
                Assert.That(container.Resolve<M0MemoryState>(), Is.Not.Null, "M0MemoryState failed to resolve");
                Assert.That(container.Resolve<M0MemoryVFXResponse>(), Is.Not.Null, "M0MemoryVFXResponse failed to resolve");
                Assert.That(container.Resolve<ITargetableRegistry>(), Is.Not.Null, "ITargetableRegistry failed to resolve");
                Assert.That(container.Resolve<M0InputRouter>(), Is.Not.Null, "M0InputRouter failed to resolve");
                Assert.That(container.Resolve<ICombatCore>(), Is.SameAs(container.Resolve<CombatCore>()));
                Assert.That(container.Resolve<ILocomotionCore>(), Is.SameAs(container.Resolve<LocomotionCore>()));
                Assert.That(container.Resolve<IM0MemoryState>(), Is.SameAs(container.Resolve<M0MemoryState>()));
            }
        }

        [Test]
        public void TargetableRegistry_Resolves_AsSingleton() {
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);

            using (var container = builder.Build()) {
                var registry1 = container.Resolve<ITargetableRegistry>();
                var registry2 = container.Resolve<ITargetableRegistry>();
                Assert.That(registry1, Is.SameAs(registry2), "ITargetableRegistry should be singleton");
            }
        }

        [Test]
        public void InputRouter_Resolves_AsSingleton() {
            var builder = new ContainerBuilder();
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            using (var container = builder.Build()) {
                var router1 = container.Resolve<M0InputRouter>();
                var router2 = container.Resolve<M0InputRouter>();
                Assert.That(router1, Is.SameAs(router2), "M0InputRouter should be singleton");
            }
        }
    }
}

#pragma warning restore ND005
