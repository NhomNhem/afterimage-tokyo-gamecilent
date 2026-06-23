using Animancer;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class AnimancerEnemyAnimationDriver : MonoBehaviour, IEnemyAnimationService {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private M0EnemyAnimationSet animationSet;
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
                PlayIdle();
            }
        }

        public void PlayIdle() {
            Play(animationSet != null ? animationSet.Idle : null, "Enemy Idle");
        }

        public void PlayIntent(EnemyIntentAnimationRequest request) {
            switch (request.IntentState) {
                case EnemyIntentState.Telegraph:
                    Play(animationSet != null ? animationSet.Telegraph : null, "Enemy Telegraph");
                    break;
                case EnemyIntentState.Commit:
                case EnemyIntentState.Active:
                    Play(animationSet != null ? animationSet.Active : null, "Enemy Active");
                    break;
                case EnemyIntentState.Recovery:
                    Play(animationSet != null ? animationSet.Recovery : null, "Enemy Recovery");
                    break;
                case EnemyIntentState.Idle:
                    PlayIdle();
                    break;
            }
        }

        public void PlayHitReaction(HitReactionAnimationRequest request) {
            Play(animationSet != null ? animationSet.HitReaction : null, "Enemy HitReaction");
        }

        private void Play(M0AnimationClipTransition transition, string label) {
            DisableRootMotion();

            if (!CanPlay(label)) {
                return;
            }

            if (transition == null || !transition.IsAssigned) {
                _logger?.LogWarning("[M0Animation] Missing optional clip for " + label + ". Assign it in M0EnemyAnimationSet if required.", this);
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
                _logger?.LogWarning("[M0Animation] Missing M0EnemyAnimationSet while playing " + label + ". Optional M0 clips are tolerable; assign the set if you need animations.", this);
                return false;
            }

            if (animancer == null) {
                _logger?.LogError("[M0Animation] Missing AnimancerComponent on enemy animation driver while playing " + label + ".", this);
                return false;
            }

            if (!animancer.enabled) {
                _logger?.LogWarning("[M0Animation] Enemy AnimancerComponent is disabled; gameplay continues without animation.", this);
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
