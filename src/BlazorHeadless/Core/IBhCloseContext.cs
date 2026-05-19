namespace BlazorHeadless;

/// <summary>
/// A shared interface implemented by cascading contexts that support a "close"
/// action — <see cref="BhDialogContext"/>, <see cref="BhPopoverContext"/>, and
/// <see cref="BhDisclosureContext"/>. This enables the <see cref="BhCloseButton"/>
/// component to close the nearest enclosing closeable ancestor without needing
/// to know which specific component it's nested inside.
/// </summary>
public interface IBhCloseContext
{
    /// <summary>Closes the enclosing component.</summary>
    Task CloseAsync();
}
