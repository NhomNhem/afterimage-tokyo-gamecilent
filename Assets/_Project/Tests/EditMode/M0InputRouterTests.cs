using GlassRefrain.Core;
using GlassRefrain.Input;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class M0InputRouterTests
    {
        [Test]
        public void RouterDefaultsToEnabledAndEmptyState()
        {
            M0InputRouter router = new M0InputRouter();

            Assert.That(router.InputEnabled, Is.True);
            Assert.That(router.Snapshot.InputEnabled, Is.True);
            Assert.That(router.Snapshot.Move.X, Is.EqualTo(0f));
            Assert.That(router.Snapshot.Move.Y, Is.EqualTo(0f));
            Assert.That(router.Snapshot.LightAttackPressed, Is.False);
            Assert.That(router.RoutingHistory, Is.Empty);
        }

        [Test]
        public void RouterCapturesRawIntentAndEnabledState()
        {
            M0InputRouter router = new M0InputRouter();

            router.SetInputEnabled(false);
            router.SetMove(new Axis2(1f, -0.5f));
            router.SetLook(new Axis2(-2f, 0.25f));
            router.SetActionPressed(InputActionIntent.LightAttack, true);

            InputIntentSnapshot snapshot = router.Snapshot;

            Assert.That(snapshot.InputEnabled, Is.False);
            Assert.That(snapshot.Move.X, Is.EqualTo(1f));
            Assert.That(snapshot.Move.Y, Is.EqualTo(-0.5f));
            Assert.That(snapshot.Look.X, Is.EqualTo(-2f));
            Assert.That(snapshot.Look.Y, Is.EqualTo(0.25f));
            Assert.That(snapshot.LightAttackPressed, Is.True);
        }

        [Test]
        public void RouterRecordsDistinctRoutingOutcomes()
        {
            M0InputRouter router = new M0InputRouter();

            router.RecordRoutingOutcome(InputActionIntent.Move, InputRoutingDisposition.Disabled, string.Empty, "disabled");
            router.RecordRoutingOutcome(InputActionIntent.LightAttack, InputRoutingDisposition.Ignored, "none", string.Empty);
            router.RecordRoutingOutcome(InputActionIntent.Dodge, InputRoutingDisposition.Routed, "Locomotion", string.Empty);
            router.RecordRoutingOutcome(InputActionIntent.Parry, InputRoutingDisposition.Rejected, "Combat", "locked out");

            Assert.That(router.RoutingHistory.Count, Is.EqualTo(4));
            Assert.That(router.RoutingHistory[0].Disposition, Is.EqualTo(InputRoutingDisposition.Disabled));
            Assert.That(router.RoutingHistory[1].Disposition, Is.EqualTo(InputRoutingDisposition.Ignored));
            Assert.That(router.RoutingHistory[2].Disposition, Is.EqualTo(InputRoutingDisposition.Routed));
            Assert.That(router.RoutingHistory[2].Accepted, Is.True);
            Assert.That(router.RoutingHistory[3].Disposition, Is.EqualTo(InputRoutingDisposition.Rejected));
            Assert.That(router.RoutingHistory[3].Reason, Is.EqualTo("locked out"));
        }

        [Test]
        public void RouterCreatesDebugSnapshot()
        {
            M0InputRouter router = new M0InputRouter();

            router.SetInputEnabled(false);
            router.RecordRoutingOutcome(InputActionIntent.LockOn, InputRoutingDisposition.Rejected, "Targeting", "no target");

            InputDebugSnapshot debugSnapshot = router.CreateDebugSnapshot();
            string joined = string.Join("\n", debugSnapshot.Details);

            Assert.That(debugSnapshot.Summary, Is.EqualTo("M0 input state"));
            Assert.That(debugSnapshot.Details, Is.Not.Null);
            StringAssert.Contains("InputEnabled: False", joined);
            StringAssert.Contains("LatestRouting:", joined);
        }
    }
}
