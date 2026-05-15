using System;
using System.Collections.Generic;
using GlassRefrain.Core;

namespace GlassRefrain.Targeting {
    /// <summary>
    /// M0 implementation of the targetable registry.
    /// Manages exactly one duel enemy for the one-on-one encounter.
    /// </summary>
    public sealed class M0TargetableRegistry : ITargetableRegistry {
        private readonly Dictionary<string, ITargetable> registeredTargets;
        private string currentDuelEnemyId;

        public M0TargetableRegistry() {
            registeredTargets = new Dictionary<string, ITargetable>();
            currentDuelEnemyId = string.Empty;
        }

        public void Register(ITargetable targetable) {
            if (targetable == null) throw new ArgumentNullException(nameof(targetable));
            if (string.IsNullOrEmpty(targetable.TargetId)) throw new ArgumentException("TargetId cannot be empty", nameof(targetable));

            registeredTargets[targetable.TargetId] = targetable;

            // M0: First registered enemy becomes the current duel enemy
            if (string.IsNullOrEmpty(currentDuelEnemyId)) {
                currentDuelEnemyId = targetable.TargetId;
            }
        }

        public void Unregister(ITargetable targetable) {
            if (targetable == null) throw new ArgumentNullException(nameof(targetable));

            var targetId = targetable.TargetId;
            if (registeredTargets.ContainsKey(targetId)) {
                registeredTargets.Remove(targetId);

                // M0: If the current duel enemy is unregistered, clear it
                if (currentDuelEnemyId == targetId) {
                    currentDuelEnemyId = string.Empty;

                    // Promote another registered target to current duel enemy if available
                    foreach (var kvp in registeredTargets) {
                        if (kvp.Value.IsTargetable) {
                            currentDuelEnemyId = kvp.Key;
                            break;
                        }
                    }
                }
            }
        }

        public ITargetable GetCurrentDuelEnemy() {
            if (string.IsNullOrEmpty(currentDuelEnemyId)) return null;
            return registeredTargets.TryGetValue(currentDuelEnemyId, out var targetable) ? targetable : null;
        }

        public bool HasRegisteredTargetable(string targetId) {
            return !string.IsNullOrEmpty(targetId) && registeredTargets.ContainsKey(targetId);
        }

        public IReadOnlyCollection<ITargetable> GetAllRegisteredTargets() {
            return registeredTargets.Values;
        }
    }
}
