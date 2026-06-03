using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The floating card that appears when the hover card is open. Hidden via the
/// HTML <c>hidden</c> attribute when closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>The content is hoverable — moving the pointer from the trigger into
///     the content keeps the card open; leaving it schedules a close.</item>
///   <item>Pressing Escape while focus is inside the content closes the card.</item>
///   <item>Emits <c>data-state="open" | "closed"</c>.</item>
/// </list>
///
/// <para>
/// When <see cref="Anchor"/> is set, the content is automatically positioned
/// relative to the trigger using the anchor positioning system (with flip and
/// shift to stay inside the viewport). Default placement: <c>"bottom"</c>.
/// </para>
/// </summary>
public class BhHoverCardContent : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhHoverCardContext HoverCardContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhHoverCardRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhHoverCardRenderContext>? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the content relative to the
    /// <see cref="BhHoverCardTrigger"/>. When set, the content is positioned with
    /// fixed positioning and auto-updates on scroll / resize. Default placement:
    /// <c>"bottom"</c> with an 8px gap.
    /// </summary>
    [Parameter]
    public BhAnchorOptions? Anchor { get; set; }

    private int _anchorHandle;
    private bool _wasOpen;

    protected override string DefaultTag => "div";

    private bool IsOpen => HoverCardContext?.IsOpen ?? false;

    private BhHoverCardRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", HoverCardContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
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
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            _anchorHandle = await Interop.AnchorStartByIdAsync(
                HoverCardContext.TriggerId, HoverCardContext.ContentId, Anchor);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.AnchorStopAsync(_anchorHandle);
            _anchorHandle = 0;
            _wasOpen = false;
        }
    }

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;
        // Cancel any pending close — the pointer moved into hoverable content.
        return HoverCardContext?.ScheduleOpenAsync() ?? Task.CompletedTask;
    }

    private Task HandlePointerLeave(PointerEventArgs args)
    {
        if (string.Equals(args.PointerType, "touch", StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;
        return HoverCardContext?.ScheduleCloseAsync() ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (HoverCardContext is null) return Task.CompletedTask;
        if (args.Key == "Escape")
            return HoverCardContext.SetOpenAsync(false);
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
