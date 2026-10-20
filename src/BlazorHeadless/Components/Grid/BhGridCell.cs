using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>Sort direction announced by a grid column header (<c>aria-sort</c>).</summary>
public enum BhSortDirection
{
    /// <summary>Not sorted (no <c>aria-sort</c>).</summary>
    None,

    /// <summary>Ascending.</summary>
    Ascending,

    /// <summary>Descending.</summary>
    Descending,
}

/// <summary>
/// A cell of a <see cref="BhGridRow"/>: <c>gridcell</c>, <c>columnheader</c>
/// (<see cref="Header"/>) or <c>rowheader</c> (<see cref="RowHeader"/>). Cells are the
/// focus targets of the grid (one tab stop); emits <c>data-focused</c>.
/// </summary>
public class BhGridCell : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhGridRow? ParentRow { get; set; }

    /// <summary>A column header (<c>role="columnheader"</c>).</summary>
    [Parameter]
    public bool Header { get; set; }

    /// <summary>A row header (<c>role="rowheader"</c>).</summary>
    [Parameter]
    public bool RowHeader { get; set; }

    /// <summary>Sort state for column headers (<c>aria-sort</c>).</summary>
    [Parameter]
    public BhSortDirection Sort { get; set; }

    /// <summary>The 1-based column index for <c>aria-colindex</c> (when columns are hidden / virtualized).</summary>
    [Parameter]
    public int? ColIndex { get; set; }

    /// <summary>Invoked when the cell is activated with Enter or a click (e.g. sort on a header).</summary>
    [Parameter]
    public EventCallback OnActivate { get; set; }

    /// <summary>
    /// Makes this cell the grid's tab stop until the user moves focus (e.g. today's date
    /// in a calendar grid). Default: the first cell of the first body row.
    /// </summary>
    [Parameter]
    public bool InitialTabStop { get; set; }

    /// <summary>The cell content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    internal BhGridRow? Row => ParentRow;

    internal string ItemId => ComponentId;

    internal void Refresh() => StateHasChanged();

    protected override void OnInitialized() => ParentRow?.RegisterCell(this);

    public void Dispose() => ParentRow?.UnregisterCell(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddAttribute(31, "onfocus", EventCallback.Factory.Create<FocusEventArgs>(this, () => ParentRow?.Grid?.SetFocus(this)));
        builder.AddAttribute(32, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => OnActivate.InvokeAsync()));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = Header ? "columnheader" : RowHeader ? "rowheader" : "gridcell";
        if (ColIndex is { } col) attrs["aria-colindex"] = col;
        if (Header && Sort != BhSortDirection.None)
            attrs["aria-sort"] = Sort == BhSortDirection.Ascending ? "ascending" : "descending";
        var grid = ParentRow?.Grid;
        var focused = grid?.IsTabStop(this) ?? false;
        attrs["tabindex"] = focused ? 0 : -1;
        attrs[BhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight Home End PageUp PageDown Space Ctrl+Home Ctrl+End Ctrl+a Meta+a";
        SetDataFlag(attrs, "focused", grid is not null && ReferenceEquals(grid.FocusedCell, this));
        return attrs;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (ParentRow?.Grid is not { } grid) return;
        if (e.Key == "Enter" && OnActivate.HasDelegate)
        {
            await OnActivate.InvokeAsync();
            return;
        }
        await grid.Grid.HandleCellKeyDownAsync(this, e);
    }
}
