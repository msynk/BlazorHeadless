using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless label that auto-wires its <c>for</c> attribute to the sibling
/// input inside the same <see cref="BhField"/>. Renders as <c>&lt;label&gt;</c>
/// by default.
/// </summary>
public class BhLabel : BhComponentBase
{
    [CascadingParameter]
    private BhFieldContext? BhFieldContext { get; set; }

    /// <summary>Label text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "label";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhFieldContext?.LabelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        // Wire the label to the input via the "for" attribute.
        if (BhFieldContext is not null)
            attrs["for"] = BhFieldContext.InputId;

        SetDataFlag(attrs, "disabled", BhFieldContext?.Disabled ?? false);

        return attrs;
    }
}
