namespace HeadlessUI.Blazor;

/// <summary>
/// Controls whether only one item may be open at a time (<see cref="Single"/>)
/// or multiple items may be open simultaneously (<see cref="Multiple"/>).
/// </summary>
public enum AccordionType
{
    /// <summary>Only one item can be open at a time. Opening an item closes the current one.</summary>
    Single,

    /// <summary>Any number of items can be open simultaneously.</summary>
    Multiple,
}

/// <summary>
/// Cascading context provided by <see cref="HAccordion"/> to all descendant accordion
/// components. Carries the current open-item state and the toggle callback so that
/// <see cref="HAccordionItem"/>, <see cref="HAccordionTrigger"/>, and
/// <see cref="HAccordionContent"/> can coordinate without prop-drilling.
/// </summary>
public sealed class AccordionContext
{
    private readonly Func<string, bool> _isOpen;
    private readonly Action<string> _toggle;

    internal AccordionContext(
        Func<string, bool> isOpen,
        Action<string> toggle,
        bool disabled,
        AccordionType type)
    {
        _isOpen = isOpen;
        _toggle = toggle;
        Disabled = disabled;
        Type = type;
    }

    /// <summary>Returns true when the item identified by <paramref name="value"/> is currently open.</summary>
    public bool IsOpen(string value) => _isOpen(value);

    /// <summary>Toggles the item identified by <paramref name="value"/> open or closed.</summary>
    public void Toggle(string value) => _toggle(value);

    /// <summary>Whether interaction is globally disabled for all items in this accordion.</summary>
    public bool Disabled { get; }

    /// <summary>The open-item selection model for this accordion (Single or Multiple).</summary>
    public AccordionType Type { get; }
}
