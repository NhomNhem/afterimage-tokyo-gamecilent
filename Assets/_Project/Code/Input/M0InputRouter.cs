using System;
using System.Collections.Generic;
using GlassRefrain.Core;

namespace GlassRefrain.Input {
    public sealed class M0InputRouter : IInputIntentSource {
        private Axis2 move;
        private Axis2 look;
        private bool lightAttackPressed;
        private bool heavyAttackPressed;
        private bool parryPressed;
        private bool dodgePressed;
        private bool counterPressed;
        private bool lockOnPressed;
        private bool resetEncounterPressed;
        private bool toggleDebugOverlayPressed;
        private bool inputEnabled;
        private InputIntentSnapshot latestSnapshot;
        private readonly List<InputRoutingResult> routingHistory;

        public M0InputRouter() {
            this.inputEnabled = true;
            routingHistory = new List<InputRoutingResult>();
            RefreshSnapshot();
        }

        public InputIntentSnapshot Snapshot => latestSnapshot;

        public IReadOnlyList<InputRoutingResult> RoutingHistory => routingHistory;

        public bool InputEnabled => inputEnabled;

        public event Action<InputIntentSnapshot> SnapshotChanged;
        public event Action<InputRoutingResult> RoutingRecorded;

        public bool SetInputEnabled(bool enabled) {
            if (inputEnabled == enabled) return false;

            inputEnabled = enabled;
            RefreshSnapshot();
            return true;
        }

        public bool SetMove(Axis2 value) {
            if (AreEqual(move, value)) return false;

            move = value;
            RefreshSnapshot();
            return true;
        }

        public bool SetLook(Axis2 value) {
            if (AreEqual(look, value)) return false;

            look = value;
            RefreshSnapshot();
            return true;
        }

        public bool SetActionPressed(InputActionIntent action, bool pressed) {
            bool changed;

            switch (action) {
                case InputActionIntent.LightAttack:
                    changed = UpdateButton(ref lightAttackPressed, pressed);
                    break;
                case InputActionIntent.HeavyAttack:
                    changed = UpdateButton(ref heavyAttackPressed, pressed);
                    break;
                case InputActionIntent.Parry:
                    changed = UpdateButton(ref parryPressed, pressed);
                    break;
                case InputActionIntent.Dodge:
                    changed = UpdateButton(ref dodgePressed, pressed);
                    break;
                case InputActionIntent.Counter:
                    changed = UpdateButton(ref counterPressed, pressed);
                    break;
                case InputActionIntent.LockOn:
                    changed = UpdateButton(ref lockOnPressed, pressed);
                    break;
                case InputActionIntent.ResetEncounter:
                    changed = UpdateButton(ref resetEncounterPressed, pressed);
                    break;
                case InputActionIntent.ToggleDebugOverlay:
                    changed = UpdateButton(ref toggleDebugOverlayPressed, pressed);
                    break;
                default:
                    return false;
            }

            if (changed) RefreshSnapshot();

            return changed;
        }

        public void RecordRoutingOutcome(
            InputActionIntent action,
            InputRoutingDisposition disposition,
            string routedTo,
            string reason) {
            var destination = routedTo ?? string.Empty;
            var detail = reason ?? string.Empty;
            var result = new InputRoutingResult(action, disposition, destination, detail);
            routingHistory.Add(result);

            var handler = RoutingRecorded;
            if (handler != null) handler(result);
        }

        public InputDebugSnapshot CreateDebugSnapshot() {
            var details = new string[] {
                "InputEnabled: " + inputEnabled,
                "Move: (" + move.X + ", " + move.Y + ")",
                "Look: (" + look.X + ", " + look.Y + ")",
                "LightAttack: " + lightAttackPressed,
                "HeavyAttack: " + heavyAttackPressed,
                "Parry: " + parryPressed,
                "Dodge: " + dodgePressed,
                "Counter: " + counterPressed,
                "LockOn: " + lockOnPressed,
                "ResetEncounter: " + resetEncounterPressed,
                "ToggleDebugOverlay: " + toggleDebugOverlayPressed,
                "RoutingCount: " + routingHistory.Count
            };

            if (routingHistory.Count > 0) {
                var latestRouting = routingHistory[routingHistory.Count - 1];
                var lastRouting = latestRouting.Intent + " | " + latestRouting.Disposition + " | " +
                                  latestRouting.RoutedTo + " | " + latestRouting.Reason;
                Array.Resize(ref details, details.Length + 1);
                details[details.Length - 1] = "LatestRouting: " + lastRouting;
            }

            return new InputDebugSnapshot("M0 input state", Array.AsReadOnly(details));
        }

        private void RefreshSnapshot() {
            latestSnapshot = new InputIntentSnapshot(
                move,
                look,
                lightAttackPressed,
                heavyAttackPressed,
                parryPressed,
                dodgePressed,
                counterPressed,
                lockOnPressed,
                resetEncounterPressed,
                toggleDebugOverlayPressed,
                inputEnabled);

            var handler = SnapshotChanged;
            if (handler != null) handler(latestSnapshot);
        }

        private static bool UpdateButton(ref bool current, bool next) {
            if (current == next) return false;

            current = next;
            return true;
        }

        private static bool AreEqual(Axis2 left, Axis2 right) {
            return left.X == right.X && left.Y == right.Y;
        }
    }
}