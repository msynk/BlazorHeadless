using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The text input that drives the combobox query. Renders as
/// <c>&lt;input role="combobox"&gt;</c> with all the ARIA wiring for the
/// list-autocomplete pattern (<c>aria-haspopup</c>, <c>aria-expanded</c>,
/// <c>aria-controls</c>, <c>aria-activedescendant</c>, <c>aria-autocomplete="list"</c>).
///
/// <para>
/// The input value is two-way bound through the parent <see cref="HCombobox{TValue}"/>
/// — typing fires the parent's <c>OnQueryChange</c>, and selecting an option writes
/// the formatted display value back here.
/// </para>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class HComboboxInput<TValue> : HeadlessComponentBase
{
    [CascadingParameter]
    private ComboboxContext<TValue> ComboboxContext { get; set; } = default!;

    /// <summary>Optional placeholder text.</summary>
    [Parameter]
    public string? Placeholder { get; set; }

    private ElementReference _elementRef;

    protected override string DefaultTag => "input";

    /// <summary>The current value rendered in the input — driven by the parent combobox.</summary>
    private string CurrentInputValue
    {
        get
        {
            if (ComboboxContext is null) return string.Empty;

            // While the panel is open and the user is typing, show the live query.
            // While closed (or in multi-select), show the formatted selected value.
            if (ComboboxContext.IsOpen)
                return ComboboxContext.Query;

            if (ComboboxContext.Multiple)
                return string.Empty;

            return ComboboxContext.DisplayValue(ComboboxContext.SingleValue);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "value", CurrentInputValue);

        builder.AddAttribute(31, "oninput",
            EventCallback.Factory.Create<ChangeEventArgs>(this, HandleInput));
        builder.AddAttribute(32, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddAttribute(33, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, HandleFocus));

        if (!string.IsNullOrEmpty(Placeholder))
            builder.AddAttribute(34, "placeholder", Placeholder);

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            ComboboxContext?.SetInputRef(e);
            Ref?.Invoke(e);
        });

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["type"] = "text";
        attrs["role"] = "combobox";
        attrs["aria-autocomplete"] = "list";
        attrs["aria-haspopup"] = "listbox";

        var isOpen = ComboboxContext?.IsOpen ?? false;
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        // Sensible defaults that prevent browser interference.
        if (!attrs.ContainsKey("autocomplete")) attrs["autocomplete"] = "off";
        if (!attrs.ContainsKey("autocorrect")) attrs["autocorrect"] = "off";
        if (!attrs.ContainsKey("spellcheck")) attrs["spellcheck"] = "false";

        if (ComboboxContext is not null)
        {
            attrs["aria-controls"] = ComboboxContext.OptionsId;

            if (isOpen && ComboboxContext.ActiveIndex >= 0)
                attrs["aria-activedescendant"] = ComboboxContext.GetOptionId(ComboboxContext.ActiveIndex);
        }

        if (ComboboxContext?.Disabled == true)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", ComboboxContext?.Disabled ?? false);

        return attrs;
    }

    private Task HandleInput(ChangeEventArgs args)
    {
        var newQuery = args.Value?.ToString() ?? string.Empty;
        return ComboboxContext?.HandleQueryAsync(newQuery) ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        return ComboboxContext?.HandleInputKeyDownAsync(args) ?? Task.CompletedTask;
    }

    private Task HandleFocus(FocusEventArgs _)
    {
        // Focusing the input opens the panel (matches Headless UI behaviour).
        return ComboboxContext?.OpenAsync() ?? Task.CompletedTask;
    }
}
