namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="HDisclosure"/> to its descendants
/// (<see cref="HDisclosureButton"/> and <see cref="HDisclosurePanel"/>).
/// Carries the open state, ARIA wiring ids, and the toggle/close callbacks
/// so the children can coordinate without prop-drilling.
/// </summary>
public sealed class DisclosureContext : ICloseContext
{
    /// <summary>Whether the disclosure panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether interaction is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to the <see cref="HDisclosureButton"/>.
    /// Referenced by <see cref="HDisclosurePanel"/> via <c>aria-labelledby</c>.
    /// </summary>
    public required string ButtonId { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to the <see cref="HDisclosurePanel"/>.
    /// Referenced by <see cref="HDisclosureButton"/> via <c>aria-controls</c>.
    /// </summary>
    public required string PanelId { get; init; }

    /// <summary>Toggles the disclosure open or closed.</summary>
    public required Action Toggle { get; init; }

    /// <summary>Closes the disclosure if currently open.</summary>
    public required Action Close { get; init; }

    /// <inheritdoc />
    Task ICloseContext.CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Exposes the current state of an <see cref="HDisclosure"/> to its render
/// fragments, enabling consumers to render content driven by open/closed state
/// (rotating chevrons, conditional labels) and to programmatically close the
/// disclosure (e.g. from a link inside the panel).
///
/// Passed via <c>RenderFragment&lt;DisclosureRenderContext&gt;</c> — access via
/// the implicit <c>@context</c> variable or a named <c>Context="d"</c> parameter.
/// </summary>
public sealed record DisclosureRenderContext
{
    /// <summary>Whether the disclosure panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the disclosure. Useful from inside the panel (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}
