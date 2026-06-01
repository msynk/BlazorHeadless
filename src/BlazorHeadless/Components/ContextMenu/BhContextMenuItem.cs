using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A single actionable item inside a <see cref="BhContextMenuContent"/>. Renders as
/// <c>&lt;li role="menuitem"&gt;</c> by default.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Click invokes <see cref="OnClick"/> and closes the menu.</item>
///   <item>Mouse-enter sets this item as the "active" highlight.</item>
///   <item>Emits <c>data-active</c> and <c>data-disabled</c> for CSS hooks.</item>
///   <item>Render-prop via <see cref="ChildContent"/> exposes <see cref="BhContextMenuItemRenderContext"/>.</item>
/// </list>
///
/// <para>
/// Provide a <see cref="Label"/> string when <see cref="ChildContent"/> contains
/// rich markup so the typeahead matcher can still find this item by text.
/// </para>
/// </summary>
public class BhContextMenuItem : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhContextMenuContext BhContextMenuContext { get; set; } = default!;

    /// <summary>The action to invoke when this item is activated (clicked or Enter/Space).</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Disables this item. Disabled items are skipped by arrow-key nav and typeahead.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional plain-text label used by the typeahead matcher. When omitted the
    /// matcher has nothing to match against, so provide this when
    /// <see cref="ChildContent"/> contains rich markup.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>
    /// Content template receiving <see cref="BhContextMenuItemRenderContext"/> for
    /// state-driven rendering.
    /// </summary>
    [Parameter]
    public RenderFragment<BhContextMenuItemRenderContext>? ChildContent { get; set; }

    private int _index = -1;

    protected override string DefaultTag => "li";

    /// <summary>Whether this item is the currently "active" (highlighted) item.</summary>
    public bool IsActive => BhContextMenuContext?.ActiveIndex == _index;

    /// <summary>Whether this item is disabled (own flag or root menu).</summary>
    public bool IsItemDisabled => Disabled || (BhContextMenuContext?.Disabled ?? false);

    private BhContextMenuItemRenderContext RenderContext => new()
    {
        IsActive = IsActive,
        Disabled = IsItemDisabled,
    };

    /// <summary>Returns the text label used by the typeahead matcher.</summary>
    internal string? GetTextLabel() => Label;

    /// <summary>Invokes the item's <see cref="OnClick"/> callback.</summary>
    internal Task InvokeClickAsync() => OnClick.InvokeAsync();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (BhContextMenuContext is not null)
            _index = BhContextMenuContext.RegisterItem(this);
    }

    public void Dispose()
    {
        BhContextMenuContext?.UnregisterItem(this);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id",
            BhContextMenuContext is not null ? BhContextMenuContext.GetItemId(_index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onmouseenter",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "menuitem";

        if (IsItemDisabled)
            attrs["aria-disabled"] = "true";

        SetDataFlag(attrs, "active", IsActive);
        SetDataFlag(attrs, "disabled", IsItemDisabled);

        return attrs;
    }

    // ── Event handling ────────────────────────────────────────────────────────

    private Task HandleClick(MouseEventArgs _)
    {
        if (IsItemDisabled) return Task.CompletedTask;
        return BhContextMenuContext?.ActivateItemAsync(_index) ?? Task.CompletedTask;
    }

    private void HandleMouseEnter(MouseEventArgs _)
    {
        if (IsItemDisabled) return;
        BhContextMenuContext?.SetActiveIndex(_index);
    }
}
