using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace HeadlessUI.Blazor;

/// <summary>
/// The popup panel containing the listbox's options. Renders as
/// <c>&lt;ul role="listbox"&gt;</c> by default (override via <see cref="HeadlessComponentBase.As"/>).
///
/// <para>
/// Hidden via the HTML <c>hidden</c> attribute when the listbox is closed,
/// matching the behaviour of the other show/hide primitives in this library.
/// Consumers can drive open/close transitions via the <c>data-state</c> hook.
/// </para>
///
/// <para>
/// When <see cref="Anchor"/> is set, the panel is automatically positioned
/// relative to the <see cref="HListboxButton{TValue}"/> using the anchor positioning system.
/// </para>
/// </summary>
public class HListboxOptions<TValue> : HeadlessComponentBase, IAsyncDisposable
{
    [Inject] private HeadlessUIInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private ListboxContext<TValue> ListboxContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="HListboxOption{TValue}"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the dropdown relative to the
    /// <see cref="HListboxButton{TValue}"/>. When set, the panel is positioned using
    /// fixed positioning and auto-updates on scroll/resize.
    /// </summary>
    [Parameter]
    public AnchorOptions? Anchor { get; set; }

    private ElementReference _elementRef;
    private int _anchorHandle;
    private bool _wasOpen;

    protected override string DefaultTag => "ul";

    private bool IsOpen => ListboxContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ListboxContext?.OptionsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        // Per ARIA, keyboard nav happens on the listbox panel itself when it has focus.
        builder.AddAttribute(35, "onkeydown",
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

        attrs["role"] = "listbox";

        if (ListboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";

        // The panel itself isn't focusable directly; assistive tech follows
        // aria-activedescendant on the button. We add tabindex=-1 so programmatic
        // focus calls work but the panel isn't in the tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, IsOpen);

        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            var buttonId = ListboxContext.ButtonId;
            var panelId = ListboxContext.OptionsId;
            _anchorHandle = await Interop.AnchorStartByIdAsync(buttonId, panelId, Anchor);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.AnchorStopAsync(_anchorHandle);
            _anchorHandle = 0;
            _wasOpen = false;
        }
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // Forward to the listbox root, which handles keyboard nav holistically.
        return ListboxContext?.HandleOptionKeyDownAsync(ListboxContext.ActiveIndex, args)
            ?? Task.CompletedTask;
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
