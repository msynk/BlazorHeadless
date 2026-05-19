using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The popup panel containing the menu's items. Renders as
/// <c>&lt;ul role="menu"&gt;</c> by default. Hidden via the HTML <c>hidden</c>
/// attribute when the menu is closed.
///
/// <para>
/// When <see cref="Anchor"/> is set, the panel is automatically positioned
/// relative to the <see cref="BhMenuButton"/> using the anchor positioning system.
/// The panel receives CSS custom properties <c>--button-width</c>, <c>--anchor-gap</c>,
/// <c>--anchor-offset</c>, and <c>--anchor-padding</c> for styling.
/// </para>
/// </summary>
public class BhMenuItems : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhMenuContext BhMenuContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="BhMenuItem"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the dropdown relative to the
    /// <see cref="BhMenuButton"/>. When set, the panel is positioned using
    /// fixed positioning and auto-updates on scroll/resize.
    ///
    /// <para><b>Usage:</b></para>
    /// <code>
    /// &lt;BhMenuItems Anchor="@(new BhAnchorOptions { To = "bottom start", Gap = 4 })"&gt;
    /// </code>
    /// Or with the implicit string conversion:
    /// <code>
    /// &lt;BhMenuItems Anchor="@((BhAnchorOptions)"bottom start")"&gt;
    /// </code>
    /// </summary>
    [Parameter]
    public BhAnchorOptions? Anchor { get; set; }

    private ElementReference _elementRef;
    private int _anchorHandle;
    private bool _wasOpen;

    protected override string DefaultTag => "ul";

    private bool IsOpen => BhMenuContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhMenuContext?.ItemsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            Ref?.Invoke(e);
        });

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menu";
        attrs["tabindex"] = -1;
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            // Use the deterministic button ID to find the reference element in JS.
            // This is more reliable than passing ElementReference which may not
            // be captured yet on the first open.
            var buttonId = BhMenuContext.ButtonId;
            var panelId = BhMenuContext.ItemsId;
            _anchorHandle = await Interop.AnchorStartByIdAsync(buttonId, panelId, Anchor);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            // Stop anchor positioning when the menu closes
            await Interop.AnchorStopAsync(_anchorHandle);
            _anchorHandle = 0;
            _wasOpen = false;
        }
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
