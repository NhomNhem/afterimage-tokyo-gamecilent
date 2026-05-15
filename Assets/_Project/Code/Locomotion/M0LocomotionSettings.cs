namespace GlassRefrain.Locomotion {
    /// <summary>
    /// M0LocomotionSettings — tuning values for camera-relative locomotion.
    /// Passed into M0PlayerLocomotion during initialization.
    /// </summary>
    public readonly struct M0LocomotionSettings {
        /// <summary>
        /// Movement speed multiplier. Input velocity is scaled by this value.
        /// Default: 5.0 (meters per second at full input).
        /// </summary>
        public float MoveSpeed { get; }

        /// <summary>
        /// Input deadzone magnitude. Inputs below this threshold are treated as zero.
        /// Default: 0.1 (normalized axis magnitude).
        /// </summary>
        public float InputDeadzone { get; }

        /// <summary>
        /// Facing lerp speed. Controls how quickly facing rotates toward movement direction.
        /// Higher values = faster rotation. Default: 8.0 (normalized per second).
        /// </summary>
        public float FacingLerpSpeed { get; }

        public M0LocomotionSettings(float moveSpeed = 5.0f, float inputDeadzone = 0.1f, float facingLerpSpeed = 8.0f) {
            MoveSpeed = moveSpeed;
            InputDeadzone = inputDeadzone;
            FacingLerpSpeed = facingLerpSpeed;
        }
    }
}
