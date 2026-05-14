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

        public M0EnemyIntentModel(string enemyId = "M0Enemy") {
            this.enemyId = enemyId ?? string.Empty;
            currentState = EnemyIntentState.Idle;
            intentLabel = "Idle";
            remainingSeconds = 0f;
            telegraph = new TelegraphStateSnapshot(string.Empty, false, 0f);
            attackIntent = new EnemyAttackIntentContext(string.Empty, string.Empty, 0f,
                new EnemyAttackTagSet(Array.Empty<string>()));
            punishWindow = new EnemyPunishWindowContext(false, 0f, string.Empty);
            RefreshSnapshot();
        }

        public EnemyIntentSnapshot Snapshot => latestSnapshot;

        public event Action<EnemyIntentSnapshot> SnapshotChanged;

        public void EnterIdle(string reason) {
            currentState = EnemyIntentState.Idle;
            intentLabel = string.IsNullOrEmpty(reason) ? "Idle" : reason;
            remainingSeconds = 0f;
            telegraph = new TelegraphStateSnapshot(telegraph.TelegraphId, false, 0f);
            attackIntent = new EnemyAttackIntentContext(string.Empty, string.Empty, 0f,
                new EnemyAttackTagSet(Array.Empty<string>()));
            ClosePunishWindow("Idle");
            RefreshSnapshot();
        }

        public void EnterTelegraph(string telegraphId, float durationSeconds, string reason) {
            currentState = EnemyIntentState.Telegraph;
            intentLabel = string.IsNullOrEmpty(reason) ? "Telegraph" : reason;
            remainingSeconds = durationSeconds;
            telegraph = new TelegraphStateSnapshot(telegraphId ?? string.Empty, true, durationSeconds);
            attackIntent = new EnemyAttackIntentContext(string.Empty, string.Empty, 0f,
                new EnemyAttackTagSet(Array.Empty<string>()));
            ClosePunishWindow("Telegraph");
            RefreshSnapshot();
        }

        public void EnterCommit(EnemyAttackIntentContext intent, float durationSeconds, string reason) {
            currentState = EnemyIntentState.Commit;
            intentLabel = string.IsNullOrEmpty(reason) ? "Commit" : reason;
            remainingSeconds = durationSeconds;
            telegraph = new TelegraphStateSnapshot(telegraph.TelegraphId, false, 0f);
            attackIntent = intent;
            ClosePunishWindow("Commit");
            RefreshSnapshot();
        }

        public void EnterActive(float durationSeconds, string reason) {
            currentState = EnemyIntentState.Active;
            intentLabel = string.IsNullOrEmpty(reason) ? "Active" : reason;
            remainingSeconds = durationSeconds;
            telegraph = new TelegraphStateSnapshot(telegraph.TelegraphId, false, 0f);
            ClosePunishWindow("Active");
            RefreshSnapshot();
        }

        public void EnterRecovery(float durationSeconds, string reason, bool openPunishWindow,
            float punishWindowSeconds, string punishSource) {
            currentState = EnemyIntentState.Recovery;
            intentLabel = string.IsNullOrEmpty(reason) ? "Recovery" : reason;
            remainingSeconds = durationSeconds;
            telegraph = new TelegraphStateSnapshot(telegraph.TelegraphId, false, 0f);

            if (openPunishWindow)
                punishWindow = new EnemyPunishWindowContext(true, punishWindowSeconds, punishSource ?? "Recovery");
            else
                ClosePunishWindow("Recovery");

            RefreshSnapshot();
        }

        public void ClosePunishWindow(string reason) {
            var source = string.IsNullOrEmpty(reason) ? punishWindow.Source : reason;
            punishWindow = new EnemyPunishWindowContext(false, 0f, source ?? string.Empty);
        }

        public void Tick(float deltaSeconds) {
            var nextRemaining = remainingSeconds - deltaSeconds;
            remainingSeconds = nextRemaining > 0f ? nextRemaining : 0f;

            if (telegraph.IsActive) {
                var telegraphRemaining = telegraph.RemainingSeconds - deltaSeconds;
                telegraph = new TelegraphStateSnapshot(
                    telegraph.TelegraphId,
                    telegraphRemaining > 0f,
                    telegraphRemaining > 0f ? telegraphRemaining : 0f);
            }

            if (punishWindow.IsOpen) {
                var punishRemaining = punishWindow.RemainingSeconds - deltaSeconds;
                punishWindow = new EnemyPunishWindowContext(
                    punishRemaining > 0f,
                    punishRemaining > 0f ? punishRemaining : 0f,
                    punishWindow.Source);
            }

            RefreshSnapshot();
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
                punishWindow);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }
    }
}