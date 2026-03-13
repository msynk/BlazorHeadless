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
///  - Styling hooks via data attributes: [data-disabled], [data-loading], etc.
///  - Attribute merging: class and style concatenate; everything else user-wins
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

    protected Dictionary<string, object> MergeAttributes(Dictionary<string, object> internalAttributes)
    {
        return AttributeUtilities.Merge(internalAttributes, AdditionalAttributes);
    }

    private static string GenerateId()
    {
        return $"h-{Guid.NewGuid().ToString("N")[..8]}";
    }
}
