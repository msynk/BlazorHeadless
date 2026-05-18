using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// The popup panel containing the combobox's options. Renders as
/// <c>&lt;ul role="listbox"&gt;</c> by default. Hidden via the HTML <c>hidden</c>
/// attribute when the combobox is closed.
/// </summary>
public class HComboboxOptions<TValue> : HeadlessComponentBase
{
    [CascadingParameter]
    private ComboboxContext<TValue> ComboboxContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="HComboboxOption{TValue}"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private bool IsOpen => ComboboxContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext?.OptionsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

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
}
