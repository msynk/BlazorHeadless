using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless native <c>&lt;select&gt;</c> that auto-wires <c>id</c>,
/// <c>aria-describedby</c>, <c>aria-invalid</c>, and <c>disabled</c> from the
/// parent <see cref="BhField"/> context.
///
/// <para>
/// Emits <c>data-disabled</c> and <c>data-invalid</c> for CSS hooks.
/// Place <c>&lt;option&gt;</c> elements inside <see cref="ChildContent"/>.
/// </para>
/// </summary>
public class BhSelect : BhComponentBase
{
    [CascadingParameter]
    private BhFieldContext? BhFieldContext { get; set; }

    /// <summary>Disables this select independently of the field's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks this select as invalid independently of the field's invalid state.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Child content — should contain <c>&lt;option&gt;</c> elements.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "select";

    private bool IsDisabled => Disabled || (BhFieldContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (BhFieldContext?.Invalid ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhFieldContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (BhFieldContext?.DescriptionId is not null)
            attrs["aria-describedby"] = BhFieldContext.DescriptionId;

        if (IsDisabled)
            attrs["disabled"] = true;

        if (IsInvalid)
            attrs["aria-invalid"] = "true";

        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);

        return attrs;
    }
}
