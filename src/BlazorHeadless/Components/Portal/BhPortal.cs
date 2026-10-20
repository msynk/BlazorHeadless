using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Renders its children into a different part of the DOM — by default a portal
/// root at the end of <c>&lt;body&gt;</c> (or the <see cref="BhPortalOutlet"/>, or
/// <see cref="Target"/>) — to escape <c>overflow: hidden</c> and stacking contexts.
///
/// <para>
/// Unlike re-rendering content elsewhere, the content stays in its place in the
/// <em>component</em> tree: cascading values (contexts of dialogs, menus, forms),
/// event handlers and re-renders keep working. Only the DOM node is moved.
/// During prerendering (no JS yet) the content renders in place.
/// </para>
///
/// <code>
/// &lt;BhPortal&gt;
///     &lt;div class="toast"&gt;Saved!&lt;/div&gt;
/// &lt;/BhPortal&gt;
/// </code>
/// </summary>
public class BhPortal : BhComponentBase, IAsyncDisposable
{
    private int _handle;
    private string? _movedTo;

    [CascadingParameter]
    private BhPortalContext? GroupContext { get; set; }

    /// <summary>Whether to teleport the content. When false it renders in place. Default: true.</summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>Id or CSS selector of the destination container. Defaults to the enclosing <see cref="BhPortalGroup"/>, the <see cref="BhPortalOutlet"/>, or a root appended to <c>&lt;body&gt;</c>.</summary>
    [Parameter]
    public string? Target { get; set; }

    /// <summary>The teleported content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private string? EffectiveTarget => Target ?? GroupContext?.TargetId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var target = EffectiveTarget ?? string.Empty;
        if (Enabled && (_handle == 0 || _movedTo != target))
        {
            if (_handle > 0) await Interop.PortalRestoreAsync(_handle);
            _handle = await Interop.PortalMoveAsync(ComponentId, EffectiveTarget);
            _movedTo = target;
        }
        else if (!Enabled && _handle > 0)
        {
            await Interop.PortalRestoreAsync(_handle);
            _handle = 0;
            _movedTo = null;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["data-bh-portal"] = "";
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "display:contents";
        return attrs;
    }

    public async ValueTask DisposeAsync()
    {
        // Blazor removes the node wherever it lives; just drop the placeholder.
        if (_handle > 0)
            await Interop.PortalRestoreAsync(_handle);
        _handle = 0;
        GC.SuppressFinalize(this);
    }
}
