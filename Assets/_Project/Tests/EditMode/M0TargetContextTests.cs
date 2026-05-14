using System.Collections.Generic;
using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Targeting;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode {
    public class M0TargetContextTests {
        [Test]
        public void TargetContextDefaultsToInactiveWithoutTarget() {
            var context = new M0TargetContext();

            Assert.That(context.Snapshot.FocusState, Is.EqualTo(TargetFocusState.Inactive));
            Assert.That(context.Snapshot.IsLockedOn, Is.False);
            Assert.That(context.Snapshot.HasTarget, Is.False);
            Assert.That(context.Snapshot.Direction.HasDirection, Is.False);
        }

        [Test]
        public void TargetContextCanAcquireAndFocusValidTarget() {
            var context = new M0TargetContext();
            context.SetTargetDirection(new TargetDirectionContext(new Axis2(0f, 1f), true, "forward"));
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", true, "Current duel enemy"));

            var result = context.RequestAcquire(new TargetAcquireRequest("Enemy_01", "Test", "Acquire target"));

            Assert.That(result.Accepted, Is.True);
            Assert.That(context.Snapshot.FocusState, Is.EqualTo(TargetFocusState.Focused));
            Assert.That(context.Snapshot.TargetId, Is.EqualTo("Enemy_01"));
            Assert.That(context.Snapshot.IsValid, Is.True);
            Assert.That(context.Snapshot.Direction.Label, Is.EqualTo("forward"));
        }

        [Test]
        public void TargetContextConsumesRawLockOnIntentAsRequestData() {
            var context = new M0TargetContext();
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", true, "Current duel enemy"));

            var consumed = context.ConsumeInputIntent(CreateInputSnapshot(true));

            Assert.That(consumed, Is.True);
            Assert.That(context.Snapshot.FocusState, Is.EqualTo(TargetFocusState.Focused));
            Assert.That(context.Snapshot.AcquireReason, Is.EqualTo("LockOn toggled on"));
        }

        [Test]
        public void TargetContextCanReleaseFocusedTarget() {
            var context = new M0TargetContext();
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", true, "Current duel enemy"));
            context.RequestAcquire(new TargetAcquireRequest("Enemy_01", "Test", "Acquire target"));

            var changed =
                context.RequestRelease(new TargetReleaseRequest(TargetReleaseReason.Manual, "Test",
                    "Player released target"));

            Assert.That(changed, Is.True);
            Assert.That(context.Snapshot.FocusState, Is.EqualTo(TargetFocusState.Inactive));
            Assert.That(context.Snapshot.ReleaseReason, Is.EqualTo("Player released target"));
            Assert.That(context.Snapshot.IsLockedOn, Is.False);
        }

        [Test]
        public void TargetContextMarksInvalidTargetsReadably() {
            var context = new M0TargetContext();
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", true, "Current duel enemy"));
            context.RequestAcquire(new TargetAcquireRequest("Enemy_01", "Test", "Acquire target"));
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", false, "Enemy defeated"));

            Assert.That(context.Snapshot.FocusState, Is.EqualTo(TargetFocusState.Invalid));
            Assert.That(context.Snapshot.IsValid, Is.False);
            Assert.That(context.Snapshot.InvalidReason, Is.EqualTo("Enemy defeated"));
        }

        [Test]
        public void TargetContextDebugSnapshotIsReadOnly() {
            var context = new M0TargetContext();
            context.SetTargetDirection(new TargetDirectionContext(new Axis2(-1f, 0f), true, "left"));
            context.SetTargetValidity(new TargetValidityContext("Enemy_01", true, "Current duel enemy"));
            context.RequestAcquire(new TargetAcquireRequest("Enemy_01", "Test", "Acquire target"));

            var debugSnapshot = context.CreateDebugSnapshot();
            var joined = string.Join("\n", debugSnapshot.Details);

            Assert.That(debugSnapshot.Summary, Is.EqualTo("M0 target context"));
            Assert.That(debugSnapshot.Details, Is.InstanceOf<IReadOnlyList<string>>());
            StringAssert.Contains("FocusState: Focused", joined);
            StringAssert.Contains("TargetId: Enemy_01", joined);
            StringAssert.Contains("Direction: left | True | (-1, 0)", joined);
        }

        [Test]
        public void TargetContextFilesDoNotReferenceLegacyInputManagerOrGeneratedDi() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Targeting/M0TargetContext.cs",
                "Assets/_Project/Code/Targeting/GlassRefrain.Targeting.asmdef"
            };

            string[] forbiddenPatterns = {
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<",
                "NhemDangFugBixs.Attributes"
            };

            foreach (var file in files) {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                var contents = File.ReadAllText(file);
                foreach (var pattern in forbiddenPatterns)
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
            }
        }

        private static InputIntentSnapshot CreateInputSnapshot(bool lockOnPressed) {
            return new InputIntentSnapshot(
                new Axis2(0f, 0f),
                new Axis2(0f, 0f),
                false,
                false,
                false,
                false,
                false,
                lockOnPressed,
                false,
                false,
                true);
        }
    }
}