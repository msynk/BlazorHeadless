using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless <c>&lt;input&gt;</c> that auto-wires <c>id</c>,
/// <c>aria-describedby</c>, <c>aria-invalid</c>, and <c>disabled</c> from the
/// parent <see cref="HField"/> context.
///
/// <para>
/// Emits <c>data-disabled</c> and <c>data-invalid</c> for CSS hooks.
/// All standard HTML input attributes (type, placeholder, value, etc.) pass
/// through via <see cref="HeadlessComponentBase.AdditionalAttributes"/>.
/// </para>
/// </summary>
public class HInput : HeadlessComponentBase
{
    [CascadingParameter]
    private FieldContext? FieldContext { get; set; }

    /// <summary>Disables this input independently of the field's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks this input as invalid independently of the field's invalid state.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    protected override string DefaultTag => "input";

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", FieldContext?.InputId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (FieldContext?.DescriptionId is not null)
            attrs["aria-describedby"] = FieldContext.DescriptionId;

        if (IsDisabled)
            attrs["disabled"] = true;

        if (IsInvalid)
            attrs["aria-invalid"] = "true";

        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);

        return attrs;
    }
}
