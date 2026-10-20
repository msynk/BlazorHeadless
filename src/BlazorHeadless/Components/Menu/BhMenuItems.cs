using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// The popup panel containing the menu's items. Renders as <c>&lt;div role="menu"&gt;</c>
/// by default and is hidden via the HTML <c>hidden</c> attribute while closed.
///
/// <para>
/// Set <see cref="BhFloatingContentBase.Anchor"/> to position it relative to the
/// <see cref="BhMenuButton"/> (with flip / shift and the <c>--button-width</c>,
/// <c>--bh-available-height</c> … CSS variables) and
/// <see cref="BhFloatingContentBase.Portal"/> to render it at the end of <c>&lt;body&gt;</c>.
/// </para>
/// </summary>
public class BhMenuItems : BhMenuContentBase
{
    [CascadingParameter]
    private BhMenuContext? MenuContext { get; set; }

    protected override bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override string ContentId => MenuContext?.ItemsId ?? ComponentId;

    protected override string? LabelledById => MenuContext?.ButtonId;

    private protected override BhMenuLevel CreateLevel(Action refresh)
        => new((IBhMenuRoot?)MenuContext?.Menu ?? DetachedMenuRoot.Instance, null, null, ContentId, Interop, refresh);

    private protected override BhMenuFocus ConsumeFocusIntent() => MenuContext?.Menu.ConsumeFocusIntent() ?? BhMenuFocus.Container;

    private protected override BhMenuFocus PeekFocusIntent() => MenuContext?.Menu.PeekFocusIntent() ?? BhMenuFocus.Container;

    protected override BhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BhLayerOptions { ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(MenuContext?.ButtonId);
        return new BhFloatingRequest
        {
            ContentId = ContentId,
            ReferenceId = MenuContext?.ButtonId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
            Focus = CreateFocusOptions(MenuContext?.ButtonId),
        };
    }

    protected override Task OnDismissAsync(BhDismissReason reason)
        => MenuContext?.CloseAsync() ?? Task.CompletedTask;
}

/// <summary>Root used when a menu panel is rendered outside of a menu (design-time / tests).</summary>
internal sealed class DetachedMenuRoot : IBhMenuRoot
{
    public static readonly DetachedMenuRoot Instance = new();
    public bool IsOpen => false;
    public bool Disabled => false;
    public BhDirection Dir => BhDirection.Ltr;
    public string? TriggerId => null;
    public BhMenubarContext? Menubar => null;
    public Task CloseAsync() => Task.CompletedTask;
}
