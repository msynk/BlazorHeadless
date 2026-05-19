using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The trigger button that opens the listbox. Renders as
/// <c>&lt;button role="combobox"&gt;</c> with the proper ARIA wiring
/// (<c>aria-haspopup</c>, <c>aria-expanded</c>, <c>aria-controls</c>,
/// <c>aria-activedescendant</c>).
/// </summary>
/// <typeparam name="TValue">The option value type. Must match the parent <see cref="BhListbox{TValue}"/>.</typeparam>
public class BhListboxButton<TValue> : BhComponentBase
{
    [CascadingParameter]
    private BhListboxContext<TValue> BhListboxContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="BhListboxButtonRenderContext{TValue}"/>
    /// for state-driven rendering (chevron icons, badge counts, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment<BhListboxButtonRenderContext<TValue>>? ChildContent { get; set; }

    private ElementReference _elementRef;

    protected override string DefaultTag => "button";

    private BhListboxButtonRenderContext<TValue> RenderContext => new()
    {
        IsOpen = BhListboxContext?.IsOpen ?? false,
        Disabled = BhListboxContext?.Disabled ?? false,
        Value = BhListboxContext is null ? default : BhListboxContext.SingleValue,
        Values = BhListboxContext?.MultiValues ?? Array.Empty<TValue>(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhListboxContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            BhListboxContext?.RegisterButton(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = BhListboxContext?.IsOpen ?? false;
        var isDisabled = BhListboxContext?.Disabled ?? false;

        attrs["type"] = "button";
        attrs["role"] = "combobox";
        attrs["aria-haspopup"] = "listbox";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (BhListboxContext is not null)
        {
            attrs["aria-controls"] = BhListboxContext.OptionsId;

            if (isOpen && BhListboxContext.ActiveIndex >= 0)
                attrs["aria-activedescendant"] = BhListboxContext.GetOptionId(BhListboxContext.ActiveIndex);
        }

        if (isDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        return BhListboxContext?.ToggleAsync() ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // While open, the button receives all navigation keys (focus stays on the
        // button; aria-activedescendant drives screen-reader announcements). While
        // closed, only the "open" keys are meaningful.
        if (BhListboxContext?.IsOpen == true)
            return BhListboxContext.HandleOptionKeyDownAsync(BhListboxContext.ActiveIndex, args);
        return BhListboxContext?.HandleButtonKeyDownAsync(args) ?? Task.CompletedTask;
    }
}
