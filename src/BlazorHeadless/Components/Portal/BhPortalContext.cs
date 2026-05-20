namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhPortalGroup"/> to nested
/// <see cref="BhPortal"/> components. Carries the target group ID
/// where portalled content should be rendered.
/// </summary>
public sealed class BhPortalContext
{
    /// <summary>
    /// The group ID where portalled content should be rendered.
    /// When null, portals render into the default <see cref="BhPortalOutlet"/>.
    /// </summary>
    public string? TargetId { get; init; }
}
