using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The floating content panel for an <see cref="BhPopover"/>. Hidden via the HTML
/// <c>hidden</c> attribute when the popover is closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Receives focus on open (first focusable element, or the panel itself).</item>
///   <item>Escape closes the popover from anywhere inside the panel.</item>
///   <item>Emits <c>data-state="open|closed"</c> for CSS-driven transitions.</item>
/// </list>
///
/// <para>
/// When <see cref="Anchor"/> is set, the panel is automatically positioned
/// relative to the <see cref="BhPopoverButton"/> using the anchor positioning system.
/// </para>
/// </summary>
public class BhPopoverPanel : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhPopoverContext BhPopoverContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhPopoverRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhPopoverRenderContext>? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the panel relative to the
    /// <see cref="BhPopoverButton"/>. When set, the panel is positioned using
    /// fixed positioning and auto-updates on scroll/resize.
    /// </summary>
    [Parameter]
    public BhAnchorOptions? Anchor { get; set; }

    private ElementReference _elementRef;
    private int _anchorHandle;
    private bool _wasOpen;
    private bool _positioned;

    protected override string DefaultTag => "div";

    private bool IsOpen => BhPopoverContext?.IsOpen ?? false;

    private BhPopoverRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = BhPopoverContext?.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhPopoverContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        // Keep the panel hidden while it's open but not yet positioned by the
        // anchor engine. This prevents a flash at the unpositioned location
        // before OnAfterRenderAsync runs the JS positioning.
        if (!IsOpen || (Anchor is not null && !_positioned))
            builder.AddAttribute(30, "hidden", true);

        builder.AddAttribute(35, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            BhPopoverContext?.RegisterPanel(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        // tabindex=-1 so the panel is programmatically focusable when it has no
        // focusable descendants, but isn't in the natural tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, IsOpen);

        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            var buttonId = BhPopoverContext.ButtonId;
            var panelId = BhPopoverContext.PanelId;
            _anchorHandle = await Interop.AnchorStartByIdAsync(buttonId, panelId, Anchor);
            _wasOpen = true;
            // JS has positioned and revealed the panel; reflect that in the
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

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && BhPopoverContext is not null)
            await BhPopoverContext.CloseAsync();
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
