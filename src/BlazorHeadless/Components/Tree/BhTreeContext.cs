namespace BlazorHeadless;

/// <summary>How items of a <see cref="BhTree{TValue}"/> can be selected.</summary>
public enum BhTreeSelectionMode
{
    /// <summary>No selection; items can only be focused and activated.</summary>
    None,

    /// <summary>One selected item (<c>aria-selected</c>).</summary>
    Single,

    /// <summary>Many selected items (<c>aria-multiselectable</c>); Ctrl / Shift extend the selection.</summary>
    Multiple,
}

/// <summary>
/// The state of a <see cref="BhTree{TValue}"/>, cascaded (fixed) to its items:
/// registered items and their hierarchy, expansion, selection and focus.
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public sealed class BhTreeContext<TValue>
{
    private readonly BhTree<TValue> _tree;

    internal BhTreeContext(BhTree<TValue> tree, string key)
    {
        _tree = tree;
        Items = new BhCollection<BhTreeItem<TValue>>(key);
    }

    internal BhTree<TValue> Tree => _tree;

    /// <summary>Every registered item, in document order.</summary>
    public BhCollection<BhTreeItem<TValue>> Items { get; }

    /// <summary>The selection mode.</summary>
    public BhTreeSelectionMode SelectionMode => _tree.SelectionMode;

    /// <summary>Whether items show tri-state checkboxes (hierarchical selection).</summary>
    public bool Checkboxes => _tree.Checkboxes;

    /// <summary>The item that currently owns the tab stop.</summary>
    public BhTreeItem<TValue>? FocusedItem { get; internal set; }

    /// <summary>Whether <paramref name="value"/> is expanded.</summary>
    public bool IsExpanded(TValue value) => _tree.IsExpanded(value);

    /// <summary>Whether <paramref name="value"/> is selected.</summary>
    public bool IsSelected(TValue value) => _tree.IsSelected(value);

    /// <summary>The children of <paramref name="parent"/> (null for top-level items), in document order.</summary>
    public IEnumerable<BhTreeItem<TValue>> ChildrenOf(BhTreeItem<TValue>? parent)
        => Items.Items.Where(i => ReferenceEquals(i.ParentItem, parent));

    /// <summary>Every descendant of <paramref name="item"/>.</summary>
    public IEnumerable<BhTreeItem<TValue>> DescendantsOf(BhTreeItem<TValue> item)
    {
        foreach (var child in ChildrenOf(item))
        {
            yield return child;
            foreach (var d in DescendantsOf(child)) yield return d;
        }
    }

    /// <summary>Items whose ancestors are all expanded, in document order.</summary>
    public IEnumerable<BhTreeItem<TValue>> VisibleItems()
        => Items.Items.Where(i => i.AncestorsExpanded);

    /// <summary>The checkbox state of <paramref name="item"/>: true, false or null (mixed).</summary>
    public bool? CheckedState(BhTreeItem<TValue> item)
    {
        var leaves = DescendantsOf(item).Where(d => !ChildrenOf(d).Any()).ToList();
        if (leaves.Count == 0) return IsSelected(item.Value);
        var checkedCount = leaves.Count(l => IsSelected(l.Value));
        return checkedCount == 0 ? false : checkedCount == leaves.Count ? true : null;
    }
}

/// <summary>Render-fragment context exposed by <see cref="BhTreeItem{TValue}"/>.</summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public sealed record BhTreeItemRenderContext<TValue>
{
    /// <summary>The item's value.</summary>
    public required TValue Value { get; init; }

    /// <summary>Depth, starting at 1.</summary>
    public required int Level { get; init; }

    /// <summary>Whether the item has (or may lazily load) children.</summary>
    public required bool IsExpandable { get; init; }

    /// <summary>Whether the item is expanded.</summary>
    public required bool IsExpanded { get; init; }

    /// <summary>Whether the item is selected.</summary>
    public required bool IsSelected { get; init; }

    /// <summary>Checkbox state: true, false or null (mixed). Only meaningful with checkboxes.</summary>
    public required bool? Checked { get; init; }

    /// <summary>Whether the item has keyboard focus (tab stop).</summary>
    public required bool IsFocused { get; init; }

    /// <summary>Whether the item is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>Whether children are being loaded.</summary>
    public required bool IsLoading { get; init; }

    /// <summary>Toggles expansion.</summary>
    public required Func<Task> ToggleExpanded { get; init; }
}
