using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Tab Group implementing the WAI-ARIA Tabs pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Selection by value or index</b> — give tabs / panels a <c>Value</c> and use <c>@bind-Value</c>, or rely on order with <c>@bind-SelectedIndex</c>.</item>
///   <item><b>Roving tabindex</b> — only the selected tab is in the tab order; arrows move within the list.</item>
///   <item><b>Keyboard</b> — Left / Right (Up / Down when vertical, mirrored in RTL), Home, End; disabled tabs are skipped; wraps unless <see cref="Loop"/> is false.</item>
///   <item><b>Automatic or manual activation</b> — <see cref="Manual"/> requires Enter / Space to activate the focused tab.</item>
///   <item><b>Panels</b> — hidden when inactive (exit animations supported); <see cref="BhTabPanel.Unmount"/> removes inactive panels.</item>
/// </list>
///
/// <code>
/// &lt;BhTabGroup @bind-Value="tab"&gt;
///     &lt;BhTabList&gt;
///         &lt;BhTab Value="account"&gt;Account&lt;/BhTab&gt;
///         &lt;BhTab Value="billing" Disabled="true"&gt;Billing&lt;/BhTab&gt;
///     &lt;/BhTabList&gt;
///     &lt;BhTabPanels&gt;
///         &lt;BhTabPanel Value="account"&gt;…&lt;/BhTabPanel&gt;
///         &lt;BhTabPanel Value="billing"&gt;…&lt;/BhTabPanel&gt;
///     &lt;/BhTabPanels&gt;
/// &lt;/BhTabGroup&gt;
/// </code>
/// </summary>
public class BhTabGroup : BhComponentBase
{
    private readonly BhCollection<BhTab> _tabs = new(BhId.New("bh-tabs"));
    private readonly BhCollection<BhTabPanel> _panels = new(BhId.New("bh-panels"));
    private int _index;
    private string? _value;
    private bool _initialSelectionResolved;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>The selected tab's value (value mode). Supplying it makes the group controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public string? Value { get; set; }

    /// <summary>Initial selected value when uncontrolled (value mode).</summary>
    [Parameter]
    public string? DefaultValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>Fires when the selected value changes.</summary>
    [Parameter]
    public EventCallback<string?> OnValueChange { get; set; }

    /// <summary>The selected tab index. Supplying it makes the group controlled (use <c>@bind-SelectedIndex</c>).</summary>
    [Parameter]
    public int SelectedIndex { get; set; }

    /// <summary>Initial selected index when uncontrolled.</summary>
    [Parameter]
    public int DefaultIndex { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedIndex"/>.</summary>
    [Parameter]
    public EventCallback<int> SelectedIndexChanged { get; set; }

    /// <summary>Fires whenever the selected index changes.</summary>
    [Parameter]
    public EventCallback<int> OnSelectedIndexChange { get; set; }

    /// <summary>Disables every tab.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Lays the tabs out vertically (Up / Down navigate).</summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>Requires Enter / Space to activate the focused tab (arrows only move focus).</summary>
    [Parameter]
    public bool Manual { get; set; }

    /// <summary>Whether arrow keys wrap around. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction (mirrors Left / Right). Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>A <see cref="BhTabList"/> and <see cref="BhTabPanels"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private BhOrientation Orientation => Vertical ? BhOrientation.Vertical : BhOrientation.Horizontal;
    private BhDirection EffectiveDir => Dir ?? CascadedDir;
    private bool ValueMode => IsProvided(nameof(Value)) || DefaultValue is not null || _tabs.Items.Any(t => t.Value is not null);
    private string? CurrentValue => IsProvided(nameof(Value)) ? Value : _value;
    private int CurrentIndex => IsProvided(nameof(SelectedIndex)) ? SelectedIndex : _index;

    protected override void OnInitialized()
    {
        _index = DefaultIndex;
        _value = DefaultValue;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await _tabs.EnsureOrderAsync(Interop);
        await _panels.EnsureOrderAsync(Interop);

        // Uncontrolled with nothing (or a disabled tab) selected: select the first enabled tab.
        if (!_initialSelectionResolved && _tabs.Count > 0)
        {
            _initialSelectionResolved = true;
            var selected = SelectedTab;
            if ((selected is null || selected.IsItemDisabled) && !IsProvided(nameof(Value)) && !IsProvided(nameof(SelectedIndex)))
            {
                var first = _tabs.First();
                if (first is not null && first != selected)
                {
                    if (ValueMode) _value = first.Value;
                    _index = _tabs.IndexOf(first);
                    StateHasChanged();
                }
            }
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhTabsContext(this, CurrentIndex, Disabled, Orientation, Manual, ComponentId, _tabs, _panels);

        builder.OpenComponent<CascadingValue<BhTabsContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());
            inner.AddElementReferenceCapture(30, CaptureElement);
            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }

    // ── Selection model ──────────────────────────────────────────────────────

    internal BhTab? SelectedTab
        => ValueMode
            ? _tabs.Items.FirstOrDefault(t => t.Value is not null && t.Value == CurrentValue)
            : CurrentIndex >= 0 && CurrentIndex < _tabs.Count ? _tabs.Items[CurrentIndex] : null;

    internal bool IsSelected(BhTab tab) => ReferenceEquals(SelectedTab, tab);

    internal BhTab? TabForPanel(BhTabPanel panel)
    {
        if (panel.Value is not null)
            return _tabs.Items.FirstOrDefault(t => t.Value == panel.Value);
        var index = _panels.IndexOf(panel);
        return index >= 0 && index < _tabs.Count ? _tabs.Items[index] : null;
    }

    internal BhTabPanel? PanelForTab(BhTab tab)
    {
        if (tab.Value is not null && _panels.Items.FirstOrDefault(p => p.Value == tab.Value) is { } byValue)
            return byValue;
        var index = _tabs.IndexOf(tab);
        return index >= 0 && index < _panels.Count && _panels.Items[index].Value is null ? _panels.Items[index] : null;
    }

    internal async Task SelectAsync(BhTab tab)
    {
        if (Disabled || tab.IsItemDisabled || IsSelected(tab)) return;
        var index = _tabs.IndexOf(tab);

        if (!IsProvided(nameof(Value))) _value = tab.Value;
        if (!IsProvided(nameof(SelectedIndex))) _index = index;

        if (tab.Value is not null || ValueMode)
        {
            await ValueChanged.InvokeAsync(tab.Value);
            await OnValueChange.InvokeAsync(tab.Value);
        }
        await SelectedIndexChanged.InvokeAsync(index);
        await OnSelectedIndexChange.InvokeAsync(index);
        StateHasChanged();
    }

    internal async Task HandleKeyDownAsync(BhTab tab, KeyboardEventArgs e)
    {
        if (Disabled) return;
        var next = BhKeyboard.NextKey(Orientation, EffectiveDir);
        var prev = BhKeyboard.PreviousKey(Orientation, EffectiveDir);

        BhTab? target = null;
        if (e.Key == next) target = _tabs.Next(tab, Loop);
        else if (e.Key == prev) target = _tabs.Previous(tab, Loop);
        else if (e.Key == "Home") target = _tabs.First();
        else if (e.Key == "End") target = _tabs.Last();
        else if (Manual && e.Key is "Enter" or " ")
        {
            await SelectAsync(tab);
            return;
        }

        if (target is null || target == tab) return;
        await Interop.FocusAsync(target.ItemId);
        if (!Manual) await SelectAsync(target);
    }
}
