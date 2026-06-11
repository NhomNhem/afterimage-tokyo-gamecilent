using GlassRefrain.Core;
using GlassRefrain.Locomotion;

namespace GlassRefrain.Bootstrap {
    public sealed class M0DodgeDisplacementBridge {
        private bool _isArmed;

        public bool IsArmed => _isArmed;

        public void Reset() {
            _isArmed = false;
        }

        public bool HandleCombatTransition(
            CombatCoreState previousState,
            M0CombatSnapshot currentSnapshot,
            IM0PlayerLocomotion locomotion) {
            if (currentSnapshot.State == CombatCoreState.DodgeStartup && previousState != CombatCoreState.DodgeStartup) {
                _isArmed = true;
                return false;
            }

            if (currentSnapshot.State != CombatCoreState.DodgeActive) {
                if (_isArmed) {
                    _isArmed = false;
                }

                return false;
            }

            if (!_isArmed) {
                return false;
            }

            _isArmed = false;
            if (locomotion == null) {
                return false;
            }

            return locomotion.TryBeginDodgeDisplacement();
        }
    }
}
