namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhCollapsible"/> to its descendants
/// (<see cref="BhCollapsibleTrigger"/> and <see cref="BhCollapsibleContent"/>).
/// Carries the open state, ARIA wiring ids, and the toggle/close callbacks
/// so the children can coordinate without prop-drilling.
/// </summary>
public sealed class BhCollapsibleContext : IBhCloseContext
{
    /// <summary>Whether the collapsible content is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether interaction is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to the <see cref="BhCollapsibleTrigger"/>.
    /// Referenced by <see cref="BhCollapsibleContent"/> via <c>aria-labelledby</c>.
    /// </summary>
    public required string TriggerId { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to the <see cref="BhCollapsibleContent"/>.
    /// Referenced by <see cref="BhCollapsibleTrigger"/> via <c>aria-controls</c>.
    /// </summary>
    public required string ContentId { get; init; }

    /// <summary>Toggles the collapsible open or closed.</summary>
    public required Action Toggle { get; init; }

    /// <summary>Closes the collapsible if currently open.</summary>
    public required Action Close { get; init; }

    /// <inheritdoc />
    Task IBhCloseContext.CloseAsync()
    {
        Close();
        return Task.CompletedTask;
    }
}

/// <summary>
/// Exposes the current state of a <see cref="BhCollapsible"/> to its render
/// fragments, enabling consumers to render content driven by open/closed state
/// (rotating chevrons, conditional labels) and to programmatically close the
/// collapsible (e.g. from a link inside the content).
///
/// Passed via <c>RenderFragment&lt;BhCollapsibleRenderContext&gt;</c> — access via
/// the implicit <c>@context</c> variable or a named <c>Context="c"</c> parameter.
/// </summary>
public sealed record BhCollapsibleRenderContext
{
    /// <summary>Whether the collapsible content is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the collapsible. Useful from inside the content (e.g. a "Close" link).</summary>
    public required Action Close { get; init; }
}
