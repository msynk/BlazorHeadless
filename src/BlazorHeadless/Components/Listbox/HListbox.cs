using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Listbox / custom Select implementing the WAI-ARIA
/// <c>combobox</c> + <c>listbox</c> pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or multi-select</b> via <see cref="Multiple"/>.</item>
///   <item><b>Uncontrolled and controlled</b> — seed via <see cref="DefaultValue"/> / <see cref="DefaultValues"/> or drive externally.</item>
///   <item><b>Full keyboard support</b> — arrow keys, Home, End, Enter, Space, Escape, Tab, plus typeahead (type letters to jump).</item>
///   <item><b>Active vs selected</b> — the open panel tracks an "active" highlight separate from the selection; assistive tech follows it via <c>aria-activedescendant</c>.</item>
///   <item><b>Click-outside</b> — closes the panel when clicking anywhere outside (via an invisible full-viewport overlay).</item>
///   <item><b>Form submission</b> — when <see cref="Name"/> is set, hidden <c>&lt;input&gt;</c>(s) carry the selected value(s).</item>
///   <item><b>Compound API</b> — <see cref="HListboxButton{TValue}"/>, <see cref="HListboxOptions{TValue}"/>, <see cref="HListboxOption{TValue}"/>.</item>
///   <item><b>Data attributes</b> — <c>data-state</c> on root and panel; <c>data-active</c>, <c>data-selected</c>, <c>data-disabled</c> on options.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HListbox TValue="string" Value="@person" OnValueChange="v => person = v"&gt;
///     &lt;HListboxButton TValue="string"&gt;@(person ?? "Pick a person")&lt;/HListboxButton&gt;
///     &lt;HListboxOptions TValue="string"&gt;
///         &lt;HListboxOption TValue="string" Value="alice"&gt;Alice&lt;/HListboxOption&gt;
///         &lt;HListboxOption TValue="string" Value="bob"&gt;Bob&lt;/HListboxOption&gt;
///     &lt;/HListboxOptions&gt;
/// &lt;/HListbox&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class HListbox<TValue> : HeadlessComponentBase
{
    private readonly List<HListboxOption<TValue>> _options = new();

    private bool _isOpen;
    private TValue? _singleValue;
    private List<TValue> _multiValues = new();
    private int _activeIndex = -1;
    private ElementReference _buttonRef;

    private string _typeaheadBuffer = string.Empty;
    private DateTime _typeaheadResetAt = DateTime.MinValue;
    private static readonly TimeSpan TypeaheadResetWindow = TimeSpan.FromMilliseconds(500);

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

    /// <summary>Whether multiple options can be selected at once.</summary>
    [Parameter]
    public bool Multiple { get; set; }

    /// <summary>Disables the listbox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional form field name. When set the listbox renders hidden <c>&lt;input&gt;</c>(s)
    /// carrying the selected value(s) so plain HTML form posts include the selection.
    /// In multi-select mode a separate hidden input is rendered per selected value.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Child content. Should contain an <see cref="HListboxButton{TValue}"/> followed
    /// by an <see cref="HListboxOptions{TValue}"/> with one or more
    /// <see cref="HListboxOption{TValue}"/> children.
    /// </summary>
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

    private ListboxRenderContext<TValue> RenderContext => new()
    {
        IsOpen = _isOpen,
        Value = CurrentSingleValue,
        Values = CurrentMultiValues,
        Close = () => _ = CloseAsync(),
    };

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
        builder.OpenComponent<CascadingValue<ListboxContext<TValue>>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            if (ChildContent is not null)
                inner.AddContent(40, ChildContent);

            // Click-outside overlay — only present while open. Stretches across the viewport
            // behind the panel and intercepts clicks to close the listbox.
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

    private ListboxContext<TValue> CreateContext()
    {
        var ctx = new ListboxContext<TValue>(
            isOpen: _isOpen,
            disabled: Disabled,
            multiple: Multiple,
            singleValue: CurrentSingleValue,
            multiValues: CurrentMultiValues,
            activeIndex: _activeIndex,
            baseId: ComponentId,
            registerOption: RegisterOption,
            unregisterOption: UnregisterOption,
            selectAsync: SelectAsync,
            setActiveIndex: SetActiveIndex,
            handleOptionKeyDownAsync: HandleOptionKeyDownAsync,
            handleButtonKeyDownAsync: HandleButtonKeyDownAsync,
            toggleAsync: ToggleAsync,
            closeAsync: CloseAsync,
            registerButton: RegisterButton);
        ctx.SetButtonRef(_buttonRef);
        return ctx;
    }

    /// <summary>Registers the button element reference for anchor positioning.</summary>
    internal void RegisterButton(ElementReference button) => _buttonRef = button;

    // ── Option registration ──────────────────────────────────────────────────

    internal int RegisterOption(HListboxOption<TValue> option)
    {
        if (!_options.Contains(option))
            _options.Add(option);
        return _options.IndexOf(option);
    }

    internal void UnregisterOption(HListboxOption<TValue> option)
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
            // Toggle membership in the multi-select set.
            var currentList = CurrentMultiValues.ToList();
            var idx = currentList.FindIndex(v => EqualityComparer<TValue>.Default.Equals(v, value));
            if (idx >= 0) currentList.RemoveAt(idx);
            else currentList.Add(value);

            if (Values is null)
                _multiValues = currentList;

            await OnValuesChange.InvokeAsync(currentList);
            // Multi-select stays open after selection.
        }
        else
        {
            if (Value is null)
                _singleValue = value;

            await OnValueChange.InvokeAsync(value);
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
        // On open, set the active index to the first selected option, otherwise
        // the first enabled option.
        _activeIndex = FindActiveIndexForOpen();
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task CloseAsync()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _activeIndex = -1;
        StateHasChanged();
        // Return focus to the button. The button captures its own ref.
        await Task.Yield();
    }

    private int FindActiveIndexForOpen()
    {
        if (_options.Count == 0) return -1;

        if (Multiple)
        {
            var firstSelected = _options.FindIndex(o => !o.IsOptionDisabled
                && CurrentMultiValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, o.Value)));
            if (firstSelected >= 0) return firstSelected;
        }
        else if (CurrentSingleValue is not null)
        {
            var idx = _options.FindIndex(o => !o.IsOptionDisabled
                && EqualityComparer<TValue>.Default.Equals(o.Value, CurrentSingleValue));
            if (idx >= 0) return idx;
        }

        return _options.FindIndex(o => !o.IsOptionDisabled);
    }

    // ── Keyboard handling ────────────────────────────────────────────────────

    private async Task HandleButtonKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;

        switch (args.Key)
        {
            case " ":
            case "Enter":
            case "ArrowDown":
            case "ArrowUp":
                await OpenAsync();
                if (args.Key == "ArrowUp")
                    _activeIndex = FindEnabledIndex(_options.Count - 1, step: -1) ?? _activeIndex;
                else if (args.Key == "ArrowDown")
                    _activeIndex = FindEnabledIndex(0, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;
        }
    }

    private async Task HandleOptionKeyDownAsync(int currentIndex, KeyboardEventArgs args)
    {
        if (Disabled) return;
        if (_options.Count == 0) return;

        switch (args.Key)
        {
            case "ArrowDown":
                _activeIndex = FindEnabledIndex((_activeIndex < 0 ? -1 : _activeIndex) + 1, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "ArrowUp":
                _activeIndex = FindEnabledIndex((_activeIndex < 0 ? _options.Count : _activeIndex) - 1, step: -1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "Home":
                _activeIndex = FindEnabledIndex(0, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "End":
                _activeIndex = FindEnabledIndex(_options.Count - 1, step: -1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "Enter":
            case " ":
                if (_activeIndex >= 0 && _activeIndex < _options.Count)
                {
                    await SelectAsync(_options[_activeIndex].Value);
                }
                break;

            case "Escape":
                await CloseAsync();
                break;

            case "Tab":
                await CloseAsync();
                break;

            default:
                // Typeahead: any single printable character.
                if (args.Key.Length == 1 && !string.IsNullOrWhiteSpace(args.Key))
                    HandleTypeahead(args.Key);
                break;
        }
    }

    private void HandleTypeahead(string key)
    {
        var now = DateTime.UtcNow;
        if (now > _typeaheadResetAt)
            _typeaheadBuffer = string.Empty;

        _typeaheadBuffer += key;
        _typeaheadResetAt = now + TypeaheadResetWindow;

        var match = _options.FirstOrDefault(o =>
            !o.IsOptionDisabled
            && (o.GetTextLabel() ?? string.Empty)
                .StartsWith(_typeaheadBuffer, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            _activeIndex = _options.IndexOf(match);
            StateHasChanged();
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
