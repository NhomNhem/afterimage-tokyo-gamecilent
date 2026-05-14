using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Memory;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0MemoryVFXResponseTests {
        [Test]
        public void AcceptedRevealStartsResponseLifecycle() {
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.1f, 0f, "subtle");

            response.OnAcceptedReveal(CreateAcceptedContext("M0RevealCandidate"));

            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Requested));
            Assert.That(response.Snapshot.State, Is.EqualTo(MemoryVFXResponseState.Requested));
            Assert.That(response.Snapshot.SourceAcceptedContext, Is.Not.Null);
            Assert.That(response.Snapshot.IntensityLabel, Is.EqualTo("subtle"));

            response.OnPlaybackStarted();
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Playing));
        }

        [Test]
        public void RejectedAndIgnoredRequestsDoNotPlay() {
            M0MemoryVFXResponse rejectedResponse = new M0MemoryVFXResponse();
            rejectedResponse.OnRejectRequest(MemoryVFXResponseReasons.GenericHit);
            rejectedResponse.OnPlaybackStarted();
            rejectedResponse.Update(1f);

            Assert.That(rejectedResponse.State, Is.EqualTo(MemoryVFXResponseState.Rejected));
            Assert.That(rejectedResponse.Snapshot.RejectionReason, Is.EqualTo(MemoryVFXResponseReasons.GenericHit));
            Assert.That(rejectedResponse.Snapshot.SourceAcceptedContext, Is.Null);

            M0MemoryVFXResponse ignoredResponse = new M0MemoryVFXResponse();
            ignoredResponse.OnIgnoreRequest(MemoryVFXResponseReasons.PresentationOnly);
            ignoredResponse.OnPlaybackStarted();
            ignoredResponse.Update(1f);

            Assert.That(ignoredResponse.State, Is.EqualTo(MemoryVFXResponseState.Ignored));
            Assert.That(ignoredResponse.Snapshot.RejectionReason, Is.EqualTo(MemoryVFXResponseReasons.PresentationOnly));
            Assert.That(ignoredResponse.Snapshot.SourceAcceptedContext, Is.Null);
        }

        [Test]
        public void CooldownBlocksImmediateReplayAndEventuallyReturnsToIdle() {
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.1f, 0.25f);

            response.OnAcceptedReveal(CreateAcceptedContext("M0RevealCandidate"));
            response.OnPlaybackStarted();
            response.Update(0.1f);

            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.CoolingDown));
            Assert.That(response.Snapshot.CooldownProgress, Is.EqualTo(0f).Within(0.001f));

            response.OnAcceptedReveal(CreateAcceptedContext("M0RevealCandidate"));
            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.CoolingDown));
            Assert.That(response.Snapshot.RejectionReason, Is.EqualTo(MemoryVFXResponseReasons.InCooldown));

            response.OnReset();
            response.OnAcceptedReveal(CreateAcceptedContext("M0RevealCandidate"));
            response.OnPlaybackStarted();
            response.Update(0.25f);

            Assert.That(response.State, Is.EqualTo(MemoryVFXResponseState.Idle));
            Assert.That(response.Snapshot.CooldownProgress, Is.EqualTo(0f));
        }

        [Test]
        public void SnapshotIsReadableAndIndependent() {
            M0MemoryVFXResponse response = new M0MemoryVFXResponse(0.2f, 0.3f);
            response.OnAcceptedReveal(CreateAcceptedContext("M0RevealCandidate"));

            IMemoryVFXResponseSnapshot requestedSnapshot = response.GetSnapshot();
            response.OnPlaybackStarted();
            response.Update(0.1f);
            IMemoryVFXResponseSnapshot playingSnapshot = response.GetSnapshot();

            Assert.That(requestedSnapshot.State, Is.EqualTo(MemoryVFXResponseState.Requested));
            Assert.That(requestedSnapshot.SourceAcceptedContext, Is.Not.Null);
            Assert.That(playingSnapshot.State, Is.EqualTo(MemoryVFXResponseState.Playing));
            Assert.That(playingSnapshot.RejectionReason, Is.EqualTo(string.Empty));
            Assert.That(playingSnapshot.CooldownProgress, Is.EqualTo(0f));
        }

        [Test]
        public void SourceFileStaysPureAndDoesNotReferenceOutOfScopeSystems() {
            string source = File.ReadAllText("Assets/_Project/Code/Memory/M0MemoryVFXResponse.cs");

            Assert.That(source.Contains("UnityEngine"), Is.False);
            Assert.That(source.Contains("Animator"), Is.False);
            Assert.That(source.Contains("SceneManager"), Is.False);
            Assert.That(source.Contains("GetComponent<"), Is.False);
            Assert.That(source.Contains("RegisterGeneratedFor<"), Is.False);
        }

        private static AcceptedMemoryRevealContext CreateAcceptedContext(string memoryId) {
            RevealRequestContext request = new RevealRequestContext(
                CombatRequestSourceType.TestHarness,
                "Counter confirmed",
                "TestHarness",
                memoryId,
                "Accepted reveal context",
                RevealRequestClassification.CounterConfirmed);

            RevealRequestResult result = new RevealRequestResult(
                RevealRequestDecision.Accepted,
                "Reveal request accepted",
                "Accepted reveal context",
                RevealRequestClassification.CounterConfirmed,
                memoryId);

            return new AcceptedMemoryRevealContext(
                memoryId,
                request,
                result,
                request.CombatResultSourceLabel,
                request.ContextLabel);
        }
    }
}
