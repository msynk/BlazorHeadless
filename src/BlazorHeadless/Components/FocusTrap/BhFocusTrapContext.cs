namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhFocusTrap"/> to its descendants.
/// Carries the enabled state so child components can react to whether focus
/// trapping is currently active.
/// </summary>
public sealed class BhFocusTrapContext
{
    /// <summary>Whether the focus trap is currently active.</summary>
    public required bool Enabled { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhFocusTrap"/>. Allows consumers
/// to render content driven by the enabled state.
/// </summary>
public sealed record BhFocusTrapRenderContext
{
    /// <summary>Whether the focus trap is currently active.</summary>
    public required bool Enabled { get; init; }
}
