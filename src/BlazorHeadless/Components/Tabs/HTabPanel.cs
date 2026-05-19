using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The content panel paired with a single <see cref="HTab"/>. Renders as
/// <c>&lt;div role="tabpanel"&gt;</c>. Hidden via the HTML <c>hidden</c> attribute
/// when its tab is not selected, giving consumers full control over open/close
/// transitions via <c>data-state</c> CSS hooks.
///
/// <para>
/// Panels pair with tabs by document order: the first <see cref="HTabPanel"/>
/// inside <see cref="HTabPanels"/> matches the first <see cref="HTab"/>, and so on.
/// </para>
/// </summary>
public class HTabPanel : HeadlessComponentBase, IDisposable
{
    [CascadingParameter]
    private TabsContext TabsContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="TabPanelRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<TabPanelRenderContext>? ChildContent { get; set; }

    private int _index = -1;

    protected override string DefaultTag => "div";

    private bool IsSelected => TabsContext?.SelectedIndex == _index;

    private TabPanelRenderContext RenderContext => new()
    {
        IsSelected = IsSelected,
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (TabsContext is not null)
            _index = TabsContext.RegisterPanel(this);
    }

    public void Dispose()
    {
        TabsContext?.UnregisterPanel(this);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", TabsContext is not null ? TabsContext.GetPanelId(_index) : ComponentId);
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
        if (TabsContext is not null && _index >= 0)
            attrs["aria-labelledby"] = TabsContext.GetTabId(_index);

        // Panels are focusable so keyboard users can tab into them after the tablist.
        attrs["tabindex"] = 0;

        SetDataState(attrs, IsSelected, "active", "inactive");

        return attrs;
    }
}
