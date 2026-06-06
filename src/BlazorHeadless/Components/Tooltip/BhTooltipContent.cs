using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The popup that appears when the tooltip is open. Hidden via the HTML
/// <c>hidden</c> attribute when the tooltip is closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Renders with <c>role="tooltip"</c> for assistive technologies.</item>
///   <item>By default the content is hoverable — moving the pointer from the
///     trigger into the content keeps it open. Disable on the parent
///     <see cref="BhTooltip"/> via <c>DisableHoverableContent</c>.</item>
///   <item>Escape closes the tooltip while focus is inside the content.</item>
///   <item>Emits <c>data-state="closed" | "delayed-open" | "instant-open"</c>.</item>
/// </list>
///
/// <para>
/// When <see cref="Anchor"/> is set, the content is automatically positioned
/// relative to the trigger using the anchor positioning system (with flip and
/// shift to stay inside the viewport).
/// </para>
/// </summary>
public class BhTooltipContent : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhTooltipContext TooltipContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhTooltipRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhTooltipRenderContext>? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the content relative to the
    /// <see cref="BhTooltipTrigger"/>. When set, the content is positioned using
    /// fixed positioning and auto-updates on scroll / resize. Default placement:
    /// <c>"top"</c> with an 8px gap.
    /// </summary>
    [Parameter]
    public BhAnchorOptions? Anchor { get; set; }

    private int _anchorHandle;
    private bool _wasOpen;
    private bool _positioned;

    protected override string DefaultTag => "div";

    private bool IsOpen => TooltipContext?.IsOpen ?? false;

    private BhTooltipRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        DelayedOpen = TooltipContext?.DelayedOpen ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", TooltipContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        // Keep the content hidden while it's open but not yet positioned by the
        // anchor engine. This prevents a flash at the unpositioned location
        // before OnAfterRenderAsync runs the JS positioning.
        if (!IsOpen || (Anchor is not null && !_positioned))
            builder.AddAttribute(30, "hidden", true);

        builder.AddAttribute(31, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnter));
        builder.AddAttribute(32, "onpointerleave",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerLeave));
        builder.AddAttribute(33, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "tooltip";
        attrs["data-state"] = ResolveStateValue();
        return attrs;
    }

    private string ResolveStateValue()
    {
        if (TooltipContext is null || !TooltipContext.IsOpen) return "closed";
        return TooltipContext.DelayedOpen ? "delayed-open" : "instant-open";
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            var triggerId = TooltipContext.TriggerId;
            var contentId = TooltipContext.ContentId;
            _anchorHandle = await Interop.AnchorStartByIdAsync(triggerId, contentId, Anchor);
            _wasOpen = true;
            // JS has positioned and revealed the element; reflect that in the
            // render so a subsequent render won't re-add the hidden attribute.
            _positioned = true;
            StateHasChanged();
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.AnchorStopAsync(_anchorHandle);
            _anchorHandle = 0;
            _wasOpen = false;
            _positioned = false;
        }
    }

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        if (TooltipContext is null) return Task.CompletedTask;
        // Cancel any pending close — the user moved into hoverable content.
        return TooltipContext.DisableHoverableContent
            ? Task.CompletedTask
            : TooltipContext.ScheduleOpenAsync();
    }

    private Task HandlePointerLeave(PointerEventArgs args)
        => TooltipContext?.ScheduleCloseAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (TooltipContext is null) return Task.CompletedTask;
        if (args.Key == "Escape")
            return TooltipContext.SetOpenAsync(false);
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_anchorHandle > 0)
        {
            try
            {
                await Interop.AnchorStopAsync(_anchorHandle);
            }
            catch (JSDisconnectedException) { /* circuit gone */ }
        }
    }
}
