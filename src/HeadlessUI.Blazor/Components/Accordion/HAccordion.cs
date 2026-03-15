using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless Accordion root component that manages which items are open, wires up
/// accessibility attributes, and provides zero visual opinion.
///
/// <para><b>Key features (inspired by Radix UI / Ark UI / Headless UI patterns):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Single or Multiple</b> — set <see cref="Type"/> to control whether
///     only one item can be open at a time or many simultaneously.
///   </item>
///   <item>
///     <b>Uncontrolled and controlled</b> — seed initial state via
///     <see cref="DefaultValue"/> / <see cref="DefaultValues"/> (uncontrolled),
///     or drive state externally via <see cref="Value"/> / <see cref="Values"/>
///     with the matching change callbacks.
///   </item>
///   <item>
///     <b>Data-attribute styling hooks</b> — emits <c>data-disabled</c> and
///     <c>data-orientation="vertical"</c> for CSS selectors.
///   </item>
///   <item>
///     <b>Polymorphic rendering</b> — renders a <c>&lt;div&gt;</c> by default;
///     override with <see cref="HeadlessComponentBase.As"/>.
///   </item>
/// </list>
///
/// <para><b>Usage (uncontrolled, single):</b></para>
/// <code>
/// &lt;HAccordion DefaultValue="item-1"&gt;
///     &lt;HAccordionItem Value="item-1"&gt;
///         &lt;HAccordionTrigger&gt;Section 1&lt;/HAccordionTrigger&gt;
///         &lt;HAccordionContent&gt;Content for section 1.&lt;/HAccordionContent&gt;
///     &lt;/HAccordionItem&gt;
/// &lt;/HAccordion&gt;
/// </code>
/// </summary>
public class HAccordion : HeadlessComponentBase
{
    private HashSet<string> _openItems = new(StringComparer.Ordinal);
    private bool _initialized;

    /// <summary>
    /// Controls the open-item selection model. Defaults to <see cref="AccordionType.Single"/>.
    /// </summary>
    [Parameter]
    public AccordionType Type { get; set; } = AccordionType.Single;

    /// <summary>Whether all items are disabled regardless of item-level settings.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    // ── Uncontrolled initial values ──────────────────────────────────────────

    /// <summary>
    /// The item value that is open by default (uncontrolled, <see cref="AccordionType.Single"/>).
    /// Ignored when <see cref="Value"/> is supplied.
    /// </summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// The item values that are open by default (uncontrolled, <see cref="AccordionType.Multiple"/>).
    /// Ignored when <see cref="Values"/> is supplied.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    // ── Controlled values ────────────────────────────────────────────────────

    /// <summary>
    /// Controlled open value for <see cref="AccordionType.Single"/>. When set the component
    /// operates in controlled mode and <see cref="OnValueChange"/> must update this value.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>
    /// Controlled open values for <see cref="AccordionType.Multiple"/>. When set the component
    /// operates in controlled mode and <see cref="OnValuesChange"/> must update this collection.
    /// </summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>
    /// Fires when the open item changes (controlled, <see cref="AccordionType.Single"/>).
    /// The argument is the newly-opened item value, or <c>null</c> when the item is closed.
    /// </summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>
    /// Fires when the set of open items changes (controlled, <see cref="AccordionType.Multiple"/>).
    /// </summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> OnValuesChange { get; set; }

    /// <summary>Child content — should contain one or more <see cref="HAccordionItem"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        // Seed uncontrolled state once from defaults on first render.
        if (Value is null && Values is null)
        {
            _openItems = new HashSet<string>(StringComparer.Ordinal);
            if (DefaultValue is not null)
                _openItems.Add(DefaultValue);
            if (DefaultValues is not null)
                foreach (var v in DefaultValues)
                    _openItems.Add(v);
        }
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        if (!_initialized) return;

        // Re-sync internal state when controlled values are updated externally.
        if (Value is not null)
            _openItems = new HashSet<string>(StringComparer.Ordinal) { Value };
        else if (Values is not null)
            _openItems = new HashSet<string>(Values, StringComparer.Ordinal);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AccordionContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", "vertical");
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Context and state management ─────────────────────────────────────────

    private AccordionContext CreateContext() => new(
        isOpen: v => _openItems.Contains(v),
        toggle: ToggleItem,
        disabled: Disabled,
        type: Type);

    private void ToggleItem(string value)
    {
        if (Disabled) return;

        if (_openItems.Contains(value))
        {
            _openItems.Remove(value);
        }
        else
        {
            if (Type == AccordionType.Single)
                _openItems.Clear();
            _openItems.Add(value);
        }

        // Fire change callbacks for controlled usage.
        if (Type == AccordionType.Single)
            _ = OnValueChange.InvokeAsync(_openItems.FirstOrDefault());
        else
            _ = OnValuesChange.InvokeAsync(_openItems.ToArray() as IReadOnlyCollection<string>);

        StateHasChanged();
    }
}
