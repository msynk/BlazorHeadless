using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// Cascading context provided by <see cref="HListbox{TValue}"/> to its descendant
/// listbox parts (<see cref="HListboxButton{TValue}"/>, <see cref="HListboxOptions{TValue}"/>,
/// <see cref="HListboxOption{TValue}"/>). Carries open state, selection, the
/// "active" (highlighted) option index, and the registration / keyboard delegates
/// the parts use to coordinate.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class ListboxContext<TValue>
{
    private readonly Func<HListboxOption<TValue>, int> _registerOption;
    private readonly Action<HListboxOption<TValue>> _unregisterOption;
    private readonly Func<TValue, Task> _selectAsync;
    private readonly Action<int> _setActiveIndex;
    private readonly Func<int, KeyboardEventArgs, Task> _handleOptionKeyDownAsync;
    private readonly Func<KeyboardEventArgs, Task> _handleButtonKeyDownAsync;
    private readonly Func<Task> _toggleAsync;
    private readonly Func<Task> _closeAsync;
    private readonly Action<ElementReference> _registerButton;

    internal ListboxContext(
        bool isOpen,
        bool disabled,
        bool multiple,
        TValue? singleValue,
        IReadOnlyCollection<TValue> multiValues,
        int activeIndex,
        string baseId,
        Func<HListboxOption<TValue>, int> registerOption,
        Action<HListboxOption<TValue>> unregisterOption,
        Func<TValue, Task> selectAsync,
        Action<int> setActiveIndex,
        Func<int, KeyboardEventArgs, Task> handleOptionKeyDownAsync,
        Func<KeyboardEventArgs, Task> handleButtonKeyDownAsync,
        Func<Task> toggleAsync,
        Func<Task> closeAsync,
        Action<ElementReference> registerButton)
    {
        IsOpen = isOpen;
        Disabled = disabled;
        Multiple = multiple;
        SingleValue = singleValue;
        MultiValues = multiValues;
        ActiveIndex = activeIndex;
        BaseId = baseId;
        _registerOption = registerOption;
        _unregisterOption = unregisterOption;
        _selectAsync = selectAsync;
        _setActiveIndex = setActiveIndex;
        _handleOptionKeyDownAsync = handleOptionKeyDownAsync;
        _handleButtonKeyDownAsync = handleButtonKeyDownAsync;
        _toggleAsync = toggleAsync;
        _closeAsync = closeAsync;
        _registerButton = registerButton;
    }

    /// <summary>Whether the options panel is currently open.</summary>
    public bool IsOpen { get; }

    /// <summary>Whether the listbox is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the listbox allows multiple selection.</summary>
    public bool Multiple { get; }

    /// <summary>The currently selected value when single-select; default for value types when none.</summary>
    public TValue? SingleValue { get; }

    /// <summary>The currently selected values when multi-select.</summary>
    public IReadOnlyCollection<TValue> MultiValues { get; }

    /// <summary>The index of the currently "active" (highlighted) option, or -1 if none.</summary>
    public int ActiveIndex { get; }

    /// <summary>The base id used to derive the button, options panel, and per-option ids.</summary>
    public string BaseId { get; }

    /// <summary>The deterministic id of the button element. Drives aria-activedescendant references.</summary>
    public string ButtonId => $"{BaseId}-button";

    /// <summary>The deterministic id of the options panel.</summary>
    public string OptionsId => $"{BaseId}-options";

    /// <summary>Returns the deterministic id for the option at <paramref name="index"/>.</summary>
    public string GetOptionId(int index) => $"{BaseId}-option-{index}";

    /// <summary>Returns whether <paramref name="value"/> matches the current selection (single or in the multi set).</summary>
    public bool IsSelected(TValue value)
    {
        if (Multiple)
            return MultiValues.Any(v => EqualityComparer<TValue>.Default.Equals(v, value));
        return EqualityComparer<TValue>.Default.Equals(SingleValue, value);
    }

    internal int RegisterOption(HListboxOption<TValue> option) => _registerOption(option);
    internal void UnregisterOption(HListboxOption<TValue> option) => _unregisterOption(option);
    internal Task SelectAsync(TValue value) => _selectAsync(value);
    internal void SetActiveIndex(int index) => _setActiveIndex(index);
    internal Task HandleOptionKeyDownAsync(int index, KeyboardEventArgs args) => _handleOptionKeyDownAsync(index, args);
    internal Task HandleButtonKeyDownAsync(KeyboardEventArgs args) => _handleButtonKeyDownAsync(args);
    internal Task ToggleAsync() => _toggleAsync();
    internal Task CloseAsync() => _closeAsync();
    internal void RegisterButton(ElementReference button) => _registerButton(button);

    /// <summary>Gets the button element reference for anchor positioning.</summary>
    internal ElementReference ButtonRef { get; private set; }

    /// <summary>Sets the button element reference.</summary>
    internal void SetButtonRef(ElementReference buttonRef) => ButtonRef = buttonRef;
}

/// <summary>
/// Render-fragment context exposed by <see cref="HListbox{TValue}"/>. Provides
/// open state, the selection, and a <c>Close()</c> callback for use anywhere
/// inside the listbox.
/// </summary>
public sealed record ListboxRenderContext<TValue>
{
    /// <summary>Whether the listbox panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>The currently selected value (single-select mode).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The currently selected values (multi-select mode).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }

    /// <summary>Closes the listbox panel.</summary>
    public required Action Close { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="HListboxButton{TValue}"/>.
/// </summary>
public sealed record ListboxButtonRenderContext<TValue>
{
    /// <summary>Whether the listbox panel is currently open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the listbox is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The currently selected value (single-select mode).</summary>
    public required TValue? Value { get; init; }

    /// <summary>The currently selected values (multi-select mode).</summary>
    public required IReadOnlyCollection<TValue> Values { get; init; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="HListboxOption{TValue}"/>.
/// </summary>
public sealed record ListboxOptionRenderContext<TValue>
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
