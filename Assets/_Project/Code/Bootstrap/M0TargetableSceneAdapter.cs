using UnityEngine;
using VContainer;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using GlassRefrain.Targeting;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace GlassRefrain.Bootstrap {
    public sealed class M0TargetableSceneAdapter : SerializedMonoBehaviour, ITargetable {
        [OdinSerialize] private string targetId = "enemy-m0-placeholder";

        private ITargetableRegistry registry;
        private INhemLogger logger;
        private bool isRegistered;

        public string TargetId => targetId;
        public bool IsTargetable => gameObject.activeInHierarchy;

        [Inject]
        public void Construct(ITargetableRegistry targetableRegistry, INhemLogger injectedLogger) {
            registry = targetableRegistry;
            logger = injectedLogger;
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
            logger?.Log($"[M0Target] SceneAdapter Construct called: targetId={targetId} active={gameObject.activeInHierarchy}");
#endif
            TryRegister("Construct");
        }

        private void Start() {
            TryRegister("Start");
        }

        private void OnEnable() {
            TryRegister("OnEnable");
        }

        private void OnDisable() {
            Unregister("OnDisable");
        }

        private void OnDestroy() {
            Unregister("OnDestroy");
            registry = null;
            logger = null;
        }

        public Axis2 GetPosition() {
            var pos = transform.position;
            return new Axis2(pos.x, pos.z);
        }

        private void TryRegister(string source) {
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
            logger?.Log($"[M0Target] SceneAdapter TryRegister: source={source} targetId={targetId} active={isActiveAndEnabled} isRegistered={isRegistered}");
#endif

            if (registry == null) {
                logger?.LogWarning("[M0Target] SceneAdapter register skipped: registry is null");
                return;
            }

            if (string.IsNullOrEmpty(targetId)) {
                logger?.LogWarning("[M0Target] SceneAdapter register skipped: targetId is empty");
                return;
            }

            if (!isActiveAndEnabled) {
                logger?.LogWarning("[M0Target] SceneAdapter register skipped: object inactive");
                return;
            }

            if (isRegistered) {
#if GR_INPUT_DEBUG || GR_M0_PROTOTYPE
                logger?.Log("[M0Target] SceneAdapter register skipped: already registered");
#endif
                return;
            }

            registry.Register(this);
            isRegistered = true;

            var currentEnemy = registry.GetCurrentDuelEnemy();
            var currentEnemyId = currentEnemy != null ? currentEnemy.TargetId : "None";
            var registeredCount = registry is M0TargetableRegistry typedRegistry
                ? typedRegistry.GetAllRegisteredTargets().Count
                : -1;
            logger?.Log($"[M0Target] SceneAdapter register success: currentDuelEnemyId={currentEnemyId} count={registeredCount}");
        }

        private void Unregister(string source) {
            if (registry == null) return;
            if (!isRegistered) return;

            registry.Unregister(this);
            isRegistered = false;

            var currentEnemy = registry.GetCurrentDuelEnemy();
            var currentEnemyId = currentEnemy != null ? currentEnemy.TargetId : "None";
            var registeredCount = registry is M0TargetableRegistry typedRegistry
                ? typedRegistry.GetAllRegisteredTargets().Count
                : -1;
            logger?.Log($"[M0Target] SceneAdapter unregister: source={source} currentDuelEnemyId={currentEnemyId} count={registeredCount}");
        }
    }
}
