namespace BlazorHeadless;

/// <summary>
/// Exposes the current state of an <see cref="BhSwitch"/> to its render fragment,
/// enabling consumers to render the visual track and thumb conditionally based on
/// the checked/disabled state.
///
/// Passed via <c>RenderFragment&lt;BhSwitchRenderContext&gt;</c> — access through the
/// implicit <c>@context</c> variable or a named <c>Context="s"</c> parameter.
/// </summary>
public sealed record BhSwitchRenderContext
{
    /// <summary>Whether the switch is currently in the checked (on) position.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether the switch is currently disabled.</summary>
    public required bool Disabled { get; init; }
}
