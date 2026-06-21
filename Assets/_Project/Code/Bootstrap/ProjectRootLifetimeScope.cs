using GlassRefrain.Camera;
using GlassRefrain.Code.Shared.DI;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using NhemDangFugBixs.VContainer;
using VContainer;
using VContainer.Unity;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for the Application.
    /// Resolves global/infrastructure services that persist across scenes.
    /// </summary>

    [LifetimeScopeFor<IProjectRootLifetimeScope>]
    public sealed class ProjectRootLifetimeScope : LifetimeScope {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterGeneratedFor<IProjectRootLifetimeScope>();

            RegisterLogging(builder);
            RegisterDebugOverlay(builder);
            RegisterCameraServices(builder);
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

        private static void RegisterCameraServices(IContainerBuilder builder)
        {
            // Cross-scene camera target provider — registered in parent scope so both gameplay and camera scenes can access.
            builder.Register<IM0CameraTargetProvider, M0CameraTargetProvider>(Lifetime.Singleton);
        }
    }
}
