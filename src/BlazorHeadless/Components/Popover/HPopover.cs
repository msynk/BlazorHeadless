using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Popover — a non-modal floating panel that can contain
/// any content (navigation links, forms, rich overlays).
///
/// <para><b>Key differences from <see cref="HDialog"/>:</b></para>
/// <list type="bullet">
///   <item><b>No focus trap</b> — Tab navigates freely inside and out of the panel.</item>
///   <item><b>No scroll lock</b> — the page remains scrollable.</item>
///   <item><b>No inert background</b> — the rest of the page stays interactive.</item>
///   <item><b>Focus moves into the panel</b> on open and returns to the button on close.</item>
/// </list>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Escape closes</b> from anywhere inside the panel.</item>
///   <item><b>Click-outside closes</b> via a transparent full-viewport overlay.</item>
///   <item><b>Group coordination</b> — inside an <see cref="HPopoverGroup"/>, opening one popover closes the others.</item>
///   <item><b>Uncontrolled and controlled</b> — seed with <see cref="DefaultOpen"/> or drive with <see cref="Open"/> + <see cref="OnOpenChange"/>.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c> on root, button, and panel.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
/// </summary>
public class HPopover : HeadlessComponentBase, IDisposable
{
    [Inject] private BlazorHeadlessInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private PopoverGroupContext? GroupContext { get; set; }

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Initial open state (uncontrolled). Ignored when <see cref="Open"/> is supplied.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Controlled open state. When non-null, <see cref="OnOpenChange"/> must update this value.</summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Child content. Should contain an <see cref="HPopoverButton"/> and an <see cref="HPopoverPanel"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isOpen;
    private bool _wasOpen;
    private ElementReference _panelRef;
    private ElementReference _buttonRef;
    private bool _hasPanelRef;
    private IJSObjectReference? _previousFocus;

    private bool IsOpen => Open ?? _isOpen;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Open is null)
            _isOpen = DefaultOpen;
        GroupContext?.Register(this);
    }

    public void Dispose()
    {
        GroupContext?.Unregister(this);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (IsOpen && !_wasOpen && _hasPanelRef)
        {
            _previousFocus = await Interop.PopoverFocusPanelAsync(_panelRef);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.PopoverRestoreFocusAsync(_previousFocus);
            _previousFocus = null;
            _wasOpen = false;
            _hasPanelRef = false;
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

        builder.OpenComponent<CascadingValue<ICloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (ICloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<PopoverContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());

                if (Ref is not null)
                    inner.AddElementReferenceCapture(30, Ref);

                inner.AddContent(40, ChildContent);

                // Click-outside overlay — only present while open.
                if (IsOpen)
                {
                    inner.OpenElement(50, "div");
                    inner.AddAttribute(51, "data-blazor-headless-overlay", true);
                    inner.AddAttribute(52, "style",
                        "position:fixed;inset:0;z-index:30;background:transparent;");
                    inner.AddAttribute(53, "onclick",
                        EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await CloseAsync()));
                    inner.CloseElement();
                }

                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    // ── Context ───────────────────────────────────────────────────────────────

    private PopoverContext CreateContext()
    {
        var ctx = new PopoverContext(
            isOpen: IsOpen,
            baseId: ComponentId,
            openAsync: OpenAsync,
            closeAsync: CloseAsync,
            toggleAsync: ToggleAsync,
            registerPanel: RegisterPanelRef,
            registerButton: RegisterButtonRef);
        ctx.SetButtonRef(_buttonRef);
        return ctx;
    }

    private void RegisterPanelRef(ElementReference panel)
    {
        _panelRef = panel;
        _hasPanelRef = true;
    }

    private void RegisterButtonRef(ElementReference button) => _buttonRef = button;

    // ── Open / Close / Toggle ─────────────────────────────────────────────────

    internal async Task OpenAsync()
    {
        if (IsOpen) return;
        SetOpen(true);
        // Notify the group so siblings close.
        if (GroupContext is not null)
            await GroupContext.CloseOthersAsync(this);
    }

    internal async Task CloseAsync()
    {
        if (!IsOpen) return;
        SetOpen(false);
        await Task.CompletedTask;
    }

    /// <summary>Called by <see cref="HPopoverGroup"/> to close this popover when a sibling opens.</summary>
    internal Task CloseFromGroupAsync() => CloseAsync();

    private async Task ToggleAsync()
    {
        if (IsOpen) await CloseAsync();
        else await OpenAsync();
    }

    private void SetOpen(bool value)
    {
        if (Open is null)
            _isOpen = value;
        _ = OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
