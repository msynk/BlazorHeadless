using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Tree View implementing the WAI-ARIA Tree pattern —
/// file explorers, navigation trees, nested settings, hierarchical pickers.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Keyboard</b> — Up / Down, Right (expand / first child), Left (collapse / parent), Home / End,
///     <c>*</c> (expand siblings), Enter (activate), Space (select / check), typeahead, Shift+arrows and Ctrl+A in multi-select.</item>
///   <item><b>Selection</b> — none, single (<c>@bind-SelectedValue</c>) or multiple (<c>@bind-SelectedValues</c>, Ctrl / Shift click).</item>
///   <item><b>Hierarchical checkboxes</b> — <see cref="Checkboxes"/>: checking a parent checks its descendants;
///     parents show the mixed state (<c>aria-checked="mixed"</c>).</item>
///   <item><b>Expansion</b> — <c>@bind-ExpandedValues</c> or uncontrolled; <see cref="LoadChildren"/> for lazy loading (<c>aria-busy</c>).</item>
///   <item><b>Roving focus</b> — one tab stop; <c>aria-level</c>, <c>aria-setsize</c>, <c>aria-posinset</c> computed for you.</item>
/// </list>
///
/// <code>
/// &lt;BhTree TValue="string" aria-label="Files" @bind-SelectedValue="selected"&gt;
///     &lt;BhTreeItem TValue="string" Value="@("src")" Label="src"&gt;
///         &lt;span class="row"&gt;&lt;BhTreeItemToggle TValue="string"&gt;▸&lt;/BhTreeItemToggle&gt; src&lt;/span&gt;
///         &lt;BhTreeGroup TValue="string"&gt;
///             &lt;BhTreeItem TValue="string" Value="@("app.cs")"&gt;app.cs&lt;/BhTreeItem&gt;
///         &lt;/BhTreeGroup&gt;
///     &lt;/BhTreeItem&gt;
/// &lt;/BhTree&gt;
/// </code>
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BhTree<TValue> : BhComponentBase
{
    private BhTreeContext<TValue>? _context;
    private HashSet<TValue>? _expanded;
    private HashSet<TValue>? _selected;
    private BhTreeItem<TValue>? _anchor;
    private readonly BhTypeahead _typeahead = new();
    private readonly HashSet<TValue> _loading = new();

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>How items can be selected. Default: <see cref="BhTreeSelectionMode.Single"/>.</summary>
    [Parameter]
    public BhTreeSelectionMode SelectionMode { get; set; } = BhTreeSelectionMode.Single;

    /// <summary>Show hierarchical tri-state checkboxes (implies multiple selection).</summary>
    [Parameter]
    public bool Checkboxes { get; set; }

    /// <summary>The selected item (single mode). Supplying it makes selection controlled.</summary>
    [Parameter]
    public TValue? SelectedValue { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedValue"/>.</summary>
    [Parameter]
    public EventCallback<TValue?> SelectedValueChanged { get; set; }

    /// <summary>The selected items (multiple / checkbox mode). Supplying it makes selection controlled.</summary>
    [Parameter]
    public IEnumerable<TValue>? SelectedValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="SelectedValues"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> SelectedValuesChanged { get; set; }

    /// <summary>Initially selected items when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultSelectedValues { get; set; }

    /// <summary>The expanded items. Supplying it makes expansion controlled (use <c>@bind-ExpandedValues</c>).</summary>
    [Parameter]
    public IEnumerable<TValue>? ExpandedValues { get; set; }

    /// <summary>Two-way binding callback for <see cref="ExpandedValues"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyCollection<TValue>> ExpandedValuesChanged { get; set; }

    /// <summary>Initially expanded items when uncontrolled.</summary>
    [Parameter]
    public IEnumerable<TValue>? DefaultExpandedValues { get; set; }

    /// <summary>Invoked on Enter or double click — open the file, navigate, …</summary>
    [Parameter]
    public EventCallback<TValue> OnItemActivate { get; set; }

    /// <summary>Loads children the first time an item marked <c>HasChildren</c> expands.</summary>
    [Parameter]
    public Func<TValue, Task>? LoadChildren { get; set; }

    /// <summary>Compares item values. Defaults to <see cref="EqualityComparer{T}.Default"/>.</summary>
    [Parameter]
    public IEqualityComparer<TValue>? Comparer { get; set; }

    /// <summary>Disables the whole tree.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (mirrors Left / Right). Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>The top-level <see cref="BhTreeItem{TValue}"/>s.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private IEqualityComparer<TValue> EffectiveComparer => Comparer ?? EqualityComparer<TValue>.Default;
    private BhTreeContext<TValue> Context => _context ??= new BhTreeContext<TValue>(this, BhId.New("bh-tree"));
    private bool IsMulti => Checkboxes || SelectionMode == BhTreeSelectionMode.Multiple;
    private BhDirection EffectiveDir => Dir ?? CascadedDir;

    internal bool IsTreeDisabled => Disabled;

    protected override void OnInitialized()
    {
        _expanded = new HashSet<TValue>(DefaultExpandedValues ?? [], EffectiveComparer);
        _selected = new HashSet<TValue>(DefaultSelectedValues ?? [], EffectiveComparer);
    }

    protected override Task OnAfterRenderAsync(bool firstRender) => Context.Items.EnsureOrderAsync(Interop);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhTreeContext<TValue>>>(40);
        builder.AddComponentParameter(41, "Value", Context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "tree";
        if (IsMulti) attrs["aria-multiselectable"] = "true";
        if (Disabled) attrs["aria-disabled"] = "true";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Expansion ────────────────────────────────────────────────────────────

    internal bool IsExpanded(TValue value)
        => IsProvided(nameof(ExpandedValues))
            ? ExpandedValues?.Contains(value, EffectiveComparer) ?? false
            : _expanded!.Contains(value);

    internal bool IsLoading(TValue value) => _loading.Contains(value);

    internal async Task SetExpandedAsync(BhTreeItem<TValue> item, bool expanded)
    {
        if (!item.IsExpandable || IsExpanded(item.Value) == expanded) return;

        if (expanded && LoadChildren is not null && item.HasChildren && !Context.ChildrenOf(item).Any())
        {
            _loading.Add(item.Value);
            item.Refresh();
            try
            {
                await LoadChildren(item.Value);
            }
            finally
            {
                _loading.Remove(item.Value);
            }
        }

        var set = new HashSet<TValue>(IsProvided(nameof(ExpandedValues)) ? ExpandedValues ?? [] : _expanded!, EffectiveComparer);
        if (expanded) set.Add(item.Value); else set.Remove(item.Value);
        if (!IsProvided(nameof(ExpandedValues))) _expanded = set;
        await ExpandedValuesChanged.InvokeAsync(set.ToArray());

        // Collapsing a branch that contains the tab stop moves it to the branch.
        if (!expanded && Context.FocusedItem is { } focused && Context.DescendantsOf(item).Contains(focused))
            Context.FocusedItem = item;

        item.Refresh();
    }

    internal Task ToggleExpandedAsync(BhTreeItem<TValue> item) => SetExpandedAsync(item, !IsExpanded(item.Value));

    // ── Selection ────────────────────────────────────────────────────────────

    internal bool IsSelected(TValue value)
    {
        if (SelectionMode == BhTreeSelectionMode.None && !Checkboxes) return false;
        if (!IsMulti && IsProvided(nameof(SelectedValue)))
            return SelectedValue is not null && EffectiveComparer.Equals(SelectedValue, value);
        if (IsMulti && IsProvided(nameof(SelectedValues)))
            return SelectedValues?.Contains(value, EffectiveComparer) ?? false;
        return _selected!.Contains(value);
    }

    private HashSet<TValue> CurrentSelection()
    {
        if (!IsMulti && IsProvided(nameof(SelectedValue)))
            return SelectedValue is null ? new HashSet<TValue>(EffectiveComparer) : new HashSet<TValue>(EffectiveComparer) { SelectedValue };
        if (IsMulti && IsProvided(nameof(SelectedValues)))
            return new HashSet<TValue>(SelectedValues ?? [], EffectiveComparer);
        return new HashSet<TValue>(_selected!, EffectiveComparer);
    }

    private async Task CommitSelectionAsync(HashSet<TValue> selection)
    {
        if (!IsProvided(nameof(SelectedValue)) && !IsProvided(nameof(SelectedValues))) _selected = selection;
        if (IsMulti)
            await SelectedValuesChanged.InvokeAsync(selection.ToArray());
        else
            await SelectedValueChanged.InvokeAsync(selection.Count > 0 ? selection.First() : default);
        // Selection changes can affect ancestors' mixed state: refresh everything.
        foreach (var item in Context.Items.Items.Where(i => i.ParentItem is null).ToArray()) item.Refresh();
    }

    internal async Task SelectAsync(BhTreeItem<TValue> item, bool toggle = false, bool range = false)
    {
        if (item.IsItemDisabled) return;
        if (SelectionMode == BhTreeSelectionMode.None && !Checkboxes) return;
        var selection = CurrentSelection();

        if (Checkboxes)
        {
            var check = Context.CheckedState(item) != true;
            foreach (var v in Context.DescendantsOf(item).Where(d => !d.IsItemDisabled).Select(d => d.Value).Append(item.Value))
            {
                if (check) selection.Add(v); else selection.Remove(v);
            }
            // Keep ancestors consistent with their descendants.
            for (var parent = item.ParentItem; parent is not null; parent = parent.ParentItem)
            {
                var all = Context.DescendantsOf(parent).Where(d => !Context.ChildrenOf(d).Any()).All(l => selection.Contains(l.Value));
                if (all) selection.Add(parent.Value); else selection.Remove(parent.Value);
            }
        }
        else if (!IsMulti)
        {
            if (selection.Contains(item.Value) && toggle) selection.Clear();
            else selection = new HashSet<TValue>(EffectiveComparer) { item.Value };
        }
        else if (range && _anchor is not null)
        {
            var visible = Context.VisibleItems().ToList();
            var a = visible.IndexOf(_anchor);
            var b = visible.IndexOf(item);
            if (a >= 0 && b >= 0)
            {
                selection = new HashSet<TValue>(EffectiveComparer);
                for (var i = Math.Min(a, b); i <= Math.Max(a, b); i++)
                    if (!visible[i].IsItemDisabled) selection.Add(visible[i].Value);
            }
        }
        else if (toggle)
        {
            if (!selection.Remove(item.Value)) selection.Add(item.Value);
            _anchor = item;
        }
        else
        {
            selection = new HashSet<TValue>(EffectiveComparer) { item.Value };
            _anchor = item;
        }

        await CommitSelectionAsync(selection);
    }

    private async Task SelectAllAsync()
    {
        var selection = new HashSet<TValue>(Context.Items.Items.Where(i => !i.IsItemDisabled).Select(i => i.Value), EffectiveComparer);
        await CommitSelectionAsync(selection);
    }

    // ── Focus / keyboard ─────────────────────────────────────────────────────

    internal bool IsTabStop(BhTreeItem<TValue> item)
    {
        var focused = Context.FocusedItem;
        if (focused is not null && Context.Items.IndexOf(focused) >= 0 && focused.AncestorsExpanded) return ReferenceEquals(focused, item);
        // Default tab stop: the first selected visible item, else the first visible item.
        var visible = Context.VisibleItems().Where(i => !i.IsItemDisabled).ToList();
        var first = visible.FirstOrDefault(i => IsSelected(i.Value)) ?? visible.FirstOrDefault();
        return ReferenceEquals(first, item);
    }

    internal void OnItemFocused(BhTreeItem<TValue> item)
    {
        if (ReferenceEquals(Context.FocusedItem, item)) return;
        var previous = Context.FocusedItem;
        Context.FocusedItem = item;
        previous?.Refresh();
        item.Refresh();
    }

    private async Task FocusItemAsync(BhTreeItem<TValue>? item)
    {
        if (item is null) return;
        OnItemFocused(item);
        await Interop.FocusAsync(item.ItemId);
    }

    internal async Task HandleKeyDownAsync(BhTreeItem<TValue> item, KeyboardEventArgs e)
    {
        if (Disabled) return;
        await Context.Items.EnsureOrderAsync(Interop);
        var visible = Context.VisibleItems().Where(i => !i.IsItemDisabled).ToList();
        var index = visible.IndexOf(item);
        var rtl = EffectiveDir == BhDirection.Rtl;
        var openKey = rtl ? "ArrowLeft" : "ArrowRight";
        var closeKey = rtl ? "ArrowRight" : "ArrowLeft";

        if (e.Key == " " && _typeahead.IsActive)
        {
            Typeahead(item, " ", visible);
            return;
        }

        switch (e.Key)
        {
            case "ArrowDown":
                if (index < visible.Count - 1)
                {
                    var next = visible[index + 1];
                    await FocusItemAsync(next);
                    if (e.ShiftKey && IsMulti && !Checkboxes) await SelectAsync(next, range: true);
                }
                return;
            case "ArrowUp":
                if (index > 0)
                {
                    var prev = visible[index - 1];
                    await FocusItemAsync(prev);
                    if (e.ShiftKey && IsMulti && !Checkboxes) await SelectAsync(prev, range: true);
                }
                return;
            case "Home":
                await FocusItemAsync(visible.FirstOrDefault());
                return;
            case "End":
                await FocusItemAsync(visible.LastOrDefault());
                return;
            case "Enter":
                if (SelectionMode == BhTreeSelectionMode.Single && !Checkboxes) await SelectAsync(item);
                await OnItemActivate.InvokeAsync(item.Value);
                return;
            case " ":
                await SelectAsync(item, toggle: IsMulti || e.CtrlKey || e.MetaKey);
                return;
            case "*":
                foreach (var sibling in Context.ChildrenOf(item.ParentItem).Where(s => s.IsExpandable).ToArray())
                    await SetExpandedAsync(sibling, true);
                return;
            case "a" or "A" when (e.CtrlKey || e.MetaKey) && IsMulti:
                await SelectAllAsync();
                return;
        }

        if (e.Key == openKey)
        {
            if (item.IsExpandable && !IsExpanded(item.Value)) await SetExpandedAsync(item, true);
            else if (item.IsExpandable) await FocusItemAsync(Context.ChildrenOf(item).FirstOrDefault(c => !c.IsItemDisabled));
        }
        else if (e.Key == closeKey)
        {
            if (item.IsExpandable && IsExpanded(item.Value)) await SetExpandedAsync(item, false);
            else await FocusItemAsync(item.ParentItem);
        }
        else if (BhTypeahead.IsTypeaheadKey(e.Key, e.CtrlKey || e.MetaKey, e.AltKey))
        {
            Typeahead(item, e.Key, visible);
        }
    }

    private void Typeahead(BhTreeItem<TValue> item, string key, List<BhTreeItem<TValue>> visible)
    {
        var match = _typeahead.Search(visible, key, item, i => i.ItemText, i => i.IsItemDisabled);
        if (match is not null) _ = FocusItemAsync(match);
    }

    internal async Task HandleClickAsync(BhTreeItem<TValue> item, MouseEventArgs e)
    {
        if (Disabled || item.IsItemDisabled) return;
        OnItemFocused(item);
        if (Checkboxes) await SelectAsync(item);
        else if (IsMulti) await SelectAsync(item, toggle: e.CtrlKey || e.MetaKey, range: e.ShiftKey);
        else if (SelectionMode == BhTreeSelectionMode.Single) await SelectAsync(item);
        if (e.Detail == 2) await OnItemActivate.InvokeAsync(item.Value);
    }
}
