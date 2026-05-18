namespace HeadlessUI.Blazor;

/// <summary>
/// Cascading context provided by <see cref="HField"/> to its descendant form
/// primitives (<see cref="HLabel"/>, <see cref="HInput"/>, <see cref="HTextarea"/>,
/// <see cref="HSelect"/>, <see cref="HDescription"/>). Carries deterministic ids
/// for ARIA wiring and the field-level disabled/invalid state.
/// </summary>
public sealed class FieldContext
{
    internal FieldContext(string baseId, bool disabled, bool invalid)
    {
        BaseId = baseId;
        Disabled = disabled;
        Invalid = invalid;
    }

    /// <summary>Base id used to derive input, label, and description ids.</summary>
    public string BaseId { get; }

    /// <summary>Whether the field is disabled (cascades to all children).</summary>
    public bool Disabled { get; }

    /// <summary>Whether the field is in an invalid state (cascades to all children).</summary>
    public bool Invalid { get; }

    /// <summary>The deterministic id for the input element.</summary>
    public string InputId => $"{BaseId}-input";

    /// <summary>The deterministic id for the label element.</summary>
    public string LabelId => $"{BaseId}-label";

    /// <summary>The deterministic id for the description element.</summary>
    public string DescriptionId => $"{BaseId}-description";
}
