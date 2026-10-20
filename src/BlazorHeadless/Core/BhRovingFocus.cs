using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The state of a roving-tabindex group: exactly one item is in the tab order
/// (<c>tabindex="0"</c>) and arrow keys move focus between items. Cascaded by
/// <see cref="BhRovingFocusGroup"/>, <see cref="BhToolbar"/> and
/// <see cref="BhToggleGroup"/>; items derive from <see cref="BhRovingItemBase"/>.
/// </summary>
public sealed class BhRovingFocusContext
{
    private readonly BhInterop _interop;
    private BhRovingItemBase? _current;

    internal BhRovingFocusContext(BhInterop interop, string key)
    {
        _interop = interop;
        Items = new BhCollection<BhRovingItemBase>(key);
    }

    /// <summary>The registered items in document order.</summary>
    public BhCollection<BhRovingItemBase> Items { get; }

    /// <summary>Which arrow keys move focus.</summary>
    public BhRovingOrientation Orientation { get; internal set; } = BhRovingOrientation.Horizontal;

    /// <summary>Whether navigation wraps at the ends.</summary>
    public bool Loop { get; internal set; } = true;

    /// <summary>Reading direction (mirrors Left / Right).</summary>
    public BhDirection Dir { get; internal set; }

    /// <summary>Whether a <see cref="BhToolbar"/> owns the group.</summary>
    internal bool IsToolbar { get; set; }

    /// <summary>The item that is currently the group's tab stop.</summary>
    public BhRovingItemBase? TabStop
    {
        get
        {
            if (_current is not null && Items.IndexOf(_current) >= 0 && !_current.IsItemDisabled) return _current;
            return Items.Items.FirstOrDefault(i => i.PreferredTabStop && !i.IsItemDisabled) ?? Items.First();
        }
    }

    internal string KeysAttribute => Orientation switch
    {
        BhRovingOrientation.Vertical => "ArrowUp ArrowDown Home End",
        BhRovingOrientation.Both => "ArrowUp ArrowDown ArrowLeft ArrowRight Home End",
        _ => "ArrowLeft ArrowRight Home End",
    };

    internal void SetCurrent(BhRovingItemBase item)
    {
        if (ReferenceEquals(_current, item)) return;
        var previous = TabStop;
        _current = item;
        previous?.Refresh();
        item.Refresh();
    }

    internal async Task<bool> HandleKeyDownAsync(BhRovingItemBase item, KeyboardEventArgs e)
    {
        if (e.CtrlKey || e.MetaKey || e.AltKey) return false;
        var rtl = Dir == BhDirection.Rtl;
        var horizontal = Orientation != BhRovingOrientation.Vertical;
        var vertical = Orientation != BhRovingOrientation.Horizontal;

        BhRovingItemBase? target = e.Key switch
        {
            "ArrowRight" when horizontal => rtl ? Items.Previous(item, Loop) : Items.Next(item, Loop),
            "ArrowLeft" when horizontal => rtl ? Items.Next(item, Loop) : Items.Previous(item, Loop),
            "ArrowDown" when vertical => Items.Next(item, Loop),
            "ArrowUp" when vertical => Items.Previous(item, Loop),
            "Home" => Items.First(),
            "End" => Items.Last(),
            _ => null,
        };
        if (target is null || ReferenceEquals(target, item)) return false;

        await Items.EnsureOrderAsync(_interop);
        SetCurrent(target);
        await _interop.FocusAsync(target.ItemId);
        await target.OnRovedToAsync();
        return true;
    }
}

/// <summary>Which arrow keys a roving-focus group responds to.</summary>
public enum BhRovingOrientation
{
    /// <summary>Left / Right.</summary>
    Horizontal,

    /// <summary>Up / Down.</summary>
    Vertical,

    /// <summary>All four arrows.</summary>
    Both,
}

/// <summary>
/// Base class for items of a roving-tabindex group (toolbar buttons, toggle-group
/// items, generic <see cref="BhRovingFocusItem"/>s). Handles registration,
/// <c>tabindex</c>, arrow-key navigation and tab-stop tracking.
/// </summary>
public abstract class BhRovingItemBase : BhComponentBase, IBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BhRovingFocusContext? RovingContext { get; set; }

    /// <summary>The group this item belongs to (null when used standalone).</summary>
    protected BhRovingFocusContext? Roving => RovingContext;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public abstract bool IsItemDisabled { get; }

    /// <inheritdoc />
    public virtual string? ItemText => null;

    /// <summary>Whether this item should be the initial tab stop (e.g. the pressed item of a single toggle group).</summary>
    protected internal virtual bool PreferredTabStop => false;

    internal void Refresh() => StateHasChanged();

    /// <summary>Called after arrow keys moved focus to this item (radio-like groups select it).</summary>
    protected internal virtual Task OnRovedToAsync() => Task.CompletedTask;

    protected override void OnInitialized() => RovingContext?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && RovingContext is not null)
            await RovingContext.Items.EnsureOrderAsync(Interop);
    }

    /// <summary>Adds the roving behaviour (keydown + focus handlers) to the element being built.</summary>
    protected void AddRovingHandlers(RenderTreeBuilder builder, int sequence)
    {
        if (RovingContext is null) return;
        builder.AddAttribute(sequence, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleRovingKeyDown));
        builder.AddAttribute(sequence + 1, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => RovingContext.SetCurrent(this)));
    }

    /// <summary>Additional key handling (Enter / Space for non-button elements …).</summary>
    protected virtual Task OnKeyDownAsync(KeyboardEventArgs e) => Task.CompletedTask;

    private async Task HandleRovingKeyDown(KeyboardEventArgs e)
    {
        if (RovingContext is not null && await RovingContext.HandleKeyDownAsync(this, e)) return;
        await OnKeyDownAsync(e);
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (RovingContext is not null)
        {
            attrs["tabindex"] = ReferenceEquals(RovingContext.TabStop, this) ? 0 : -1;
            attrs["data-bh-item"] = RovingContext.Items.Key;
            attrs[BhKeyboard.KeysAttribute] = RovingContext.KeysAttribute;
        }
        return attrs;
    }

    public virtual void Dispose() => RovingContext?.Items.Unregister(this);
}
