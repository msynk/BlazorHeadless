using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless description element linked to the sibling input via
/// <c>aria-describedby</c>. Renders as <c>&lt;p&gt;</c> by default.
///
/// <para>
/// Place inside an <see cref="BhField"/> alongside an <see cref="BhInput"/>,
/// <see cref="BhTextarea"/>, or <see cref="BhSelect"/> — the ARIA wiring is automatic.
/// </para>
/// </summary>
public class BhDescription : BhComponentBase
{
    [CascadingParameter]
    private BhFieldContext? BhFieldContext { get; set; }

    /// <summary>Description text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "p";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhFieldContext?.DescriptionId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }
}
