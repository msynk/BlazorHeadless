using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// An optional toggle button that opens / closes the combobox panel — typically
/// used for the "▾" chevron at the right edge of the input. Renders as a
/// <c>&lt;button&gt;</c> by default.
///
/// <para>
/// Clicking the button toggles the panel. <c>tabindex="-1"</c> by default so it's
/// outside the document tab order — focus and keyboard handling stay on the input.
/// </para>
/// </summary>
public class HComboboxButton<TValue> : HeadlessComponentBase
{
    [CascadingParameter]
    private ComboboxContext<TValue> ComboboxContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="ComboboxButtonRenderContext{TValue}"/>.</summary>
    [Parameter]
    public RenderFragment<ComboboxButtonRenderContext<TValue>>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private ComboboxButtonRenderContext<TValue> RenderContext => new()
    {
        IsOpen = ComboboxContext?.IsOpen ?? false,
        Disabled = ComboboxContext?.Disabled ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["type"] = "button";
        attrs["tabindex"] = -1;
        attrs["aria-label"] = "Toggle options";

        var isOpen = ComboboxContext?.IsOpen ?? false;
        var isDisabled = ComboboxContext?.Disabled ?? false;

        if (isDisabled) attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        return ComboboxContext?.ToggleAsync() ?? Task.CompletedTask;
    }
}
