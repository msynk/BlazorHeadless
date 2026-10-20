using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>Whether a <see cref="BhToggleGroup"/> allows one or many pressed items.</summary>
public enum BhToggleGroupType
{
    /// <summary>At most one item is on (behaves like a radio group).</summary>
    Single,

    /// <summary>Any number of items can be on.</summary>
    Multiple,
}

/// <summary>
/// A set of two-state buttons — text alignment, view modes, filters. A single
/// tab stop with arrow-key navigation. In <see cref="BhToggleGroupType.Single"/>
/// mode it is a <c>radiogroup</c> of <c>radio</c>s; in multiple mode a <c>group</c>
/// of <c>aria-pressed</c> buttons. Inside a <see cref="BhToolbar"/> its items join
/// the toolbar's arrow-key navigation.
///
/// <code>
/// &lt;BhToggleGroup @bind-Value="align" aria-label="Alignment"&gt;
///     &lt;BhToggleGroupItem Value="left" aria-label="Left"&gt;⇤&lt;/BhToggleGroupItem&gt;
///     &lt;BhToggleGroupItem Value="center" aria-label="Center"&gt;↔&lt;/BhToggleGroupItem&gt;
/// &lt;/BhToggleGroup&gt;
/// </code>
/// </summary>
public class BhToggleGroup : BhComponentBase
{
    private BhRovingFocusContext? _ownRoving;
    private string? _value;
    private List<string> _values = new();

    [CascadingParameter]
    private BhRovingFocusContext? ParentRoving { get; set; }

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>Single (default) or multiple selection.</summary>
    [Parameter]
    public BhToggleGroupType Type { get; set; } = BhToggleGroupType.Single;

    /// <summary>The pressed item (single). Supplying it makes the group controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Initial pressed item when uncontrolled (single).</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>The pressed items (multiple). Supplying it makes the group controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IEnumerable<string>? Values { get; set; }

    /// <summary>Initial pressed items when uncontrolled (multiple).</summary>
    [Parameter]
    public IEnumerable<string>? DefaultValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<string>> ValuesChanged { get; set; }

    /// <summary>In single mode, whether clicking the pressed item turns it off (leaving none). Default: true.</summary>
    [Parameter]
    public bool AllowEmpty { get; set; } = true;

    /// <summary>Disables every item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Arrow-key orientation (ignored inside a toolbar). Default: horizontal.</summary>
    [Parameter]
    public BhRovingOrientation Orientation { get; set; } = BhRovingOrientation.Horizontal;

    /// <summary>Whether arrow keys wrap. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>The items.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool InToolbar => ParentRoving is { IsToolbar: true };

    /// <summary>
    /// Single groups behave like radio groups: arrow keys select (APG). Inside a toolbar
    /// arrow keys only move focus, and Space / Enter select (APG toolbar).
    /// </summary>
    internal bool SelectsOnFocus => Type == BhToggleGroupType.Single && !InToolbar;

    private BhRovingFocusContext Roving => InToolbar ? ParentRoving! : _ownRoving ??= new BhRovingFocusContext(Interop, BhId.New("bh-toggles"));

    private IReadOnlyCollection<string> PressedValues => Type == BhToggleGroupType.Single
        ? (CurrentValue is null ? [] : [CurrentValue])
        : IsProvided(nameof(Values)) ? Values as IReadOnlyCollection<string> ?? Values?.ToArray() ?? [] : _values;

    private string? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;

    protected override void OnInitialized()
    {
        _value = DefaultValue;
        if (DefaultValues is not null) _values = DefaultValues.ToList();
    }

    protected override void OnParametersSet()
    {
        if (!InToolbar)
        {
            Roving.Orientation = Orientation;
            Roving.Loop = Loop;
            Roving.Dir = Dir ?? CascadedDir;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhToggleGroupContext(this, Type, PressedValues, Disabled);

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhRovingFocusContext>>(40);
        builder.AddComponentParameter(41, "Value", Roving);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenComponent<CascadingValue<BhToggleGroupContext>>(0);
            inner.AddComponentParameter(1, "Value", context);
            inner.AddComponentParameter(2, "ChildContent", ChildContent);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = Type == BhToggleGroupType.Single ? "radiogroup" : "group";
        if (!InToolbar && Orientation != BhRovingOrientation.Both)
            SetDataValue(attrs, "orientation", Orientation == BhRovingOrientation.Vertical ? "vertical" : "horizontal");
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal async Task ToggleAsync(string value)
    {
        if (Disabled) return;
        if (Type == BhToggleGroupType.Single)
        {
            var next = CurrentValue == value ? (AllowEmpty ? null : value) : value;
            if (next == CurrentValue) return;
            if (!IsProvided(nameof(Value))) _value = next;
            await ValueChanged.InvokeAsync(next);
        }
        else
        {
            var list = PressedValues.ToList();
            if (!list.Remove(value)) list.Add(value);
            if (!IsProvided(nameof(Values))) _values = list;
            await ValuesChanged.InvokeAsync(list);
        }
        StateHasChanged();
    }
}

/// <summary>State of a <see cref="BhToggleGroup"/> cascaded to its items.</summary>
public sealed class BhToggleGroupContext
{
    internal BhToggleGroupContext(BhToggleGroup group, BhToggleGroupType type, IReadOnlyCollection<string> pressed, bool disabled)
    {
        Group = group;
        Type = type;
        Pressed = pressed;
        Disabled = disabled;
    }

    internal BhToggleGroup Group { get; }

    /// <summary>Single or multiple.</summary>
    public BhToggleGroupType Type { get; }

    /// <summary>The pressed values.</summary>
    public IReadOnlyCollection<string> Pressed { get; }

    /// <summary>Whether the group is disabled.</summary>
    public bool Disabled { get; }

    /// <summary>Whether <paramref name="value"/> is pressed.</summary>
    public bool IsPressed(string value) => Pressed.Contains(value);
}
