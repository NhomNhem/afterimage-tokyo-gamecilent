using VContainer;
using VContainer.Unity;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for the Application.
    /// Resolves global/infrastructure services that persist across scenes.
    /// </summary>
    public sealed class ProjectRootLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder) {
            // Manual VContainer registration for M0.
            // ADR-0004: All registrations MUST be manual in composition roots.

            // ProjectRoot owns application-level services only.
            // It MUST NOT own gameplay truth (Combat, Locomotion, etc.).
        }
    }
}
