using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A group of checkboxes bound to one collection of values (<c>role="group"</c>).
/// Each <see cref="BhCheckbox"/> inside with a <c>Value</c> toggles that value.
/// Supports <c>@bind-Values</c> with <c>EditForm</c> validation, and exposes the
/// aggregate state so a "select all" checkbox can show the mixed state.
///
/// <code>
/// &lt;BhCheckboxGroup @bind-Values="toppings" aria-label="Toppings"&gt;
///     &lt;BhCheckbox SelectAll="true"&gt;All&lt;/BhCheckbox&gt;
///     &lt;BhCheckbox Value="cheese"&gt;Cheese&lt;/BhCheckbox&gt;
///     &lt;BhCheckbox Value="olives"&gt;Olives&lt;/BhCheckbox&gt;
/// &lt;/BhCheckboxGroup&gt;
/// </code>
/// </summary>
public class BhCheckboxGroup : BhComponentBase, IDisposable
{
    private readonly HashSet<string> _known = new(StringComparer.Ordinal);
    private List<string> _values = new();
    private BhFieldBinding? _binding;

    [CascadingParameter]
    private BhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The checked values. Supplying it makes the group controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>Initial values when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<IEnumerable<string>?>>? ValuesExpression { get; set; }

    /// <summary>Fires whenever the values change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> OnValuesChange { get; set; }

    /// <summary>All values the group offers. Optional — defaults to the values of the rendered checkboxes. Used by "select all".</summary>
    [Parameter]
    public IEnumerable<string>? AllValues { get; set; }

    /// <summary>Disables every checkbox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhCheckboxGroupContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhCheckboxGroupContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    private IReadOnlyCollection<string> CurrentValues => IsProvided(nameof(Values))
        ? Values as IReadOnlyCollection<string> ?? Values?.ToArray() ?? []
        : _values;

    protected override void OnInitialized()
    {
        if (DefaultValues is not null) _values = DefaultValues.ToList();
    }

    protected override void OnParametersSet()
    {
        if (ValuesExpression is not null || _binding is not null)
        {
            _binding ??= new BhFieldBinding(() => InvokeAsync(StateHasChanged));
            _binding.Update(EditContext, ValuesExpression);
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var all = (AllValues ?? _known).ToArray();
        var current = CurrentValues;
        var context = new BhCheckboxGroupContext(this, current, all, Disabled || (FieldContext?.Disabled ?? false));

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhCheckboxGroupContext>>(40);
        builder.AddComponentParameter(41, "Value", context);
        builder.AddComponentParameter(42, "ChildContent", BhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        if (FieldContext?.LabelledBy is { } label) attrs["aria-labelledby"] = label;
        if (FieldContext?.DescribedBy is { } describedBy) attrs["aria-describedby"] = describedBy;
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal void Know(string value)
    {
        if (_known.Add(value)) StateHasChanged();
    }

    internal async Task SetValuesAsync(IReadOnlyCollection<string> values)
    {
        if (!IsProvided(nameof(Values))) _values = values.ToList();
        await ValuesChanged.InvokeAsync(values);
        await OnValuesChange.InvokeAsync(values);
        _binding?.NotifyChanged();
        if (_binding is not null)
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        StateHasChanged();
    }

    public void Dispose() => _binding?.Dispose();
}

/// <summary>State of a <see cref="BhCheckboxGroup"/>, cascaded to its checkboxes and passed to its content.</summary>
public sealed class BhCheckboxGroupContext
{
    private readonly BhCheckboxGroup _group;

    internal BhCheckboxGroupContext(BhCheckboxGroup group, IReadOnlyCollection<string> values, IReadOnlyCollection<string> all, bool disabled)
    {
        _group = group;
        Values = values;
        All = all;
        Disabled = disabled;
    }

    /// <summary>The checked values.</summary>
    public IReadOnlyCollection<string> Values { get; }

    /// <summary>All values the group offers.</summary>
    public IReadOnlyCollection<string> All { get; }

    /// <summary>Whether the group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether every value is checked.</summary>
    public bool AllChecked => All.Count > 0 && All.All(Values.Contains);

    /// <summary>Whether some, but not all, values are checked (for a "select all" checkbox's mixed state).</summary>
    public bool SomeChecked => Values.Count > 0 && !AllChecked;

    /// <summary>Whether <paramref name="value"/> is checked.</summary>
    public bool IsChecked(string value) => Values.Contains(value);

    /// <summary>Checks or unchecks <paramref name="value"/>.</summary>
    public Task SetAsync(string value, bool isChecked)
    {
        _group.Know(value);
        var next = Values.ToList();
        if (isChecked && !next.Contains(value)) next.Add(value);
        if (!isChecked) next.Remove(value);
        return _group.SetValuesAsync(next);
    }

    internal void Register(string value) => _group.Know(value);

    /// <summary>Checks or unchecks every value (bind to a "select all" checkbox's <c>OnCheckedChange</c>).</summary>
    public Task SetAll(bool isChecked) => _group.SetValuesAsync(isChecked ? All.ToArray() : []);
}
