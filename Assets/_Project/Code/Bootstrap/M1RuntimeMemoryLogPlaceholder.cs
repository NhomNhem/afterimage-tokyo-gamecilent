using System;
using System.Collections.Generic;
using GlassRefrain.Core;
using GlassRefrain.Memory;

namespace GlassRefrain.Bootstrap {
    public sealed class M1RuntimeMemoryLogPlaceholder {
        private const string FallbackMemoryLabel = "Memory Fragment";
        private const string RevealedStateLabel = "Revealed";

        private readonly List<string> _entries = new List<string>();
        private readonly HashSet<string> _displayedOutcomeKeys = new HashSet<string>(StringComparer.Ordinal);

        public IReadOnlyList<string> Entries {
            get { return _entries; }
        }

        public bool TryAppendAcceptedInteraction(
            MemoryInteractionSnapshot interactionSnapshot,
            MemoryStateSnapshot memorySnapshot) {
            if (interactionSnapshot.LastOutcome != MemoryInteractOutcome.Accepted) {
                return false;
            }

            if (!memorySnapshot.LastResult.Accepted) {
                return false;
            }

            string outcomeKey = ResolveOutcomeKey(interactionSnapshot, memorySnapshot);
            if (!_displayedOutcomeKeys.Add(outcomeKey)) {
                return false;
            }

            _entries.Add(ResolveEntryLabel(interactionSnapshot, memorySnapshot) + ": " + RevealedStateLabel);
            return true;
        }

        private static string ResolveOutcomeKey(
            MemoryInteractionSnapshot interactionSnapshot,
            MemoryStateSnapshot memorySnapshot) {
            string memoryId = ResolveMemoryId(interactionSnapshot, memorySnapshot);
            return string.IsNullOrEmpty(memoryId)
                ? FallbackMemoryLabel
                : memoryId;
        }

        private static string ResolveEntryLabel(
            MemoryInteractionSnapshot interactionSnapshot,
            MemoryStateSnapshot memorySnapshot) {
            string memoryId = ResolveMemoryId(interactionSnapshot, memorySnapshot);
            return string.IsNullOrEmpty(memoryId)
                ? FallbackMemoryLabel
                : memoryId;
        }

        private static string ResolveMemoryId(
            MemoryInteractionSnapshot interactionSnapshot,
            MemoryStateSnapshot memorySnapshot) {
            if (!string.IsNullOrEmpty(memorySnapshot.MemoryId)) {
                return memorySnapshot.MemoryId;
            }

            if (!string.IsNullOrEmpty(memorySnapshot.LastResult.MemoryId)) {
                return memorySnapshot.LastResult.MemoryId;
            }

            if (!string.IsNullOrEmpty(memorySnapshot.LastRequest.MemoryId)) {
                return memorySnapshot.LastRequest.MemoryId;
            }

            return interactionSnapshot.NearbyFragmentId;
        }
    }
}
