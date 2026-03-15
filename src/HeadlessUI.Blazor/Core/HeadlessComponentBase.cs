using Microsoft.AspNetCore.Components;

namespace HeadlessUI.Blazor;

/// <summary>
/// Base class for all headless components. Provides the shared infrastructure:
/// polymorphic element rendering via <see cref="As"/>, automatic ID generation,
/// element reference forwarding via <see cref="Ref"/>, and attribute merging
/// that combines internal accessibility/data attributes with user-supplied ones.
///
/// Design principles (aligned with Radix UI, Headless UI, and Ark UI):
///  - Zero visual opinion: no CSS, no styles, only semantic HTML and data-* hooks
///  - Accessibility built-in: ARIA attributes managed internally
///  - Polymorphic rendering: any HTML element via the As parameter
///  - Styling hooks via data attributes: [data-state], [data-disabled], etc.
///  - Attribute merging: class and style concatenate; everything else user-wins
///
/// Subclass authoring pattern:
/// <code>
/// protected override string DefaultTag => "div";
///
/// protected override Dictionary&lt;string, object&gt; BuildComponentAttributes()
/// {
///     var attrs = base.BuildComponentAttributes();
///     SetDataState(attrs, IsOpen);
///     attrs["aria-expanded"] = IsOpen ? "true" : "false";
///     return attrs;
/// }
/// </code>
/// Then in BuildRenderTree call <see cref="GetFinalAttributes"/> once:
/// <code>
/// builder.AddMultipleAttributes(10, GetFinalAttributes());
/// </code>
/// </summary>
public abstract class HeadlessComponentBase : ComponentBase
{
    private string? _componentId;

    /// <summary>
    /// Override the rendered HTML element. Each component declares its own
    /// <see cref="DefaultTag"/> (e.g. "button") which is used when As is null.
    /// </summary>
    [Parameter]
    public string? As { get; set; }

    /// <summary>
    /// Explicit HTML id. When omitted a stable auto-generated id is used.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Callback to capture the underlying <see cref="ElementReference"/>
    /// for DOM access / focus management.
    /// </summary>
    [Parameter]
    public Action<ElementReference>? Ref { get; set; }

    /// <summary>
    /// Captures all HTML attributes not matched by explicit parameters.
    /// These are merged onto the root element, giving consumers full control
    /// over class, style, data-*, aria-*, and any other HTML attribute.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>
    /// The default HTML element tag rendered when <see cref="As"/> is not set.
    /// </summary>
    protected abstract string DefaultTag { get; }

    /// <summary>
    /// The resolved HTML tag: user-supplied <see cref="As"/> or <see cref="DefaultTag"/>.
    /// </summary>
    protected string Tag => string.IsNullOrEmpty(As) ? DefaultTag : As;

    /// <summary>
    /// The resolved HTML id: user-supplied <see cref="Id"/> or auto-generated.
    /// </summary>
    protected string ComponentId => Id ?? (_componentId ??= GenerateId());

    // ── Attribute building ───────────────────────────────────────────────────

    /// <summary>
    /// Override to provide this component's internal attributes: ARIA roles,
    /// data-state, data-* flags, and any other non-visual attributes the
    /// component manages. The base implementation returns an empty dictionary.
    /// Always call <c>base.BuildComponentAttributes()</c> to start with
    /// the base dictionary and add to it.
    /// </summary>
    /// <remarks>
    /// Do NOT call <see cref="AttributeUtilities.Merge"/> here — that is done
    /// automatically by <see cref="GetFinalAttributes"/>. Return the raw
    /// internal-only dictionary from this method.
    /// </remarks>
    protected virtual Dictionary<string, object> BuildComponentAttributes()
        => new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the final merged attribute dictionary ready to pass to
    /// <c>builder.AddMultipleAttributes</c>. Merges the result of
    /// <see cref="BuildComponentAttributes"/> with user-supplied
    /// <see cref="AdditionalAttributes"/>: <c>class</c> and <c>style</c>
    /// are concatenated; all other user-supplied values take precedence.
    /// </summary>
    protected Dictionary<string, object> GetFinalAttributes()
        => AttributeUtilities.Merge(BuildComponentAttributes(), AdditionalAttributes);

    /// <summary>
    /// Merges <paramref name="internalAttributes"/> with
    /// <see cref="AdditionalAttributes"/>. Prefer <see cref="GetFinalAttributes"/>
    /// combined with <see cref="BuildComponentAttributes"/> for new components.
    /// </summary>
    protected Dictionary<string, object> MergeAttributes(Dictionary<string, object> internalAttributes)
        => AttributeUtilities.Merge(internalAttributes, AdditionalAttributes);

    // ── Data-attribute helpers ────────────────────────────────────────────────

    /// <summary>
    /// Sets <c>data-state</c> following the Radix UI convention.
    /// When <paramref name="condition"/> is true the value is
    /// <paramref name="trueValue"/> (default <c>"open"</c>),
    /// otherwise <paramref name="falseValue"/> (default <c>"closed"</c>).
    /// </summary>
    /// <example>
    /// SetDataState(attrs, IsOpen);                          // "open" / "closed"
    /// SetDataState(attrs, IsChecked, "checked", "unchecked");
    /// SetDataState(attrs, IsSelected, "selected", "unselected");
    /// </example>
    protected static void SetDataState(
        Dictionary<string, object> attrs,
        bool condition,
        string trueValue = "open",
        string falseValue = "closed")
        => attrs["data-state"] = condition ? trueValue : falseValue;

    /// <summary>
    /// Adds a boolean presence attribute <c>data-{name}</c> (empty string
    /// value) when <paramref name="condition"/> is true, omitting it otherwise.
    /// Use for flags like <c>data-disabled</c>, <c>data-loading</c>,
    /// <c>data-required</c>.
    /// </summary>
    protected static void SetDataFlag(
        Dictionary<string, object> attrs,
        string name,
        bool condition)
        => AttributeUtilities.SetDataAttribute(attrs, name, condition);

    /// <summary>
    /// Sets <c>data-{name}="{value}"</c> unconditionally.
    /// Use for enumerated data attributes like
    /// <c>data-orientation="horizontal"</c>, <c>data-side="top"</c>.
    /// </summary>
    protected static void SetDataValue(
        Dictionary<string, object> attrs,
        string name,
        string value)
        => AttributeUtilities.SetDataAttribute(attrs, name, value);

    // ── Internals ─────────────────────────────────────────────────────────────

    private static string GenerateId()
        => $"h-{Guid.NewGuid().ToString("N")[..8]}";
}
