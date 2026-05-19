using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// An optional backdrop rendered behind the popover panel. Clicking it closes
/// the popover. Useful for mobile-style full-screen flyouts.
///
/// <para>
/// For most desktop popovers the click-outside overlay built into
/// <see cref="HPopover"/> is sufficient and this component isn't needed.
/// </para>
/// </summary>
public class HPopoverBackdrop : HeadlessComponentBase
{
    [CascadingParameter]
    private PopoverContext PopoverContext { get; set; } = default!;

    /// <summary>Optional content inside the backdrop (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, async _ =>
            {
                if (PopoverContext is not null)
                    await PopoverContext.CloseAsync();
            }));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

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
}
