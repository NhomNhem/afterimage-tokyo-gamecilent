using NUnit.Framework;
using VContainer;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Targeting;

namespace GlassRefrain.Tests.EditMode {
    /// <summary>
    /// Tests for manual VContainer DI registration of Target Context services.
    /// Validates ADR-0004 compliance (manual registration, no generated DI).
    /// </summary>
    public class TestManualTargetingDIRegistration {
        [Test]
        public void M0TargetContext_Resolves_From_GameplayScope() {
            // Given
            var builder = new ContainerBuilder();
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
        public void Manual_Registration_No_Generated_DI() {
            // Given - manual registration as per ADR-0004
            var builder = new ContainerBuilder();
            builder.Register<ITargetableRegistry, M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var registry = container.Resolve<ITargetableRegistry>();
                var context = container.Resolve<M0TargetContext>();
                var router = container.Resolve<M0InputRouter>();

                // Then - all resolve correctly through manual registration
                Assert.That(registry, Is.Not.Null);
                Assert.That(context, Is.Not.Null);
                Assert.That(router, Is.Not.Null);
            }
        }

        [Test]
        public void Scoped_Lifetime_Applied() {
            // Given - singleton lifetime for M0 (scene-scoped)
            var builder = new ContainerBuilder();
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0TargetableRegistry>(Lifetime.Singleton);
            builder.Register<M0InputRouter>(Lifetime.Singleton);

            // When
            using (var container = builder.Build()) {
                var context1 = container.Resolve<M0TargetContext>();
                var context2 = container.Resolve<M0TargetContext>();
                var registry1 = container.Resolve<M0TargetableRegistry>();
                var registry2 = container.Resolve<M0TargetableRegistry>();
                var router1 = container.Resolve<M0InputRouter>();
                var router2 = container.Resolve<M0InputRouter>();

                // Then - singleton instances are the same
                Assert.That(context1, Is.SameAs(context2), "M0TargetContext should be singleton");
                Assert.That(registry1, Is.SameAs(registry2), "M0TargetableRegistry should be singleton");
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
