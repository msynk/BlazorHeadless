using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A menu item inside a <see cref="BhMenuRadioGroup"/> (<c>role="menuitemradio"</c>).
/// Activating it checks it. Use <see cref="BhMenuItemIndicator"/> to render a dot
/// only while checked.
/// </summary>
public class BhMenuRadioItem : BhMenuItemBase
{
    [CascadingParameter]
    private BhMenuRadioGroupContext? Group { get; set; }

    /// <summary>The value this item represents.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Whether choosing the item closes the menu. Default: false.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhMenuItemRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhMenuItemRenderContext>? Template { get; set; }

    protected override string Role => "menuitemradio";

    /// <summary>Whether this item is the checked one.</summary>
    public bool IsChecked => Group is not null && Group.Value == Value;

    internal override async Task SelectAsync(bool viaKeyboard)
    {
        if (Group is not null)
            await Group.SelectAsync(Value);
        if (CloseOnSelect)
            await CloseMenuAsync();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetAriaBool(attrs, "aria-checked", IsChecked);
        SetDataState(attrs, IsChecked, "checked", "unchecked");
        return attrs;
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        builder.OpenComponent<CascadingValue<BhMenuItemIndicatorContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", new BhMenuItemIndicatorContext(IsChecked));
        builder.AddComponentParameter(sequence + 2, "ChildContent", BhContent.Pick(Template, ChildContent, new BhMenuItemRenderContext
        {
            IsActive = IsActive,
            Disabled = IsItemDisabled,
            Checked = IsChecked,
        }));
        builder.CloseComponent();
    }
}
