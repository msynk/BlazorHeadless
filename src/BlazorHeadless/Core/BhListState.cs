namespace BlazorHeadless;

/// <summary>
/// The shared option-list engine behind <see cref="BhListbox{TValue}"/> and
/// <see cref="BhCombobox{TValue}"/>: option registry (or a data-driven item list
/// for virtualization), the highlighted ("active") option, keyboard navigation,
/// typeahead and selection queries.
///
/// <para>
/// The active option is tracked by element id, never by index, so options can be
/// added, removed, filtered or virtualized without the highlight or
/// <c>aria-activedescendant</c> drifting.
/// </para>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public sealed class BhListState<TValue>
{
    private readonly BhInterop _interop;
    private readonly Func<TValue, bool> _isSelected;
    private readonly Action _refreshOwner;
    private readonly Dictionary<string, BhOptionBase<TValue>> _rendered = new(StringComparer.Ordinal);

    internal BhListState(string key, BhInterop interop, Func<TValue, bool> isSelected, Action refreshOwner)
    {
        Key = key;
        _interop = interop;
        _isSelected = isSelected;
        _refreshOwner = refreshOwner;
        Options = new BhCollection<BhOptionBase<TValue>>(key);
    }

    /// <summary>The collection key (rendered as <c>data-bh-item</c> on options).</summary>
    public string Key { get; }

    /// <summary>Registered option components (component-driven mode).</summary>
    public BhCollection<BhOptionBase<TValue>> Options { get; }

    /// <summary>The id of the highlighted option, or null.</summary>
    public string? ActiveId { get; private set; }

    /// <summary>The id of the options panel (scroll container).</summary>
    public string? PanelId { get; internal set; }

    internal BhTypeahead Typeahead { get; } = new();

    // ── Data-driven (virtualized) mode ───────────────────────────────────────

    internal IReadOnlyList<TValue>? Items { get; private set; }
    internal Func<TValue, bool>? ItemDisabled { get; private set; }
    internal Func<TValue, string?>? ItemText { get; private set; }
    internal float ItemSize { get; private set; }

    /// <summary>Whether options come from a data list (virtualization) instead of registered components.</summary>
    public bool IsDataMode => Items is not null;

    internal void SetItems(IReadOnlyList<TValue>? items, Func<TValue, bool>? disabled, Func<TValue, string?>? text, float itemSize)
    {
        Items = items;
        ItemDisabled = disabled;
        ItemText = text;
        ItemSize = itemSize;
    }

    /// <summary>The id of the data item at <paramref name="index"/> (data mode).</summary>
    public string DataItemId(int index) => $"{Key}-o{index}";

    // ── Model ────────────────────────────────────────────────────────────────

    /// <summary>The number of options.</summary>
    public int Count => Items?.Count ?? Options.Count;

    internal string IdAt(int index) => Items is not null ? DataItemId(index) : Options.Items[index].ItemId;

    internal TValue ValueAt(int index) => Items is not null ? Items[index] : Options.Items[index].Value;

    internal bool DisabledAt(int index)
        => Items is not null ? ItemDisabled?.Invoke(Items[index]) ?? false : Options.Items[index].IsItemDisabled;

    internal string? TextAt(int index)
        => Items is not null
            ? ItemText?.Invoke(Items[index]) ?? Items[index]?.ToString()
            : Options.Items[index].ItemText;

    internal int IndexOfId(string? id)
    {
        if (string.IsNullOrEmpty(id)) return -1;
        if (Items is not null)
        {
            var prefix = Key + "-o";
            return id.StartsWith(prefix, StringComparison.Ordinal)
                && int.TryParse(id.AsSpan(prefix.Length), out var i) && i >= 0 && i < Items.Count ? i : -1;
        }
        for (var i = 0; i < Options.Count; i++)
            if (Options.Items[i].ItemId == id) return i;
        return -1;
    }

    /// <summary>The index of the highlighted option, or -1.</summary>
    public int ActiveIndex => IndexOfId(ActiveId);

    /// <summary>Whether the highlighted option exists and is enabled.</summary>
    public bool HasActive => ActiveIndex >= 0;

    /// <summary>The value of the highlighted option.</summary>
    public bool TryGetActiveValue(out TValue value)
    {
        var index = ActiveIndex;
        if (index < 0) { value = default!; return false; }
        value = ValueAt(index);
        return true;
    }

    internal bool IsSelected(TValue value) => _isSelected(value);

    // ── Rendered option tracking (targeted refresh) ──────────────────────────

    internal void Attach(BhOptionBase<TValue> option)
    {
        _rendered[option.ItemId] = option;
        if (!IsDataMode) Options.Register(option);
    }

    internal void Detach(BhOptionBase<TValue> option, string id)
    {
        if (_rendered.TryGetValue(id, out var existing) && ReferenceEquals(existing, option))
            _rendered.Remove(id);
        Options.Unregister(option);
    }

    // ── Highlight ────────────────────────────────────────────────────────────

    /// <summary>Highlights the option with the given id (null clears the highlight).</summary>
    internal void SetActiveId(string? id, bool scroll = true)
    {
        if (ActiveId == id) return;
        var previous = ActiveId;
        ActiveId = id;
        if (previous is not null && _rendered.TryGetValue(previous, out var p)) p.Refresh();
        if (id is not null && _rendered.TryGetValue(id, out var n)) n.Refresh();
        _refreshOwner();
        if (id is not null && scroll && PanelId is not null)
        {
            if (IsDataMode)
                _ = _interop.ScrollToIndexAsync(PanelId, IndexOfId(id), ItemSize, id);
            else
                _ = _interop.ScrollIntoViewAsync(id);
        }
    }

    internal void SetActiveIndex(int index, bool scroll = true)
        => SetActiveId(index >= 0 && index < Count ? IdAt(index) : null, scroll);

    internal int FirstEnabled()
    {
        for (var i = 0; i < Count; i++) if (!DisabledAt(i)) return i;
        return -1;
    }

    internal int LastEnabled()
    {
        for (var i = Count - 1; i >= 0; i--) if (!DisabledAt(i)) return i;
        return -1;
    }

    internal int Step(int from, int step, bool loop)
    {
        var count = Count;
        if (count == 0) return -1;
        if (from < 0) return step > 0 ? FirstEnabled() : LastEnabled();
        var i = from;
        for (var attempts = 0; attempts < count; attempts++)
        {
            i += step;
            if (i >= count) { if (!loop) return from; i = 0; }
            else if (i < 0) { if (!loop) return from; i = count - 1; }
            if (!DisabledAt(i)) return i;
        }
        return from;
    }

    internal int Offset(int from, int delta)
    {
        var current = from < 0 ? (delta > 0 ? FirstEnabled() : LastEnabled()) : from;
        var steps = Math.Abs(delta);
        for (var s = 0; s < steps; s++)
        {
            var next = Step(current, Math.Sign(delta), loop: false);
            if (next == current) break;
            current = next;
        }
        return current;
    }

    /// <summary>The index of the first selected, enabled option, or -1.</summary>
    internal int FirstSelected()
    {
        for (var i = 0; i < Count; i++)
            if (!DisabledAt(i) && _isSelected(ValueAt(i))) return i;
        return -1;
    }

    internal void Search(string key)
    {
        var index = Typeahead.SearchIndex(Count, key, ActiveIndex, TextAt, DisabledAt);
        if (index >= 0) SetActiveIndex(index);
    }

    /// <summary>
    /// After options changed: keeps the highlight valid. When the highlighted option
    /// disappeared it moves to <paramref name="fallbackToFirst"/> ? the first option : nothing.
    /// </summary>
    internal void Reconcile(bool fallbackToFirst)
    {
        if (ActiveId is not null && IndexOfId(ActiveId) >= 0) return;
        SetActiveIndex(fallbackToFirst ? FirstEnabled() : -1, scroll: false);
    }

    /// <summary>Text of the options whose value is selected (for button labels).</summary>
    internal IEnumerable<string> SelectedTexts()
    {
        for (var i = 0; i < Count; i++)
            if (_isSelected(ValueAt(i)) && TextAt(i) is { Length: > 0 } text) yield return text;
    }

    internal Task EnsureOrderAsync() => IsDataMode ? Task.CompletedTask : Options.EnsureOrderAsync(_interop);
}
