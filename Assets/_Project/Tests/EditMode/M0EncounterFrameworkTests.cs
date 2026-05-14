using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Encounter;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    [TestFixture]
    public sealed class M0EncounterFrameworkTests {
        [Test]
        public void EncounterStartsUninitializedAndBecomesReadyAfterRegistration() {
            var encounter = new M0EncounterFramework("M0Encounter");

            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Uninitialized));

            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerA", "TestHarness", "Player registered"));
            encounter.RegisterEnemy(new EncounterParticipantRegistration("EnemyA", "TestHarness", "Enemy registered"));

            var result = encounter.Prepare("Prepare duel");

            Assert.That(result.Accepted, Is.True);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Ready));
            Assert.That(encounter.Snapshot.ParticipantCount, Is.EqualTo(2));
            Assert.That(encounter.Snapshot.ReadinessBlockers.Count, Is.EqualTo(0));
        }

        [Test]
        public void MissingParticipantBlocksReadiness() {
            var encounter = new M0EncounterFramework("M0Encounter");

            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerA", "TestHarness", "Player registered"));
            var result = encounter.Prepare("Prepare duel");

            Assert.That(result.Accepted, Is.False);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Preparing));
            Assert.That(encounter.Snapshot.ReadinessBlockers.Count, Is.GreaterThan(0));
        }

        [Test]
        public void DuplicateRegistrationBlocksReadiness() {
            var encounter = new M0EncounterFramework("M0Encounter");

            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerA", "TestHarness", "Player registered"));
            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerB", "TestHarness", "Duplicate player registered"));
            encounter.RegisterEnemy(new EncounterParticipantRegistration("EnemyA", "TestHarness", "Enemy registered"));

            var result = encounter.Prepare("Prepare duel");

            Assert.That(result.Accepted, Is.False);
            Assert.That(encounter.Snapshot.ReadinessBlockers.Count, Is.GreaterThan(0));
        }

        [Test]
        public void StartMovesReadyEncounterToActiveAndObservationCanCompleteOrFailIt() {
            var encounter = CreateReadyEncounter();

            var start = encounter.Start("Start duel");
            Assert.That(start.Accepted, Is.True);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Active));

            encounter.ObserveRevealAccepted("M0RevealCandidate", "Reveal accepted");
            Assert.That(encounter.Snapshot.Observation.RevealAccepted, Is.True);

            var complete = encounter.ObserveEnemyDefeated("Enemy defeated");
            Assert.That(complete.Accepted, Is.True);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Completed));
        }

        [Test]
        public void PlayerDefeatAndAbortAndResetAreExplicit() {
            var encounter = CreateReadyEncounter();
            encounter.Start("Start duel");

            var fail = encounter.ObservePlayerDefeated("Player defeated");
            Assert.That(fail.Accepted, Is.True);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Failed));

            var abort = encounter.ObserveManualAbort("Manual abort");
            Assert.That(abort.Accepted, Is.False);

            var reset = encounter.Reset("Reset duel");
            Assert.That(reset.Accepted, Is.True);
            Assert.That(encounter.Snapshot.State, Is.EqualTo(EncounterLifecycleState.Ready));
        }

        [Test]
        public void SnapshotIsReadOnlyAndReadable() {
            var encounter = CreateReadyEncounter();
            encounter.Start("Start duel");
            encounter.ObserveRevealAccepted("M0RevealCandidate", "Reveal accepted");

            var snapshot = encounter.Snapshot;

            Assert.That(snapshot.IsActive, Is.True);
            Assert.That(snapshot.Observation.RevealAccepted, Is.True);
            Assert.That(snapshot.LastReason, Is.Not.Empty);
            Assert.That(snapshot.PlayerParticipant.IsRegistered, Is.True);
            Assert.That(snapshot.EnemyParticipant.IsRegistered, Is.True);
        }

        [Test]
        public void ElapsedTimeIsTrackedInSnapshotAndResetsWithEncounter() {
            var encounter = CreateReadyEncounter();

            encounter.AdvanceElapsedTime(1.5f);
            encounter.Start("Start duel");
            encounter.AdvanceElapsedTime(0.5f);

            Assert.That(encounter.Snapshot.ElapsedSeconds, Is.EqualTo(2.0f).Within(0.0001f));

            encounter.Reset("Reset duel");

            Assert.That(encounter.Snapshot.ElapsedSeconds, Is.EqualTo(0f));
        }

        [Test]
        public void SourceFileStaysPureAndDoesNotReferenceOutOfScopeSystems() {
            string source = File.ReadAllText("Assets/_Project/Code/Encounter/M0EncounterFramework.cs");

            Assert.That(source.Contains("UnityEngine"), Is.False);
            Assert.That(source.Contains("SceneManager"), Is.False);
            Assert.That(source.Contains("Instantiate("), Is.False);
            Assert.That(source.Contains("GetComponent<"), Is.False);
            Assert.That(source.Contains("RegisterGeneratedFor<"), Is.False);
            Assert.That(source.Contains("NhemDangFugBixs"), Is.False);
        }

        private static M0EncounterFramework CreateReadyEncounter() {
            var encounter = new M0EncounterFramework("M0Encounter");
            encounter.RegisterPlayer(new EncounterParticipantRegistration("PlayerA", "TestHarness", "Player registered"));
            encounter.RegisterEnemy(new EncounterParticipantRegistration("EnemyA", "TestHarness", "Enemy registered"));
            encounter.Prepare("Prepare duel");
            return encounter;
        }
    }
}
