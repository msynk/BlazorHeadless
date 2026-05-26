using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The element that opens the tooltip on hover or focus. Renders as a
/// <c>&lt;button&gt;</c> by default; set <see cref="BhComponentBase.As"/> to
/// render as any element.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Pointer enter / focus schedule open after the configured delay.</item>
///   <item>Pointer leave / blur schedule close (with a short grace period for hoverable content).</item>
///   <item>Pressing Escape, Enter, or Space closes the tooltip immediately.</item>
///   <item>Emits <c>data-state="closed" | "delayed-open" | "instant-open"</c>.</item>
/// </list>
/// </summary>
public class BhTooltipTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhTooltipContext TooltipContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhTooltipRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhTooltipRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private BhTooltipRenderContext RenderContext => new()
    {
        IsOpen = TooltipContext?.IsOpen ?? false,
        DelayedOpen = TooltipContext?.DelayedOpen ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", TooltipContext?.TriggerId ?? ComponentId);
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
        builder.AddAttribute(35, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        builder.AddElementReferenceCapture(40, e =>
        {
            TooltipContext?.RegisterTrigger(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";

        if (TooltipContext is not null)
        {
            attrs["aria-describedby"] = TooltipContext.ContentId;
            attrs["data-state"] = ResolveStateValue();
        }

        return attrs;
    }

    private string ResolveStateValue()
    {
        if (TooltipContext is null || !TooltipContext.IsOpen) return "closed";
        return TooltipContext.DelayedOpen ? "delayed-open" : "instant-open";
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        // Touch pointers don't open tooltips on hover — they require focus / click.
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;
        return TooltipContext?.ScheduleOpenAsync() ?? Task.CompletedTask;
    }

    private Task HandlePointerLeave(PointerEventArgs args)
        => TooltipContext?.ScheduleCloseAsync() ?? Task.CompletedTask;

    private Task HandleFocus(FocusEventArgs args)
        => TooltipContext?.SetOpenAsync(true) ?? Task.CompletedTask;

    private Task HandleBlur(FocusEventArgs args)
        => TooltipContext?.SetOpenAsync(false) ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (TooltipContext is null) return Task.CompletedTask;
        // Per Radix: Escape, Enter, and Space all close an open tooltip without delay.
        if (args.Key is "Escape" or "Enter" or " ")
            return TooltipContext.SetOpenAsync(false);
        return Task.CompletedTask;
    }

    private Task HandleClick(MouseEventArgs args)
    {
        // Activating the trigger (button / link) hides the tooltip — the user
        // has acted on the underlying control.
        return TooltipContext?.SetOpenAsync(false) ?? Task.CompletedTask;
    }
}
