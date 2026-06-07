using System;
using GlassRefrain.Combat;
using GlassRefrain.Locomotion;
using GlassRefrain.Memory;
using NhemDangFugBixs.NhemLogging;
using VContainer;

namespace GlassRefrain.Bootstrap {
    /// <summary>
    /// Registers config-backed M0 runtime services that need explicit factory construction.
    /// </summary>
    public sealed class M0RuntimeServiceCompositionRegistrar {
        private readonly M0CombatTimingConfig _combatTimingConfig;
        private readonly M0LocomotionConfig _locomotionConfig;
        private readonly M0MemoryRuntimeTuningConfig _memoryRuntimeTuningConfig;

        public M0RuntimeServiceCompositionRegistrar(
            M0CombatTimingConfig combatTimingConfig,
            M0LocomotionConfig locomotionConfig,
            M0MemoryRuntimeTuningConfig memoryRuntimeTuningConfig) {
            _combatTimingConfig = combatTimingConfig;
            _locomotionConfig = locomotionConfig;
            _memoryRuntimeTuningConfig = memoryRuntimeTuningConfig;
        }

        public void Register(IContainerBuilder builder) {
            M0CombatTimingSettings combatTimingSettings = CreateCombatTimingSettings();
            M0LocomotionSettings locomotionSettings = CreateLocomotionSettings();
            M0MemoryRuntimeTuningSettings memoryRuntimeTuningSettings = CreateMemoryRuntimeTuningSettings();

            builder.Register(resolver => new M0CombatCore(
                    combatTimingSettings,
                    resolver.Resolve<INhemLogger>()),
                Lifetime.Singleton)
                .As<IM0CombatCore>()
                .AsSelf();

            builder.Register(_ => new M0PlayerLocomotion(locomotionSettings), Lifetime.Singleton)
                .As<IM0PlayerLocomotion>()
                .AsSelf();

            builder.Register(_ => new M0MemoryState(memoryRuntimeTuningSettings.DefaultRevealCandidateId), Lifetime.Singleton)
                .As<IM0MemoryState>()
                .AsSelf();

            builder.Register(_ => new M0MemoryVFXResponse(
                    memoryRuntimeTuningSettings.RevealFeedbackDurationSeconds,
                    memoryRuntimeTuningSettings.RevealFeedbackCooldownSeconds,
                    memoryRuntimeTuningSettings.RevealFeedbackIntensityLabel),
                Lifetime.Singleton)
                .AsSelf();
        }

        private M0CombatTimingSettings CreateCombatTimingSettings() {
            if (_combatTimingConfig == null) {
                throw new InvalidOperationException("M0RuntimeServiceCompositionRegistrar requires an assigned M0CombatTimingConfig.");
            }

            return _combatTimingConfig.ToSettings();
        }

        private M0LocomotionSettings CreateLocomotionSettings() {
            if (_locomotionConfig == null) {
                throw new InvalidOperationException("M0RuntimeServiceCompositionRegistrar requires an assigned M0LocomotionConfig.");
            }

            return _locomotionConfig.ToSettings();
        }

        private M0MemoryRuntimeTuningSettings CreateMemoryRuntimeTuningSettings() {
            if (_memoryRuntimeTuningConfig == null) {
                throw new InvalidOperationException("M0RuntimeServiceCompositionRegistrar requires an assigned M0MemoryRuntimeTuningConfig.");
            }

            return _memoryRuntimeTuningConfig.ToSettings();
        }
    }
}
