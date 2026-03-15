using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// A single item within an <see cref="HAccordion"/>. Represents one collapsible
/// section and provides the <see cref="AccordionItemContext"/> cascaded to its
/// <see cref="HAccordionTrigger"/> and <see cref="HAccordionContent"/> children.
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HAccordionItem Value="item-1"&gt;
///     &lt;HAccordionTrigger&gt;Section 1&lt;/HAccordionTrigger&gt;
///     &lt;HAccordionContent&gt;Content for section 1.&lt;/HAccordionContent&gt;
/// &lt;/HAccordionItem&gt;
/// </code>
/// </summary>
public class HAccordionItem : HeadlessComponentBase
{
    [CascadingParameter]
    private AccordionContext AccordionContext { get; set; } = default!;

    /// <summary>The unique string identifier for this item within the accordion.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this item independently of the root accordion's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Child content — should contain an <see cref="HAccordionTrigger"/> and an <see cref="HAccordionContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => AccordionContext?.IsOpen(Value) ?? false;
    private bool IsDisabled => Disabled || (AccordionContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<AccordionItemContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateItemContext());
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
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private AccordionItemContext CreateItemContext() => new()
    {
        Value = Value,
        IsOpen = IsOpen,
        Disabled = IsDisabled,
        TriggerId = $"{ComponentId}-trigger",
        ContentId = $"{ComponentId}-content",
        Toggle = () => AccordionContext?.Toggle(Value),
    };
}
