using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// An optional backdrop rendered behind the popover panel while it is open.
/// Clicking it closes the popover. Useful for mobile-style full-screen flyouts.
/// </summary>
public class BhPopoverBackdrop : BhComponentBase
{
    [CascadingParameter]
    private BhPopoverContext? PopoverContext { get; set; }

    /// <summary>Optional content inside the backdrop (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (PopoverContext is { IsOpen: false }) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        SetDataState(attrs, PopoverContext?.IsOpen ?? false);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => PopoverContext?.DismissAsync(BhDismissReason.PointerDownOutside) ?? Task.CompletedTask;
}
