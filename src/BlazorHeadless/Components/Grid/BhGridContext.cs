namespace BlazorHeadless;

/// <summary>State of a <see cref="BhGrid"/> cascaded to rows and cells.</summary>
public sealed class BhGridContext
{
    private Dictionary<string, BhGridRow>? _rowsByKey;

    internal BhGridContext(BhGrid grid, string key)
    {
        Grid = grid;
        Rows = new BhCollection<BhGridRow>(key);
        Rows.Changed += () => _rowsByKey = null;
    }

    internal BhGrid Grid { get; }

    /// <summary>The registered rows in document order.</summary>
    public BhCollection<BhGridRow> Rows { get; }

    /// <summary>The cell holding the tab stop.</summary>
    public BhGridCell? FocusedCell { get; private set; }

    /// <summary>Whether the grid is a treegrid.</summary>
    public bool IsTreeGrid => Grid.TreeGrid;

    /// <summary>The row selection mode.</summary>
    public BhGridSelectionMode SelectionMode => Grid.SelectionMode;

    /// <summary>Whether the row with <paramref name="key"/> is selected.</summary>
    public bool IsSelected(string? key) => Grid.IsSelected(key);

    /// <summary>Whether the row with <paramref name="key"/> is expanded.</summary>
    public bool IsExpanded(string? key) => Grid.IsExpanded(key);

    /// <summary>
    /// Whether <paramref name="row"/> is shown: in a treegrid, rows whose ancestor
    /// (via <see cref="BhGridRow.ParentKey"/>) is collapsed are hidden.
    /// </summary>
    public bool IsRowVisible(BhGridRow row)
    {
        if (!IsTreeGrid || row.ParentKey is null) return true;
        _rowsByKey ??= Rows.Items.Where(r => r.Key is not null).GroupBy(r => r.Key!).ToDictionary(g => g.Key, g => g.First());
        var parentKey = row.ParentKey;
        for (var depth = 0; parentKey is not null && depth < 256; depth++)
        {
            if (!IsExpanded(parentKey)) return false;
            parentKey = _rowsByKey.TryGetValue(parentKey, out var parent) ? parent.ParentKey : null;
        }
        return true;
    }

    /// <summary>The visible rows in document order.</summary>
    internal List<BhGridRow> VisibleRows() => Rows.Items.Where(IsRowVisible).ToList();

    internal bool IsTabStop(BhGridCell cell)
    {
        if (FocusedCell is not null && FocusedCell.Row is { } row && Rows.IndexOf(row) >= 0 && row.Cells.Contains(FocusedCell) && IsRowVisible(row))
            return ReferenceEquals(FocusedCell, cell);
        // A cell that asked to be the initial tab stop wins.
        var preferred = Rows.Items.Where(IsRowVisible).SelectMany(r => r.Cells).FirstOrDefault(c => c.InitialTabStop);
        if (preferred is not null) return ReferenceEquals(preferred, cell);
        // Default: the first cell of the first visible body row (or the first row).
        var first = Rows.Items.FirstOrDefault(r => !r.IsHeaderRow && r.Cells.Count > 0 && IsRowVisible(r)) ?? Rows.Items.FirstOrDefault(r => r.Cells.Count > 0);
        return first is not null && ReferenceEquals(first.Cells[0], cell);
    }

    /// <summary>Re-renders every cell (their tab stop may have moved).</summary>
    internal void RefreshCells()
    {
        foreach (var row in Rows.Items)
            foreach (var c in row.Cells) c.Refresh();
    }

    internal void SetFocus(BhGridCell cell)
    {
        if (ReferenceEquals(FocusedCell, cell)) return;
        var previous = FocusedCell;
        FocusedCell = cell;
        previous?.Refresh();
        cell.Refresh();
        if (previous is null)
            foreach (var row in Rows.Items) foreach (var c in row.Cells) if (!ReferenceEquals(c, cell)) c.Refresh();
    }
}
