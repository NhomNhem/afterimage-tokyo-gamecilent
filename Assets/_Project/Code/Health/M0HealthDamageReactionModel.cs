using System;
using GlassRefrain.Code.Shared.DI;
using GlassRefrain.Core;
using NhemDangFugBixs.Attributes;

namespace GlassRefrain.Health {
    [AutoRegisterIn<IGameplayLifetimeScope>, As<IM0HealthDamageReactionModel>, AsSelf]
    public sealed class M0HealthDamageReactionModel : IM0HealthDamageReactionModel {
        private readonly float maxHealth;
        private float currentHealth;
        private HealthState state;
        private DamageApplicationResult lastDamageResult;
        private HitReactionContext hitReaction;
        private DefeatStateContext defeat;
        private HealthStateSnapshot latestSnapshot;

        public M0HealthDamageReactionModel(float maxHealth = 100f) {
            this.maxHealth = maxHealth > 0f ? maxHealth : 100f;
            currentHealth = this.maxHealth;
            state = HealthState.Living;
            lastDamageResult = new DamageApplicationResult(DamageApplicationResultType.Ignored, "No damage processed yet", 0f);
            hitReaction = new HitReactionContext(string.Empty, string.Empty, 0f);
            defeat = new DefeatStateContext(false, string.Empty);
            RefreshSnapshot();
        }

        public HealthStateSnapshot Snapshot => latestSnapshot;

        public event Action<HealthStateSnapshot> SnapshotChanged;

        public DamageApplicationResult ApplyDamage(DamageApplicationContext request) {
            if (state == HealthState.Disabled || defeat.IsDefeated) {
                lastDamageResult = new DamageApplicationResult(DamageApplicationResultType.Ignored,
                    "Target is already disabled", 0f);
                RefreshSnapshot();
                return lastDamageResult;
            }

            if (!IsResolvedCombatOutcome(request)) {
                lastDamageResult = new DamageApplicationResult(DamageApplicationResultType.Rejected,
                    "Damage rejected: unresolved or invalid combat outcome", 0f);
                RefreshSnapshot();
                return lastDamageResult;
            }

            if (request.Amount <= 0f) {
                lastDamageResult = new DamageApplicationResult(DamageApplicationResultType.Rejected,
                    "Damage amount must be greater than zero", 0f);
                RefreshSnapshot();
                return lastDamageResult;
            }

            var applied = request.Amount;
            currentHealth -= applied;
            if (currentHealth < 0f) currentHealth = 0f;

            state = currentHealth <= 0f ? HealthState.Disabled : HealthState.Damaged;
            hitReaction = new HitReactionContext(request.SourceId, "HitReactPlaceholder", 0.2f);
            defeat = currentHealth <= 0f
                ? new DefeatStateContext(true, "Health reached zero")
                : new DefeatStateContext(false, string.Empty);
            lastDamageResult =
                new DamageApplicationResult(DamageApplicationResultType.Accepted, "Damage applied", applied);
            RefreshSnapshot();
            return lastDamageResult;
        }

        public void EnterRecovery(string reason, float suppressionSeconds) {
            if (state == HealthState.Disabled) return;

            state = HealthState.Recovering;
            hitReaction = new HitReactionContext(hitReaction.SourceId,
                string.IsNullOrEmpty(reason) ? "Recovering" : reason, suppressionSeconds);
            RefreshSnapshot();
        }

        public void EnterLiving(string reason) {
            if (state == HealthState.Disabled) return;

            state = HealthState.Living;
            hitReaction = new HitReactionContext(hitReaction.SourceId, string.IsNullOrEmpty(reason) ? "Living" : reason,
                0f);
            RefreshSnapshot();
        }

        private void RefreshSnapshot() {
            latestSnapshot = new HealthStateSnapshot(
                state,
                currentHealth,
                maxHealth,
                !defeat.IsDefeated,
                lastDamageResult,
                hitReaction,
                defeat);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }

        private static bool IsResolvedCombatOutcome(DamageApplicationContext request) {
            if (string.IsNullOrWhiteSpace(request.SourceId)) return false;
            if (string.IsNullOrWhiteSpace(request.ContextLabel)) return false;

            var label = request.ContextLabel.Trim();
            if (label.Equals("Rejected", StringComparison.OrdinalIgnoreCase)) return false;
            if (label.Equals("Invalid", StringComparison.OrdinalIgnoreCase)) return false;
            if (label.Equals("Blocked", StringComparison.OrdinalIgnoreCase)) return false;
            if (label.IndexOf("Rejected", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (label.IndexOf("Invalid", StringComparison.OrdinalIgnoreCase) >= 0) return false;
            if (label.IndexOf("Blocked", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            return true;
        }
    }
}
