using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Combobox (autocomplete) — a text input paired with a
/// filterable popup of options, implementing the WAI-ARIA <c>combobox</c> pattern
/// with <c>aria-autocomplete="list"</c>. Focus stays in the input; the highlighted
/// option is announced via <c>aria-activedescendant</c>.
///
/// <para><b>Filtering is consumer-driven.</b> <see cref="OnQueryChange"/> fires as the
/// user types; render only the matching options (sync, async, server-side, fuzzy …).
/// <see cref="BhFilter"/> provides ready-made matchers.</para>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or multi-select</b>; <c>@bind-Value</c> / <c>@bind-Values</c>; <c>EditForm</c> validation; <see cref="BhField"/> wiring.</item>
///   <item><b>Keyboard</b> — ArrowDown / ArrowUp (open + navigate), Alt+ArrowDown, PageUp / PageDown, Enter, Escape (close, then clear), Tab (accept).</item>
///   <item><b>Auto-highlight</b> of the first match while typing (<see cref="AutoHighlight"/>).</item>
///   <item><b>Display value</b> — <see cref="DisplayValue"/> converts the selection back to input text.</item>
///   <item><b>Large lists</b> — data-driven and virtualized options via <see cref="BhComboboxOptions{TValue}.Items"/>.</item>
///   <item><b>Empty state</b> — <see cref="BhComboboxEmpty{TValue}"/> renders when nothing matches.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhCombobox TValue="string" @bind-Value="selected" OnQueryChange="q => query = q"&gt;
///     &lt;BhComboboxInput TValue="string" /&gt;
///     &lt;BhComboboxOptions TValue="string" Anchor="@("bottom start")"&gt;
///         @foreach (var item in Filtered)
///         {
///             &lt;BhComboboxOption TValue="string" Value="@item"&gt;@item&lt;/BhComboboxOption&gt;
///         }
///     &lt;/BhComboboxOptions&gt;
/// &lt;/BhCombobox&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BhCombobox<TValue> : BhComponentBase, IDisposable
{
    private BhListState<TValue>? _state;
    private BhFieldBinding? _binding;
    private bool _open;
    private TValue? _value;
    private bool _hasValue;
    private List<TValue> _values = new();
    private string _query = string.Empty;
    private bool _userTyped;
    private BhMenuFocus? _pendingHighlight;

    [CascadingParameter]
    private BhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The selected value (single-select). Supplying it makes the combobox controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Value</c>).</summary>
    [Parameter]
    public Expression<Func<TValue?>>? ValueExpression { get; set; }

    /// <summary>Initial value when uncontrolled (single-select).</summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>Fires when the selected value changes (single-select).</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChange { get; set; }

    /// <summary>The selected values (multi-select). Supplying it makes the combobox controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<TValue>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> ValuesChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation (set by <c>@bind-Values</c>).</summary>
    [Parameter]
    public Expression<Func<IEnumerable<TValue>?>>? ValuesExpression { get; set; }

    /// <summary>Initial values when uncontrolled (multi-select).</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultValues { get; set; }

    /// <summary>Fires when the selected values change (multi-select).</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> OnValuesChange { get; set; }

    /// <summary>Fires whenever the text typed into the input changes.</summary>
    [Parameter]
    public EventCallback<string> OnQueryChange { get; set; }

    /// <summary>Fires when the options panel opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Converts a value to the text shown in the input. Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TValue?, string>? DisplayValue { get; set; }

    /// <summary>Whether multiple options can be selected.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Allows clearing the selection by emptying the input (single-select).</summary>
    [Parameter]
    public bool Nullable { get; set; }

    /// <summary>Open the options when the input receives focus. Default: false.</summary>
    [Parameter]
    public bool OpenOnFocus { get; set; }

    /// <summary>Highlight the first option while typing so Enter selects it. Default: true.</summary>
    [Parameter]
    public bool AutoHighlight { get; set; } = true;

    /// <summary>Disables the combobox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the combobox invalid (in addition to EditContext validation).</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Marks the combobox required.</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>Compares option values with the selection. Defaults to <see cref="EqualityComparer{T}.Default"/>.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>Optional form field name for hidden inputs carrying the selection.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Converts a value to its form-post representation. Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TValue, string?>? FormatValue { get; set; }

    /// <summary>Child content: <see cref="BhComboboxInput{TValue}"/>, optional <see cref="BhComboboxButton{TValue}"/>, <see cref="BhComboboxOptions{TValue}"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;

    private BhListState<TValue> State
    {
        get
        {
            if (_state is null)
            {
                _state = new BhListState<TValue>(OptionsId, Interop, IsSelected, () => Input?.Refresh());
                _state.Options.Changed += () => Empty?.Refresh();
            }
            return _state;
        }
    }

    internal BhComboboxInput<TValue>? Input { get; set; }
    internal BhComboboxEmpty<TValue>? Empty { get; set; }

    /// <summary>Whether the options panel is open.</summary>
    public bool IsOpen => _open;

    /// <summary>The current query (text typed by the user).</summary>
    public string Query => _query;

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false);
    private bool IsInvalid => Invalid || (FieldContext?.Invalid ?? false) || (_binding?.IsInvalid ?? false);
    private bool IsRequired => Required || (FieldContext?.Required ?? false);

    private TValue? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;
    private bool HasSingleValue => IsProvided(nameof(Value)) ? Value is not null : _hasValue;

    private IReadOnlyCollection<TValue> CurrentValues => IsProvided(nameof(Values))
        ? Values as IReadOnlyCollection<TValue> ?? Values?.ToArray() ?? []
        : _values;

    private string InputId => Id is null && FieldContext is not null ? FieldContext.ControlId : $"{ComponentId}-input";
    private string ButtonId => $"{ComponentId}-button";
    private string OptionsId => $"{ComponentId}-options";

    private bool IsSelected(TValue value)
        => Multiple
            ? CurrentValues.Contains(value, EffectiveComparer)
            : HasSingleValue && EffectiveComparer.Equals(CurrentValue!, value);

    internal string Display(TValue? value)
        => DisplayValue is not null ? DisplayValue(value) : value?.ToString() ?? string.Empty;

    /// <summary>The text the input shows: the live query while typing, the selection's display text otherwise.</summary>
    internal string InputText
        => _userTyped || Multiple ? _query : HasSingleValue ? Display(CurrentValue) : _query;

    protected override void OnInitialized()
    {
        if (DefaultValue is not null)
        {
            _value = DefaultValue;
            _hasValue = true;
        }
        if (DefaultValues is not null)
            _values = DefaultValues.ToList();
    }

    protected override void OnParametersSet()
    {
        LambdaExpression? expression = Multiple ? ValuesExpression : ValueExpression;
        if (expression is not null || _binding is not null)
        {
            _binding ??= new BhFieldBinding(() => InvokeAsync(StateHasChanged));
            _binding.Update(EditContext, expression);
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_open) return;

        if (_pendingHighlight is { } highlight)
        {
            _pendingHighlight = null;
            await State.EnsureOrderAsync();
            var selected = State.FirstSelected();
            var index = highlight switch
            {
                BhMenuFocus.First => selected >= 0 ? selected : State.FirstEnabled(),
                BhMenuFocus.Last => selected >= 0 ? selected : State.LastEnabled(),
                _ => -1,
            };
            if (index >= 0) State.SetActiveIndex(index);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        State.PanelId = OptionsId;
        var context = new BhComboboxContext<TValue>(this, State, _open, IsDisabled, Multiple, IsInvalid, IsRequired,
            CurrentValue, HasSingleValue, CurrentValues, _query, ComponentId, InputId, ButtonId, OptionsId, FieldContext);

        builder.OpenComponent<CascadingValue<BhListState<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", State);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", (RenderFragment)(outer =>
        {
            outer.OpenComponent<CascadingValue<BhComboboxContext<TValue>>>(0);
            outer.AddComponentParameter(1, "Value", context);
            outer.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                RenderHiddenInputs(inner, 50);
                inner.CloseElement();
            }));
            outer.CloseComponent();
        }));
        builder.CloseComponent();
    }

    private void RenderHiddenInputs(RenderTreeBuilder builder, int sequence)
    {
        if (string.IsNullOrEmpty(Name)) return;
        string? Format(TValue v) => FormatValue is not null ? FormatValue(v) : v?.ToString();

        builder.OpenRegion(sequence);
        if (Multiple)
        {
            foreach (var v in CurrentValues)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "hidden");
                builder.AddAttribute(2, "name", Name);
                builder.AddAttribute(3, "value", Format(v));
                builder.CloseElement();
            }
        }
        else if (HasSingleValue)
        {
            builder.OpenElement(10, "input");
            builder.AddAttribute(11, "type", "hidden");
            builder.AddAttribute(12, "name", Name);
            builder.AddAttribute(13, "value", Format(CurrentValue!));
            builder.CloseElement();
        }
        builder.CloseRegion();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _open);
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", IsInvalid);
        return attrs;
    }

    // ── Open / close ─────────────────────────────────────────────────────────

    internal async Task OpenAsync(BhMenuFocus highlight)
    {
        if (IsDisabled) return;
        _pendingHighlight = highlight;
        if (_open)
        {
            StateHasChanged();
            return;
        }
        _open = true;
        StateHasChanged();
        await OnOpenChange.InvokeAsync(true);
    }

    /// <summary>Closes the options panel. The input reverts to the selection's text.</summary>
    public async Task CloseAsync()
    {
        if (!_open) return;
        _open = false;
        State.SetActiveId(null, scroll: false);

        if (_userTyped && !Multiple)
        {
            // Typing without choosing: an emptied input clears a nullable
            // combobox, anything else reverts to the selected value's text.
            if (Nullable && string.IsNullOrEmpty(_query) && HasSingleValue)
                await SetSingleAsync(default);
            await SetQueryAsync(string.Empty, userTyped: false);
        }

        StateHasChanged();
        await OnOpenChange.InvokeAsync(false);
    }

    internal Task ToggleAsync() => _open ? CloseAsync() : OpenAsync(BhMenuFocus.None);

    // ── Query ────────────────────────────────────────────────────────────────

    internal async Task HandleInputAsync(string text)
    {
        await SetQueryAsync(text, userTyped: true);
        if (!_open)
        {
            _open = true;
            await OnOpenChange.InvokeAsync(true);
        }
        StateHasChanged();
    }

    private async Task SetQueryAsync(string text, bool userTyped)
    {
        _userTyped = userTyped;
        if (_query == text) return;
        _query = text;
        await OnQueryChange.InvokeAsync(text);
    }

    /// <summary>Called by the options panel after a render: keeps the highlight valid as results change.</summary>
    internal void ReconcileHighlight() => State.Reconcile(fallbackToFirst: AutoHighlight && _userTyped);

    // ── Selection ────────────────────────────────────────────────────────────

    internal async Task SelectAsync(TValue value)
    {
        if (IsDisabled) return;

        if (Multiple)
        {
            var list = CurrentValues.ToList();
            var index = list.FindIndex(v => EffectiveComparer.Equals(v, value));
            if (index >= 0) list.RemoveAt(index);
            else list.Add(value);

            if (!IsProvided(nameof(Values)))
                _values = list;
            await ValuesChanged.InvokeAsync(list);
            await OnValuesChange.InvokeAsync(list);
        }
        else
        {
            await SetSingleAsync(value);
            await SetQueryAsync(string.Empty, userTyped: false);
            await CloseAsync();
        }

        _binding?.NotifyChanged();
        if (_binding is not null)
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        StateHasChanged();
    }

    private async Task SetSingleAsync(TValue? value)
    {
        if (!IsProvided(nameof(Value)))
        {
            _value = value;
            _hasValue = value is not null;
        }
        await ValueChanged.InvokeAsync(value);
        await OnValueChange.InvokeAsync(value);
    }

    // ── Keyboard (focus stays in the input) ──────────────────────────────────

    internal async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        if (IsDisabled) return;

        switch (e.Key)
        {
            case "ArrowDown":
                if (!_open) await OpenAsync(e.AltKey ? BhMenuFocus.None : BhMenuFocus.First);
                else State.SetActiveIndex(State.Step(State.ActiveIndex, +1, loop: false));
                break;

            case "ArrowUp":
                if (!_open) await OpenAsync(BhMenuFocus.Last);
                else if (e.AltKey) await CloseAsync();
                else State.SetActiveIndex(State.Step(State.ActiveIndex, -1, loop: false));
                break;

            case "PageDown":
                if (_open) State.SetActiveIndex(State.Offset(State.ActiveIndex, 10));
                break;

            case "PageUp":
                if (_open) State.SetActiveIndex(State.Offset(State.ActiveIndex, -10));
                break;

            case "Enter":
                if (_open && State.TryGetActiveValue(out var value) && !State.DisabledAt(State.ActiveIndex))
                    await SelectAsync(value);
                break;

            case "Escape":
                if (_open)
                {
                    await CloseAsync();
                }
                else if (!Multiple && (HasSingleValue || _query.Length > 0))
                {
                    // Second Escape clears the input (APG).
                    if (Nullable) await SetSingleAsync(default);
                    await SetQueryAsync(string.Empty, userTyped: Nullable);
                    StateHasChanged();
                }
                break;

            case "Tab":
                if (_open && !Multiple && State.TryGetActiveValue(out var active) && !State.DisabledAt(State.ActiveIndex))
                    await SelectAsync(active);
                else if (_open)
                    await CloseAsync();
                break;
        }
    }

    internal Task HandleFocusAsync() => OpenOnFocus && !_open ? OpenAsync(BhMenuFocus.None) : Task.CompletedTask;

    internal ValueTask FocusInputAsync() => new(Interop.FocusAsync(InputId).AsTask());

    public void Dispose() => _binding?.Dispose();
}
