using System.Collections.Generic;
using System.IO;
using System;
using GlassRefrain.Core;
using GlassRefrain.Input;
using GlassRefrain.Locomotion;
using NUnit.Framework;
using UnityEngine;

namespace GlassRefrain.Tests.EditMode {
    public class M0PlayerLocomotionTests {
        [Test]
        public void LocomotionDefaultsToUninitializedWithDeferredCameraBasis() {
            var locomotion = new M0PlayerLocomotion();

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Uninitialized));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.True);
            Assert.That(locomotion.Snapshot.CameraMovementBasis.IsValid, Is.False);
            Assert.That(locomotion.Snapshot.CameraMovementBasis.CameraModeLabel, Is.EqualTo("Deferred"));
        }

        [Test]
        public void LocomotionConsumesRawMoveIntentFromInputSnapshot() {
            var router = new M0InputRouter();
            router.SetMove(new Axis2(1f, -0.25f));

            var locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(router.Snapshot);

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Moving));
            Assert.That(locomotion.Snapshot.MoveIntent.X, Is.EqualTo(1f));
            Assert.That(locomotion.Snapshot.MoveIntent.Y, Is.EqualTo(-0.25f));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.True);
        }

        [Test]
        public void LocomotionBecomesIdleForZeroMoveIntent() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(CreateInputSnapshot(0f, 0f, true));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Idle));
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("No move intent"));
            Assert.That(locomotion.Snapshot.HasMoveIntent, Is.False);
        }

        [Test]
        public void LocomotionBecomesRestrictedWhenMovementIsBlocked() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetMovementRestriction(new MovementRestrictionContext(false, true, 1f, "CombatCore"));
            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, false));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Restricted));
            Assert.That(locomotion.Snapshot.InputEnabled, Is.False);
            Assert.That(locomotion.Snapshot.MovementRestriction.CanTranslate, Is.False);
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("Input disabled"));
        }

        [Test]
        public void LocomotionBecomesRecoveringWhenRecoveryContextIsActive() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetRecoveryContext(new RecoveryContext(RecoverySource.CombatCore, true, 0.35f,
                "Recovering after committed action"));

            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Recovering));
            Assert.That(locomotion.Snapshot.Recovery.IsRecovering, Is.True);
            Assert.That(locomotion.Snapshot.StateDetail, Is.EqualTo("Recovering after committed action"));
        }

        [Test]
        public void LocomotionDebugSnapshotIsReadOnlyAndDerivedFromState() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, true));

            var debugSnapshot = locomotion.CreateDebugSnapshot();
            var joined = string.Join("\n", debugSnapshot.Details);

            Assert.That(debugSnapshot.Summary, Is.EqualTo("M0 locomotion state"));
            Assert.That(debugSnapshot.Details, Is.InstanceOf<IReadOnlyList<string>>());
            StringAssert.Contains("State: Moving", joined);
            StringAssert.Contains("MoveIntent: (1, 0)", joined);
            StringAssert.Contains("CameraBasis: False | Deferred", joined);
        }

        [Test]
        public void LocomotionFilesDoNotReferenceLegacyInputManagerOrGeneratedDi() {
            string[] files = {
                "Assets/_Project/Code/Core/M0Contracts.cs",
                "Assets/_Project/Code/Locomotion/M0PlayerLocomotion.cs",
                "Assets/_Project/Code/Locomotion/GlassRefrain.Locomotion.asmdef"
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

        [Test]
        public void DodgeDisplacementMovesPlayerWhenTriggered() {
            var locomotion = new M0PlayerLocomotion(new M0LocomotionSettings(
                moveSpeed: 5f,
                inputDeadzone: 0.1f,
                facingLerpSpeed: 8f,
                dodgeDistance: 1.5f,
                dodgeSpeed: 10f,
                dodgeDurationSeconds: 0.3f));

            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "FreeLook"));

            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, true));
            locomotion.ProcessMovementInput(0.016f);
            var before = locomotion.GetMovementSnapshot().Position;

            Assert.That(locomotion.TryBeginDodgeDisplacement(), Is.True);
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);

            var after = locomotion.GetMovementSnapshot().Position;
            Assert.That(Vector3.Distance(before, after), Is.GreaterThan(0.01f));
        }

        [Test]
        public void DodgeDisplacementRejectsDuplicateTriggerDuringSameCycle() {
            var locomotion = new M0PlayerLocomotion();

            Assert.That(locomotion.TryBeginDodgeDisplacement(), Is.True);
            Assert.That(locomotion.TryBeginDodgeDisplacement(), Is.False);
        }

        [Test]
        public void InvalidDodgeSettingsFailFast() {
            Assert.Throws<ArgumentOutOfRangeException>(() => new M0PlayerLocomotion(
                new M0LocomotionSettings(dodgeDistance: 0f)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new M0PlayerLocomotion(
                new M0LocomotionSettings(dodgeSpeed: 0f)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new M0PlayerLocomotion(
                new M0LocomotionSettings(dodgeDurationSeconds: 0f)));
        }

        [Test]
        public void ResetForEncounter_RestoresPositionFacingAndClearsVelocity() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "FreeLook"));

            locomotion.ConsumeInputIntent(CreateInputSnapshot(1f, 0f, true));
            locomotion.ProcessMovementInput(0.1f);
            locomotion.UpdatePosition(0.1f);
            Assert.That(locomotion.GetMovementSnapshot().Position.sqrMagnitude, Is.GreaterThan(0.0001f));

            var resetPosition = new Vector3(2f, 0f, -1f);
            var resetFacing = new Vector3(0f, 0f, -1f);
            locomotion.ResetForEncounter(resetPosition, resetFacing);

            var snapshot = locomotion.GetMovementSnapshot();
            Assert.That(snapshot.Position, Is.EqualTo(resetPosition));
            Assert.That(snapshot.Facing, Is.EqualTo(resetFacing));
            Assert.That(snapshot.Velocity, Is.EqualTo(Vector3.zero));
            Assert.That(locomotion.Snapshot.State, Is.EqualTo(LocomotionState.Idle));
        }

        [Test]
        public void ResetForEncounter_UsesProvidedAuthoredBaselineEvenWhenNonZero() {
            var locomotion = new M0PlayerLocomotion();
            locomotion.SetCameraMovementBasis(new CameraMovementBasisSnapshot(
                new Axis2(0f, 1f),
                new Axis2(1f, 0f),
                true,
                "FreeLook"));

            locomotion.ConsumeInputIntent(CreateInputSnapshot(-1f, 0f, true));
            locomotion.ProcessMovementInput(0.2f);
            locomotion.UpdatePosition(0.2f);

            var authoredBaselinePosition = new Vector3(4.5f, 0f, -2.75f);
            var authoredBaselineFacing = new Vector3(0f, 0f, 1f);
            locomotion.ResetForEncounter(authoredBaselinePosition, authoredBaselineFacing);

            var snapshot = locomotion.GetMovementSnapshot();
            Assert.That(snapshot.Position, Is.EqualTo(authoredBaselinePosition));
            Assert.That(snapshot.Position, Is.Not.EqualTo(Vector3.zero));
            Assert.That(snapshot.Facing, Is.EqualTo(authoredBaselineFacing));
            Assert.That(snapshot.Velocity, Is.EqualTo(Vector3.zero));
        }

        private static InputIntentSnapshot CreateInputSnapshot(float moveX, float moveY, bool inputEnabled) {
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
