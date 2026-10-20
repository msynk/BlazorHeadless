using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless form field wrapper that generates ids and wires its parts
/// together: <see cref="BhLabel"/>, a control (<see cref="BhInput"/>,
/// <see cref="BhTextarea"/>, <see cref="BhSelect"/>, or any BlazorHeadless control
/// such as <see cref="BhListbox{TValue}"/>, <see cref="BhSwitch"/>, <see cref="BhCheckbox"/>),
/// <see cref="BhDescription"/>s and <see cref="BhErrorMessage"/>s.
///
/// <para>
/// The label's <c>for</c> / <c>aria-labelledby</c>, the control's
/// <c>aria-describedby</c>, <c>aria-invalid</c>, <c>required</c> and
/// <c>disabled</c> are all managed for you and only reference parts that are
/// actually rendered. Inside an <c>EditForm</c>, a control bound with
/// <c>@bind-Value</c> reports its validation messages, which
/// <see cref="BhErrorMessage"/> displays.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhField&gt;
///     &lt;BhLabel&gt;Email&lt;/BhLabel&gt;
///     &lt;BhDescription&gt;We'll never share it.&lt;/BhDescription&gt;
///     &lt;BhInput type="email" @bind-Value="model.Email" /&gt;
///     &lt;BhErrorMessage /&gt;
/// &lt;/BhField&gt;
/// </code>
/// </summary>
public class BhField : BhComponentBase
{
    private BhFieldContext? _context;

    [CascadingParameter]
    private BhFieldsetContext? FieldsetContext { get; set; }

    /// <summary>Disables the field's control.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the field invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the field's control as required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Label, control, descriptions and error messages.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BhFieldContext Context => _context ??= new BhFieldContext(ComponentId, StateHasChanged);

    protected override void OnParametersSet()
    {
        Context.Disabled = Disabled || (FieldsetContext?.Disabled ?? false);
        Context.ExplicitInvalid = Invalid;
        Context.Required = Required;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhFieldContext>>(0);
        builder.AddComponentParameter(1, "Value", Context);
        // Not fixed: parts without parameters (e.g. <BhErrorMessage />) must re-render
        // when the field changes (validation, registered descriptions).
        builder.AddComponentParameter(2, "IsFixed", false);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", Context.Disabled);
        SetDataFlag(attrs, "invalid", Context.Invalid);
        SetDataFlag(attrs, "required", Required);
        return attrs;
    }
}
