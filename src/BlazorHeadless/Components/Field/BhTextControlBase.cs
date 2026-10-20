using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Shared implementation of the native text controls (<see cref="BhInput"/>,
/// <see cref="BhTextarea"/>, <see cref="BhSelect"/>): <see cref="BhField"/> wiring,
/// optional <c>@bind-Value</c>, and <c>EditForm</c> validation.
///
/// <para>
/// When <see cref="Value"/> is not supplied the control is uncontrolled and every
/// HTML attribute (including <c>value</c>, <c>@onchange</c>, <c>@oninput</c>)
/// passes straight through.
/// </para>
/// </summary>
public abstract class BhTextControlBase : BhComponentBase, IDisposable
{
    private BhFieldBinding? _binding;

    [CascadingParameter]
    private BhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The bound value. Supplying it makes the control controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<string?>>? ValueExpression { get; set; }

    /// <summary>When the bound value updates: <see cref="BhBindOn.Change"/> (default) or on every keystroke.</summary>
    [Parameter]
    public BhBindOn BindOn { get; set; } = BhBindOn.Change;

    /// <summary>Disables the control independently of the field's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the control invalid independently of the field's state.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the control required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Whether the control is disabled (own flag, field or fieldset).</summary>
    protected bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false);

    /// <summary>Whether the control is invalid (own flag, field, or validation messages).</summary>
    protected bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);

    /// <summary>Whether the control is required.</summary>
    protected bool IsRequired => Required || (FieldContext?.Required ?? false);

    /// <summary>The control's element id.</summary>
    protected string ControlId => Id ?? FieldContext?.ControlId ?? ComponentId;

    private bool IsBound => IsProvided(nameof(Value)) || ValueChanged.HasDelegate;

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BhFieldBinding(() => InvokeAsync(StateHasChanged));
            _binding.Update(EditContext, ValueExpression);
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        }
    }

    /// <summary>Adds <c>value</c> and the change / input handler when bound.</summary>
    protected void AddValueBinding(RenderTreeBuilder builder, int sequence)
    {
        if (!IsBound) return;
        builder.AddAttribute(sequence, "value", Value);
        builder.AddAttribute(sequence + 1, BindOn == BhBindOn.Input ? "oninput" : "onchange",
            EventCallback.Factory.CreateBinder<string?>(this, OnValueChanged, Value));
        builder.SetUpdatesAttributeName("value");
    }

    private void OnValueChanged(string? value)
    {
        Value = value;
        _ = ValueChanged.InvokeAsync(value);
        _binding?.NotifyChanged();
        if (_binding is not null)
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (FieldContext?.DescribedBy is { } describedBy)
            attrs["aria-describedby"] = describedBy;
        if (IsDisabled)
            attrs["disabled"] = true;
        if (IsRequired)
            attrs["required"] = true;
        if (IsInvalid)
            attrs["aria-invalid"] = "true";
        if (_binding?.CssClass is { Length: > 0 } css)
            attrs["class"] = css;

        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        return attrs;
    }

    public virtual void Dispose() => _binding?.Dispose();
}

/// <summary>When a bound text control writes its value back.</summary>
public enum BhBindOn
{
    /// <summary>On the <c>change</c> event (blur / commit).</summary>
    Change,

    /// <summary>On every <c>input</c> event (each keystroke).</summary>
    Input,
}
