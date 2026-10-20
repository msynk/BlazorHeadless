using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Shared implementation of two-state controls (<see cref="BhCheckbox"/>,
/// <see cref="BhSwitch"/>): <c>@bind-Checked</c>, <c>EditForm</c> validation,
/// <see cref="BhField"/> wiring, keyboard support for non-button elements and a
/// hidden input for plain form posts.
/// </summary>
public abstract class BhCheckableBase : BhComponentBase, IDisposable
{
    private BhFieldBinding? _binding;
    private bool _checked;

    [CascadingParameter]
    private BhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>Whether the control is checked. Supplying it makes the control controlled (use <c>@bind-Checked</c>).</summary>
    [Parameter]
    public bool Checked { get; set; }

    /// <summary>Initial state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultChecked { get; set; }

    /// <summary>Two-way binding callback for <see cref="Checked"/>.</summary>
    [Parameter]
    public EventCallback<bool> CheckedChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Checked</c>).</summary>
    [Parameter]
    public Expression<Func<bool>>? CheckedExpression { get; set; }

    /// <summary>Fires whenever the checked state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnCheckedChange { get; set; }

    /// <summary>Disables the control.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the control required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Marks the control invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Form field name: a hidden input carrying <see cref="Value"/> is rendered while checked.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The value submitted with the form while checked. Default: <c>"on"</c>.</summary>
    [Parameter]
    public string Value { get; set; } = "on";

    protected override string DefaultTag => "button";

    /// <summary>The ARIA role (<c>checkbox</c> or <c>switch</c>).</summary>
    protected abstract string Role { get; }

    /// <summary>Whether Enter also toggles (true for switches, false for checkboxes per APG).</summary>
    protected virtual bool EnterToggles => false;

    /// <summary>The effective checked state.</summary>
    public bool IsChecked => IsProvided(nameof(Checked)) ? Checked : _checked;

    /// <summary>Whether the control is disabled (own flag, field or fieldset).</summary>
    protected bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false);

    /// <summary>Whether the control is invalid.</summary>
    protected bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);

    /// <summary>Whether the control is required.</summary>
    protected bool IsRequired => Required || (FieldContext?.Required ?? false);

    /// <summary>Whether the control renders a native button.</summary>
    protected bool IsNativeButton => Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    /// <summary>The control's id (the field's control id inside a <see cref="BhField"/>).</summary>
    protected string ControlId => Id ?? FieldContext?.ControlId ?? ComponentId;

    protected override void OnInitialized()
    {
        _checked = DefaultChecked;
        // A <label for> can only target a native button; other tags are focused by the label.
        if (!IsNativeButton) FieldContext?.SetControlFocus(() => Interop.FocusAsync(ControlId).AsTask());
    }

    protected override void OnParametersSet()
    {
        if (CheckedExpression is not null || _binding is not null)
        {
            _binding ??= new BhFieldBinding(() => InvokeAsync(StateHasChanged));
            _binding.Update(EditContext, CheckedExpression);
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        }
    }

    /// <summary>Renders the element's children.</summary>
    protected abstract void RenderContent(RenderTreeBuilder builder, int sequence);

    /// <summary>Computes the next checked state when toggled.</summary>
    protected virtual bool NextChecked() => !IsChecked;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ControlId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        if (!IsNativeButton || !EnterToggles)
            builder.AddAttribute(31, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddElementReferenceCapture(40, CaptureElement);
        RenderContent(builder, 50);
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name) && ShouldSubmit)
        {
            builder.OpenElement(70, "input");
            builder.AddAttribute(71, "type", "hidden");
            builder.AddAttribute(72, "name", Name);
            builder.AddAttribute(73, "value", Value);
            builder.CloseElement();
        }
    }

    /// <summary>Whether the hidden form input is rendered.</summary>
    protected virtual bool ShouldSubmit => IsChecked;

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = Role;

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (IsDisabled) attrs["disabled"] = true;
            // Native buttons activate on Enter; checkboxes must not (APG).
            if (!EnterToggles) attrs[BhKeyboard.KeysAttribute] = "Enter";
        }
        else
        {
            attrs["tabindex"] = IsDisabled ? -1 : 0;
            attrs[BhKeyboard.KeysAttribute] = "Space Enter";
            if (IsDisabled) attrs["aria-disabled"] = "true";
            // <label for> only labels labelable elements; reference it explicitly.
            if (FieldContext?.LabelledBy is { } labelledBy) attrs["aria-labelledby"] = labelledBy;
        }

        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        if (IsRequired) attrs["aria-required"] = "true";
        if (IsInvalid) attrs["aria-invalid"] = "true";

        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        SetDataFlag(attrs, "required", IsRequired);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _) => IsDisabled ? Task.CompletedTask : ToggleAsync();

    private Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (IsDisabled) return Task.CompletedTask;
        if (!IsNativeButton && (e.Key == " " || (e.Key == "Enter" && EnterToggles)))
            return ToggleAsync();
        return Task.CompletedTask;
    }

    /// <summary>Toggles the control.</summary>
    public async Task ToggleAsync()
    {
        var next = NextChecked();
        await SetCheckedAsync(next);
    }

    /// <summary>Sets the checked state and notifies listeners.</summary>
    protected virtual async Task SetCheckedAsync(bool value)
    {
        if (!IsProvided(nameof(Checked)))
            _checked = value;
        await CheckedChanged.InvokeAsync(value);
        await OnCheckedChange.InvokeAsync(value);
        _binding?.NotifyChanged();
        if (_binding is not null)
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        StateHasChanged();
    }

    public virtual void Dispose()
    {
        _binding?.Dispose();
        if (!IsNativeButton) FieldContext?.SetControlFocus(null);
    }
}
