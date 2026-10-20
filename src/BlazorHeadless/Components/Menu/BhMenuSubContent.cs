using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The nested panel of a <see cref="BhMenuSub"/> (<c>role="menu"</c>). Positioned
/// next to its trigger — <c>"right start"</c> by default (<c>"left start"</c> in RTL),
/// flipping when there isn't room — unless <see cref="BhFloatingContentBase.Anchor"/>
/// is set explicitly.
/// </summary>
public class BhMenuSubContent : BhMenuContentBase
{
    [CascadingParameter]
    private BhMenuSub? Sub { get; set; }

    /// <summary>Gap (px) between the trigger item and the submenu when using the default anchor. Default: 0.</summary>
    [Parameter]
    public int SideOffset { get; set; }

    /// <summary>Alignment offset (px) when using the default anchor. Default: -4 (aligns first items).</summary>
    [Parameter]
    public int AlignOffset { get; set; } = -4;

    protected override bool IsOpen => Sub?.IsOpen ?? false;

    protected override string ContentId => Sub?.ContentId ?? ComponentId;

    protected override string? LabelledById => Sub?.TriggerId;

    protected override BhAnchorOptions? EffectiveAnchor => Anchor ?? new BhAnchorOptions
    {
        To = Sub?.Dir == BhDirection.Rtl ? "left start" : "right start",
        Gap = SideOffset,
        Offset = AlignOffset,
    };

    protected override void OnInitialized()
    {
        if (Sub is not null) Sub.Content = this;
    }

    private protected override BhMenuLevel CreateLevel(Action refresh)
        => new(Sub?.Parent?.Root ?? DetachedMenuRoot.Instance, Sub?.Parent, Sub, ContentId, Interop, refresh);

    private protected override BhMenuFocus ConsumeFocusIntent() => Sub?.ConsumeFocusIntent() ?? BhMenuFocus.None;

    private protected override BhMenuFocus PeekFocusIntent() => Sub?.PeekFocusIntent() ?? BhMenuFocus.None;

    protected override BhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BhLayerOptions { Escape = false, ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(Sub?.TriggerId);
        return new BhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = Sub?.TriggerId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = CreateFocusOptions(Sub?.Parent?.ContentId),
        };
    }

    protected override Task OnDismissAsync(BhDismissReason reason)
        => Sub?.CloseAsync(focusParent: false) ?? Task.CompletedTask;

    protected override Task OnPointerEnterContentAsync(PointerEventArgs args)
    {
        Sub?.CancelClose();
        return Task.CompletedTask;
    }

    internal void Refresh() => StateHasChanged();

    /// <summary>Moves keyboard focus into an already-open submenu.</summary>
    internal async Task EnterAsync(BhMenuFocus focus)
    {
        await Level.FocusAsync();
        await Level.Items.EnsureOrderAsync(Interop);
        Level.SetActive(focus == BhMenuFocus.Last ? Level.Items.Last() : Level.Items.First());
    }

    public override ValueTask DisposeAsync()
    {
        if (Sub?.Content == this) Sub.Content = null;
        return base.DisposeAsync();
    }
}
