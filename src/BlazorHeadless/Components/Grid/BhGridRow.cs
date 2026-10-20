using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A row of a <see cref="BhGrid"/> (<c>role="row"</c>). Give data rows a unique
/// <see cref="Key"/> for selection / expansion. In a treegrid set <see cref="Level"/>,
/// <see cref="HasChildren"/> and <see cref="ParentKey"/>: rows under a collapsed ancestor are
/// <c>hidden</c> and skipped by keyboard navigation (or render them only while the parent is
/// expanded — see <see cref="BhGridContext.IsExpanded"/>).
/// Emits <c>data-selected</c>, <c>data-state="open|closed"</c>, <c>data-level</c>.
/// </summary>
public class BhGridRow : BhComponentBase, IBhCollectionItem, IDisposable
{
    private readonly List<BhGridCell> _cells = new();

    [CascadingParameter]
    private BhGridContext? GridContext { get; set; }

    /// <summary>The row's key (required for selection and expansion).</summary>
    [Parameter]
    public string? Key { get; set; }

    /// <summary>Hierarchy level (treegrid), starting at 1.</summary>
    [Parameter]
    public int? Level { get; set; }

    /// <summary>Whether the row has child rows (treegrid).</summary>
    [Parameter]
    public bool HasChildren { get; set; }

    /// <summary>The parent row's key (treegrid): hides the row while the parent is collapsed and enables ArrowLeft to the parent.</summary>
    [Parameter]
    public string? ParentKey { get; set; }

    /// <summary>Marks a header row (excluded from selection).</summary>
    [Parameter]
    public bool IsHeaderRow { get; set; }

    /// <summary>Disables selection of the row.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The 1-based row index for <c>aria-rowindex</c> (virtualized grids).</summary>
    [Parameter]
    public int? RowIndex { get; set; }

    /// <summary>The row's cells.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal List<BhGridCell> Cells => _cells;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled;

    /// <inheritdoc />
    public string? ItemText => null;

    internal BhGridContext? Grid => GridContext;

    internal void RegisterCell(BhGridCell cell)
    {
        if (!_cells.Contains(cell)) _cells.Add(cell);
    }

    internal void UnregisterCell(BhGridCell cell) => _cells.Remove(cell);

    protected override void OnInitialized() => GridContext?.Rows.Register(this);

    public void Dispose() => GridContext?.Rows.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BhGridRow>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", (RenderFragment)(inner =>
        {
            // The state changes every render: parts such as <BhGridRowToggle /> re-render with it.
            inner.OpenComponent<CascadingValue<BhGridRowState>>(0);
            inner.AddComponentParameter(1, "Value", new BhGridRowState(
                Key, Level, HasChildren,
                GridContext?.IsExpanded(Key) ?? false,
                GridContext?.IsSelected(Key) ?? false));
            inner.AddComponentParameter(2, "ChildContent", ChildContent);
            inner.CloseComponent();
        }));
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "row";
        if (RowIndex is { } index) attrs["aria-rowindex"] = index;
        if (GridContext is not null)
        {
            attrs["data-bh-item"] = GridContext.Rows.Key;
            if (GridContext.IsTreeGrid)
            {
                if (Level is { } level)
                {
                    attrs["aria-level"] = level;
                    attrs["data-level"] = level;
                    attrs["style"] = $"--bh-grid-level:{level}";
                }
                if (HasChildren)
                {
                    var expanded = GridContext.IsExpanded(Key);
                    SetAriaBool(attrs, "aria-expanded", expanded);
                    SetDataState(attrs, expanded);
                }
            }
            if (GridContext.SelectionMode != BhGridSelectionMode.None && !IsHeaderRow)
            {
                var selected = GridContext.IsSelected(Key);
                SetAriaBool(attrs, "aria-selected", selected);
                SetDataFlag(attrs, "selected", selected);
            }
        }
        if (GridContext is not null && !GridContext.IsRowVisible(this)) attrs["hidden"] = true;
        if (Disabled) attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs e) => GridContext?.Grid.HandleRowClickAsync(this, e) ?? Task.CompletedTask;
}
