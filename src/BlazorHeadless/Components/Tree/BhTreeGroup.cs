using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The children of a <see cref="BhTreeItem{TValue}"/> (<c>&lt;ul role="group"&gt;</c>).
/// Hidden while the parent is collapsed (exit animations and
/// <c>--bh-content-height</c> supported); set <see cref="Unmount"/> to skip rendering
/// collapsed branches (large or lazily loaded trees).
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BhTreeGroup<TValue> : BhComponentBase
{
    [CascadingParameter]
    private BhTreeContext<TValue>? TreeContext { get; set; }

    [CascadingParameter]
    private BhTreeItem<TValue>? ParentItem { get; set; }

    /// <summary>Don't render children while collapsed. Note: hierarchical checkbox states only account for rendered items.</summary>
    [Parameter]
    public bool Unmount { get; set; }

    /// <summary>Child <see cref="BhTreeItem{TValue}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private bool IsOpen => ParentItem is null || (TreeContext?.IsExpanded(ParentItem.Value) ?? true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (Unmount && !IsOpen) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        attrs["data-bh-presence"] = "";
        if (!IsOpen) attrs["hidden"] = true;
        SetDataState(attrs, IsOpen);
        return attrs;
    }
}
