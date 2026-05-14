using System;
using GlassRefrain.Core;

namespace GlassRefrain.Enemy {
    public sealed class M0EnemyIntentModel {
        private readonly string enemyId;

        private EnemyIntentState currentState;
        private string intentLabel;
        private float remainingSeconds;

        private TelegraphStateSnapshot telegraph;
        private EnemyAttackIntentContext attackIntent;
        private EnemyPunishWindowContext punishWindow;
        private EnemyIntentSnapshot latestSnapshot;
        public event Action<EnemyIntentSnapshot> SnapshotChanged;

        public M0EnemyIntentModel(string enemyId = "M0Enemy") {
            this.enemyId = enemyId ?? string.Empty;

            currentState = EnemyIntentState.Idle;
            intentLabel = "Idle";
            remainingSeconds = 0f;

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
            currentState = EnemyIntentState.Idle;
            intentLabel = string.IsNullOrEmpty(reason) ? "Idle" : reason;
            remainingSeconds = 0f;

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
                punishWindow
            );

            OnSnapshotChanged(latestSnapshot);
        }

        private void OnSnapshotChanged(EnemyIntentSnapshot snapshot) {
            SnapshotChanged?.Invoke(snapshot);
        }

        private static float ClampDuration(float seconds) {
            return seconds > 0f ? seconds : 0f;
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