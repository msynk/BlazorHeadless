namespace BlazorHeadless;

/// <summary>
/// Render-fragment context exposed by <see cref="BhCheckbox"/> for state-driven
/// rendering of the visual check mark, indeterminate dash, etc.
/// </summary>
public sealed record BhCheckboxRenderContext
{
    /// <summary>Whether the checkbox is currently checked.</summary>
    public required bool IsChecked { get; init; }

    /// <summary>
    /// Whether the checkbox is in the indeterminate (mixed) state. When true the
    /// checkbox visually represents "neither fully checked nor fully unchecked"
    /// — typical for "select all" toggles when only some children are selected.
    /// </summary>
    public required bool IsIndeterminate { get; init; }

    /// <summary>Whether the checkbox is currently disabled.</summary>
    public required bool Disabled { get; init; }
}
