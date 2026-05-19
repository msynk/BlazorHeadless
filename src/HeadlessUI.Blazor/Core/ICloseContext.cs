namespace HeadlessUI.Blazor;

/// <summary>
/// A shared interface implemented by cascading contexts that support a "close"
/// action — <see cref="DialogContext"/>, <see cref="PopoverContext"/>, and
/// <see cref="DisclosureContext"/>. This enables the <see cref="HCloseButton"/>
/// component to close the nearest enclosing closeable ancestor without needing
/// to know which specific component it's nested inside.
/// </summary>
public interface ICloseContext
{
    /// <summary>Closes the enclosing component.</summary>
    Task CloseAsync();
}
