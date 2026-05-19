using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The container for the set of <see cref="BhTab"/> elements. Renders a
/// <c>&lt;div role="tablist"&gt;</c> with the proper <c>aria-orientation</c>.
/// </summary>
public class BhTabList : BhComponentBase
{
    [CascadingParameter]
    private BhTabsContext BhTabsContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="BhTab"/> components.</summary>
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

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "tablist";
        attrs["aria-orientation"] = BhTabsContext?.Orientation == BhTabsOrientation.Vertical ? "vertical" : "horizontal";
        SetDataValue(attrs, "orientation", BhTabsContext?.Orientation == BhTabsOrientation.Vertical ? "vertical" : "horizontal");
        return attrs;
    }
}
