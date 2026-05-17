using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// An individual tab in an <see cref="HTabGroup"/>. Renders as a native
/// <c>&lt;button role="tab"&gt;</c> by default and participates in the group's
/// roving-tabindex keyboard navigation.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Sets <c>role="tab"</c>, <c>aria-selected</c>, <c>aria-controls</c> (panel id), and <c>tabindex</c> (0 when active, -1 otherwise).</item>
///   <item>Click selects the tab.</item>
///   <item>Arrow / Home / End keys delegate to the parent group for roving-tabindex navigation.</item>
///   <item>Emits <c>data-state="active"|"inactive"</c> and <c>data-disabled</c> for CSS hooks.</item>
///   <item>Render-prop via <see cref="ChildContent"/> exposes <see cref="TabRenderContext"/> for state-driven content.</item>
/// </list>
/// </summary>
public class HTab : HeadlessComponentBase, IDisposable
{
    [CascadingParameter]
    private TabsContext TabsContext { get; set; } = default!;

    /// <summary>Disables this tab. The arrow-key navigator skips disabled tabs automatically.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Content template receiving <see cref="TabRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<TabRenderContext>? ChildContent { get; set; }

    private int _index = -1;
    private ElementReference _elementRef;

    protected override string DefaultTag => "button";

    /// <summary>Whether this tab is the currently selected one.</summary>
    public bool IsSelected => TabsContext?.SelectedIndex == _index;

    /// <summary>Whether this tab is disabled (own flag or root group).</summary>
    public bool IsTabDisabled => Disabled || (TabsContext?.Disabled ?? false);

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    private TabRenderContext RenderContext => new()
    {
        IsSelected = IsSelected,
        Disabled = IsTabDisabled,
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (TabsContext is not null)
            _index = TabsContext.RegisterTab(this);
    }

    public void Dispose()
    {
        TabsContext?.UnregisterTab(this);
    }

    /// <summary>Moves keyboard focus to this tab. Invoked internally by the parent group during arrow-key navigation.</summary>
    internal ValueTask FocusAsync() => _elementRef.FocusAsync();

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", TabsContext is not null ? TabsContext.GetTabId(_index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "tab";
        attrs["aria-selected"] = IsSelected ? "true" : "false";

        // aria-controls points at the corresponding panel.
        if (TabsContext is not null && _index >= 0)
            attrs["aria-controls"] = TabsContext.GetPanelId(_index);

        // Roving tabindex: only the active tab is in the tab order.
        attrs["tabindex"] = IsSelected ? 0 : -1;

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (IsTabDisabled)
                attrs["disabled"] = true;
        }
        else
        {
            if (IsTabDisabled)
                attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsSelected, "active", "inactive");
        SetDataFlag(attrs, "disabled", IsTabDisabled);

        return attrs;
    }

    // ── Event handling ───────────────────────────────────────────────────────

    private Task HandleClick(MouseEventArgs _)
    {
        if (IsTabDisabled) return Task.CompletedTask;
        return TabsContext?.SelectAsync(_index) ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        return TabsContext?.HandleKeyDownAsync(_index, args) ?? Task.CompletedTask;
    }
}
