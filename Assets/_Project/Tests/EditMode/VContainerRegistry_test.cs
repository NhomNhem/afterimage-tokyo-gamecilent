using NUnit.Framework;
using VContainer;
using GlassRefrain.Combat;
using GlassRefrain.Locomotion;
using GlassRefrain.Targeting;
using GlassRefrain.Health;
using GlassRefrain.Enemy;
using GlassRefrain.Memory;

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
            
            // When (Manual registration as seen in GameplayLifetimeScope)
            builder.Register<M0CombatCore>(Lifetime.Singleton);
            builder.Register<M0PlayerLocomotion>(Lifetime.Singleton);
            builder.Register<M0TargetContext>(Lifetime.Singleton);
            builder.Register<M0HealthDamageReactionModel>(Lifetime.Singleton);
            builder.Register<M0EnemyIntentModel>(Lifetime.Singleton);
            builder.Register<M0MemoryState>(Lifetime.Singleton);

            // Then
            using (var container = builder.Build()) {
                Assert.That(container.Resolve<M0CombatCore>(), Is.Not.Null, "M0CombatCore failed to resolve");
                Assert.That(container.Resolve<M0PlayerLocomotion>(), Is.Not.Null, "M0PlayerLocomotion failed to resolve");
                Assert.That(container.Resolve<M0TargetContext>(), Is.Not.Null, "M0TargetContext failed to resolve");
                Assert.That(container.Resolve<M0HealthDamageReactionModel>(), Is.Not.Null, "M0HealthDamageReactionModel failed to resolve");
                Assert.That(container.Resolve<M0EnemyIntentModel>(), Is.Not.Null, "M0EnemyIntentModel failed to resolve");
                Assert.That(container.Resolve<M0MemoryState>(), Is.Not.Null, "M0MemoryState failed to resolve");
            }
        }
    }
}
