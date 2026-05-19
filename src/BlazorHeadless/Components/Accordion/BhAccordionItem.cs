using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A single item within an <see cref="BhAccordion"/>. Represents one collapsible
/// section and provides the <see cref="BhAccordionItemContext"/> cascaded to its
/// <see cref="BhAccordionTrigger"/> and <see cref="BhAccordionContent"/> children.
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhAccordionItem Value="item-1"&gt;
///     &lt;BhAccordionTrigger&gt;Section 1&lt;/BhAccordionTrigger&gt;
///     &lt;BhAccordionContent&gt;Content for section 1.&lt;/BhAccordionContent&gt;
/// &lt;/BhAccordionItem&gt;
/// </code>
/// </summary>
public class BhAccordionItem : BhComponentBase
{
    [CascadingParameter]
    private BhAccordionContext BhAccordionContext { get; set; } = default!;

    /// <summary>The unique string identifier for this item within the accordion.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this item independently of the root accordion's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Child content — should contain an <see cref="BhAccordionTrigger"/> and an <see cref="BhAccordionContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => BhAccordionContext?.IsOpen(Value) ?? false;
    private bool IsDisabled => Disabled || (BhAccordionContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhAccordionItemContext>>(0);
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

    private BhAccordionItemContext CreateItemContext() => new()
    {
        Value = Value,
        IsOpen = IsOpen,
        Disabled = IsDisabled,
        TriggerId = $"{ComponentId}-trigger",
        ContentId = $"{ComponentId}-content",
        Toggle = () => BhAccordionContext?.Toggle(Value),
    };
}
