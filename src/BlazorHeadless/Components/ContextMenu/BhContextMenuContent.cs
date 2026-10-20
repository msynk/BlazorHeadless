using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// The menu panel of a <see cref="BhContextMenu"/> (<c>role="menu"</c>), positioned
/// at the pointer with collision handling. Accepts the same children as
/// <see cref="BhMenuItems"/>: items, checkbox / radio items, groups, labels,
/// separators and submenus.
///
/// <para>Emits <c>data-side</c> / <c>data-align</c> describing the resolved placement.</para>
/// </summary>
public class BhContextMenuContent : BhMenuContentBase, IBhPointAnchoredContent
{
    [CascadingParameter]
    private BhContextMenuContext? MenuContext { get; set; }

    /// <summary>Minimum distance (px) kept between the menu and the viewport edges. Default: 8.</summary>
    [Parameter]
    public int CollisionPadding { get; set; } = 8;

    protected override bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override string ContentId => MenuContext?.ContentId ?? ComponentId;

    protected override BhAnchorOptions? EffectiveAnchor => Anchor ?? new BhAnchorOptions
    {
        To = "bottom start",
        Padding = CollisionPadding,
    };

    protected override void OnInitialized()
    {
        if (MenuContext is not null) MenuContext.Menu.Content = this;
    }

    private protected override BhMenuLevel CreateLevel(Action refresh)
        => new((IBhMenuRoot?)MenuContext?.Menu ?? DetachedMenuRoot.Instance, null, null, ContentId, Interop, refresh);

    private protected override BhMenuFocus ConsumeFocusIntent() => MenuContext?.Menu.ConsumeFocusIntent() ?? BhMenuFocus.Container;

    private protected override BhMenuFocus PeekFocusIntent() => MenuContext?.Menu.PeekFocusIntent() ?? BhMenuFocus.Container;

    protected override BhFloatingRequest CreateFloatingRequest() => new()
    {
        ContentId = ContentId,
        Point = (MenuContext?.X ?? 0, MenuContext?.Y ?? 0),
        Anchor = EffectiveAnchor,
        ArrowId = ArrowId,
        Portal = EffectivePortal,
        PortalTarget = PortalTarget,
        Layer = new BhLayerOptions { ParentHandle = ParentLayerHandle },
        Focus = CreateFocusOptions(null),
    };

    protected override Task OnDismissAsync(BhDismissReason reason)
        => MenuContext?.CloseAsync() ?? Task.CompletedTask;

    async Task IBhPointAnchoredContent.MoveToAsync(double x, double y)
    {
        Level.Reset();
        await Floating.SetPointAsync(x, y);
    }

    public override ValueTask DisposeAsync()
    {
        if (MenuContext?.Menu.Content == this) MenuContext.Menu.Content = null;
        return base.DisposeAsync();
    }
}
