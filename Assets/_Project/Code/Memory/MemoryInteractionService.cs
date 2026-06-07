using System;
using System.Collections.Generic;
using GlassRefrain.Code.Shared.DI;
using GlassRefrain.Core;
using NhemDangFugBixs.Attributes;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;

namespace GlassRefrain.Memory {
    public enum MemoryInteractOutcome {
        None = 0,
        NoEligibleFragment = 1,
        Accepted = 2,
        Rejected = 3,
        DuplicateIgnored = 4
    }

    public readonly struct MemoryInteractionSnapshot {
        public string NearbyFragmentId { get; }
        public bool HasEligibleFragment { get; }
        public bool InteractPressed { get; }
        public MemoryInteractOutcome LastOutcome { get; }
        public string LastReason { get; }

        public MemoryInteractionSnapshot(
            string nearbyFragmentId,
            bool hasEligibleFragment,
            bool interactPressed,
            MemoryInteractOutcome lastOutcome,
            string lastReason) {
            NearbyFragmentId = nearbyFragmentId ?? string.Empty;
            HasEligibleFragment = hasEligibleFragment;
            InteractPressed = interactPressed;
            LastOutcome = lastOutcome;
            LastReason = lastReason ?? string.Empty;
        }
    }

    interface IMemoryInteractionService {
        MemoryInteractionSnapshot Snapshot { get; }
        event Action<MemoryInteractionSnapshot> SnapshotChanged;
        void RegisterFragment(MemoryFragment fragment);
        void UnregisterFragment(MemoryFragment fragment);
        void Tick(Vector3 playerPosition, bool interactPressed);
    }

    [AutoRegisterIn<IGameplayLifetimeScope>(Lifetime = NhemLifetime.Scoped)]
    public sealed class MemoryInteractionService : IMemoryInteractionService  {
        private readonly IM0MemoryState _memoryState;
        private readonly INhemLogger _logger;
        private readonly List<MemoryFragment> _fragments;
        private readonly HashSet<string> _collectedFragmentIds;
        private MemoryInteractionSnapshot _snapshot;

        public MemoryInteractionService(IM0MemoryState memoryState, INhemLogger logger) {
            _memoryState = memoryState;
            _logger = logger;
            _fragments = new List<MemoryFragment>();
            _collectedFragmentIds = new HashSet<string>(StringComparer.Ordinal);
            _snapshot = new MemoryInteractionSnapshot(string.Empty, false, false, MemoryInteractOutcome.None, string.Empty);
        }

        public MemoryInteractionSnapshot Snapshot => _snapshot;
        public event Action<MemoryInteractionSnapshot> SnapshotChanged;

        public void RegisterFragment(MemoryFragment fragment) {
            if (fragment == null || _fragments.Contains(fragment)) {
                return;
            }

            _fragments.Add(fragment);
        }

        public void UnregisterFragment(MemoryFragment fragment) {
            if (fragment == null) {
                return;
            }

            _fragments.Remove(fragment);
        }

        public void Tick(Vector3 playerPosition, bool interactPressed) {
            var eligibleFragment = FindEligibleFragment(playerPosition, out var nearestDistance);
            var eligibleId = eligibleFragment != null ? eligibleFragment.FragmentId : string.Empty;
            var hasEligible = eligibleFragment != null;
            var outcome = MemoryInteractOutcome.None;
            var reason = hasEligible
                ? "Eligible fragment in range (" + nearestDistance.ToString("F2") + "m)"
                : "No eligible fragment in range";

            if (interactPressed) {
                if (!hasEligible) {
                    outcome = MemoryInteractOutcome.NoEligibleFragment;
                } else if (_collectedFragmentIds.Contains(eligibleId)) {
                    outcome = MemoryInteractOutcome.DuplicateIgnored;
                    reason = "Fragment already collected";
                } else {
                    var request = new RevealRequestContext(
                        CombatRequestSourceType.InputMapping,
                        "MemoryFragmentInteract",
                        eligibleId,
                        eligibleId,
                        "M1 Fragment Interact");

                    _memoryState.IntakeRevealRequest(request);
                    var evaluation = _memoryState.EvaluateRequestedReveal();
                    if (evaluation.Accepted) {
                        _memoryState.AdvancePhase("Memory fragment interaction accepted");
                        outcome = MemoryInteractOutcome.Accepted;
                        reason = "Reveal accepted by MemoryState";
                        _collectedFragmentIds.Add(eligibleId);
                    } else {
                        outcome = MemoryInteractOutcome.Rejected;
                        reason = "Reveal rejected by MemoryState: " + evaluation.Reason;
                    }

                    _logger?.Log("[M1Memory] Interaction result: fragmentId=" + eligibleId +
                                 " outcome=" + outcome + " reason=" + reason);
                }
            }

            UpdateSnapshot(new MemoryInteractionSnapshot(eligibleId, hasEligible, interactPressed, outcome, reason));
        }

        private MemoryFragment FindEligibleFragment(Vector3 playerPosition, out float nearestDistance) {
            MemoryFragment nearest = null;
            nearestDistance = float.MaxValue;
            for (var index = 0; index < _fragments.Count; index++) {
                var fragment = _fragments[index];
                if (fragment == null || !fragment.isActiveAndEnabled) {
                    continue;
                }

                if (!fragment.IsEligible(playerPosition, out var distance)) {
                    continue;
                }

                if (distance < nearestDistance) {
                    nearest = fragment;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private void UpdateSnapshot(MemoryInteractionSnapshot snapshot) {
            _snapshot = snapshot;
            SnapshotChanged?.Invoke(_snapshot);
        }
    }
}
