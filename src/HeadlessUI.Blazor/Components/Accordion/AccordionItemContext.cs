namespace HeadlessUI.Blazor;

/// <summary>
/// Cascading context provided by <see cref="HAccordionItem"/> to its descendants
/// (<see cref="HAccordionTrigger"/> and <see cref="HAccordionContent"/>).
/// Carries per-item identity, state, and the item-scoped toggle callback.
/// </summary>
public sealed class AccordionItemContext
{
    /// <summary>The unique string identifier of this accordion item.</summary>
    public required string Value { get; init; }

    /// <summary>Whether this item's panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether this item (or the root accordion) is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to this item's <see cref="HAccordionTrigger"/>.
    /// Referenced by <see cref="HAccordionContent"/> via <c>aria-labelledby</c>.
    /// </summary>
    public required string TriggerId { get; init; }

    /// <summary>
    /// The HTML <c>id</c> assigned to this item's <see cref="HAccordionContent"/> panel.
    /// Referenced by <see cref="HAccordionTrigger"/> via <c>aria-controls</c>.
    /// </summary>
    public required string ContentId { get; init; }

    /// <summary>Toggles this item open or closed. Calls back into <see cref="AccordionContext"/>.</summary>
    public required Action Toggle { get; init; }
}

/// <summary>
/// Exposes the current state of an <see cref="HAccordionTrigger"/> to its render
/// fragment, enabling consumers to conditionally render content (e.g. a rotating
/// chevron icon) driven by the trigger's own open/closed state.
///
/// Passed via <c>RenderFragment&lt;AccordionTriggerContext&gt;</c> — access via the
/// implicit <c>@context</c> variable or a named <c>Context="t"</c> parameter.
/// </summary>
public sealed record AccordionTriggerContext
{
    /// <summary>Whether the associated accordion item panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the trigger (or the root accordion) is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Shorthand: the trigger can receive interaction.</summary>
    public bool Interactive => !Disabled;
}
