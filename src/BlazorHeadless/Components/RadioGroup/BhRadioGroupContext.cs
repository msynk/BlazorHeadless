namespace BlazorHeadless;

/// <summary>The orientation of an <see cref="BhRadioGroup"/>.</summary>
public enum BhRadioGroupOrientation
{
    /// <summary>Radios flow horizontally.</summary>
    Horizontal,

    /// <summary>Radios flow vertically.</summary>
    Vertical,
}

/// <summary>Cascading context provided by <see cref="BhRadioGroup"/> to its radios.</summary>
public sealed class BhRadioGroupContext
{
    internal BhRadioGroupContext(
        string? selectedValue,
        bool disabled,
        bool required,
        BhRadioGroupOrientation orientation,
        BhRadioGroup group,
        BhCollection<BhRadio> radios)
    {
        SelectedValue = selectedValue;
        Disabled = disabled;
        Required = required;
        Orientation = orientation;
        Group = group;
        Radios = radios;
    }

    internal BhRadioGroup Group { get; }
    internal BhCollection<BhRadio> Radios { get; }

    /// <summary>The checked value, or null.</summary>
    public string? SelectedValue { get; }

    /// <summary>Whether the whole group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether the group is required.</summary>
    public bool Required { get; }

    /// <summary>The orientation.</summary>
    public BhRadioGroupOrientation Orientation { get; }

    /// <summary>Checks the radio with <paramref name="value"/>.</summary>
    public Task SelectAsync(string value) => Group.SelectAsync(value);
}

/// <summary>Render-fragment context exposed by <see cref="BhRadio"/>.</summary>
public sealed record BhRadioRenderContext
{
    /// <summary>Whether this radio is checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>Whether this radio (or the group) is disabled.</summary>
    public required bool Disabled { get; init; }
}
