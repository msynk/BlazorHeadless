using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Renders all portalled content from <see cref="BhPortal"/> components that are
/// not inside a <see cref="BhPortalGroup"/>. Place this component once in your
/// root layout (e.g. <c>MainLayout.razor</c>) so that portals can teleport their
/// content here, escaping overflow and z-index stacking contexts.
///
/// <para><b>Usage (in MainLayout.razor):</b></para>
/// <code>
/// @inherits LayoutComponentBase
///
/// &lt;div class="page"&gt;
///     @Body
/// &lt;/div&gt;
///
/// &lt;BhPortalOutlet /&gt;
/// </code>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
/// </summary>
public class BhPortalOutlet : ComponentBase, IDisposable
{
    [Inject] private BhPortalService PortalService { get; set; } = default!;

    private bool _disposed;

    protected override void OnInitialized()
    {
        PortalService.Subscribe(OnPortalsChanged);
    }

    private void OnPortalsChanged()
    {
        if (_disposed) return;
        _ = InvokeAsync(StateHasChanged);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var entries = PortalService.GetEntries();
        if (entries.Count == 0) return;

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-headlessui-portal-root", "");
        builder.AddAttribute(2, "style", "display:contents;");

        int seq = 10;
        foreach (var entry in entries)
        {
            builder.AddContent(seq++, entry.Content);
        }

        builder.CloseElement();
    }

    public void Dispose()
    {
        _disposed = true;
        PortalService.Unsubscribe();
    }
}
