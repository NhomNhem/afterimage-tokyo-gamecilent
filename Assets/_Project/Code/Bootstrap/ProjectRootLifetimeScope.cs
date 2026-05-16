using NhemDangFugBixs.NhemLogging;
using VContainer;
using VContainer.Unity;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for the Application.
    /// Resolves global/infrastructure services that persist across scenes.
    /// </summary>
    public sealed class ProjectRootLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder)
        {
            RegisterLogging(builder);
            RegisterDebugOverlay(builder);
        }

        private static void RegisterLogging(IContainerBuilder builder)
        {
#if GR_DEBUG_LOGS
            builder.Register<INhemLogger, NhemUnityLogger>(Lifetime.Singleton);
#else
    builder.Register<INhemLogger, NhemNullLogger>(Lifetime.Singleton);
#endif
        }

        private static void RegisterDebugOverlay(IContainerBuilder builder)
        {
#if GR_DEBUG_OVERLAY
            //builder.RegisterComponentInHierarchy<M0DebugOverlayPresenter>().As<IDebugOverlaySink>();
#else
    builder.Register<IDebugOverlaySink, NullDebugOverlaySink>(Lifetime.Singleton);
#endif
        }
    }
}
