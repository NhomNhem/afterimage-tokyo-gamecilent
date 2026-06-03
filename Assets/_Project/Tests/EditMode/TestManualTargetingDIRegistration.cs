using NUnit.Framework;
using VContainer;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Targeting;

#pragma warning disable ND005 // Test-only containers intentionally validate isolated manual wiring.

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Tests for isolated VContainer registration of Target Context services.
    /// Runtime composition now uses Nhem DI source generation, while these tests keep
    /// dependency shape explicit for targeted container coverage.
    /// </summary>
    public class TestManualTargetingDIRegistration {
        [Test]
        public void M0TargetContext_Resolves_From_GameplayScope() {
            // Given
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var context = container.Resolve<M0TargetContext>();

                // Then
                Assert.That(context, Is.Not.Null);
                Assert.That(context, Is.InstanceOf<M0TargetContext>());
            }
        }

        [Test]
        public void Explicit_Targeting_Registration_Resolves_Dependencies() {
            // Given
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var registry = container.Resolve<ITargetableRegistry>();
                var context = container.Resolve<M0TargetContext>();
                var router = container.Resolve<M0InputRouter>();

                // Then - all resolve correctly through explicit test registration
                Assert.That(registry, Is.Not.Null);
                Assert.That(context, Is.Not.Null);
                Assert.That(router, Is.Not.Null);
            }
        }

        [Test]
        public void Scoped_Lifetime_Applied() {
            // Given - singleton lifetime for M0 (scene-scoped)
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var context1 = container.Resolve<M0TargetContext>();
                var context2 = container.Resolve<M0TargetContext>();
                var registry1 = container.Resolve<ITargetableRegistry>();
                var registry2 = container.Resolve<ITargetableRegistry>();
                var router1 = container.Resolve<M0InputRouter>();
                var router2 = container.Resolve<M0InputRouter>();

                // Then - singleton instances are the same
                Assert.That(context1, Is.SameAs(context2), "M0TargetContext should be singleton");
                Assert.That(registry1, Is.SameAs(registry2), "ITargetableRegistry should be singleton");
                Assert.That(router1, Is.SameAs(router2), "M0InputRouter should be singleton");
            }
        }

        [Test]
        public void ITargetableRegistry_Resolves_To_M0TargetableRegistry() {
            // Given
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var registry = container.Resolve<ITargetableRegistry>();

                // Then
                Assert.That(registry, Is.InstanceOf<M0TargetableRegistry>());
            }
        }
    }
}

#pragma warning restore ND005
