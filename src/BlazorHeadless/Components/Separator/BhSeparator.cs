using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A visual or semantic divider. Semantic separators get <c>role="separator"</c>
/// and <c>aria-orientation</c>; set <see cref="Decorative"/> when it's purely visual.
/// Emits <c>data-orientation</c>.
/// </summary>
public class BhSeparator : BhComponentBase
{
    /// <summary>Horizontal (default) or vertical.</summary>
    [Parameter]
    public BhOrientation Orientation { get; set; } = BhOrientation.Horizontal;

    /// <summary>Purely visual: removed from the accessibility tree.</summary>
    [Parameter]
    public bool Decorative { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Decorative)
        {
            attrs["role"] = "none";
        }
        else
        {
            attrs["role"] = "separator";
            if (Orientation == BhOrientation.Vertical) attrs["aria-orientation"] = "vertical";
        }
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        return attrs;
    }
}
