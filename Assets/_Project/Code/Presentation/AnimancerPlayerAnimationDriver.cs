using Animancer;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class AnimancerPlayerAnimationDriver : MonoBehaviour, IPlayerAnimationService {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private M0PlayerAnimationSet animationSet;
        [SerializeField] private bool disableRootMotion = true;
        [SerializeField] private bool playIdleOnEnable = true;

        private INhemLogger _logger;
        private string _currentClipName = string.Empty;

        [Inject]
         public void Construct(INhemLogger logger) {
            _logger = logger;
        }

        private void Awake() {
            DisableRootMotion();
        }

        private void OnEnable() {
            DisableRootMotion();
            if (playIdleOnEnable) {
                PlayNeutral();
            }
        }

        public void PlayNeutral() {
            Play(animationSet != null ? animationSet.Idle : null, "Player Idle");
        }

        public void PlayLocomotion(LocomotionStateSnapshot snapshot) {
            if (snapshot.State == LocomotionState.Moving) {
                Play(animationSet != null ? animationSet.Locomotion : null, "Player Locomotion");
                return;
            }

            if (snapshot.State == LocomotionState.Idle || snapshot.State == LocomotionState.Uninitialized) {
                PlayNeutral();
            }
        }

        public void PlayAttack(AttackAnimationRequest request) {
            var transition = request.AttackType == CombatActionType.HeavyAttack
                ? animationSet != null ? animationSet.HeavyAttack : null
                : animationSet != null ? animationSet.LightAttack : null;

            Play(transition, request.AttackType == CombatActionType.HeavyAttack ? "Player HeavyAttack" : "Player LightAttack");
        }

        public void PlayDodge(DodgeAnimationRequest request) {
            Play(animationSet != null ? animationSet.Dodge : null, "Player Dodge");
        }

        public void PlayParry(ParryAnimationRequest request) {
            Play(animationSet != null ? animationSet.Parry : null, "Player Parry");
        }

        public void PlayCounter(AttackAnimationRequest request) {
            Play(animationSet != null ? animationSet.Counter : null, "Player Counter");
        }

        private void Play(M0AnimationClipTransition transition, string label) {
            DisableRootMotion();

            if (!CanPlay(label)) {
                return;
            }

            if (transition == null || !transition.IsAssigned) {
                _logger?.LogWarning("[M0Animation] Missing optional clip for " + label + ". Assign it in M0PlayerAnimationSet if required.", this);
                return;
            }

            var clip = transition.Clip;
            if (_currentClipName == clip.name) {
                return;
            }

            _currentClipName = clip.name;
            animancer.Play(clip, transition.FadeDuration).Time = 0f;
        }

        private bool CanPlay(string label) {
            if (animationSet == null) {
                _logger?.LogWarning("[M0Animation] Missing M0PlayerAnimationSet while playing " + label + ". Optional M0 clips are tolerable; assign the set if you need animations.", this);
                return false;
            }

            if (animancer == null) {
                _logger?.LogError("[M0Animation] Missing AnimancerComponent on player animation driver while playing " + label + ".", this);
                return false;
            }

            if (!animancer.enabled) {
                _logger?.LogWarning("[M0Animation] Player AnimancerComponent is disabled; gameplay continues without animation.", this);
                return false;
            }

            return true;
        }

        private void DisableRootMotion() {
            if (!disableRootMotion || animancer == null || animancer.Animator == null) return;

            animancer.Animator.applyRootMotion = false;
        }
    }
}
