using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// Cascading context provided by <see cref="HCombobox{TValue}"/> to its descendant
/// parts (<see cref="HComboboxInput{TValue}"/>, <see cref="HComboboxButton{TValue}"/>,
/// <see cref="HComboboxOptions{TValue}"/>, <see cref="HComboboxOption{TValue}"/>).
///
/// <para>
/// Carries open / selection / "active" (highlighted) state plus the registration
/// and keyboard delegates the parts use to coordinate.
/// </para>
/// </summary>
public sealed class ComboboxContext<TValue>
{
    private readonly Func<HComboboxOption<TValue>, int> _registerOption;
    private readonly Action<HComboboxOption<TValue>> _unregisterOption;
    private readonly Func<TValue, Task> _selectAsync;
    private readonly Action<int> _setActiveIndex;
    private readonly Func<KeyboardEventArgs, Task> _handleInputKeyDownAsync;
    private readonly Func<string, Task> _handleQueryAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Func<Task> _openAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Func<TValue?, string> _displayValue;

    internal ComboboxContext(
        bool isOpen,
        bool disabled,
        bool multiple,
        TValue? singleValue,
        IReadOnlyCollection<TValue> multiValues,
        int activeIndex,
        string query,
        string baseId,
        Func<HComboboxOption<TValue>, int> registerOption,
        Action<HComboboxOption<TValue>> unregisterOption,
        Func<TValue, Task> selectAsync,
        Action<int> setActiveIndex,
        Func<KeyboardEventArgs, Task> handleInputKeyDownAsync,
        Func<string, Task> handleQueryAsync,
        Func<Task> toggleAsync,
        Func<Task> openAsync,
        Func<Task> closeAsync,
        Func<TValue?, string> displayValue)
    {
        IsOpen = isOpen;
        Disabled = disabled;
        Multiple = multiple;
        SingleValue = singleValue;
        MultiValues = multiValues;
        ActiveIndex = activeIndex;
        Query = query;
        BaseId = baseId;
        _registerOption = registerOption;
        _unregisterOption = unregisterOption;
        _selectAsync = selectAsync;
        _setActiveIndex = setActiveIndex;
        _handleInputKeyDownAsync = handleInputKeyDownAsync;
        _handleQueryAsync = handleQueryAsync;
        _toggleAsync = toggleAsync;
        _openAsync = openAsync;
        _closeAsync = closeAsync;
        _displayValue = displayValue;
    }

    /// <summary>Whether the options panel is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the combobox is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the combobox allows multi-select.</summary>
    public bool Multiple { get; }

    /// <summary>The currently selected value (single-select mode).</summary>
    public TValue? SingleValue { get; }

    /// <summary>The currently selected values (multi-select mode).</summary>
    public IReadOnlyCollection<TValue> MultiValues { get; }

    /// <summary>The currently active (highlighted) option index, or -1 if none.</summary>
    public int ActiveIndex { get; }

    /// <summary>The current query string typed into the input.</summary>
    public string Query { get; }

    /// <summary>The base id used to derive deterministic input/options/option ids.</summary>
    public string BaseId { get; }

    /// <summary>The deterministic id of the input element.</summary>
    public string InputId => $"{BaseId}-input";

    /// <summary>The deterministic id of the options panel.</summary>
    public string OptionsId => $"{BaseId}-options";

    /// <summary>Returns the deterministic id for the option at <paramref name="index"/>.</summary>
    public string GetOptionId(int index) => $"{BaseId}-option-{index}";

    /// <summary>Returns the display string for <paramref name="value"/> using the configured display function.</summary>
    public string DisplayValue(TValue? value) => _displayValue(value);

    /// <summary>Returns whether <paramref name="value"/> is currently in the selection.</summary>
    public bool IsSelected(TValue value)
    {
        if (Multiple)
            return MultiValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, value));
        return EqualityComparer<TValue>.Default.Equals(SingleValue, value);
    }

    internal int RegisterOption(HComboboxOption<TValue> option) => _registerOption(option);
    internal void UnregisterOption(HComboboxOption<TValue> option) => _unregisterOption(option);
    internal Task SelectAsync(TValue value) => _selectAsync(value);
    internal void SetActiveIndex(int index) => _setActiveIndex(index);
    internal Task HandleInputKeyDownAsync(KeyboardEventArgs args) => _handleInputKeyDownAsync(args);
    internal Task HandleQueryAsync(string query) => _handleQueryAsync(query);
    internal Task ToggleAsync() => _toggleAsync();
    internal Task OpenAsync() => _openAsync();
    internal Task CloseAsync() => _closeAsync();

    /// <summary>Gets the input element reference for anchor positioning.</summary>
    internal ElementReference InputRef { get; private set; }

    /// <summary>Sets the input element reference.</summary>
    internal void SetInputRef(ElementReference inputRef) => InputRef = inputRef;
}

/// <summary>
/// Render-fragment context exposed by <see cref="HComboboxButton{TValue}"/>.
/// </summary>
public sealed record ComboboxButtonRenderContext<TValue>
{
    /// <summary>Whether the listbox panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the combobox is disabled.</summary>
    public required bool Disabled { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="HComboboxOption{TValue}"/>.
/// </summary>
public sealed record ComboboxOptionRenderContext<TValue>
{
    /// <summary>The option's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Whether this option is currently selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Whether this option is currently the "active" (highlighted) option.</summary>
    public required bool IsActive { get; init; }

    /// <summary>Whether this option is disabled.</summary>
    public required bool Disabled { get; init; }
}
