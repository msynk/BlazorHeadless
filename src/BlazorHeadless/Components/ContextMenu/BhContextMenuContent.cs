using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The panel that pops out when the context menu is open. Renders as
/// <c>&lt;ul role="menu"&gt;</c> by default and is hidden via the HTML
/// <c>hidden</c> attribute while the menu is closed.
///
/// <para>
/// When open, the panel is positioned at the pointer coordinates captured by the
/// <see cref="BhContextMenuTrigger"/> using fixed positioning, with viewport
/// collision handling. It receives <c>data-side</c> and <c>data-align</c>
/// attributes describing the resolved placement so consumers can drive
/// direction-aware animations.
/// </para>
/// </summary>
public class BhContextMenuContent : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhContextMenuContext BhContextMenuContext { get; set; } = default!;

    /// <summary>
    /// Child content. Should contain one or more <see cref="BhContextMenuItem"/>,
    /// <see cref="BhContextMenuGroup"/>, <see cref="BhContextMenuLabel"/>, and
    /// <see cref="BhContextMenuSeparator"/> components.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Minimum space (px) the panel keeps from the viewport edges when positioned.
    /// Defaults to 8.
    /// </summary>
    [Parameter]
    public int CollisionPadding { get; set; } = 8;

    private bool _wasOpen;
    private ElementReference _elementRef;

    protected override string DefaultTag => "ul";

    private bool IsOpen => BhContextMenuContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhContextMenuContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

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
        attrs["aria-orientation"] = "vertical";

        if (BhContextMenuContext is not null && IsOpen && BhContextMenuContext.ActiveIndex >= 0)
            attrs["aria-activedescendant"] = BhContextMenuContext.GetItemId(BhContextMenuContext.ActiveIndex);

        SetDataState(attrs, IsOpen);
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (BhContextMenuContext is null) return;

        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            await Interop.ContextMenuPositionAsync(
                BhContextMenuContext.ContentId,
                BhContextMenuContext.X,
                BhContextMenuContext.Y,
                CollisionPadding);

            // Move focus to the panel so keyboard navigation works immediately.
            try
            {
                await _elementRef.FocusAsync();
            }
            catch (JSException) { /* element may have been detached */ }
        }
        else if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            await Interop.ContextMenuResetAsync(BhContextMenuContext.ContentId);
        }
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
        => BhContextMenuContext?.HandleContentKeyDownAsync(args) ?? Task.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        if (_wasOpen && BhContextMenuContext is not null)
            await Interop.ContextMenuResetAsync(BhContextMenuContext.ContentId);
    }
}
