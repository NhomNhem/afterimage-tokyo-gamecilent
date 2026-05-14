using GlassRefrain.Core;

namespace GlassRefrain.DebugOverlay {
    public sealed class M0DebugOverlaySnapshotAggregator {
        private readonly bool[] channelVisibility;

        public M0DebugOverlaySnapshotAggregator(bool defaultVisible = true) {
            channelVisibility = new bool[9];
            for (var i = 0; i < channelVisibility.Length; i++) {
                channelVisibility[i] = defaultVisible;
            }
        }

        public bool IsChannelVisible(DebugOverlayChannelId channelId) {
            return channelVisibility[(int)channelId];
        }

        public void SetChannelVisible(DebugOverlayChannelId channelId, bool visible) {
            channelVisibility[(int)channelId] = visible;
        }

        public bool ToggleChannelVisibility(DebugOverlayChannelId channelId) {
            var nextVisible = !IsChannelVisible(channelId);
            SetChannelVisible(channelId, nextVisible);
            return nextVisible;
        }

        public DebugOverlayAggregateSnapshot Capture(
            InputIntentSnapshot input,
            InputRoutingResult? lastInputRouting,
            LocomotionStateSnapshot locomotion,
            TargetContextSnapshot targetContext,
            M0CombatSnapshot combatCore,
            EnemyIntentSnapshot enemyIntent,
            HealthStateSnapshot health,
            MemoryStateSnapshot memoryState,
            IMemoryVFXResponseSnapshot memoryVfxResponse,
            EncounterLifecycleSnapshot encounterFramework) {
            return new DebugOverlayAggregateSnapshot(
                CreateChannel(DebugOverlayChannelId.Input, "Input", input, ResolveInputReason(lastInputRouting)),
                CreateChannel(DebugOverlayChannelId.Locomotion, "Locomotion", locomotion, locomotion.StateDetail),
                CreateChannel(DebugOverlayChannelId.TargetContext, "Target Context", targetContext, ResolveTargetReason(targetContext)),
                CreateChannel(DebugOverlayChannelId.CombatCore, "Combat Core", combatCore, ResolveCombatReason(combatCore)),
                CreateChannel(DebugOverlayChannelId.EnemyIntent, "Enemy Intent / Telegraph", enemyIntent, ResolveEnemyReason(enemyIntent)),
                CreateChannel(DebugOverlayChannelId.Health, "Health / Damage / Hit Reaction", health, ResolveHealthReason(health)),
                CreateChannel(DebugOverlayChannelId.MemoryState, "Memory State", memoryState, memoryState.LastResult.Reason),
                CreateChannel(DebugOverlayChannelId.MemoryVFXResponse, "Memory VFX Response", memoryVfxResponse, memoryVfxResponse.RejectionReason),
                CreateChannel(DebugOverlayChannelId.EncounterFramework, "Encounter Framework", encounterFramework, encounterFramework.LastReason));
        }

        private DebugOverlayChannelSnapshot<TSnapshot> CreateChannel<TSnapshot>(
            DebugOverlayChannelId channelId,
            string channelLabel,
            TSnapshot snapshot,
            string lastReason) {
            return new DebugOverlayChannelSnapshot<TSnapshot>(
                channelId,
                channelLabel,
                IsChannelVisible(channelId),
                snapshot,
                lastReason);
        }

        private static string ResolveInputReason(InputRoutingResult? routingResult) {
            return routingResult.HasValue ? routingResult.Value.Reason : string.Empty;
        }

        private static string ResolveTargetReason(TargetContextSnapshot targetContext) {
            if (!string.IsNullOrEmpty(targetContext.InvalidReason)) {
                return targetContext.InvalidReason;
            }

            if (!string.IsNullOrEmpty(targetContext.ReleaseReason)) {
                return targetContext.ReleaseReason;
            }

            return targetContext.AcquireReason;
        }

        private static string ResolveCombatReason(M0CombatSnapshot combatCore) {
            if (!string.IsNullOrEmpty(combatCore.LastActionResult.Reason)) {
                return combatCore.LastActionResult.Reason;
            }

            return combatCore.LastResolutionResult.Detail;
        }

        private static string ResolveEnemyReason(EnemyIntentSnapshot enemyIntent) {
            if (!string.IsNullOrEmpty(enemyIntent.IntentLabel)) {
                return enemyIntent.IntentLabel;
            }

            if (!string.IsNullOrEmpty(enemyIntent.PunishWindow.Source)) {
                return enemyIntent.PunishWindow.Source;
            }

            return enemyIntent.Telegraph.TelegraphId;
        }

        private static string ResolveHealthReason(HealthStateSnapshot health) {
            if (!string.IsNullOrEmpty(health.LastDamageResult.Reason)) {
                return health.LastDamageResult.Reason;
            }

            return health.Defeat.Reason;
        }
    }
}
