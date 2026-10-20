using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// An optional button that opens / closes the combobox popup — typically the "▾"
/// chevron at the end of the input. It is outside the tab order
/// (<c>tabindex="-1"</c>); clicking it keeps focus in the input.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BhComboboxButton<TValue> : BhComponentBase
{
    [CascadingParameter]
    private BhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhComboboxButtonRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhComboboxButtonRenderContext<TValue>>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => ComboboxContext?.IsOpen ?? false;
    private bool IsDisabled => ComboboxContext?.Disabled ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, static () => { }));
        builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, new BhComboboxButtonRenderContext<TValue> { IsOpen = IsOpen, Disabled = IsDisabled }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";
        attrs["tabindex"] = -1;
        if (GetAdditionalAttribute("aria-label") is null && GetAdditionalAttribute("aria-labelledby") is null)
            attrs["aria-label"] = "Show options";
        SetAriaBool(attrs, "aria-expanded", IsOpen);
        if (ComboboxContext is not null)
            attrs["aria-controls"] = ComboboxContext.OptionsId;
        if (IsDisabled) attrs["disabled"] = true;
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs _)
    {
        if (IsDisabled || ComboboxContext is null) return;
        await ComboboxContext.Combobox.FocusInputAsync();
        await ComboboxContext.Combobox.ToggleAsync();
    }
}
