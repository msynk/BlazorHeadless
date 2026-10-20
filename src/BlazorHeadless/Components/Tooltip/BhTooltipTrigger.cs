using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The element that shows the tooltip on hover / keyboard focus. Renders as a
/// <c>&lt;button&gt;</c> by default (use <c>As</c> for links or other focusable
/// elements) and is described by the tooltip through <c>aria-describedby</c>.
/// </summary>
public class BhTooltipTrigger : BhComponentBase
{
    private bool _pointerDown;

    [CascadingParameter]
    private BhTooltipContext? TooltipContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhTooltipRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhTooltipRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", TooltipContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnter));
        builder.AddAttribute(31, "onpointerleave",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerLeave));
        builder.AddAttribute(32, "onpointerdown",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerDown));
        builder.AddAttribute(33, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(34, "onblur",
            EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));
        builder.AddAttribute(35, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddAttribute(36, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, new BhTooltipRenderContext
            {
                IsOpen = TooltipContext?.IsOpen ?? false,
                DelayedOpen = TooltipContext?.DelayedOpen ?? false,
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";
        if (TooltipContext is not null)
        {
            if (TooltipContext.HasContent)
                BhAttributeUtilities.AppendIdRefs(attrs, "aria-describedby", TooltipContext.ContentId);
            attrs["data-state"] = TooltipContext.DataState;
        }
        return attrs;
    }

    private Task HandlePointerEnter(PointerEventArgs args)
        => args.PointerType == "touch" ? Task.CompletedTask : TooltipContext?.ScheduleOpenAsync() ?? Task.CompletedTask;

    private Task HandlePointerLeave(PointerEventArgs args)
    {
        _pointerDown = false;
        return TooltipContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
    }

    private void HandlePointerDown(PointerEventArgs args) => _pointerDown = true;

    private Task HandleFocus(FocusEventArgs args)
    {
        // Focus caused by a click shouldn't pop a tooltip over what was just clicked.
        if (_pointerDown) return Task.CompletedTask;
        return TooltipContext?.SetOpenAsync(true) ?? Task.CompletedTask;
    }

    private Task HandleBlur(FocusEventArgs args)
    {
        _pointerDown = false;
        return TooltipContext?.SetOpenAsync(false) ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && TooltipContext is { IsOpen: true }
            ? TooltipContext.SetOpenAsync(false)
            : Task.CompletedTask;

    private Task HandleClick(MouseEventArgs args)
        => TooltipContext is { IsOpen: true } ? TooltipContext.SetOpenAsync(false) : Task.CompletedTask;
}
