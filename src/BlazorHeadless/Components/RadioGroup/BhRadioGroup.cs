using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Radio Group implementing the WAI-ARIA Radio pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Roving tabindex</b> — only the selected radio (or the first enabled one) is in the tab order; arrow keys move within the group.</item>
///   <item><b>Arrow-key selection</b> — Left/Right (or Up/Down for vertical) move and select. Disabled radios are skipped.</item>
///   <item><b>Uncontrolled and controlled</b> — seed via <see cref="DefaultValue"/> or drive externally with <see cref="Value"/> + <see cref="OnValueChange"/>.</item>
///   <item><b>Form submission</b> — when <see cref="Name"/> is set the group renders a hidden <c>&lt;input&gt;</c> with the selected value, so plain HTML form posts include the group's selection.</item>
///   <item><b>Data attributes</b> — emits <c>data-orientation</c>, <c>data-disabled</c>; each <see cref="BhRadio"/> emits <c>data-state="checked"|"unchecked"</c>.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhRadioGroup DefaultValue="medium" class="radio-group"&gt;
///     &lt;BhRadio Value="small"  class="radio"&gt;Small&lt;/BhRadio&gt;
///     &lt;BhRadio Value="medium" class="radio"&gt;Medium&lt;/BhRadio&gt;
///     &lt;BhRadio Value="large"  class="radio"&gt;Large&lt;/BhRadio&gt;
/// &lt;/BhRadioGroup&gt;
/// </code>
/// </summary>
public class BhRadioGroup : BhComponentBase
{
    private readonly List<BhRadio> _radios = new();

    private string? _selectedValue;
    private bool _initialized;

    /// <summary>Initial selected value when uncontrolled. Ignored when <see cref="Value"/> is supplied.</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Controlled selected value. When non-null the component runs in controlled mode
    /// and <see cref="OnValueChange"/> must update this value.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Fires whenever the selected value changes.</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>Whether the entire group is disabled. Individual <see cref="BhRadio"/> components can also be disabled independently.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the group is marked required (sets <c>aria-required="true"</c>).</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// Renders the group vertically. Switches arrow-key navigation to Up/Down and emits
    /// <c>aria-orientation="vertical"</c>. Defaults to vertical, the more common layout for radios.
    /// </summary>
    [Parameter]
    public bool Vertical { get; set; } = true;

    /// <summary>
    /// Optional form field name. When set the group renders a hidden
    /// <c>&lt;input type="hidden"&gt;</c> carrying the selected value, so plain
    /// HTML form posts include the group's selection.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Child content. Should contain one or more <see cref="BhRadio"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BhRadioGroupOrientation Orientation =>
        Vertical ? BhRadioGroupOrientation.Vertical : BhRadioGroupOrientation.Horizontal;

    private string? SelectedValue => Value ?? _selectedValue;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Value is null)
            _selectedValue = DefaultValue;
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        _ = _initialized;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhRadioGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);

            // Hidden form field carrying the current selection.
            if (!string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(SelectedValue))
            {
                inner.OpenElement(50, "input");
                inner.AddAttribute(51, "type", "hidden");
                inner.AddAttribute(52, "name", Name);
                inner.AddAttribute(53, "value", SelectedValue);
                inner.CloseElement();
            }

            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "radiogroup";
        attrs["aria-orientation"] = Vertical ? "vertical" : "horizontal";

        if (Required)
            attrs["aria-required"] = "true";

        if (Disabled)
            attrs["aria-disabled"] = "true";

        SetDataValue(attrs, "orientation", Vertical ? "vertical" : "horizontal");
        SetDataFlag(attrs, "disabled", Disabled);

        return attrs;
    }

    // ── Registration ─────────────────────────────────────────────────────────

    internal int RegisterRadio(BhRadio radio)
    {
        if (!_radios.Contains(radio))
            _radios.Add(radio);
        return _radios.IndexOf(radio);
    }

    internal void UnregisterRadio(BhRadio radio)
    {
        _radios.Remove(radio);
    }

    /// <summary>
    /// Returns true when the radio at <paramref name="index"/> should be the
    /// member of the group that participates in the document tab order.
    /// The selected radio takes precedence; otherwise the first enabled radio.
    /// </summary>
    internal bool IsTabStop(int index)
    {
        if (index < 0 || index >= _radios.Count) return false;

        // Selected radio is always the tab stop.
        var selectedIdx = _radios.FindIndex(r => r.Value == SelectedValue && !r.IsRadioDisabled);
        if (selectedIdx >= 0)
            return index == selectedIdx;

        // Otherwise the first enabled radio.
        var firstEnabled = _radios.FindIndex(r => !r.IsRadioDisabled);
        return index == firstEnabled;
    }

    // ── Selection and keyboard handling ──────────────────────────────────────

    private BhRadioGroupContext CreateContext() => new(
        selectedValue: SelectedValue,
        disabled: Disabled,
        required: Required,
        orientation: Orientation,
        registerRadio: RegisterRadio,
        unregisterRadio: UnregisterRadio,
        isTabStop: IsTabStop,
        selectAsync: SelectAsync,
        handleKeyDownAsync: HandleKeyDownAsync);

    private async Task SelectAsync(string? value)
    {
        if (Disabled) return;
        if (value == SelectedValue) return;

        // Refuse to select a disabled radio.
        var radio = _radios.FirstOrDefault(r => r.Value == value);
        if (radio is { IsRadioDisabled: true }) return;

        if (Value is null)
            _selectedValue = value;

        await OnValueChange.InvokeAsync(value);
        StateHasChanged();
    }

    private async Task HandleKeyDownAsync(int currentIndex, KeyboardEventArgs args)
    {
        if (Disabled) return;
        if (_radios.Count == 0) return;

        var prevKey = Vertical ? "ArrowUp" : "ArrowLeft";
        var nextKey = Vertical ? "ArrowDown" : "ArrowRight";

        int? targetIndex = args.Key switch
        {
            var k when k == prevKey => FindEnabledIndex(currentIndex - 1, step: -1),
            var k when k == nextKey => FindEnabledIndex(currentIndex + 1, step: +1),
            " " => currentIndex, // Space selects the focused radio
            _ => null,
        };

        if (targetIndex is null) return;

        // Move focus and (per ARIA Radio pattern) select the target.
        await _radios[targetIndex.Value].FocusAsync();
        await SelectAsync(_radios[targetIndex.Value].Value);
    }

    private int? FindEnabledIndex(int start, int step)
    {
        if (_radios.Count == 0) return null;

        var i = ((start % _radios.Count) + _radios.Count) % _radios.Count;

        for (var attempts = 0; attempts < _radios.Count; attempts++)
        {
            if (!_radios[i].IsRadioDisabled)
                return i;
            i = ((i + step) % _radios.Count + _radios.Count) % _radios.Count;
        }

        return null;
    }
}
