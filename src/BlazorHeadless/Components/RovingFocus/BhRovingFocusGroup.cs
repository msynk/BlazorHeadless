using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A generic roving-tabindex container: the whole group is one tab stop and the
/// arrow keys move between <see cref="BhRovingFocusItem"/>s (and any other
/// <see cref="BhRovingItemBase"/>). Use it to build custom composite widgets —
/// card grids, chip lists, segmented controls.
/// </summary>
public class BhRovingFocusGroup : BhComponentBase
{
    private BhRovingFocusContext? _context;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>Which arrow keys move focus. Default: horizontal.</summary>
    [Parameter]
    public BhRovingOrientation Orientation { get; set; } = BhRovingOrientation.Horizontal;

    /// <summary>Whether navigation wraps. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction. Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>The items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The group's state.</summary>
    protected BhRovingFocusContext Context => _context ??= new BhRovingFocusContext(Interop, BhId.New("bh-rove"));

    protected override void OnParametersSet()
    {
        Context.Orientation = Orientation;
        Context.Loop = Loop;
        Context.Dir = Dir ?? CascadedDir;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhRovingFocusContext>>(40);
        builder.AddComponentParameter(41, "Value", Context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Orientation != BhRovingOrientation.Both)
            SetDataValue(attrs, "orientation", Orientation == BhRovingOrientation.Vertical ? "vertical" : "horizontal");
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }
}
