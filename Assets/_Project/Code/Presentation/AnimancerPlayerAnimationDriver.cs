using System;
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
        private bool _isCombatMode;
        private CartesianMixerState _locomotionMixer;
        private float _defaultFadeDuration = 0.25f;

        private static readonly int MoveAmountHash = Animator.StringToHash("moveAmount");
        private static readonly int StrafeAmountHash = Animator.StringToHash("strafeAmount");
        private static readonly int RotationHash = Animator.StringToHash("rotation");

        private bool _hitReactionToggle;
        private const float HitReactionFadeFromIdle = 0.15f;
        private const float HitReactionFadeFromAction = 0.1f;
        private const float HitReactionFadeFromReaction = 0.05f;

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

        public void SetCombatMode(bool isCombatMode) {
            if (_isCombatMode == isCombatMode) return;

            _isCombatMode = isCombatMode;
            _currentClipName = string.Empty;
            _locomotionMixer = null;
        }

        public void PlayNeutral() {
            _locomotionMixer = null;
            var transition = _isCombatMode
                ? (animationSet != null ? animationSet.CombatIdle : null)
                : (animationSet != null ? animationSet.Idle : null);
            Play(transition, _isCombatMode ? "Player CombatIdle" : "Player Idle");
        }

        public void PlayLocomotion(LocomotionStateSnapshot snapshot, Vector2 relativeMovementDirection) {
            if (snapshot.State == LocomotionState.Idle || snapshot.State == LocomotionState.Uninitialized) {
                PlayNeutral();
                return;
            }

            if (snapshot.State != LocomotionState.Moving) return;

            PlayLocomotionBlended(relativeMovementDirection);
        }

        public void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot, Vector2 relativeMovementDirection) {
            if (state == LocomotionState.Idle || state == LocomotionState.Uninitialized) {
                PlayNeutral();
                return;
            }

            PlayLocomotionBlended(relativeMovementDirection);
        }

        public void PlayAttack(AttackAnimationRequest request) {
            _locomotionMixer = null;

            var transition = ResolveAttackTransition(request);
            Play(transition, "Player Attack " + request.AttackType + " " + request.CombatState);
        }

        private M0AnimationClipTransition ResolveAttackTransition(AttackAnimationRequest request) {
            if (animationSet == null) return null;

            if (request.CombatState == CombatCoreState.AttackStartup) {
                var windup = animationSet.AttackWindup;
                if (windup != null && windup.IsAssigned) return windup;
            }

            if (request.CombatState == CombatCoreState.AttackRecovery) {
                var recovery = animationSet.AttackRecovery;
                if (recovery != null && recovery.IsAssigned) return recovery;
            }

            return request.AttackType == CombatActionType.HeavyAttack
                ? animationSet.HeavyAttack
                : animationSet.LightAttack;
        }

        public void PlayDodge(DodgeAnimationRequest request) {
            _locomotionMixer = null;

            var transition = ResolveDodgeTransition(request.CombatState);
            Play(transition, "Player Dodge " + request.CombatState);
        }

        private M0AnimationClipTransition ResolveDodgeTransition(CombatCoreState combatState) {
            if (animationSet == null) return null;

            switch (combatState) {
                case CombatCoreState.DodgeStartup: {
                    var phase = animationSet.DodgeStartup;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.DodgeActive: {
                    var phase = animationSet.DodgeActive;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.DodgeRecovery: {
                    var phase = animationSet.DodgeRecovery;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
            }

            return animationSet.Dodge;
        }

        public void PlayParry(ParryAnimationRequest request) {
            _locomotionMixer = null;

            var transition = ResolveParryTransition(request.CombatState);
            Play(transition, "Player Parry " + request.CombatState);
        }

        private M0AnimationClipTransition ResolveParryTransition(CombatCoreState combatState) {
            if (animationSet == null) return null;

            switch (combatState) {
                case CombatCoreState.ParryStartup: {
                    var phase = animationSet.ParryStartup;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.ParryActive: {
                    var phase = animationSet.ParryActive;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.ParryRecovery: {
                    var phase = animationSet.ParryRecovery;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
            }

            return animationSet.Parry;
        }

        public void PlayCounter(CounterAnimationRequest request) {
            _locomotionMixer = null;

            var transition = ResolveCounterTransition(request.CombatState);
            Play(transition, "Player Counter " + request.CombatState);
        }

        private M0AnimationClipTransition ResolveCounterTransition(CombatCoreState combatState) {
            if (animationSet == null) return null;

            switch (combatState) {
                case CombatCoreState.CounterWindow: {
                    var phase = animationSet.CounterStartup;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.CounterActive: {
                    var phase = animationSet.CounterActive;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
                case CombatCoreState.RevealBeat: {
                    var phase = animationSet.CounterRecovery;
                    if (phase != null && phase.IsAssigned) return phase;
                    break;
                }
            }

            return animationSet.Counter;
        }

        public void PlayDash(DashDirection direction) {
            _locomotionMixer = null;
            var transition = GetDashTransition(direction);
#if GR_M0_PROTOTYPE
            var clipName = transition != null && transition.IsAssigned ? transition.Clip.name : "null";
            _logger?.Log("[M0Animation] PlayDash direction=" + direction + " clip=" + clipName + " prevClip=" + _currentClipName);
#endif
            Play(transition, "Player Dash " + direction);
        }

        public void PlayHitReaction(HitReactionAnimationRequest request) {
            _locomotionMixer = null;

            if (animationSet == null) {
                Play(null, "Player HitReaction");
                return;
            }

            var transition = ResolveHitReactionTransition();
            var fadeDuration = ResolveHitReactionFadeDuration();

            DisableRootMotion();

            if (!CanPlay("Player HitReaction")) {
                return;
            }

            if (transition == null || !transition.IsAssigned) {
                _logger?.LogWarning("[M0Animation] Missing hit reaction clip. Assign hitReaction in M0PlayerAnimationSet.", this);
                return;
            }

            var clip = transition.Clip;
            var previousClipName = _currentClipName;
            _currentClipName = clip.name;
            animancer.Play(clip, fadeDuration).Time = 0f;

            _hitReactionToggle = !_hitReactionToggle;

#if GR_M0_PROTOTYPE
            _logger?.Log("[M0Animation] HitReaction: clip=" + clip.name + " fade=" + fadeDuration + "s from=" + previousClipName);
#endif
        }

        private M0AnimationClipTransition ResolveHitReactionTransition() {
            var primary = animationSet.HitReaction;
            var alternate = animationSet.HitReaction2;

            if (_hitReactionToggle && alternate != null && alternate.IsAssigned) {
                return alternate;
            }

            return primary;
        }

        private float ResolveHitReactionFadeDuration() {
            if (string.IsNullOrEmpty(_currentClipName)) {
                return HitReactionFadeFromIdle;
            }

            if (_currentClipName.Contains("HitReact")) {
                return HitReactionFadeFromReaction;
            }

            if (_currentClipName.Contains("Attack") || _currentClipName.Contains("Dodge") ||
                _currentClipName.Contains("Parry") || _currentClipName.Contains("Counter")) {
                return HitReactionFadeFromAction;
            }

            return HitReactionFadeFromIdle;
        }

        public void PlayStun() {
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.Stun : null, "Player Stun");
        }

        public void PlayTurn(TurnDirection direction) {
            _locomotionMixer = null;

            if (animationSet == null) return;

            M0AnimationClipTransition transition = direction == TurnDirection.Left
                ? animationSet.TurnLeft : animationSet.TurnRight;

            if (transition == null || !transition.IsAssigned) return;

            Play(transition, "Player Turn " + direction);
        }

        public void SetLocomotionParameters(float moveAmount, float strafeAmount, float rotationValue) {
            if (animancer == null || animancer.Animator == null) return;

            animancer.Animator.SetFloat(MoveAmountHash, moveAmount, 0.2f, Time.deltaTime);
            animancer.Animator.SetFloat(StrafeAmountHash, strafeAmount, 0.2f, Time.deltaTime);
            animancer.Animator.SetFloat(RotationHash, rotationValue, 0.35f, Time.deltaTime);
        }

        private void PlayLocomotionBlended(Vector2 relativeDirection) {
            DisableRootMotion();

            if (!CanPlay("Player Locomotion Mixer")) {
                return;
            }

            if (!_isCombatMode) {
                PlaySimpleLocomotion();
                return;
            }

            if (_locomotionMixer == null) {
                BuildLocomotionMixer();
            }

            if (_locomotionMixer != null) {
                _locomotionMixer.Parameter = relativeDirection;
            }
        }

        private void BuildLocomotionMixer() {
            var forwardClip = GetClipTransition(animationSet.CombatLocomotion);
            var backClip = GetClipTransition(animationSet.WalkBack);
            var leftClip = GetClipTransition(animationSet.WalkLeft);
            var rightClip = GetClipTransition(animationSet.WalkRight);

            if (forwardClip == null) {
                _logger?.LogWarning("[M0Animation] CombatLocomotion clip not assigned. Falling back to simple locomotion.", this);
                PlaySimpleLocomotion();
                return;
            }

            var mixer = new CartesianMixerState();
            mixer.SetGraph(animancer.Graph);

            mixer.Add(forwardClip, new Vector2(0f, 1f));

            if (backClip != null) {
                mixer.Add(backClip, new Vector2(0f, -1f));
            }

            if (leftClip != null) {
                mixer.Add(leftClip, new Vector2(-1f, 0f));
            }

            if (rightClip != null) {
                mixer.Add(rightClip, new Vector2(1f, 0f));
            }

            _locomotionMixer = mixer;
            _currentClipName = "LocomotionMixer";
            animancer.Play(_locomotionMixer, _defaultFadeDuration);
        }

        private void PlaySimpleLocomotion() {
            var transition = _isCombatMode
                ? (animationSet != null ? animationSet.CombatLocomotion : null)
                : (animationSet != null ? animationSet.Locomotion : null);
            Play(transition, _isCombatMode ? "Player CombatLocomotion" : "Player Locomotion");
        }

        private M0AnimationClipTransition GetDashTransition(DashDirection direction) {
            if (animationSet == null) return null;

            switch (direction) {
                case DashDirection.Back: return animationSet.DashBack;
                case DashDirection.Left: return animationSet.DashLeft;
                case DashDirection.Right: return animationSet.DashRight;
                default: return animationSet.Dash;
            }
        }

        private static AnimationClip GetClipTransition(M0AnimationClipTransition transition) {
            if (transition == null || !transition.IsAssigned) return null;
            return transition.Clip;
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
