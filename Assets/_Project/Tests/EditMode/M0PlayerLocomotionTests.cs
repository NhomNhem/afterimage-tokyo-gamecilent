using System.Collections.Generic;
using System.IO;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using NUnit.Framework;

namespace GlassRefrain.Tests.EditMode
{
    public class M0PlayerLocomotionTests
    {
        [Test]
        public void LocomotionDefaultsToUninitializedWithDeferredCameraBasis()
        {
            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Uninitialized));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.True);
            Assert.That(locomotion.Snapshot.CameraMovementBasis.IsValid, Is.False);
            Assert.That(locomotion.Snapshot.CameraMovementBasis.CameraModeLabel, Is.EqualTo("Deferred"));
        }

        [Test]
        public void LocomotionConsumesRawMoveIntentFromInputSnapshot()
        {
            M0InputRouter router = new M0InputRouter();
            router.SetMove(new Axis2(1f, -0.25f));

            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(router.Snapshot);

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Moving));
            Assert.That(locomotion.Snapshot.MoveIntent.X, Is.EqualTo(1f));
            Assert.That(locomotion.Snapshot.MoveIntent.Y, Is.EqualTo(-0.25f));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.True);
        }

        [Test]
        public void LocomotionBecomesIdleForZeroMoveIntent()
        {
            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(CreateInputSnapshot(0f, 0f, true));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Idle));
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("No move intent"));
            Assert.That(locomotion.Snapshot.HasMoveIntent, Is.False);
        }

        [Test]
        public void LocomotionBecomesRestrictedWhenMovementIsBlocked()
        {
            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();
            locomotion.SetMovementRestriction(new MovementRestrictionContext(false, true, 1f, "CombatCore"));
            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, false));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Restricted));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.False);
            Assert.That(locomotion.Snapshot.MovementRestriction.CanTranslate, Is.False);
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("Input disabled"));
        }

        [Test]
        public void LocomotionBecomesRecoveringWhenRecoveryContextIsActive()
        {
            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();
            locomotion.SetRecoveryContext(new RecoveryContext(RecoverySource.CombatCore, true, 0.35f, "Recovering after committed action"));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Recovering));
            Assert.That(locomotion.Snapshot.Recovery.IsRecovering, Is.True);
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("Recovering after committed action"));
        }

        [Test]
        public void LocomotionDebugSnapshotIsReadOnlyAndDerivedFromState()
        {
            M0PlayerLocomotion locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, true));

            LocomotionDebugSnapshot debugSnapshot = locomotion.CreateDebugSnapshot();
            string joined = string.Join("\n", debugSnapshot.Details);

            Assert.That(debugSnapshot.Summary, Is.EqualTo("M0 locomotion state"));
            Assert.That(debugSnapshot.Details, Is.InstanceOf<IReadOnlyList<string>>());
            StringAssert.Contains("State: Moving", joined);
            StringAssert.Contains("MoveIntent: (1, 0)", joined);
            StringAssert.Contains("CameraBasis: False | Deferred", joined);
        }

        [Test]
        public void LocomotionFilesDoNotReferenceLegacyInputManagerOrGeneratedDi()
        {
            string[] files =
            {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Locomotion/M0PlayerLocomotion.cs",
                "Assets/_Project/Code/Locomotion/GlassRefrain.Locomotion.asmdef"
            };

            string[] forbiddenPatterns =
            {
                "InputManager",
                "UnityEngine.Input;",
                "UnityEngine.Input ",
                "RegisterGeneratedFor<",
                "NhemDangFugBixs.Attributes"
            };

            foreach (string file in files)
            {
                Assert.That(File.Exists(file), Is.True, "Expected file to exist: " + file);

                string contents = File.ReadAllText(file);
                foreach (string pattern in forbiddenPatterns)
                {
                    Assert.That(contents.Contains(pattern), Is.False, file + " contains forbidden pattern: " + pattern);
                }
            }
        }

        private static InputIntentSnapshot CreateInputSnapshot(float moveX, float moveY, bool inputEnabled)
        {
            return new InputIntentSnapshot(
                new Axis2(moveX, moveY),
                new Axis2(0f, 0f),
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                inputEnabled);
        }
    }
}
