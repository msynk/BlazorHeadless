using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The link that opens the hover card on hover or focus. Renders as an
/// <c>&lt;a&gt;</c> by default (matching Radix), so it remains a real,
/// navigable link for keyboard and touch users; set
/// <see cref="BhComponentBase.As"/> to render as any element.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Pointer enter / focus schedule the card to open after <c>OpenDelay</c>.</item>
///   <item>Pointer leave / blur schedule the card to close after <c>CloseDelay</c>.</item>
///   <item>Touch pointers are ignored (and focus is suppressed on touch) so tapping the link still works.</item>
///   <item>Emits <c>data-state="open" | "closed"</c>.</item>
/// </list>
/// </summary>
public class BhHoverCardTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhHoverCardContext HoverCardContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhHoverCardRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhHoverCardRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "a";

    private BhHoverCardRenderContext RenderContext => new()
    {
        IsOpen = HoverCardContext?.IsOpen ?? false,
    };

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

        // Prevent focus (and the synthetic mouse events) on touch devices so the
        // hover card doesn't fight with tapping the link. Mirrors Radix.
        builder.AddAttribute(34, "ontouchstart",
            EventCallback.Factory.Create<TouchEventArgs>(this, () => { }));
        builder.AddEventPreventDefaultAttribute(35, "ontouchstart", true);

        builder.AddElementReferenceCapture(40, e =>
        {
            HoverCardContext?.RegisterTrigger(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, HoverCardContext?.IsOpen ?? false);
        return attrs;
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        // Touch pointers don't open the card on hover.
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;
        return HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;
    }

    private Task HandlePointerLeave(PointerEventArgs args)
    {
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;
        return HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
    }

    private Task HandleFocus(FocusEventArgs args)
        => HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;

    private Task HandleBlur(FocusEventArgs args)
        => HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
}
