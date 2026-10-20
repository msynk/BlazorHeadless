using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The link that reveals a <see cref="BhHoverCard"/> on hover or focus. Renders as
/// <c>&lt;a&gt;</c> by default — give it an <c>href</c> so it remains a working link.
/// </summary>
public class BhHoverCardTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhHoverCardContext? HoverCardContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhHoverCardRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhHoverCardRenderContext>? Template { get; set; }

    protected override string DefaultTag => "a";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", HoverCardContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnter));
        builder.AddAttribute(31, "onpointerleave",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerLeave));
        builder.AddAttribute(32, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));
        builder.AddAttribute(33, "onblur",
            EventCallback.Factory.Create<FocusEventArgs>(this, HandleBlur));
        builder.AddAttribute(34, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, new BhHoverCardRenderContext { IsOpen = HoverCardContext?.IsOpen ?? false }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, HoverCardContext?.IsOpen ?? false);
        return attrs;
    }

    private Task HandlePointerEnter(PointerEventArgs args)
        => args.PointerType == "touch" ? Task.CompletedTask : HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;

    private Task HandlePointerLeave(PointerEventArgs args)
        => args.PointerType == "touch" ? Task.CompletedTask : HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;

    private Task HandleFocus(FocusEventArgs args) => HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;

    private Task HandleBlur(FocusEventArgs args) => HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
        => args.Key == "Escape" && HoverCardContext is { IsOpen: true }
            ? HoverCardContext.SetOpenAsync(false)
            : Task.CompletedTask;
}
