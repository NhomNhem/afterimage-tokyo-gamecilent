#nullable enable
using System;
using GlassRefrain.Code.Shared.DI;
using NhemDangFugBixs.NhemLogging;
using GlassRefrain.Core;
using NhemDangFugBixs.Attributes;

namespace GlassRefrain.Enemy {
    /// <summary>
    /// Read/write surface of the M0 enemy intent state machine.
    /// Consumers that do not need the concrete type should depend on this interface.
    /// </summary>
    public interface IM0EnemyIntentModel {
        EnemyIntentSnapshot Snapshot { get; }
        event Action<EnemyIntentSnapshot> SnapshotChanged;

        void EnterIdle(string reason);
        void EnterTelegraph(string telegraphId, float durationSeconds, string reason);
        void EnterCommit(EnemyAttackIntentContext intent, float durationSeconds, string reason);
        void EnterActive(float durationSeconds, string reason);
        void EnterRecovery(
            float durationSeconds,
            string reason,
            bool openPunishWindow,
            float punishWindowSeconds,
            string punishSource);
        void ClosePunishWindow(string reason);
        void Tick(float deltaSeconds);
        void ResetForEncounter(string reason);
    }

    [AutoRegisterIn<IGameplayLifetimeScope>(Lifetime = NhemLifetime.Singleton)]
    [AsSelf]
    public sealed class M0EnemyIntentModel : IM0EnemyIntentModel {
        private readonly string enemyId;
        private readonly INhemLogger? logger;

        private EnemyIntentState currentState;
        private string intentLabel;
        private float remainingSeconds;
        private float phaseDurationSeconds;

        private TelegraphStateSnapshot telegraph;
        private EnemyAttackIntentContext attackIntent;
        private EnemyPunishWindowContext punishWindow;
        private EnemyIntentSnapshot latestSnapshot;
        public event Action<EnemyIntentSnapshot>? SnapshotChanged;

        public M0EnemyIntentModel(INhemLogger? logger = null) {
            this.enemyId = "M0Enemy";
            this.logger = logger;

            currentState = EnemyIntentState.Idle;
            intentLabel = "Idle";
            remainingSeconds = 0f;
            phaseDurationSeconds = 0f;

            telegraph = new TelegraphStateSnapshot(string.Empty, false, 0f);

            attackIntent = new EnemyAttackIntentContext(
                string.Empty,
                string.Empty,
                0f,
                new EnemyAttackTagSet(Array.Empty<string>())
            );

            punishWindow = new EnemyPunishWindowContext(false, 0f, string.Empty);

            RefreshSnapshot();
        }

        public EnemyIntentSnapshot Snapshot => latestSnapshot;

        public void EnterIdle(string reason) {
            var previousState = currentState;
            currentState = EnemyIntentState.Idle;
            intentLabel = string.IsNullOrEmpty(reason) ? "Idle" : reason;
            remainingSeconds = 0f;
            phaseDurationSeconds = 0f;

#if GR_M0_PROTOTYPE
            if (previousState != EnemyIntentState.Idle) {
                logger?.Log($"[M0Enemy] State changed: {previousState} -> Idle");
            }
#endif

            telegraph = new TelegraphStateSnapshot(
                telegraph.TelegraphId,
                false,
                0f
            );

            attackIntent = CreateEmptyAttackIntent();

            ClosePunishWindow("Idle");

            RefreshSnapshot();
        }

        public void EnterTelegraph(string telegraphId, float durationSeconds, string reason) {
            durationSeconds = ClampDuration(durationSeconds);

            currentState = EnemyIntentState.Telegraph;
            intentLabel = string.IsNullOrEmpty(reason) ? "Telegraph" : reason;
            remainingSeconds = durationSeconds;
            phaseDurationSeconds = durationSeconds;

#if GR_M0_PROTOTYPE
            logger?.Log($"[M0Enemy] State changed: Idle -> Telegraph duration={durationSeconds}");
#endif

            telegraph = new TelegraphStateSnapshot(
                telegraphId ?? string.Empty,
                durationSeconds > 0f,
                durationSeconds
            );

            attackIntent = CreateEmptyAttackIntent();

            ClosePunishWindow("Telegraph");

            RefreshSnapshot();
        }

        public void EnterCommit(EnemyAttackIntentContext intent, float durationSeconds, string reason) {
            durationSeconds = ClampDuration(durationSeconds);

            currentState = EnemyIntentState.Commit;
            intentLabel = string.IsNullOrEmpty(reason) ? "Commit" : reason;
            remainingSeconds = durationSeconds;
            phaseDurationSeconds = durationSeconds;

#if GR_M0_PROTOTYPE
            var tags = intent.AttackTags.Tags;
            var tagsStr = tags != null && tags.Length > 0 ? string.Join(",", tags) : "none";
            logger?.Log($"[M0Enemy] State changed: Telegraph -> Commit duration={durationSeconds} tags={tagsStr}");
#endif

            telegraph = new TelegraphStateSnapshot(
                telegraph.TelegraphId,
                false,
                0f
            );

            attackIntent = intent;

            ClosePunishWindow("Commit");

            RefreshSnapshot();
        }

        public void EnterActive(float durationSeconds, string reason) {
            durationSeconds = ClampDuration(durationSeconds);

            currentState = EnemyIntentState.Active;
            intentLabel = string.IsNullOrEmpty(reason) ? "Active" : reason;
            remainingSeconds = durationSeconds;
            phaseDurationSeconds = durationSeconds;

#if GR_M0_PROTOTYPE
            var tags = attackIntent.AttackTags.Tags;
            var tagsStr = tags != null && tags.Length > 0 ? string.Join(",", tags) : "none";
            var isParryEligible = tags != null && System.Array.IndexOf(tags, "ParryEligible") >= 0;
            logger?.Log($"[M0Enemy] State changed: Commit -> Active duration={durationSeconds} tags={tagsStr} ParryEligible={isParryEligible}");
#endif

            telegraph = new TelegraphStateSnapshot(
                telegraph.TelegraphId,
                false,
                0f
            );

            ClosePunishWindow("Active");

            RefreshSnapshot();
        }

        public void EnterRecovery(
            float durationSeconds,
            string reason,
            bool openPunishWindow,
            float punishWindowSeconds,
            string punishSource
        ) {
            durationSeconds = ClampDuration(durationSeconds);
            punishWindowSeconds = ClampDuration(punishWindowSeconds);

            currentState = EnemyIntentState.Recovery;
            intentLabel = string.IsNullOrEmpty(reason) ? "Recovery" : reason;
            remainingSeconds = durationSeconds;
            phaseDurationSeconds = durationSeconds;

#if GR_M0_PROTOTYPE
            logger?.Log($"[M0Enemy] State changed: Active -> Recovery duration={durationSeconds}");
#endif

            telegraph = new TelegraphStateSnapshot(
                telegraph.TelegraphId,
                false,
                0f
            );

            if (openPunishWindow && punishWindowSeconds > 0f) {
                punishWindow = new EnemyPunishWindowContext(
                    true,
                    punishWindowSeconds,
                    punishSource ?? "Recovery"
                );
            }
            else {
                ClosePunishWindow("Recovery");
            }

            RefreshSnapshot();
        }

        public void ClosePunishWindow(string reason) {
            var source = string.IsNullOrEmpty(reason)
                ? punishWindow.Source
                : reason;

            punishWindow = new EnemyPunishWindowContext(
                false,
                0f,
                source ?? string.Empty
            );
        }

        public void Tick(float deltaSeconds) {
            if (deltaSeconds <= 0f)
                return;

            remainingSeconds = ClampDuration(remainingSeconds - deltaSeconds);

            TickTelegraph(deltaSeconds);
            TickPunishWindow(deltaSeconds);

            RefreshSnapshot();
        }

        public void ResetForEncounter(string reason) {
            EnterIdle(string.IsNullOrEmpty(reason) ? "EncounterReset" : reason);
        }

        private void TickTelegraph(float deltaSeconds) {
            if (!telegraph.IsActive)
                return;

            var nextRemaining = ClampDuration(telegraph.RemainingSeconds - deltaSeconds);

            telegraph = new TelegraphStateSnapshot(
                telegraph.TelegraphId,
                nextRemaining > 0f,
                nextRemaining
            );
        }

        private void TickPunishWindow(float deltaSeconds) {
            if (!punishWindow.IsOpen)
                return;

            var nextRemaining = ClampDuration(punishWindow.RemainingSeconds - deltaSeconds);

            punishWindow = new EnemyPunishWindowContext(
                nextRemaining > 0f,
                nextRemaining,
                punishWindow.Source
            );
        }

        private void RefreshSnapshot() {
            latestSnapshot = new EnemyIntentSnapshot(
                currentState,
                enemyId,
                intentLabel,
                currentState == EnemyIntentState.Telegraph,
                remainingSeconds,
                telegraph,
                attackIntent,
                punishWindow,
                EnemyTelegraphReadabilitySnapshot.FromIntentState(
                    currentState,
                    ResolvePhaseLabel(),
                    remainingSeconds,
                    ResolvePhaseProgress01(),
                    attackIntent.AttackTags,
                    punishWindow,
                    ResolveReadabilityReason())
            );

            OnSnapshotChanged(latestSnapshot);
        }

        private void OnSnapshotChanged(EnemyIntentSnapshot snapshot) {
            SnapshotChanged?.Invoke(snapshot);
        }

        private static float ClampDuration(float seconds) {
            return seconds > 0f ? seconds : 0f;
        }

        private string ResolvePhaseLabel() {
            if (!string.IsNullOrEmpty(intentLabel)) {
                return intentLabel;
            }

            return currentState.ToString();
        }

        private string ResolveReadabilityReason() {
            if (!string.IsNullOrEmpty(intentLabel)) {
                return intentLabel;
            }

            if (!string.IsNullOrEmpty(punishWindow.Source)) {
                return punishWindow.Source;
            }

            return telegraph.TelegraphId;
        }

        private float ResolvePhaseProgress01() {
            if (phaseDurationSeconds <= 0f) {
                return remainingSeconds <= 0f ? 1f : 0f;
            }

            var elapsed = phaseDurationSeconds - remainingSeconds;
            var progress = elapsed / phaseDurationSeconds;
            if (progress < 0f) {
                return 0f;
            }

            return progress > 1f ? 1f : progress;
        }

        private static EnemyAttackIntentContext CreateEmptyAttackIntent() {
            return new EnemyAttackIntentContext(
                string.Empty,
                string.Empty,
                0f,
                new EnemyAttackTagSet(Array.Empty<string>())
            );
        }
    }
}
