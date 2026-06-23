using GlassRefrain.Application;
using GlassRefrain.Combat;
using GlassRefrain.Core;
using GlassRefrain.Locomotion;
using NUnit.Framework;
using R3;

namespace GlassRefrain.Tests.EditMode {
    [TestFixture]
    public class M0TargetFocusStrafeTests {
        [Test]
        public void SetHasTargetFocus_WhenTrue_UpdatesSnapshot() {
            var resolver = new PlayerStateResolver(null, null);
            resolver.SetHasTargetFocus(true);
            Assert.That(resolver.CurrentSnapshot.HasTargetFocus, Is.True);
        }

        [Test]
        public void SetHasTargetFocus_WhenFalse_UpdatesSnapshot() {
            var resolver = new PlayerStateResolver(null, null);
            resolver.SetHasTargetFocus(false);
            Assert.That(resolver.CurrentSnapshot.HasTargetFocus, Is.False);
        }

        [Test]
        public void SetHasTargetFocus_WhenToggled_EmitsNewSnapshot() {
            var resolver = new PlayerStateResolver(null, null);
            var emitted = false;
            var subscription = resolver.StateChanges.Subscribe(_ => emitted = true);
            resolver.SetHasTargetFocus(true);
            Assert.That(emitted, Is.True);
            subscription.Dispose();
        }

        [Test]
        public void SetHasTargetFocus_WhenSameValue_DoesNotEmit() {
            var resolver = new PlayerStateResolver(null, null);
            var emissionCount = 0;
            var subscription = resolver.StateChanges.Subscribe(_ => emissionCount++);
            subscription.Dispose();
            resolver.SetHasTargetFocus(false);
            var subscription2 = resolver.StateChanges.Subscribe(_ => emissionCount++);
            resolver.SetHasTargetFocus(false);
            Assert.That(emissionCount, Is.EqualTo(0));
            subscription2.Dispose();
        }

        [Test]
        public void SetHasTargetFocus_WhenTrue_ResolvedStateUnchanged() {
            var resolver = new PlayerStateResolver(null, null);
            var initialState = resolver.CurrentSnapshot.ResolvedState;
            resolver.SetHasTargetFocus(true);
            Assert.That(resolver.CurrentSnapshot.ResolvedState, Is.EqualTo(initialState));
        }

        [Test]
        public void SetStrafeMode_WhenTrue_FacesCameraForward() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Test"));
            locomotion.SetStrafeMode(true);
            locomotion.ConsumeInputIntent(new InputIntentSnapshot(
                new Axis2(0f, 1f), new Axis2(0f, 0f), false, false, false, false,
                false, false, false, false, true));
            locomotion.ProcessMovementInput(0.016f);
            var snapshot = locomotion.GetMovementSnapshot();
            Assert.That(snapshot.Facing.z, Is.GreaterThan(0.1f));
            Assert.That(snapshot.Facing.x, Is.LessThan(snapshot.Facing.z));
        }

        [Test]
        public void SetStrafeMode_WhenFalse_FacesMovementDirection() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Test"));
            locomotion.SetStrafeMode(false);
            locomotion.ConsumeInputIntent(new InputIntentSnapshot(
                new Axis2(1f, 0f), new Axis2(0f, 0f), false, false, false, false,
                false, false, false, false, true));
            locomotion.ProcessMovementInput(0.016f);
            var snapshot = locomotion.GetMovementSnapshot();
            Assert.That(snapshot.Facing.x, Is.GreaterThan(0.1f));
        }

        [Test]
        public void UpdatePosition_DuringDodge_DoesNotOverwriteFacing() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Test"));
            locomotion.ConsumeInputIntent(new InputIntentSnapshot(
                new Axis2(-1f, 0f), new Axis2(0f, 0f), false, false, false, false,
                false, false, false, false, true));
            locomotion.ProcessMovementInput(0.016f);
            var facingBeforeDodge = locomotion.GetMovementSnapshot().Facing;
            locomotion.TryBeginDodgeDisplacement();
            locomotion.UpdatePosition(0.016f);
            var facingAfterDodge = locomotion.GetMovementSnapshot().Facing;
            Assert.That(facingAfterDodge, Is.EqualTo(facingBeforeDodge));
        }

        [Test]
        public void SetStrafeMode_WhenTrue_WithZeroInput_FacingStable() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f), new Axis2(1f, 0f), true, "Test"));
            locomotion.SetStrafeMode(true);
            var facingBefore = locomotion.GetMovementSnapshot().Facing;
            locomotion.ConsumeInputIntent(new InputIntentSnapshot(
                new Axis2(0f, 0f), new Axis2(0f, 0f), false, false, false, false,
                false, false, false, false, true));
            locomotion.ProcessMovementInput(0.016f);
            var facingAfter = locomotion.GetMovementSnapshot().Facing;
            Assert.That(facingAfter, Is.EqualTo(facingBefore));
        }
    }
}
