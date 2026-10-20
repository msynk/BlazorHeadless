using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Checkbox (<c>role="checkbox"</c>) with tri-state support.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Checked</c> with <c>EditForm</c> validation, or uncontrolled with <see cref="BhCheckableBase.DefaultChecked"/>.</item>
///   <item><b>Indeterminate</b> — <see cref="Indeterminate"/> renders <c>aria-checked="mixed"</c> / <c>data-state="indeterminate"</c>; activating it checks the box.</item>
///   <item><b>Groups</b> — inside a <see cref="BhCheckboxGroup"/> the checkbox toggles its <see cref="BhCheckableBase.Value"/> in the group's values;
///     <see cref="SelectAll"/> turns it into the group's tri-state "select all" box.</item>
///   <item><b>Keyboard</b> — Space toggles; Enter doesn't (APG).</item>
///   <item><b>Forms</b> — <see cref="BhField"/> wiring and a hidden input for plain posts.</item>
/// </list>
///
/// <code>
/// &lt;BhField&gt;
///     &lt;BhCheckbox @bind-Checked="accepted"&gt;&lt;BhCheckboxIndicator&gt;✓&lt;/BhCheckboxIndicator&gt;&lt;/BhCheckbox&gt;
///     &lt;BhLabel&gt;I accept the terms&lt;/BhLabel&gt;
/// &lt;/BhField&gt;
/// </code>
/// </summary>
public class BhCheckbox : BhCheckableBase
{
    [CascadingParameter]
    private BhCheckboxGroupContext? Group { get; set; }

    /// <summary>Shows the mixed state (<c>aria-checked="mixed"</c>). Activating checks the box.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>
    /// Inside a <see cref="BhCheckboxGroup"/>: makes this the group's "select all" box. It is
    /// checked when every value is checked, mixed when some are, and toggling it checks or
    /// clears them all.
    /// </summary>
    [Parameter]
    public bool SelectAll { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhCheckboxRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhCheckboxRenderContext>? Template { get; set; }

    protected override string Role => "checkbox";

    private bool IsSelectAll => SelectAll && Group is not null;
    private bool InGroup => Group is not null && !SelectAll && IsProvided(nameof(Value));

    /// <summary>Whether the box is checked (from the group when inside a <see cref="BhCheckboxGroup"/>).</summary>
    public bool IsCheckedEffective => IsSelectAll ? Group!.AllChecked : InGroup ? Group!.IsChecked(Value) : IsChecked;

    private bool EffectiveIndeterminate => IsSelectAll ? Group!.SomeChecked : Indeterminate;

    private bool EffectiveDisabled => IsDisabled || (Group?.Disabled ?? false);

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (InGroup) Group!.Register(Value);
    }

    protected override bool NextChecked() => EffectiveIndeterminate || !IsCheckedEffective;

    protected override bool ShouldSubmit => !IsSelectAll && (IsCheckedEffective || Indeterminate);

    protected override async Task SetCheckedAsync(bool value)
    {
        if (EffectiveDisabled) return;
        if (IsSelectAll)
        {
            await Group!.SetAll(value);
            await OnCheckedChange.InvokeAsync(value);
            return;
        }
        if (InGroup)
        {
            await Group!.SetAsync(Value, value);
            await OnCheckedChange.InvokeAsync(value);
            return;
        }
        await base.SetCheckedAsync(value);
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        var context = new BhCheckboxRenderContext
        {
            IsChecked = IsCheckedEffective,
            IsIndeterminate = EffectiveIndeterminate,
            Disabled = EffectiveDisabled,
        };
        builder.OpenComponent<CascadingValue<BhCheckboxRenderContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", context);
        builder.AddComponentParameter(sequence + 2, "ChildContent", BhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-checked"] = EffectiveIndeterminate ? "mixed" : IsCheckedEffective ? "true" : "false";
        if (EffectiveIndeterminate)
            SetDataValue(attrs, "state", "indeterminate");
        else
            SetDataState(attrs, IsCheckedEffective, "checked", "unchecked");
        if (EffectiveDisabled && !IsDisabled)
        {
            if (IsNativeButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
            SetDataFlag(attrs, "disabled", true);
        }
        return attrs;
    }
}

/// <summary>Render-fragment context exposed by <see cref="BhCheckbox"/> (and cascaded to <see cref="BhCheckboxIndicator"/>).</summary>
public sealed record BhCheckboxRenderContext
{
    /// <summary>Whether the box is checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether the box shows the mixed state.</summary>
    public required bool IsIndeterminate { get; init; }

    /// <summary>Whether the box is disabled.</summary>
    public required bool Disabled { get; init; }
}
