using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A menu item that toggles a boolean setting (<c>role="menuitemcheckbox"</c>).
/// Supports <c>@bind-Checked</c>. Use <see cref="BhMenuItemIndicator"/> inside it to
/// render a check mark only while checked.
///
/// <code>
/// &lt;BhMenuCheckboxItem @bind-Checked="showStatusBar"&gt;
///     &lt;BhMenuItemIndicator&gt;✓&lt;/BhMenuItemIndicator&gt; Status bar
/// &lt;/BhMenuCheckboxItem&gt;
/// </code>
/// </summary>
public class BhMenuCheckboxItem : BhMenuItemBase
{
    private bool _checked;
    private BhMenuItemIndicatorContext? _indicator;

    /// <summary>Whether the item is checked. Supplying it makes the item controlled.</summary>
    [Parameter]
    public bool Checked { get; set; }

    /// <summary>Initial state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultChecked { get; set; }

    /// <summary>Two-way binding callback for <see cref="Checked"/>.</summary>
    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    /// <summary>Fires whenever the checked state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnCheckedChange { get; set; }

    /// <summary>Whether toggling closes the menu. Default: false (the user can see the change).</summary>
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

    protected override string Role => "menuitemcheckbox";

    /// <summary>Whether the item is checked.</summary>
    public bool IsChecked => IsProvided(nameof(Checked)) ? Checked : _checked;

    protected override void OnInitialized()
    {
        _checked = DefaultChecked;
        base.OnInitialized();
    }

    internal override async Task SelectAsync(bool viaKeyboard)
    {
        var next = !IsChecked;
        if (!IsProvided(nameof(Checked)))
            _checked = next;
        await CheckedChanged.InvokeAsync(next);
        await OnCheckedChange.InvokeAsync(next);
        StateHasChanged();
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
        _indicator = new BhMenuItemIndicatorContext(IsChecked);
        builder.OpenComponent<CascadingValue<BhMenuItemIndicatorContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", _indicator);
        builder.AddComponentParameter(sequence + 2, "ChildContent", BhContent.Pick(Template, ChildContent, new BhMenuItemRenderContext
        {
            IsActive = IsActive,
            Disabled = IsItemDisabled,
            Checked = IsChecked,
        }));
        builder.CloseComponent();
    }
}
