namespace BlazorHeadless;

/// <summary>
/// The state of a <see cref="BhCommand"/>: the search, the scored / filtered items,
/// the highlighted item and the page stack. Cascaded (fixed) to every part;
/// parts are refreshed individually when their visibility or highlight changes.
/// </summary>
public sealed class BhCommandContext
{
    private readonly BhCommand _command;
    private readonly Dictionary<BhCommandItem, double> _scores = new();
    private readonly List<BhCommandGroup> _groups = new();
    private readonly List<Action> _listeners = new();

    internal BhCommandContext(BhCommand command, BhInterop interop, string baseId)
    {
        _command = command;
        Interop = interop;
        BaseId = baseId;
        Items = new BhCollection<BhCommandItem>($"{baseId}-items");
        // Items register after parts like <BhCommandEmpty> rendered: tell them the count changed.
        Items.Changed += () =>
        {
            foreach (var listener in _listeners.ToArray()) listener();
        };
    }

    internal BhInterop Interop { get; }

    internal BhCommandInput? Input { get; set; }

    internal BhCommand Command => _command;

    /// <summary>The base id.</summary>
    public string BaseId { get; }

    /// <summary>The id of the input.</summary>
    public string InputId => $"{BaseId}-input";

    /// <summary>The id of the list (<c>role="listbox"</c>).</summary>
    public string ListId => $"{BaseId}-list";

    /// <summary>The id of the accessible label.</summary>
    public string LabelId => $"{BaseId}-label";

    /// <summary>All registered items in document order (visible or not).</summary>
    public BhCollection<BhCommandItem> Items { get; }

    /// <summary>The current search text.</summary>
    public string Search => _command.CurrentSearch;

    /// <summary>The current page (null for the root page).</summary>
    public string? Page => _command.CurrentPage;

    /// <summary>The page stack (root first).</summary>
    public IReadOnlyList<string> Pages => _command.PageStack;

    /// <summary>The highlighted item.</summary>
    public BhCommandItem? ActiveItem { get; private set; }

    /// <summary>The number of items matching the search.</summary>
    public int VisibleCount => Items.Items.Count(IsVisible);

    /// <summary>Whether filtering is enabled.</summary>
    public bool ShouldFilter => _command.ShouldFilter;

    /// <summary>Whether results are sorted by relevance.</summary>
    public bool Sort => _command.Sort;

    /// <summary>Pushes a page (nested command menu) and clears the search.</summary>
    public Task PushPageAsync(string page) => _command.PushPageAsync(page);

    /// <summary>Returns to the previous page.</summary>
    public Task PopPageAsync() => _command.PopPageAsync();

    /// <summary>Clears the search.</summary>
    public Task ClearSearchAsync() => _command.SetSearchAsync(string.Empty);

    /// <summary>Whether <paramref name="item"/> matches the current search.</summary>
    public bool IsVisible(BhCommandItem item)
        => item.ForceMount || !ShouldFilter || string.IsNullOrWhiteSpace(Search) || Score(item) > 0;

    /// <summary>The relevance score of <paramref name="item"/> (1 when not searching).</summary>
    public double Score(BhCommandItem item) => _scores.TryGetValue(item, out var s) ? s : 1;

    /// <summary>The 0-based visual order of <paramref name="item"/> when sorting.</summary>
    public int Rank(BhCommandItem item)
    {
        if (!Sort || string.IsNullOrWhiteSpace(Search)) return Items.IndexOf(item);
        return NavigationOrder().ToList().IndexOf(item);
    }

    /// <summary>Whether <paramref name="group"/> has visible items.</summary>
    public bool IsVisible(BhCommandGroup group)
        => group.ForceMount || Items.Items.Any(i => ReferenceEquals(i.Group, group) && IsVisible(i));

    /// <summary>Best score within a group (for sorting groups).</summary>
    public double GroupScore(BhCommandGroup group)
        => Items.Items.Where(i => ReferenceEquals(i.Group, group) && IsVisible(i)).Select(Score).DefaultIfEmpty(0).Max();

    internal void RegisterGroup(BhCommandGroup group)
    {
        if (!_groups.Contains(group)) _groups.Add(group);
    }

    internal void UnregisterGroup(BhCommandGroup group) => _groups.Remove(group);

    internal void Subscribe(Action listener) => _listeners.Add(listener);

    internal void Unsubscribe(Action listener) => _listeners.Remove(listener);

    /// <summary>Visible, enabled items in navigation order.</summary>
    internal IEnumerable<BhCommandItem> NavigationOrder()
    {
        var visible = Items.Items.Where(i => IsVisible(i) && !i.IsItemDisabled);
        if (!Sort || string.IsNullOrWhiteSpace(Search)) return visible;
        return visible
            .OrderByDescending(i => i.Group is null ? Score(i) : GroupScore(i.Group))
            .ThenBy(i => i.Group is null ? -1 : _groups.IndexOf(i.Group))
            .ThenByDescending(Score);
    }

    /// <summary>Re-scores every item for the current search and refreshes what changed.</summary>
    internal void Refilter()
    {
        _scores.Clear();
        if (ShouldFilter && !string.IsNullOrWhiteSpace(Search))
        {
            foreach (var item in Items.Items)
                _scores[item] = _command.ScoreItem(item.ItemValue, Search, item.Keywords ?? []);
        }

        foreach (var item in Items.Items.ToArray()) item.Refresh();
        foreach (var group in _groups.ToArray()) group.Refresh();
        foreach (var listener in _listeners.ToArray()) listener();

        // Keep the highlight on a visible item; default to the first one.
        if (ActiveItem is null || !IsVisible(ActiveItem) || ActiveItem.IsItemDisabled)
            SetActive(NavigationOrder().FirstOrDefault(), scroll: true);
    }

    internal void SetActive(BhCommandItem? item, bool scroll = true)
    {
        if (ReferenceEquals(item, ActiveItem)) return;
        var previous = ActiveItem;
        ActiveItem = item;
        previous?.Refresh();
        item?.Refresh();
        Input?.Refresh();
        _command.OnActiveChanged(item);
        if (item is not null && scroll)
            _ = Interop.ScrollIntoViewAsync(item.ItemId);
    }

    internal void Move(int delta, bool loop)
    {
        var order = NavigationOrder().ToList();
        if (order.Count == 0) return;
        var index = ActiveItem is null ? -1 : order.IndexOf(ActiveItem);
        var next = index + delta;
        if (next >= order.Count) next = loop ? 0 : order.Count - 1;
        if (next < 0) next = loop ? order.Count - 1 : 0;
        SetActive(order[next]);
    }

    internal void MoveToGroup(int direction)
    {
        var order = NavigationOrder().ToList();
        if (order.Count == 0) return;
        var currentGroup = ActiveItem?.Group;
        var index = ActiveItem is null ? -1 : order.IndexOf(ActiveItem);
        if (direction > 0)
        {
            var next = order.Skip(index + 1).FirstOrDefault(i => !ReferenceEquals(i.Group, currentGroup));
            if (next is not null) SetActive(next);
        }
        else
        {
            // Start of the current group, then start of the previous one.
            var groupStart = order.FirstOrDefault(i => ReferenceEquals(i.Group, currentGroup));
            if (groupStart is not null && !ReferenceEquals(groupStart, ActiveItem))
            {
                SetActive(groupStart);
                return;
            }
            var previous = order.Take(Math.Max(0, index)).LastOrDefault(i => !ReferenceEquals(i.Group, currentGroup));
            if (previous is not null)
                SetActive(order.First(i => ReferenceEquals(i.Group, previous.Group)));
        }
    }

    internal void First() => SetActive(NavigationOrder().FirstOrDefault());

    internal void Last() => SetActive(NavigationOrder().LastOrDefault());

    internal Task SelectActiveAsync() => ActiveItem is { IsItemDisabled: false } item ? item.SelectAsync() : Task.CompletedTask;

    internal bool IsActive(BhCommandItem item) => ReferenceEquals(item, ActiveItem);
}
