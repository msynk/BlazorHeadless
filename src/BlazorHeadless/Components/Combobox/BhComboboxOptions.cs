using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// The popup panel containing the combobox's options. Renders as
/// <c>&lt;ul role="listbox"&gt;</c> by default. Hidden via the HTML <c>hidden</c>
/// attribute when the combobox is closed.
///
/// <para>
/// When <see cref="Anchor"/> is set, the panel is automatically positioned
/// relative to the <see cref="BhComboboxInput{TValue}"/> using the anchor positioning system.
/// </para>
/// </summary>
public class BhComboboxOptions<TValue> : BhComponentBase, IAsyncDisposable
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    [CascadingParameter]
    private BhComboboxContext<TValue> BhComboboxContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="BhComboboxOption{TValue}"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Configures automatic positioning of the dropdown relative to the
    /// <see cref="BhComboboxInput{TValue}"/>. When set, the panel is positioned using
    /// fixed positioning and auto-updates on scroll/resize.
    /// </summary>
    [Parameter]
    public BhAnchorOptions? Anchor { get; set; }

    private ElementReference _elementRef;
    private int _anchorHandle;
    private bool _wasOpen;

    protected override string DefaultTag => "ul";

    private bool IsOpen => BhComboboxContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhComboboxContext?.OptionsId ?? ComponentId);
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

        if (BhComboboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";

        SetDataState(attrs, IsOpen);
        return attrs;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Anchor is null) return;

        if (IsOpen && !_wasOpen)
        {
            var inputId = BhComboboxContext.InputId;
            var panelId = BhComboboxContext.OptionsId;
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
