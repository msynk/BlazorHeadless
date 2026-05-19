using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Combobox — a typeable text input paired with a filterable
/// dropdown of options. Implements the WAI-ARIA <c>combobox</c> pattern with the
/// "list autocomplete" model: focus stays on the input at all times and
/// <c>aria-activedescendant</c> drives screen-reader announcements of the
/// highlighted option.
///
/// <para><b>Filtering is consumer-driven.</b> The combobox emits a
/// <see cref="OnQueryChange"/> event whenever the user types; the consumer is
/// responsible for filtering its data and rendering only the matching
/// <see cref="HComboboxOption{TValue}"/> children. This keeps the component
/// agnostic about how filtering happens (sync, async, server-side, fuzzy, …).</para>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or multi-select</b> via <see cref="Multiple"/>.</item>
///   <item><b>Uncontrolled and controlled</b> for both selection and the query string.</item>
///   <item><b>Full keyboard support</b> — arrow keys (with auto-open), Home, End, Enter, Escape, Tab.</item>
///   <item><b>Display value</b> — pass a <see cref="DisplayValue"/> function to convert the selected value back to the input text.</item>
///   <item><b>Click-outside closes</b> via a transparent full-viewport overlay.</item>
///   <item><b>Form submission</b> — when <see cref="Name"/> is set, hidden <c>&lt;input&gt;</c>(s) carry the selected value(s).</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HCombobox TValue="string"
///            Value="@selected"
///            OnValueChange="v => selected = v"
///            OnQueryChange="q => query = q"
///            DisplayValue="v => v ?? string.Empty"&gt;
///     &lt;HComboboxInput TValue="string" /&gt;
///     &lt;HComboboxOptions TValue="string"&gt;
///         @foreach (var item in filtered)
///         {
///             &lt;HComboboxOption TValue="string" Value="@item"&gt;@item&lt;/HComboboxOption&gt;
///         }
///     &lt;/HComboboxOptions&gt;
/// &lt;/HCombobox&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class HCombobox<TValue> : HeadlessComponentBase
{
    private readonly List<HComboboxOption<TValue>> _options = new();

    private bool _isOpen;
    private TValue? _singleValue;
    private List<TValue> _multiValues = new();
    private int _activeIndex = -1;
    private string _query = string.Empty;

    private bool _initialized;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Single-select controlled value.</summary>
    [Parameter]
    public TValue? Value { get; set; }

    /// <summary>Single-select uncontrolled initial value. Ignored when <see cref="Value"/> is supplied.</summary>
    [Parameter]
    public TValue? DefaultValue { get; set; }

    /// <summary>Fires when the selected value changes (single-select).</summary>
    [Parameter]
    public EventCallback<TValue?> OnValueChange { get; set; }

    /// <summary>Multi-select controlled values.</summary>
    [Parameter]
    public IEnumerable<TValue>? Values { get; set; }

    /// <summary>Multi-select uncontrolled initial values. Ignored when <see cref="Values"/> is supplied.</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultValues { get; set; }

    /// <summary>Fires when the selected values change (multi-select).</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> OnValuesChange { get; set; }

    /// <summary>Whether multi-select mode is enabled.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Disables the combobox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional form field name. When set, hidden <c>&lt;input&gt;</c>(s) carry the
    /// selected value(s) for plain HTML form posts.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Function that converts a selected value to its display string for the input field.
    /// Defaults to <c>value?.ToString() ?? string.Empty</c>. Useful when binding to
    /// objects whose <c>ToString()</c> isn't suitable.
    /// </summary>
    [Parameter]
    public Func<TValue?, string>? DisplayValue { get; set; }

    /// <summary>
    /// Fires whenever the user types into the input. The consumer is responsible for
    /// filtering its data based on the query and re-rendering with the filtered
    /// <see cref="HComboboxOption{TValue}"/> children.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnQueryChange { get; set; }

    /// <summary>Child content. Should contain an <see cref="HComboboxInput{TValue}"/> followed by an <see cref="HComboboxOptions{TValue}"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Computed state ───────────────────────────────────────────────────────

    private TValue? CurrentSingleValue =>
        Value is not null ? Value : (Values is null ? _singleValue : default);

    private IReadOnlyCollection<TValue> CurrentMultiValues =>
        Values is not null
            ? Values as IReadOnlyCollection<TValue> ?? Values.ToArray()
            : _multiValues;

    private string DisplayValueOrDefault(TValue? value) =>
        DisplayValue is not null
            ? DisplayValue(value)
            : (value?.ToString() ?? string.Empty);

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Multiple)
        {
            if (Values is null && DefaultValues is not null)
                _multiValues = DefaultValues.ToList();
        }
        else
        {
            if (Value is null && DefaultValue is not null)
                _singleValue = DefaultValue;
        }
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        _ = _initialized;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<ComboboxContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);

            // Click-outside overlay (only present while open).
            if (_isOpen)
            {
                inner.OpenElement(50, "div");
                inner.AddAttribute(51, "data-blazor-headless-overlay", true);
                inner.AddAttribute(52, "style",
                    "position:fixed;inset:0;z-index:30;background:transparent;");
                inner.AddAttribute(53, "onclick",
                    EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await CloseAsync()));
                inner.CloseElement();
            }

            // Hidden form fields.
            if (!string.IsNullOrEmpty(Name))
            {
                if (Multiple)
                {
                    var seq = 60;
                    foreach (var v in CurrentMultiValues)
                    {
                        inner.OpenElement(seq++, "input");
                        inner.AddAttribute(seq++, "type", "hidden");
                        inner.AddAttribute(seq++, "name", Name);
                        inner.AddAttribute(seq++, "value", v?.ToString() ?? string.Empty);
                        inner.CloseElement();
                    }
                }
                else if (CurrentSingleValue is not null)
                {
                    inner.OpenElement(60, "input");
                    inner.AddAttribute(61, "type", "hidden");
                    inner.AddAttribute(62, "name", Name);
                    inner.AddAttribute(63, "value", CurrentSingleValue.ToString() ?? string.Empty);
                    inner.CloseElement();
                }
            }

            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _isOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Context assembly ─────────────────────────────────────────────────────

    private ComboboxContext<TValue> CreateContext() => new(
        isOpen: _isOpen,
        disabled: Disabled,
        multiple: Multiple,
        singleValue: CurrentSingleValue,
        multiValues: CurrentMultiValues,
        activeIndex: _activeIndex,
        query: _query,
        baseId: ComponentId,
        registerOption: RegisterOption,
        unregisterOption: UnregisterOption,
        selectAsync: SelectAsync,
        setActiveIndex: SetActiveIndex,
        handleInputKeyDownAsync: HandleInputKeyDownAsync,
        handleQueryAsync: HandleQueryAsync,
        toggleAsync: ToggleAsync,
        openAsync: OpenAsync,
        closeAsync: CloseAsync,
        displayValue: DisplayValueOrDefault);

    // ── Option registration ──────────────────────────────────────────────────

    internal int RegisterOption(HComboboxOption<TValue> option)
    {
        if (!_options.Contains(option))
            _options.Add(option);
        return _options.IndexOf(option);
    }

    internal void UnregisterOption(HComboboxOption<TValue> option)
    {
        _options.Remove(option);
    }

    private void SetActiveIndex(int index)
    {
        if (index == _activeIndex) return;
        _activeIndex = index;
        StateHasChanged();
    }

    // ── Selection ────────────────────────────────────────────────────────────

    private async Task SelectAsync(TValue value)
    {
        if (Disabled) return;

        if (Multiple)
        {
            // Toggle membership.
            var list = CurrentMultiValues.ToList();
            var idx = list.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, value));
            if (idx >= 0) list.RemoveAt(idx);
            else list.Add(value);

            if (Values is null) _multiValues = list;
            await OnValuesChange.InvokeAsync(list);
            // Multi-select keeps the panel open and doesn't overwrite the query.
        }
        else
        {
            if (Value is null) _singleValue = value;
            await OnValueChange.InvokeAsync(value);

            // Update the input's display text to match the new selection.
            _query = DisplayValueOrDefault(value);
            await OnQueryChange.InvokeAsync(_query);

            await CloseAsync();
        }

        StateHasChanged();
    }

    // ── Open / Close / Toggle ────────────────────────────────────────────────

    private async Task ToggleAsync()
    {
        if (Disabled) return;
        if (_isOpen) await CloseAsync();
        else await OpenAsync();
    }

    private Task OpenAsync()
    {
        if (Disabled) return Task.CompletedTask;
        _isOpen = true;
        _activeIndex = FindInitialActiveIndex();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task CloseAsync()
    {
        if (!_isOpen) return Task.CompletedTask;
        _isOpen = false;
        _activeIndex = -1;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private int FindInitialActiveIndex()
    {
        if (_options.Count == 0) return -1;

        if (!Multiple && CurrentSingleValue is not null)
        {
            var idx = _options.FindIndex(o => !o.IsOptionDisabled
                && EqualityComparer<TValue>.Default.Equals(o.Value, CurrentSingleValue));
            if (idx >= 0) return idx;
        }
        return _options.FindIndex(o => !o.IsOptionDisabled);
    }

    // ── Query / typing ───────────────────────────────────────────────────────

    private async Task HandleQueryAsync(string newQuery)
    {
        _query = newQuery ?? string.Empty;

        // Typing implies the user wants to see results.
        if (!_isOpen)
            await OpenAsync();

        await OnQueryChange.InvokeAsync(_query);

        // After the consumer re-renders with the filtered set, default the
        // active index back to the first enabled option. We can't tell exactly
        // which options will exist after the next render, so we conservatively
        // reset to the first enabled option that's currently registered.
        _activeIndex = _options.FindIndex(o => !o.IsOptionDisabled);
        StateHasChanged();
    }

    // ── Keyboard handling (input) ────────────────────────────────────────────

    private async Task HandleInputKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;

        switch (args.Key)
        {
            case "ArrowDown":
                if (!_isOpen) await OpenAsync();
                else _activeIndex = FindEnabledIndex(_activeIndex + 1, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "ArrowUp":
                if (!_isOpen) await OpenAsync();
                else _activeIndex = FindEnabledIndex(_activeIndex < 0 ? _options.Count - 1 : _activeIndex - 1, step: -1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "Home":
                if (_isOpen)
                {
                    _activeIndex = FindEnabledIndex(0, step: +1) ?? _activeIndex;
                    StateHasChanged();
                }
                break;

            case "End":
                if (_isOpen)
                {
                    _activeIndex = FindEnabledIndex(_options.Count - 1, step: -1) ?? _activeIndex;
                    StateHasChanged();
                }
                break;

            case "Enter":
                if (_isOpen && _activeIndex >= 0 && _activeIndex < _options.Count)
                {
                    await SelectAsync(_options[_activeIndex].Value);
                }
                break;

            case "Escape":
                if (_isOpen)
                    await CloseAsync();
                break;

            case "Tab":
                // Headless UI behaviour: in single-select mode Tab also commits the
                // active option (so focusing away accepts the highlighted item).
                if (_isOpen && !Multiple
                    && _activeIndex >= 0 && _activeIndex < _options.Count)
                {
                    await SelectAsync(_options[_activeIndex].Value);
                }
                else if (_isOpen)
                {
                    await CloseAsync();
                }
                break;
        }
    }

    private int? FindEnabledIndex(int start, int step)
    {
        if (_options.Count == 0) return null;

        var i = ((start % _options.Count) + _options.Count) % _options.Count;
        for (var attempts = 0; attempts < _options.Count; attempts++)
        {
            if (!_options[i].IsOptionDisabled)
                return i;
            i = ((i + step) % _options.Count + _options.Count) % _options.Count;
        }
        return null;
    }
}
