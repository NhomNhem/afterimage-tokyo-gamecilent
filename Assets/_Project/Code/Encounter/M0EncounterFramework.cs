using System;
using GlassRefrain.Core;

namespace GlassRefrain.Encounter {
    public sealed class M0EncounterFramework {
        private readonly string encounterId;
        private EncounterLifecycleState state;
        private EncounterParticipantContext playerParticipant;
        private EncounterParticipantContext enemyParticipant;
        private EncounterReadinessBlocker[] readinessBlockers;
        private bool playerDuplicateRegistration;
        private bool enemyDuplicateRegistration;
        private bool playerDefeated;
        private bool enemyDefeated;
        private bool revealAccepted;
        private bool manualResetRequested;
        private bool manualAbortRequested;
        private string memoryId;
        private string observationReason;
        private EncounterLifecycleResult lastResult;
        private string lastReason;
        private float elapsedSeconds;
        private EncounterLifecycleSnapshot latestSnapshot;

        public M0EncounterFramework(string encounterId = "M0Encounter") {
            this.encounterId = string.IsNullOrEmpty(encounterId) ? "M0Encounter" : encounterId;
            state = EncounterLifecycleState.Uninitialized;
            playerParticipant = CreateUnregisteredParticipant(EncounterParticipantRole.Player);
            enemyParticipant = CreateUnregisteredParticipant(EncounterParticipantRole.Enemy);
            readinessBlockers = new EncounterReadinessBlocker[0];
            playerDuplicateRegistration = false;
            enemyDuplicateRegistration = false;
            playerDefeated = false;
            enemyDefeated = false;
            revealAccepted = false;
            manualResetRequested = false;
            manualAbortRequested = false;
            memoryId = string.Empty;
            observationReason = string.Empty;
            elapsedSeconds = 0f;
            lastResult = new EncounterLifecycleResult(
                EncounterLifecycleRequestKind.Prepare,
                false,
                EncounterLifecycleState.Uninitialized,
                EncounterLifecycleState.Uninitialized,
                "Encounter not prepared yet");
            lastReason = string.Empty;
            RecalculateReadinessBlockers();
            RefreshSnapshot();
        }

        public EncounterLifecycleSnapshot Snapshot {
            get { return latestSnapshot; }
        }

        public event Action<EncounterLifecycleSnapshot> SnapshotChanged;

        public void RegisterPlayer(EncounterParticipantRegistration registration) {
            if (playerParticipant.IsRegistered) {
                playerDuplicateRegistration = true;
                lastReason = "Duplicate player registration";
                RecalculateReadinessBlockers();
                RefreshSnapshot();
                return;
            }

            playerParticipant = BuildParticipant(EncounterParticipantRole.Player, registration);
            RecalculateReadinessBlockers();
            RefreshSnapshot();
        }

        public void RegisterEnemy(EncounterParticipantRegistration registration) {
            if (enemyParticipant.IsRegistered) {
                enemyDuplicateRegistration = true;
                lastReason = "Duplicate enemy registration";
                RecalculateReadinessBlockers();
                RefreshSnapshot();
                return;
            }

            enemyParticipant = BuildParticipant(EncounterParticipantRole.Enemy, registration);
            RecalculateReadinessBlockers();
            RefreshSnapshot();
        }

        public EncounterLifecycleResult Prepare(string reason) {
            var previous = state;
            state = EncounterLifecycleState.Preparing;
            lastReason = NormalizeReason(reason, "Preparing encounter");
            RecalculateReadinessBlockers();

            if (readinessBlockers.Length == 0) {
                state = EncounterLifecycleState.Ready;
                lastResult = new EncounterLifecycleResult(
                    EncounterLifecycleRequestKind.Prepare,
                    true,
                    previous,
                    state,
                    lastReason);
            }
            else {
                lastResult = new EncounterLifecycleResult(
                    EncounterLifecycleRequestKind.Prepare,
                    false,
                    previous,
                    state,
                    SummarizeBlockers());
            }

            RefreshSnapshot();
            return lastResult;
        }

        public EncounterLifecycleResult Start(string reason) {
            var previous = state;
            if (state != EncounterLifecycleState.Ready || readinessBlockers.Length > 0) {
                lastReason = NormalizeReason(reason, "Encounter not ready to start");
                lastResult = new EncounterLifecycleResult(
                    EncounterLifecycleRequestKind.Start,
                    false,
                    previous,
                    state,
                    lastReason);
                RefreshSnapshot();
                return lastResult;
            }

            state = EncounterLifecycleState.Starting;
            lastReason = NormalizeReason(reason, "Encounter starting");
            RefreshSnapshot();

            state = EncounterLifecycleState.Active;
            lastResult = new EncounterLifecycleResult(
                EncounterLifecycleRequestKind.Start,
                true,
                previous,
                state,
                lastReason);
            RefreshSnapshot();
            return lastResult;
        }

        public EncounterLifecycleResult Complete(string reason) {
            return TransitionFromObservation(
                EncounterLifecycleRequestKind.Complete,
                EncounterLifecycleState.Completing,
                EncounterLifecycleState.Completed,
                enemyDefeated,
                NormalizeReason(reason, "Enemy defeated"));
        }

        public EncounterLifecycleResult Fail(string reason) {
            return TransitionFromObservation(
                EncounterLifecycleRequestKind.Fail,
                EncounterLifecycleState.Failed,
                EncounterLifecycleState.Failed,
                playerDefeated,
                NormalizeReason(reason, "Player defeated"));
        }

        public EncounterLifecycleResult Abort(string reason) {
            var previous = state;
            if (state != EncounterLifecycleState.Active && state != EncounterLifecycleState.Starting) {
                lastReason = NormalizeReason(reason, "Encounter not active");
                lastResult = new EncounterLifecycleResult(
                    EncounterLifecycleRequestKind.Abort,
                    false,
                    previous,
                    state,
                    lastReason);
                RefreshSnapshot();
                return lastResult;
            }

            state = EncounterLifecycleState.Aborted;
            lastReason = NormalizeReason(reason, "Encounter aborted");
            lastResult = new EncounterLifecycleResult(
                EncounterLifecycleRequestKind.Abort,
                true,
                previous,
                state,
                lastReason);
            RefreshSnapshot();
            return lastResult;
        }

        public EncounterLifecycleResult Reset(string reason) {
            var previous = state;
            state = EncounterLifecycleState.Resetting;
            lastReason = NormalizeReason(reason, "Encounter resetting");
            elapsedSeconds = 0f;
            manualResetRequested = false;
            manualAbortRequested = false;
            playerDuplicateRegistration = false;
            enemyDuplicateRegistration = false;
            playerDefeated = false;
            enemyDefeated = false;
            revealAccepted = false;
            memoryId = string.Empty;
            observationReason = string.Empty;
            RecalculateReadinessBlockers();
            RefreshSnapshot();

            if (readinessBlockers.Length == 0) {
                state = EncounterLifecycleState.Ready;
            }
            else {
                state = EncounterLifecycleState.Preparing;
            }

            lastResult = new EncounterLifecycleResult(
                EncounterLifecycleRequestKind.Reset,
                true,
                previous,
                state,
                lastReason);
            RefreshSnapshot();
            return lastResult;
        }

        public void AdvanceElapsedTime(float deltaSeconds) {
            if (deltaSeconds < 0f) {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds));
            }

            elapsedSeconds += deltaSeconds;
            lastReason = "Elapsed time advanced";
            RefreshSnapshot();
        }

        public EncounterLifecycleResult ObservePlayerDefeated(string reason) {
            playerDefeated = true;
            observationReason = NormalizeReason(reason, "Player defeated observed");
            RefreshSnapshot();
            return Fail(observationReason);
        }

        public EncounterLifecycleResult ObserveEnemyDefeated(string reason) {
            enemyDefeated = true;
            observationReason = NormalizeReason(reason, "Enemy defeated observed");
            RefreshSnapshot();
            return Complete(observationReason);
        }

        public void ObserveRevealAccepted(string acceptedMemoryId, string reason) {
            revealAccepted = true;
            memoryId = acceptedMemoryId ?? string.Empty;
            observationReason = NormalizeReason(reason, "Reveal accepted observed");
            RefreshSnapshot();
        }

        public EncounterLifecycleResult ObserveManualAbort(string reason) {
            manualAbortRequested = true;
            observationReason = NormalizeReason(reason, "Manual abort requested");
            RefreshSnapshot();
            return Abort(observationReason);
        }

        public EncounterLifecycleResult ObserveManualReset(string reason) {
            manualResetRequested = true;
            observationReason = NormalizeReason(reason, "Manual reset requested");
            RefreshSnapshot();
            return Reset(observationReason);
        }

        private EncounterLifecycleResult TransitionFromObservation(
            EncounterLifecycleRequestKind kind,
            EncounterLifecycleState intermediateState,
            EncounterLifecycleState finalState,
            bool conditionMet,
            string reason) {
            var previous = state;
            if (state != EncounterLifecycleState.Active || !conditionMet) {
                lastReason = reason;
                lastResult = new EncounterLifecycleResult(kind, false, previous, state, lastReason);
                RefreshSnapshot();
                return lastResult;
            }

            state = intermediateState;
            lastReason = reason;
            RefreshSnapshot();

            state = finalState;
            lastResult = new EncounterLifecycleResult(kind, true, previous, state, lastReason);
            RefreshSnapshot();
            return lastResult;
        }

        private void RecalculateReadinessBlockers() {
            var blockers = new System.Collections.Generic.List<EncounterReadinessBlocker>();

            if (!playerParticipant.IsRegistered || string.IsNullOrEmpty(playerParticipant.ParticipantId)) {
                blockers.Add(new EncounterReadinessBlocker("missing_player", "Player participant is missing"));
            }

            if (!enemyParticipant.IsRegistered || string.IsNullOrEmpty(enemyParticipant.ParticipantId)) {
                blockers.Add(new EncounterReadinessBlocker("missing_enemy", "Enemy participant is missing"));
            }

            if (playerDuplicateRegistration) {
                blockers.Add(new EncounterReadinessBlocker("duplicate_player_registration", "Player participant was registered more than once"));
            }

            if (enemyDuplicateRegistration) {
                blockers.Add(new EncounterReadinessBlocker("duplicate_enemy_registration", "Enemy participant was registered more than once"));
            }

            if (playerParticipant.IsRegistered &&
                enemyParticipant.IsRegistered &&
                !string.IsNullOrEmpty(playerParticipant.ParticipantId) &&
                playerParticipant.ParticipantId == enemyParticipant.ParticipantId) {
                blockers.Add(new EncounterReadinessBlocker("duplicate_participant", "Player and enemy cannot share the same participant id"));
            }

            readinessBlockers = blockers.ToArray();
        }

        private EncounterParticipantContext BuildParticipant(
            EncounterParticipantRole role,
            EncounterParticipantRegistration registration) {
            var participantId = registration.ParticipantId ?? string.Empty;
            var isRegistered = !string.IsNullOrEmpty(participantId);
            var reason = isRegistered ? NormalizeReason(registration.Reason, "Registered") : "Missing participant id";
            return new EncounterParticipantContext(role, participantId, registration.SourceLabel, isRegistered, reason);
        }

        private EncounterParticipantContext CreateUnregisteredParticipant(EncounterParticipantRole role) {
            return new EncounterParticipantContext(role, string.Empty, string.Empty, false, "Not registered");
        }

        private string SummarizeBlockers() {
            if (readinessBlockers.Length == 0) {
                return string.Empty;
            }

            return readinessBlockers[0].Reason;
        }

        private string NormalizeReason(string reason, string fallback) {
            return string.IsNullOrEmpty(reason) ? fallback : reason;
        }

        private EncounterObservationContext BuildObservation() {
            return new EncounterObservationContext(
                playerDefeated,
                enemyDefeated,
                revealAccepted,
                manualResetRequested,
                manualAbortRequested,
                memoryId,
                observationReason);
        }

        private void RefreshSnapshot() {
            latestSnapshot = new EncounterLifecycleSnapshot(
                encounterId,
                state,
                playerParticipant,
                enemyParticipant,
                Array.AsReadOnly((EncounterReadinessBlocker[])readinessBlockers.Clone()),
                BuildObservation(),
                lastResult,
                lastReason,
                elapsedSeconds);

            var handler = SnapshotChanged;
            if (handler != null) {
                handler(latestSnapshot);
            }
        }
    }
}
