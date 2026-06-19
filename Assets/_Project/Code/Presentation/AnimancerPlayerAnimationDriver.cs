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
        [SerializeField, Range(90f, 170f)] private float sharpTurnAngleDegrees = 130f;
        [SerializeField, Min(0f)] private float defaultTurnLockDuration = 0.35f;

        private INhemLogger _logger;
        private string _currentClipName = string.Empty;
        private bool _isCombatMode;
        private CartesianMixerState _locomotionMixer;
        private float _defaultFadeDuration = 0.25f;

        private bool _isTurnActive;
        private float _turnRemainingSeconds;
        private Vector2 _lastStableWorldDirection = Vector2.zero;
        private bool _hasStableDirection;
        private bool _mixerLockedDuringTurn;

        public bool IsTurnActive => _isTurnActive;
        public Action<bool> TurnActiveChanged { get; set; }

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

        private void Update() {
            if (!_isTurnActive) return;

            _turnRemainingSeconds -= Time.deltaTime;
            if (_turnRemainingSeconds > 0f) return;

            EndTurn();
        }

        public void SetCombatMode(bool isCombatMode) {
            if (_isCombatMode == isCombatMode) return;

            if (_isTurnActive) {
                EndTurn();
            }

            _isCombatMode = isCombatMode;
            _currentClipName = string.Empty;
            _locomotionMixer = null;
            _hasStableDirection = false;
            _lastStableWorldDirection = Vector2.zero;
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
                _hasStableDirection = false;
                _lastStableWorldDirection = Vector2.zero;
                PlayNeutral();
                return;
            }

            if (snapshot.State != LocomotionState.Moving) return;

            if (_isTurnActive) {
                UpdateMixerDuringTurn();
                return;
            }

            TryDetectSharpTurn(snapshot.WorldVelocity);
            PlayLocomotionBlended(relativeMovementDirection);
        }

        public void PlayLocomotion(LocomotionState state, PlayerStateSnapshot fullSnapshot, Vector2 relativeMovementDirection) {
            if (state == LocomotionState.Idle || state == LocomotionState.Uninitialized) {
                PlayNeutral();
                return;
            }

            if (_isTurnActive) {
                UpdateMixerDuringTurn();
                return;
            }

            PlayLocomotionBlended(relativeMovementDirection);
        }

        public void PlayTurn(TurnDirection direction) {
            DisableRootMotion();

            if (!CanPlay("Player Turn " + direction)) {
                return;
            }

            var transition = GetTurnTransition(direction);
            if (transition == null || !transition.IsAssigned) {
                _logger?.LogWarning(
                    "[M0Animation] Missing optional clip for Player Turn " + direction +
                    ". Assign it in M0PlayerAnimationSet if you want a hard-pivot turn.", this);
                return;
            }

            BeginTurn(transition);
        }

        public void PlayAttack(AttackAnimationRequest request) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;

            var transition = ResolveAttackTransition(request);
            Play(transition, "Player Attack " + request.AttackType + " " + request.CombatState);
        }

        private M0AnimationClipTransition ResolveAttackTransition(AttackAnimationRequest request) {
            if (animationSet == null) return null;

            // Phase-specific clips (optional — fall back to main attack clip)
            if (request.CombatState == CombatCoreState.AttackStartup) {
                var windup = animationSet.AttackWindup;
                if (windup != null && windup.IsAssigned) return windup;
            }

            if (request.CombatState == CombatCoreState.AttackRecovery) {
                var recovery = animationSet.AttackRecovery;
                if (recovery != null && recovery.IsAssigned) return recovery;
            }

            // Main attack clip (active phase, or fallback for any phase)
            return request.AttackType == CombatActionType.HeavyAttack
                ? animationSet.HeavyAttack
                : animationSet.LightAttack;
        }

        public void PlayDodge(DodgeAnimationRequest request) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.Dodge : null, "Player Dodge");
        }

        public void PlayParry(ParryAnimationRequest request) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.Parry : null, "Player Parry");
        }

        public void PlayCounter(AttackAnimationRequest request) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.Counter : null, "Player Counter");
        }

        public void PlayDash(DashDirection direction) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            var transition = GetDashTransition(direction);
            Play(transition, "Player Dash " + direction);
        }

        public void PlayHitReaction(AttackAnimationRequest request) {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.HitReaction : null, "Player HitReaction");
        }

        public void PlayStun() {
            if (_isTurnActive) EndTurn();
            _locomotionMixer = null;
            Play(animationSet != null ? animationSet.Stun : null, "Player Stun");
        }

        private void TryDetectSharpTurn(Vector3 worldVelocity) {
            var worldXZ = new Vector2(worldVelocity.x, worldVelocity.z);
            var worldMag = Mathf.Sqrt(worldXZ.x * worldXZ.x + worldXZ.y * worldXZ.y);
            if (worldMag < 0.1f) return;

            var normalizedWorld = worldXZ / worldMag;

            if (!_hasStableDirection || _lastStableWorldDirection.sqrMagnitude < 0.01f) {
                _lastStableWorldDirection = normalizedWorld;
                _hasStableDirection = true;
                return;
            }

            var dot = Vector2.Dot(_lastStableWorldDirection, normalizedWorld);
            dot = Mathf.Clamp(dot, -1f, 1f);
            var angleDegrees = Mathf.Acos(dot) * Mathf.Rad2Deg;

            if (angleDegrees >= sharpTurnAngleDegrees) {
                var turnDir = ResolveTurnDirection(_lastStableWorldDirection, normalizedWorld);
                PlayTurn(turnDir);
                _lastStableWorldDirection = normalizedWorld;
                return;
            }

            _lastStableWorldDirection = Vector2.Lerp(_lastStableWorldDirection, normalizedWorld, 0.25f);
            if (_lastStableWorldDirection.sqrMagnitude > 0.0001f) {
                _lastStableWorldDirection = _lastStableWorldDirection.normalized;
            }
        }

        private static TurnDirection ResolveTurnDirection(Vector2 from, Vector2 to) {
            var cross = from.x * to.y - from.y * to.x;
            var angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(from, to), -1f, 1f)) * Mathf.Rad2Deg;

            if (angle >= 160f) return TurnDirection.Turn180;
            return cross >= 0f ? TurnDirection.Left90 : TurnDirection.Right90;
        }

        private void BeginTurn(M0AnimationClipTransition transition) {
            if (_isTurnActive) return;

            if (_locomotionMixer != null) {
                _locomotionMixer.Parameter = Vector2.zero;
                _mixerLockedDuringTurn = true;
            }

            _isTurnActive = true;
            _turnRemainingSeconds = defaultTurnLockDuration > 0f ? defaultTurnLockDuration : 0.35f;

            var clip = transition.Clip;
            _currentClipName = clip.name;
            var state = animancer.Play(clip, transition.FadeDuration);
            state.Time = 0f;

            if (state.Length > 0f) {
                var clipDrivenLock = state.Length * 0.9f;
                if (clipDrivenLock > _turnRemainingSeconds) {
                    _turnRemainingSeconds = clipDrivenLock;
                }
            }

            TurnActiveChanged?.Invoke(true);

#if GR_M0_PROTOTYPE
            _logger?.Log("[M0Animation] Turn started: " + clip.name + " lock=" + _turnRemainingSeconds + "s");
#endif
        }

        private void EndTurn() {
            if (!_isTurnActive) return;

            _isTurnActive = false;
            _turnRemainingSeconds = 0f;
            _mixerLockedDuringTurn = false;
            TurnActiveChanged?.Invoke(false);

#if GR_M0_PROTOTYPE
            _logger?.Log("[M0Animation] Turn ended; resuming locomotion blending.");
#endif
        }

        private void UpdateMixerDuringTurn() {
            if (!_mixerLockedDuringTurn || _locomotionMixer == null) return;
            _locomotionMixer.Parameter = Vector2.zero;
        }

        private M0AnimationClipTransition GetTurnTransition(TurnDirection direction) {
            if (animationSet == null) return null;

            switch (direction) {
                case TurnDirection.Turn180:
                    return animationSet.Turn180;
                case TurnDirection.Left90:
                    return animationSet.TurnLeft90;
                case TurnDirection.Right90:
                    return animationSet.TurnRight90;
                default:
                    return animationSet.Turn180;
            }
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
