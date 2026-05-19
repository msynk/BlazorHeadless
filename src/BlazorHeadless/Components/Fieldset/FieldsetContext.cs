namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="HFieldset"/> to its descendant
/// components (<see cref="HLegend"/>, <see cref="HField"/>, and form controls).
/// Carries the fieldset-level disabled state and the legend id for ARIA labelling.
/// </summary>
public sealed class FieldsetContext
{
    internal FieldsetContext(string legendId, bool disabled)
    {
        LegendId = legendId;
        Disabled = disabled;
    }

    /// <summary>The deterministic id for the legend element (used for aria-labelledby).</summary>
    public string LegendId { get; }

    /// <summary>Whether the fieldset is disabled (cascades to all children).</summary>
    public bool Disabled { get; }
}
