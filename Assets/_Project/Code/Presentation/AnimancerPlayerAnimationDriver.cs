using System;
using Animancer;
using GlassRefrain.Core;
using NhemDangFugBixs.NhemLogging;
using UnityEngine;
using VContainer;

namespace GlassRefrain.Presentation {
    public sealed class AnimancerPlayerAnimationDriver : MonoBehaviour, IPlayerAnimationService {
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private PlayerAnimLibrary library;
        [SerializeField] private bool disableRootMotion = true;
        [SerializeField] private bool playIdleOnEnable = true;

        private INhemLogger _logger;
        private AnimancerState _currentState;
        private bool _isCombatMode;
        private CartesianMixerState _locomotionMixer;
        private float _defaultFadeDuration = 0.25f;

        private bool _hitReactionToggle;
        private const float HitReactionFadeFromIdle = 0.15f;
        private const float HitReactionFadeFromAction = 0.1f;
        private const float HitReactionFadeFromReaction = 0.05f;

        private static readonly int MoveAmountHash = Animator.StringToHash("moveAmount");
        private static readonly int StrafeAmountHash = Animator.StringToHash("strafeAmount");
        private static readonly int RotationHash = Animator.StringToHash("rotation");

        [Inject]
        public void Construct(INhemLogger logger) { _logger = logger; }

        private void Awake() { DisableRootMotion(); }

        private void OnEnable() {
            DisableRootMotion();
            if (playIdleOnEnable) PlayNeutral();
        }

        public void SetCombatMode(bool isCombatMode) {
            if (_isCombatMode == isCombatMode) return;
            _isCombatMode = isCombatMode;
            _currentState = null;
            _locomotionMixer = null;
        }

        public void PlayNeutral() {
            _locomotionMixer = null;
            var t = _isCombatMode ? library.CombatIdle : library.Idle;
            Play(t, 0);
        }

        public void PlayLocomotion(LocomotionStateSnapshot snapshot, Vector2 relDir) {
            if (snapshot.State is LocomotionState.Idle or LocomotionState.Uninitialized) { PlayNeutral(); return; }
            if (snapshot.State != LocomotionState.Moving) return;
            PlayLocomotionBlended(relDir);
        }

        public void PlayAttack(AttackAnimationRequest req) {
            _locomotionMixer = null;
            Play(ResolveAttack(req), 1);
        }

        private ClipTransition ResolveAttack(AttackAnimationRequest req) {
            if (library.AttackWindup.Clip != null && req.CombatState == CombatCoreState.AttackStartup)
                return library.AttackWindup;
            if (library.AttackRecovery.Clip != null && req.CombatState == CombatCoreState.AttackRecovery)
                return library.AttackRecovery;
            return req.AttackType == CombatActionType.HeavyAttack ? library.HeavyAttack : library.LightAttack;
        }

        public void PlayDodge(DodgeAnimationRequest req) {
            _locomotionMixer = null;
            Play(ResolveDodge(req.CombatState), 1);
        }

        private ClipTransition ResolveDodge(CombatCoreState s) {
            if (library.DodgeStartup.Clip != null && s == CombatCoreState.DodgeStartup) return library.DodgeStartup;
            if (library.DodgeActive.Clip != null && s == CombatCoreState.DodgeActive) return library.DodgeActive;
            if (library.DodgeRecovery.Clip != null && s == CombatCoreState.DodgeRecovery) return library.DodgeRecovery;
            return library.Dodge;
        }

        public void PlayDash(DashDirection dir) {
            _locomotionMixer = null;
            Play(GetDashClip(dir), 1);
        }

        private ClipTransition GetDashClip(DashDirection dir) => dir switch {
            DashDirection.Back => library.DashBack,
            DashDirection.Left => library.DashLeft,
            DashDirection.Right => library.DashRight,
            _ => library.Dash
        };

        public void PlayParry(ParryAnimationRequest req) {
            _locomotionMixer = null;
            Play(ResolveParry(req.CombatState), 1);
        }

        private ClipTransition ResolveParry(CombatCoreState s) {
            if (library.ParryStartup.Clip != null && s == CombatCoreState.ParryStartup) return library.ParryStartup;
            if (library.ParryActive.Clip != null && s == CombatCoreState.ParryActive) return library.ParryActive;
            if (library.ParryRecovery.Clip != null && s == CombatCoreState.ParryRecovery) return library.ParryRecovery;
            return library.Parry;
        }

        public void PlayCounter(CounterAnimationRequest req) {
            _locomotionMixer = null;
            Play(ResolveCounter(req.CombatState), 1);
        }

        private ClipTransition ResolveCounter(CombatCoreState s) {
            if (library.CounterStartup.Clip != null && s == CombatCoreState.CounterWindow) return library.CounterStartup;
            if (library.CounterActive.Clip != null && s == CombatCoreState.CounterActive) return library.CounterActive;
            if (library.CounterRecovery.Clip != null && s == CombatCoreState.RevealBeat) return library.CounterRecovery;
            return library.Counter;
        }

        public void PlayHitReaction(HitReactionAnimationRequest req) {
            _locomotionMixer = null;
            var t = _hitReactionToggle && library.HitReaction2.Clip != null ? library.HitReaction2 : library.HitReaction;
            DisableRootMotion();
            if (!CanPlay()) return;
            if (t.Clip == null) { _logger?.LogWarning("[M0Animation] Missing hit reaction clip.", this); return; }
            _currentState = animancer.Play(t);
            _currentState.Time = 0f;
            _hitReactionToggle = !_hitReactionToggle;
        }

        public void PlayStun() {
            _locomotionMixer = null;
            Play(library.Stun, 1);
        }

        public void PlayEnterCombat() {
            _locomotionMixer = null;
            _currentState = Play(library.CombatEnter, 1);
            if (_currentState != null)
                _currentState.Events(this).OnEnd = PlayNeutral;
        }

        public void PlayExitCombat() {
            _locomotionMixer = null;
            _currentState = Play(library.CombatExit, 0);
            if (_currentState != null)
                _currentState.Events(this).OnEnd = PlayNeutral;
        }

        public void PlayJump() {
            _locomotionMixer = null;
            _currentState = Play(library.Jump, 0);
            if (_currentState != null)
                _currentState.Events(this).OnEnd = PlayNeutral;
        }

        public void SetLocomotionParameters(float move, float strafe, float rot) {
            if (animancer?.Animator == null) return;
            animancer.Animator.SetFloat(MoveAmountHash, move, 0.2f, Time.deltaTime);
            animancer.Animator.SetFloat(StrafeAmountHash, strafe, 0.2f, Time.deltaTime);
            animancer.Animator.SetFloat(RotationHash, rot, 0.35f, Time.deltaTime);
        }

        private void PlayLocomotionBlended(Vector2 relDir) {
            DisableRootMotion();
            if (!CanPlay()) return;
            if (!_isCombatMode) { PlaySimpleLocomotion(); return; }
            if (_locomotionMixer == null) BuildLocomotionMixer();
            if (_locomotionMixer != null) _locomotionMixer.Parameter = relDir;
        }

        private void BuildLocomotionMixer() {
            var fwd = library.CombatLocomotion.Clip;
            var walk = library.CombatWalk.Clip;
            var back = library.WalkBack.Clip;
            var left = library.WalkLeft.Clip;
            var right = library.WalkRight.Clip;
            if (fwd == null) { PlaySimpleLocomotion(); return; }

            var mixer = new CartesianMixerState();
            mixer.SetGraph(animancer.Graph);
            mixer.Add(fwd, new Vector2(0, 1));
            if (walk != null) mixer.Add(walk, new Vector2(0, 0.5f));
            if (back != null) mixer.Add(back, new Vector2(0, -1));
            if (left != null) mixer.Add(left, new Vector2(-1, 0));
            if (right != null) mixer.Add(right, new Vector2(1, 0));

            _locomotionMixer = mixer;
            _currentState = mixer;
            animancer.Play(_locomotionMixer, _defaultFadeDuration);
        }

        private void PlaySimpleLocomotion() {
            Play(_isCombatMode ? library.CombatLocomotion : library.Locomotion, 0);
        }

        /// <summary>
        /// Plays a transition on the specified layer.
        /// Layer 0 = locomotion (full body), Layer 1 = actions (upper body with AvatarMask).
        /// </summary>
        private AnimancerState Play(ClipTransition t, int layerIndex) {
            DisableRootMotion();
            if (!CanPlay()) return null;
            if (t?.Clip == null) return null;

            if (_currentState?.Clip == t.Clip) return _currentState;

            _currentState = animancer.Layers[0].Play(t);
            _currentState.Time = 0f;
            return _currentState;
        }

        /// <summary>Legacy overload for backward compatibility.</summary>
        private void PlayTransition(ClipTransition t, string label) {
            Play(t, 0);
        }

        private bool CanPlay() {
            if (library == null) { _logger?.LogWarning("[M0Animation] Missing PlayerAnimLibrary.", this); return false; }
            if (animancer == null) { _logger?.LogError("[M0Animation] Missing AnimancerComponent.", this); return false; }
            if (!animancer.enabled) { _logger?.LogWarning("[M0Animation] Disabled.", this); return false; }
            return true;
        }

        private void DisableRootMotion() {
            if (!disableRootMotion || animancer?.Animator == null) return;
            animancer.Animator.applyRootMotion = false;
        }
    }
}
