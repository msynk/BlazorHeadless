using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless, accessible Tab Group implementing the WAI-ARIA Tabs pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Roving tabindex</b> — only the active tab is in the tab order; arrow keys move within the list.</item>
///   <item><b>Full keyboard support</b> — Left/Right (or Up/Down for vertical), Home, End, with disabled-tab skipping and wrap-around.</item>
///   <item><b>Automatic vs manual activation</b> — set <see cref="Manual"/> to require Enter/Space to activate (focus-only arrow nav).</item>
///   <item><b>Uncontrolled and controlled</b> — seed via <see cref="DefaultIndex"/> or drive externally with <see cref="SelectedIndex"/> and <see cref="OnSelectedIndexChange"/>.</item>
///   <item><b>Compound API</b> — compose with <see cref="HTabList"/>, <see cref="HTab"/>, <see cref="HTabPanels"/>, <see cref="HTabPanel"/>.</item>
///   <item><b>Data attributes</b> — emits <c>data-orientation</c> and <c>data-disabled</c> on the root and <c>data-state="active"|"inactive"</c> on each tab and panel.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HTabGroup&gt;
///     &lt;HTabList&gt;
///         &lt;HTab&gt;Account&lt;/HTab&gt;
///         &lt;HTab&gt;Profile&lt;/HTab&gt;
///         &lt;HTab Disabled="true"&gt;Billing&lt;/HTab&gt;
///     &lt;/HTabList&gt;
///     &lt;HTabPanels&gt;
///         &lt;HTabPanel&gt;Account settings&lt;/HTabPanel&gt;
///         &lt;HTabPanel&gt;Profile settings&lt;/HTabPanel&gt;
///         &lt;HTabPanel&gt;Billing settings&lt;/HTabPanel&gt;
///     &lt;/HTabPanels&gt;
/// &lt;/HTabGroup&gt;
/// </code>
/// </summary>
public class HTabGroup : HeadlessComponentBase
{
    private readonly List<HTab> _tabs = new();
    private readonly List<HTabPanel> _panels = new();

    private int _selectedIndex;
    private bool _initialized;

    /// <summary>The initial selected tab index when uncontrolled. Ignored when <see cref="SelectedIndex"/> is supplied.</summary>
    [Parameter]
    public int DefaultIndex { get; set; }

    /// <summary>
    /// Controlled selected tab index. When non-null the component runs in controlled mode
    /// and <see cref="OnSelectedIndexChange"/> must update this value.
    /// </summary>
    [Parameter]
    public int? SelectedIndex { get; set; }

    /// <summary>Fires whenever the selected index changes.</summary>
    [Parameter]
    public EventCallback<int> OnSelectedIndexChange { get; set; }

    /// <summary>Whether all tabs are disabled. Individual tabs can also be disabled via <c>HTab.Disabled</c>.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Renders tabs vertically. Switches arrow-key navigation to Up/Down and emits <c>aria-orientation="vertical"</c>.</summary>
    [Parameter]
    public bool Vertical { get; set; }

    /// <summary>
    /// When true, arrow keys move keyboard focus only and Space/Enter is required to activate
    /// the focused tab. When false (the default), arrow keys both move focus and select.
    /// </summary>
    [Parameter]
    public bool Manual { get; set; }

    /// <summary>Child content. Should contain an <see cref="HTabList"/> and an <see cref="HTabPanels"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private TabsOrientation Orientation => Vertical ? TabsOrientation.Vertical : TabsOrientation.Horizontal;

    private int CurrentIndex => SelectedIndex ?? _selectedIndex;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (SelectedIndex is null)
            _selectedIndex = DefaultIndex;
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        _ = _initialized;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<TabsContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Vertical ? "vertical" : "horizontal");
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Tab and Panel registration ───────────────────────────────────────────

    internal int RegisterTab(HTab tab)
    {
        if (!_tabs.Contains(tab))
            _tabs.Add(tab);
        return _tabs.IndexOf(tab);
    }

    internal void UnregisterTab(HTab tab)
    {
        _tabs.Remove(tab);
    }

    internal int RegisterPanel(HTabPanel panel)
    {
        if (!_panels.Contains(panel))
            _panels.Add(panel);
        return _panels.IndexOf(panel);
    }

    internal void UnregisterPanel(HTabPanel panel)
    {
        _panels.Remove(panel);
    }

    // ── Selection and keyboard handling ──────────────────────────────────────

    private TabsContext CreateContext() => new(
        selectedIndex: CurrentIndex,
        disabled: Disabled,
        orientation: Orientation,
        manual: Manual,
        baseId: ComponentId,
        registerTab: RegisterTab,
        unregisterTab: UnregisterTab,
        registerPanel: RegisterPanel,
        unregisterPanel: UnregisterPanel,
        selectAsync: SelectAsync,
        handleKeyDownAsync: HandleKeyDownAsync);

    private async Task SelectAsync(int index)
    {
        if (Disabled) return;
        if (index < 0 || index >= _tabs.Count) return;
        if (_tabs[index].IsTabDisabled) return;
        if (index == CurrentIndex) return;

        if (SelectedIndex is null)
            _selectedIndex = index;

        await OnSelectedIndexChange.InvokeAsync(index);
        StateHasChanged();
    }

    private async Task HandleKeyDownAsync(int currentIndex, KeyboardEventArgs args)
    {
        if (Disabled) return;
        if (_tabs.Count == 0) return;

        var prevKey = Vertical ? "ArrowUp" : "ArrowLeft";
        var nextKey = Vertical ? "ArrowDown" : "ArrowRight";

        int? targetIndex = args.Key switch
        {
            var k when k == prevKey => FindEnabledIndex(currentIndex - 1, step: -1),
            var k when k == nextKey => FindEnabledIndex(currentIndex + 1, step: +1),
            "Home" => FindEnabledIndex(0, step: +1),
            "End" => FindEnabledIndex(_tabs.Count - 1, step: -1),
            "Enter" or " " => Manual ? currentIndex : null, // explicit activation in manual mode
            _ => null,
        };

        if (targetIndex is null) return;

        // Move focus to the target tab (always — both auto and manual modes do this).
        await _tabs[targetIndex.Value].FocusAsync();

        // In automatic-activation mode, also select the focused tab.
        if (!Manual || args.Key is "Enter" or " ")
            await SelectAsync(targetIndex.Value);
    }

    private int? FindEnabledIndex(int start, int step)
    {
        if (_tabs.Count == 0) return null;

        // Wrap into range first.
        var i = ((start % _tabs.Count) + _tabs.Count) % _tabs.Count;

        for (var attempts = 0; attempts < _tabs.Count; attempts++)
        {
            if (!_tabs[i].IsTabDisabled)
                return i;

            i = ((i + step) % _tabs.Count + _tabs.Count) % _tabs.Count;
        }

        return null; // every tab is disabled
    }
}
