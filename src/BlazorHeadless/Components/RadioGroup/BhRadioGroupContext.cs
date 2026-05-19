using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>The orientation of an <see cref="BhRadioGroup"/>. Drives keyboard navigation and the <c>aria-orientation</c> attribute.</summary>
public enum BhRadioGroupOrientation
{
    /// <summary>Radios flow horizontally; Left/Right arrow keys navigate and select.</summary>
    Horizontal,

    /// <summary>Radios flow vertically; Up/Down arrow keys navigate and select.</summary>
    Vertical,
}

/// <summary>
/// Cascading context provided by <see cref="BhRadioGroup"/> to its descendant
/// <see cref="BhRadio"/> elements. Carries selection state, ARIA orientation,
/// and registration callbacks for the group's roving-tabindex keyboard nav.
/// </summary>
public sealed class BhRadioGroupContext
{
    private readonly Func<BhRadio, int> _registerRadio;
    private readonly Action<BhRadio> _unregisterRadio;
    private readonly Func<int, bool> _isTabStop;
    private readonly Func<string?, Task> _selectAsync;
    private readonly Func<int, KeyboardEventArgs, Task> _handleKeyDownAsync;

    internal BhRadioGroupContext(
        string? selectedValue,
        bool disabled,
        bool required,
        BhRadioGroupOrientation orientation,
        Func<BhRadio, int> registerRadio,
        Action<BhRadio> unregisterRadio,
        Func<int, bool> isTabStop,
        Func<string?, Task> selectAsync,
        Func<int, KeyboardEventArgs, Task> handleKeyDownAsync)
    {
        SelectedValue = selectedValue;
        Disabled = disabled;
        Required = required;
        Orientation = orientation;
        _registerRadio = registerRadio;
        _unregisterRadio = unregisterRadio;
        _isTabStop = isTabStop;
        _selectAsync = selectAsync;
        _handleKeyDownAsync = handleKeyDownAsync;
    }

    /// <summary>The currently selected radio value, or null if none is selected.</summary>
    public string? SelectedValue { get; }

    /// <summary>Whether the entire group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the group is marked required.</summary>
    public bool Required { get; }

    /// <summary>Orientation (horizontal or vertical).</summary>
    public BhRadioGroupOrientation Orientation { get; }

    internal int RegisterRadio(BhRadio radio) => _registerRadio(radio);
    internal void UnregisterRadio(BhRadio radio) => _unregisterRadio(radio);
    internal bool IsTabStop(int index) => _isTabStop(index);
    internal Task SelectAsync(string? value) => _selectAsync(value);
    internal Task HandleKeyDownAsync(int index, KeyboardEventArgs args) => _handleKeyDownAsync(index, args);
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhRadio"/> for state-driven
/// rendering of dot indicators, ring styles, etc.
/// </summary>
public sealed record BhRadioRenderContext
{
    /// <summary>Whether this radio is the currently selected one.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether this radio (or the entire group) is disabled.</summary>
    public required bool Disabled { get; init; }
}
