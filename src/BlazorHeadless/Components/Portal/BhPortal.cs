using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless component that renders its children into a different part of the
/// DOM tree — by default, the <see cref="BhPortalOutlet"/> placed in your root
/// layout. This is useful for tooltips, dropdowns, modals, and any UI that needs
/// to escape overflow/z-index stacking contexts.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>DOM teleportation</b> — Content is rendered at the outlet location, escaping stacking contexts.</item>
///   <item><b>Enable/disable</b> — Toggle portalling on and off via <see cref="Enabled"/>; when disabled, content renders in place.</item>
///   <item><b>PortalGroup support</b> — When nested inside a <see cref="BhPortalGroup"/>, content renders in place within the group (already escaped from the original stacking context).</item>
///   <item><b>Cleanup</b> — Content is automatically removed from the outlet when the component disposes.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup and
/// a <see cref="BhPortalOutlet"/> in your root layout.</b></para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhPortal&gt;
///     &lt;div class="tooltip"&gt;I render at the outlet (end of layout)!&lt;/div&gt;
/// &lt;/BhPortal&gt;
/// </code>
/// </summary>
public class BhPortal : ComponentBase, IDisposable
{
    [Inject] private BhPortalService PortalService { get; set; } = default!;

    /// <summary>
    /// Whether the portal is enabled. When true (default), content is teleported
    /// to the outlet (or rendered in place if inside a PortalGroup).
    /// When false, content renders in its natural DOM position.
    /// </summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Child content rendered inside the portal.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Receives the portal group context from an ancestor <see cref="BhPortalGroup"/>.
    /// When present, content renders in place (within the group) rather than teleporting to the outlet.
    /// </summary>
    [CascadingParameter]
    private BhPortalContext? GroupContext { get; set; }

    private string? _entryId;

    /// <summary>
    /// Whether this portal should teleport to the outlet (true) or render in place (false).
    /// Renders in place when: disabled, or inside a PortalGroup.
    /// </summary>
    private bool ShouldTeleport => Enabled && GroupContext is null;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnParametersSet()
    {
        if (ShouldTeleport && ChildContent is not null)
        {
            if (_entryId is null)
            {
                // Register with the service — content will be rendered by the outlet.
                _entryId = PortalService.Register(ChildContent);
            }
            else
            {
                // Already registered — update content.
                PortalService.Update(_entryId, ChildContent);
            }
        }
        else if (!ShouldTeleport && _entryId is not null)
        {
            // No longer teleporting — unregister so content renders in place.
            PortalService.Unregister(_entryId);
            _entryId = null;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ChildContent is null) return;

        if (!ShouldTeleport)
        {
            // Render in place: disabled, or inside a PortalGroup.
            builder.AddContent(0, ChildContent);
        }
        // When teleporting, content is rendered by the BhPortalOutlet — nothing here.
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_entryId is not null)
        {
            PortalService.Unregister(_entryId);
            _entryId = null;
        }
    }
}
