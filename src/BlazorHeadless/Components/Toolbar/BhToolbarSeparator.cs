using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>A separator between toolbar controls (vertical in a horizontal toolbar).</summary>
public class BhToolbarSeparator : BhComponentBase
{
    [CascadingParameter]
    private BhRovingFocusContext? RovingContext { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var orientation = RovingContext?.Orientation == BhRovingOrientation.Vertical ? "horizontal" : "vertical";
        attrs["role"] = "separator";
        attrs["aria-orientation"] = orientation;
        SetDataValue(attrs, "orientation", orientation);
        return attrs;
    }
}
