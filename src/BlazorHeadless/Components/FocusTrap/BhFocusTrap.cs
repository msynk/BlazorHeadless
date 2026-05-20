using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A low-level headless component that traps keyboard focus within its container.
/// When enabled, Tab and Shift+Tab cycle through focusable elements inside the
/// trap boundary without escaping to the rest of the page.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Focus cycling</b> — Tab wraps from last to first; Shift+Tab wraps from first to last.</item>
///   <item><b>Initial focus</b> — Optionally focus a specific element when the trap activates.</item>
///   <item><b>Focus restoration</b> — Returns focus to the previously-focused element when the trap deactivates.</item>
///   <item><b>Dynamic enable/disable</b> — Toggle trapping on and off via the <see cref="Enabled"/> parameter.</item>
///   <item><b>Data attributes</b> — Emits <c>data-state="active"|"inactive"</c> for CSS hooks.</item>
/// </list>
///
/// <para>
/// Used internally by <see cref="BhDialog"/> but also useful standalone for custom
/// modal-like patterns, slide-over panels, or any UI that needs to constrain
/// keyboard navigation.
/// </para>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhFocusTrap Enabled="isActive"&gt;
///     &lt;input type="text" placeholder="First" /&gt;
///     &lt;button&gt;Action&lt;/button&gt;
///     &lt;input type="text" placeholder="Last" /&gt;
/// &lt;/BhFocusTrap&gt;
/// </code>
/// </summary>
public class BhFocusTrap : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    /// <summary>
    /// Whether the focus trap is currently active. When true, focus is constrained
    /// within this component's rendered element. Defaults to true.
    /// </summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional element reference that should receive initial focus when the trap
    /// activates. If not set, the first focusable element inside the container is used.
    /// </summary>
    [Parameter]
    public ElementReference? InitialFocus { get; set; }

    /// <summary>
    /// Optional element reference that should receive focus when the trap deactivates.
    /// If not set, focus returns to whatever element was active before the trap was enabled.
    /// </summary>
    [Parameter]
    public ElementReference? ReturnFocus { get; set; }

    /// <summary>
    /// Child content rendered inside the focus trap container.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool _wasEnabled;
    private int _lockHandle = -1;
    private ElementReference _containerRef;
    private bool _hasContainerRef;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Enabled && !_wasEnabled && _hasContainerRef)
        {
            _lockHandle = await Interop.FocusTrapLockAsync(
                _containerRef, InitialFocus, ReturnFocus);
            _wasEnabled = true;
        }
        else if (!Enabled && _wasEnabled)
        {
            await Interop.FocusTrapUnlockAsync(_lockHandle);
            _lockHandle = -1;
            _wasEnabled = false;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhFocusTrapContext { Enabled = Enabled };

        builder.OpenComponent<CascadingValue<BhFocusTrapContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            inner.AddElementReferenceCapture(30, CaptureContainerRef);

            if (Ref is not null)
                inner.AddElementReferenceCapture(31, Ref);

            if (ChildContent is not null)
                inner.AddContent(40, ChildContent);

            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, Enabled, "active", "inactive");
        return attrs;
    }

    private void CaptureContainerRef(ElementReference el)
    {
        _containerRef = el;
        _hasContainerRef = true;
    }

    // ── Cleanup ──────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_lockHandle > 0)
            await Interop.FocusTrapUnlockAsync(_lockHandle);
    }
}
