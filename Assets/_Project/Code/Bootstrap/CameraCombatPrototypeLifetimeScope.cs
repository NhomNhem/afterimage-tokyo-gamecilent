using UnityEngine;
using VContainer;
using VContainer.Unity;
using GlassRefrain.Camera;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Manual VContainer composition root for M0 Camera scene.
    /// Registers M0CombatCameraService and wires M0CombatCameraAdapter.
    /// </summary>
    public sealed class CameraCombatPrototypeLifetimeScope : LifetimeScope {
        [SerializeField] private M0CombatCameraAdapter cameraAdapter = null!;

        [Header("Camera Settings")]
        [SerializeField] private float distance = 6f;
        [SerializeField] private float minDistance = 2f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float pivotHeight = 1.5f;
        [SerializeField] private float yawSensitivity = 0.15f;
        [SerializeField] private float pitchSensitivity = 0.1f;
        [SerializeField] private float minPitch = -10f;
        [SerializeField] private float maxPitch = 45f;
        [SerializeField] private float positionSmoothTime = 0.15f;
        [SerializeField] private float rotationSmoothTime = 0.1f;
        [SerializeField] private float collisionRadius = 0.3f;
        [SerializeField] private float collisionSafetyMargin = 0.5f;
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float baseFOV = 60f;
        [SerializeField] private float maxFOVExpansion = 5f;
        [SerializeField] private float fovSmoothTime = 0.3f;
        [SerializeField] private float shakeDecay = 5f;
        [SerializeField] private float shakeFrequency = 15f;
        [SerializeField] private float lockOnDistance = 7f;
        [SerializeField] private float lockOnHeight = 3.5f;

        protected override void Configure(IContainerBuilder builder) {
            var settings = new M0CombatCameraSettings(
                distance, minDistance, height, pivotHeight,
                yawSensitivity, pitchSensitivity, minPitch, maxPitch,
                positionSmoothTime, rotationSmoothTime,
                collisionRadius, collisionSafetyMargin, collisionMask,
                baseFOV, maxFOVExpansion, fovSmoothTime,
                shakeDecay, shakeFrequency,
                lockOnDistance, lockOnHeight);

            builder.Register(_ => new M0CombatCameraService(settings), Lifetime.Singleton)
                .As<IM0CombatCameraService>()
                .AsSelf();

            builder.UseComponents(components => {
                components.AddInstance(cameraAdapter);
            });
        }
    }
}
