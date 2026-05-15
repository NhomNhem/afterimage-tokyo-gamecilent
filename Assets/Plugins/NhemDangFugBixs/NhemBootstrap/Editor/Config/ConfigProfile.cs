namespace NhemBootstrap.Editor.Config {
    /// <summary>Predefined configuration profile for the bootstrap tool.</summary>
    public enum ConfigProfile {
        /// <summary>Minimal setup: essential folders only, no packages.</summary>
        Minimal,
        /// <summary>Full Clean Architecture setup with all folders and packages.</summary>
        Full,
        /// <summary>User-defined custom configuration.</summary>
        Custom
    }
}
