using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The content panel paired with a single <see cref="BhTab"/>. Renders as
/// <c>&lt;div role="tabpanel"&gt;</c>. Hidden via the HTML <c>hidden</c> attribute
/// when its tab is not selected, giving consumers full control over open/close
/// transitions via <c>data-state</c> CSS hooks.
///
/// <para>
/// Panels pair with tabs by document order: the first <see cref="BhTabPanel"/>
/// inside <see cref="BhTabPanels"/> matches the first <see cref="BhTab"/>, and so on.
/// </para>
/// </summary>
public class BhTabPanel : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhTabsContext BhTabsContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhTabPanelRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhTabPanelRenderContext>? ChildContent { get; set; }

    private int _index = -1;

    protected override string DefaultTag => "div";

    private bool IsSelected => BhTabsContext?.SelectedIndex == _index;

    private BhTabPanelRenderContext RenderContext => new()
    {
        IsSelected = IsSelected,
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (BhTabsContext is not null)
            _index = BhTabsContext.RegisterPanel(this);
    }

    public void Dispose()
    {
        BhTabsContext?.UnregisterPanel(this);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhTabsContext is not null ? BhTabsContext.GetPanelId(_index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsSelected)
            builder.AddAttribute(30, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "tabpanel";

        // aria-labelledby points back at the corresponding tab.
        if (BhTabsContext is not null && _index >= 0)
            attrs["aria-labelledby"] = BhTabsContext.GetTabId(_index);

        // Panels are focusable so keyboard users can tab into them after the tablist.
        attrs["tabindex"] = 0;

        SetDataState(attrs, IsSelected, "active", "inactive");

        return attrs;
    }
}
