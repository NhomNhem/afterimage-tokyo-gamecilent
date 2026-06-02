using GlassRefrain.Core;
using GlassRefrain.Memory;
using NhemDangFugBixs.NhemLogging;
using NUnit.Framework;
using UnityEngine;

namespace GlassRefrain.Tests.EditMode {
    public class MemoryInteractionServiceTests {
        [Test]
        public void Tick_WithEligibleFragmentAndInteract_AcceptsThroughMemoryState() {
            var memoryState = new M0MemoryState("fragment-a");
            var service = new MemoryInteractionService(memoryState, new NhemNullLogger());
            var fragmentObject = new GameObject("FragmentA");
            var fragment = fragmentObject.AddComponent<MemoryFragment>();

            SetInteractionRadius(fragment, 2.5f);
            service.RegisterFragment(fragment);

            service.Tick(Vector3.zero, true);

            Assert.That(service.Snapshot.LastOutcome, Is.EqualTo(MemoryInteractOutcome.Accepted));
            Assert.That(memoryState.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Responding));

            Object.DestroyImmediate(fragmentObject);
        }

        [Test]
        public void Tick_WithoutEligibleFragmentAndInteract_IsSafe() {
            var memoryState = new M0MemoryState("fragment-a");
            var service = new MemoryInteractionService(memoryState, new NhemNullLogger());

            service.Tick(Vector3.zero, true);

            Assert.That(service.Snapshot.LastOutcome, Is.EqualTo(MemoryInteractOutcome.NoEligibleFragment));
            Assert.That(memoryState.Snapshot.Phase, Is.EqualTo(MemoryRevealPhase.Dormant));
        }

        [Test]
        public void Tick_DuplicateInteraction_IsIgnoredSafely() {
            var memoryState = new M0MemoryState("fragment-a");
            var service = new MemoryInteractionService(memoryState, new NhemNullLogger());
            var fragmentObject = new GameObject("FragmentA");
            var fragment = fragmentObject.AddComponent<MemoryFragment>();
            SetInteractionRadius(fragment, 3f);
            service.RegisterFragment(fragment);

            service.Tick(Vector3.zero, true);
            service.Tick(Vector3.zero, true);

            Assert.That(service.Snapshot.LastOutcome, Is.EqualTo(MemoryInteractOutcome.DuplicateIgnored));

            Object.DestroyImmediate(fragmentObject);
        }

        private static void SetInteractionRadius(MemoryFragment fragment, float value) {
            var field = typeof(MemoryFragment).GetField("interactionRadius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(fragment, value);
        }
    }
}
