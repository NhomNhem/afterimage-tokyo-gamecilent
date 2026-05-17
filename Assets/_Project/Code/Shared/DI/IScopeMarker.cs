namespace _Project.Code.Shared.DI {
    /// <summary>
    /// Marker base for all DI scope markers.
    /// Scope markers are conceptual DI boundaries, not runtime services.
    /// </summary>
    public interface IScopeMarker { }
    public interface IProjectRootLifetimeScope : IScopeMarker { }
    public interface IGameplayLifetimeScope : IScopeMarker { }
}