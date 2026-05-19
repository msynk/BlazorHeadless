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
/// <typeparam name="TValue">The option value type. Must match the parent <see cref="HListbox{TValue}"/>.</typeparam>
public class HListboxButton<TValue> : HeadlessComponentBase
{
    [CascadingParameter]
    private ListboxContext<TValue> ListboxContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="ListboxButtonRenderContext{TValue}"/>
    /// for state-driven rendering (chevron icons, badge counts, etc.).
    /// </summary>
    [Parameter]
    public RenderFragment<ListboxButtonRenderContext<TValue>>? ChildContent { get; set; }

    private ElementReference _elementRef;

    protected override string DefaultTag => "button";

    private ListboxButtonRenderContext<TValue> RenderContext => new()
    {
        IsOpen = ListboxContext?.IsOpen ?? false,
        Disabled = ListboxContext?.Disabled ?? false,
        Value = ListboxContext is null ? default : ListboxContext.SingleValue,
        Values = ListboxContext?.MultiValues ?? Array.Empty<TValue>(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ListboxContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            ListboxContext?.RegisterButton(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = ListboxContext?.IsOpen ?? false;
        var isDisabled = ListboxContext?.Disabled ?? false;

        attrs["type"] = "button";
        attrs["role"] = "combobox";
        attrs["aria-haspopup"] = "listbox";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (ListboxContext is not null)
        {
            attrs["aria-controls"] = ListboxContext.OptionsId;

            if (isOpen && ListboxContext.ActiveIndex >= 0)
                attrs["aria-activedescendant"] = ListboxContext.GetOptionId(ListboxContext.ActiveIndex);
        }

        if (isDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        return ListboxContext?.ToggleAsync() ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // While open, the button receives all navigation keys (focus stays on the
        // button; aria-activedescendant drives screen-reader announcements). While
        // closed, only the "open" keys are meaningful.
        if (ListboxContext?.IsOpen == true)
            return ListboxContext.HandleOptionKeyDownAsync(ListboxContext.ActiveIndex, args);
        return ListboxContext?.HandleButtonKeyDownAsync(args) ?? Task.CompletedTask;
    }
}
