using System.Reflection;
using GlassRefrain.Application;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Enemy;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using GlassRefrain.Presentation;
using NhemDangFugBixs.NhemLogging;
using VContainer;
using VContainer.Unity;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Registers and wires explicit M0 scene component references for the gameplay scope.
    /// </summary>
    public sealed class M0SceneCompositionRegistrar {
        private readonly M0GameplayTickHandler _tickHandler;
        private readonly PlayerMover _playerMover;
        private readonly M0TargetableSceneAdapter _targetableAdapter;
        private readonly M0EnemyIntentLoopDriver _loopDriver;
        private readonly M0DirectPlayerInput _playerInput;
        private readonly M0CombatVisualFeedbackAdapter _visualFeedbackAdapter;
        private readonly M0CombatDebugOverlayAdapter _debugOverlayAdapter;
        private readonly AnimationFacade _animationFacade;
        private readonly AnimancerPlayerAnimationDriver _playerAnimationDriver;
        private readonly AnimancerEnemyAnimationDriver _enemyAnimationDriver;
        private readonly MemoryRaycastProProbe _memoryProbe;
        private readonly MemoryFragment[] _memoryFragments;

        private INhemLogger _logger;

        public M0SceneCompositionRegistrar(
            M0GameplayTickHandler tickHandler,
            PlayerMover playerMover,
            M0TargetableSceneAdapter targetableAdapter,
            M0EnemyIntentLoopDriver loopDriver,
            M0DirectPlayerInput playerInput,
            M0CombatVisualFeedbackAdapter visualFeedbackAdapter,
            M0CombatDebugOverlayAdapter debugOverlayAdapter,
            AnimationFacade animationFacade,
            AnimancerPlayerAnimationDriver playerAnimationDriver,
            AnimancerEnemyAnimationDriver enemyAnimationDriver,
            MemoryRaycastProProbe memoryProbe,
            MemoryFragment[] memoryFragments) {
            _tickHandler = tickHandler;
            _playerMover = playerMover;
            _targetableAdapter = targetableAdapter;
            _loopDriver = loopDriver;
            _playerInput = playerInput;
            _visualFeedbackAdapter = visualFeedbackAdapter;
            _debugOverlayAdapter = debugOverlayAdapter;
            _animationFacade = animationFacade;
            _playerAnimationDriver = playerAnimationDriver;
            _enemyAnimationDriver = enemyAnimationDriver;
            _memoryProbe = memoryProbe;
            _memoryFragments = memoryFragments;
        }

        public void Register(IContainerBuilder builder) {
            RegisterSceneComponents(builder);
            RegisterBuildWiring(builder);
        }

        private void RegisterSceneComponents(IContainerBuilder builder) {
            builder.UseComponents(components => {
                if (_tickHandler != null) components.AddInstance(_tickHandler);
                if (_targetableAdapter != null) components.AddInstance(_targetableAdapter);
                if (_visualFeedbackAdapter != null) components.AddInstance(_visualFeedbackAdapter);
                if (_debugOverlayAdapter != null) components.AddInstance(_debugOverlayAdapter);
                if (_playerInput != null) components.AddInstance(_playerInput);
                if (_loopDriver != null) components.AddInstance(_loopDriver);

                if (_playerAnimationDriver != null)
                    components.AddInstance(_playerAnimationDriver).As<IPlayerAnimationService>();

                if (_enemyAnimationDriver != null)
                    components.AddInstance(_enemyAnimationDriver).As<IEnemyAnimationService>();
            });

            RegisterOrFindComponent<AnimationFacade>(builder, _animationFacade);
            RegisterOrFindComponent<PlayerMover>(builder, _playerMover);
        }

        private static void RegisterOrFindComponent<T>(IContainerBuilder builder, T instance) where T : UnityEngine.Component {
            if (instance != null)
                builder.RegisterComponent(instance);
            else
                builder.RegisterComponentInHierarchy<T>();
        }

        private void RegisterBuildWiring(IContainerBuilder builder) {
            builder.RegisterBuildCallback(container => {
                _logger = container.Resolve<INhemLogger>();
                _playerInput?.SetLogger(_logger);

                InjectMemorySceneParticipants(container);
                WirePresentationAdapters();
                WireEnemyLoop(container);
            });
        }

        private void WirePresentationAdapters() {
            if (_tickHandler != null) {
                _tickHandler.SetVisualFeedbackAdapter(_visualFeedbackAdapter);
                _tickHandler.SetDebugOverlayAdapter(_debugOverlayAdapter);

                RepairOdinField(_tickHandler.GetType(), _tickHandler, "animationFacade", _animationFacade);
                RepairOdinField(_tickHandler.GetType(), _tickHandler, "playerMover", _playerMover);
            }

            if (_animationFacade == null || _playerAnimationDriver == null || _enemyAnimationDriver == null) {
                _logger?.LogWarning("[M0Bootstrap] Animation presentation not assigned; animation playback disabled for this M0 smoke run.");
            }
        }

        private void WireEnemyLoop(IObjectResolver container) {
            if (_loopDriver == null) {
                _logger?.LogWarning("[GameplayLifetimeScope] loopDriver is null in build callback");
                return;
            }

            var enemyIntentModel = container.Resolve<M0EnemyIntentModel>();
            _loopDriver.Construct(enemyIntentModel, _logger);
            _playerInput?.SetEnemyDebugHarness(_loopDriver);
        }

        private static void RepairOdinField(System.Type type, object target, string fieldName, object value) {
            var field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null && field.GetValue(target) == null) {
                field.SetValue(target, value);
            }
        }

        private int InjectMemorySceneParticipants(IObjectResolver container) {
            if (_memoryProbe != null) {
                container.Inject(_memoryProbe);
            } else {
                _logger?.LogWarning("[M0Bootstrap] MemoryRaycastProProbe reference is not assigned; memory interact debug probe unavailable.");
            }

            int injectedFragmentCount = 0;
            if (_memoryFragments != null) {
                for (int i = 0; i < _memoryFragments.Length; i++) {
                    var fragment = _memoryFragments[i];
                    if (fragment == null) {
                        continue;
                    }

                    container.Inject(fragment);
                    injectedFragmentCount++;
                }
            }

            _logger?.Log("[M0Bootstrap] Memory DI injected: probe=" + (_memoryProbe != null) + " fragments=" + injectedFragmentCount);
            if (injectedFragmentCount == 0) {
                _logger?.LogWarning("[M0Bootstrap] No MemoryFragment references assigned; memory interaction fragments will not register.");
            }

            return injectedFragmentCount;
        }
    }
}
