using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A simple container for the set of <see cref="HTabPanel"/> elements. Rendering
/// is purely passive — the panels themselves manage their own visibility based on
/// the active tab index.
/// </summary>
public class HTabPanels : HeadlessComponentBase
{
    /// <summary>Child content. Should contain one <see cref="HTabPanel"/> per <see cref="HTab"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }
}
