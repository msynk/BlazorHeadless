using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace HeadlessUI.Blazor;

/// <summary>
/// The popup panel containing the combobox's options. Renders as
/// <c>&lt;ul role="listbox"&gt;</c> by default. Hidden via the HTML <c>hidden</c>
/// attribute when the combobox is closed.
///
/// <para>
/// When <see cref="Anchor"/> is set, the panel is automatically positioned
/// relative to the <see cref="HComboboxInput{TValue}"/> using the anchor positioning system.
/// </para>
/// </summary>
public class HComboboxOptions<TValue> : HeadlessComponentBase, IAsyncDisposable
{
    [Inject] private HeadlessUIInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private ComboboxContext<TValue> ComboboxContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="HComboboxOption{TValue}"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the dropdown relative to the
    /// <see cref="HComboboxInput{TValue}"/>. When set, the panel is positioned using
    /// fixed positioning and auto-updates on scroll/resize.
    /// </summary>
    [Parameter]
    public AnchorOptions? Anchor { get; set; }

    private ElementReference _elementRef;
    private int _anchorHandle;
    private bool _wasOpen;

    protected override string DefaultTag => "ul";

    private bool IsOpen => ComboboxContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.OptionsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

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

        if (ComboboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";

        SetDataState(attrs, IsOpen);
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            var inputId = ComboboxContext.InputId;
            var panelId = ComboboxContext.OptionsId;
            _anchorHandle = await Interop.AnchorStartByIdAsync(inputId, panelId, Anchor);
            _wasOpen = true;
        }
        else if (!IsOpen && _wasOpen)
        {
            await Interop.AnchorStopAsync(_anchorHandle);
            _anchorHandle = 0;
            _wasOpen = false;
        }
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
