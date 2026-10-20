using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>Information about a completed reorder.</summary>
/// <typeparam name="TItem">The item type.</typeparam>
public sealed record BhReorderEventArgs<TItem>(TItem Item, int OldIndex, int NewIndex);

/// <summary>Customizable screen-reader announcements for <see cref="BhSortable{TItem}"/>.</summary>
public sealed class BhSortableAnnouncements
{
    /// <summary>Instructions referenced by every drag handle (<c>aria-describedby</c>).</summary>
    public string Instructions { get; set; } =
        "To pick up an item, press Space or Enter. While dragging, use the arrow keys to move it. Press Space or Enter again to drop it, or Escape to cancel.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> PickedUp { get; set; } =
        (label, position, count) => $"Picked up {label}. Position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Moved { get; set; } =
        (label, position, count) => $"{label} moved to position {position} of {count}.";

    /// <summary>(label, position, count) → message.</summary>
    public Func<string, int, int, string> Dropped { get; set; } =
        (label, position, count) => $"Dropped {label} at position {position} of {count}.";

    /// <summary>(label, position) → message.</summary>
    public Func<string, int, string> Cancelled { get; set; } =
        (label, position) => $"Reordering cancelled. {label} returned to position {position}.";
}

/// <summary>
/// A headless, accessible sortable list: reorder items by dragging with a pointer
/// (mouse, pen, touch) or entirely with the keyboard, with live screen-reader
/// announcements of every step.
///
/// <para><b>Pointer:</b> drag an item (or its <see cref="BhSortableHandle{TItem}"/>); siblings
/// shift out of the way in the browser and the final order is committed on drop.
/// Escape cancels.</para>
/// <para><b>Keyboard:</b> focus a handle, press Space / Enter to pick the item up,
/// arrow keys to move it, Space / Enter to drop, Escape to cancel.</para>
/// <para><b>Data:</b> <c>@bind-Items</c>, or pass a mutable <see cref="IList{T}"/> and it
/// is reordered in place; <see cref="OnReorder"/> reports each move.</para>
///
/// <code>
/// &lt;BhSortable TItem="Task" @bind-Items="tasks" ItemKey="t =&gt; t.Id" ItemLabel="t =&gt; t.Title" Context="s"&gt;
///     &lt;BhSortableHandle TItem="Task" aria-label="@($"Reorder {s.Item.Title}")"&gt;⠿&lt;/BhSortableHandle&gt;
///     @s.Item.Title
/// &lt;/BhSortable&gt;
/// </code>
/// </summary>
/// <typeparam name="TItem">The item type.</typeparam>
public class BhSortable<TItem> : BhComponentBase, IAsyncDisposable
{
    private List<TItem>? _preview;
    private int _grabbedFrom = -1;
    private int _grabbedIndex = -1;
    private int _pointerDragIndex = -1;
    private int _handle;
    private DotNetObjectReference<BhSortable<TItem>>? _selfRef;
    private string? _focusAfterRender;

    /// <summary>The items. Use <c>@bind-Items</c>, or pass a mutable list to reorder it in place.</summary>
    [Parameter, EditorRequired]
    public IList<TItem> Items { get; set; } = [];

    /// <summary>Two-way binding callback for <see cref="Items"/> (receives a new list).</summary>
    [Parameter]
    public EventCallback<IList<TItem>> ItemsChanged { get; set; }

    /// <summary>Fires after an item moved.</summary>
    [Parameter]
    public EventCallback<BhReorderEventArgs<TItem>> OnReorder { get; set; }

    /// <summary>A stable key per item (used as <c>@key</c>). Defaults to the item itself.</summary>
    [Parameter]
    public Func<TItem, object>? ItemKey { get; set; }

    /// <summary>Text naming an item in announcements. Defaults to <c>ToString()</c>.</summary>
    [Parameter]
    public Func<TItem, string>? ItemLabel { get; set; }

    /// <summary>Renders an item's content; receives <see cref="BhSortableItemContext{TItem}"/>.</summary>
    [Parameter]
    public RenderFragment<BhSortableItemContext<TItem>>? ChildContent { get; set; }

    /// <summary>Vertical (default) or horizontal list (which arrow keys move).</summary>
    [Parameter]
    public BhOrientation Orientation { get; set; } = BhOrientation.Vertical;

    /// <summary>Disables reordering.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The element rendered for each item. Default: <c>li</c> (with the default <c>ul</c> container).</summary>
    [Parameter]
    public string ItemTag { get; set; } = "li";

    /// <summary>Screen-reader messages (localize here).</summary>
    [Parameter]
    public BhSortableAnnouncements Announcements { get; set; } = new();

    protected override string DefaultTag => "ul";

    private IReadOnlyList<TItem> Display => _preview ?? (IReadOnlyList<TItem>)Items.ToList();

    internal string InstructionsId => $"{ComponentId}-instructions";

    private string Label(TItem item) => ItemLabel?.Invoke(item) ?? item?.ToString() ?? string.Empty;

    internal bool IsGrabbed(int index) => _grabbedIndex == index;

    internal bool IsDragging(int index) => _grabbedIndex == index || _pointerDragIndex == index;

    internal bool IsDisabled => Disabled;

    internal string HandleId(int index) => $"{ComponentId}-handle-{index}";

    internal string ContainerId => ComponentId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var options = new Dictionary<string, object?> { ["orientation"] = Orientation.ToAttribute() };
        if (firstRender)
        {
            _selfRef = DotNetObjectReference.Create(this);
            _handle = await Interop.SortableAttachAsync(_selfRef, ComponentId, options);
        }
        else
        {
            await Interop.SortableUpdateAsync(_handle, options);
        }

        if (_focusAfterRender is { } id)
        {
            _focusAfterRender = null;
            await Interop.FocusAsync(id);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);

        builder.OpenComponent<CascadingValue<BhSortable<TItem>>>(35);
        builder.AddComponentParameter(36, "Value", this);
        builder.AddComponentParameter(37, "IsFixed", true);
        builder.AddComponentParameter(38, "ChildContent", (RenderFragment)(inner =>
        {
            var items = Display;
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                inner.OpenComponent<BhSortableItem<TItem>>(40);
                inner.SetKey(ItemKey?.Invoke(item) ?? item);
                inner.AddComponentParameter(41, nameof(BhSortableItem<TItem>.Sortable), this);
                inner.AddComponentParameter(42, nameof(BhSortableItem<TItem>.Item), item);
                inner.AddComponentParameter(43, nameof(BhSortableItem<TItem>.Index), i);
                inner.AddComponentParameter(44, nameof(BhSortableItem<TItem>.Tag), ItemTag);
                inner.AddComponentParameter(45, nameof(BhSortableItem<TItem>.ChildContent), ChildContent);
                inner.CloseComponent();
            }
        }));
        builder.CloseComponent();
        builder.CloseElement();

        // Instructions referenced by every handle.
        builder.OpenElement(60, "div");
        builder.AddAttribute(61, "id", InstructionsId);
        builder.AddAttribute(62, "hidden", true);
        builder.AddContent(63, Announcements.Instructions);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", Disabled);
        SetDataFlag(attrs, "dragging", _grabbedIndex >= 0 || _pointerDragIndex >= 0);
        return attrs;
    }

    // ── Keyboard reordering ──────────────────────────────────────────────────

    internal async Task HandleKeyDownAsync(int index, KeyboardEventArgs e)
    {
        if (Disabled) return;
        var count = Display.Count;
        var next = Orientation == BhOrientation.Vertical ? "ArrowDown" : "ArrowRight";
        var prev = Orientation == BhOrientation.Vertical ? "ArrowUp" : "ArrowLeft";

        if (_grabbedIndex < 0)
        {
            if (e.Key is " " or "Enter")
            {
                _preview = Items.ToList();
                _grabbedFrom = _grabbedIndex = index;
                await AnnounceAsync(Announcements.PickedUp(Label(_preview[index]), index + 1, count));
                StateHasChanged();
            }
            return;
        }

        if (e.Key == next || e.Key == prev)
        {
            var target = Math.Clamp(_grabbedIndex + (e.Key == next ? 1 : -1), 0, count - 1);
            if (target == _grabbedIndex) return;
            var item = _preview![_grabbedIndex];
            _preview.RemoveAt(_grabbedIndex);
            _preview.Insert(target, item);
            _grabbedIndex = target;
            _focusAfterRender = HandleId(target);
            await AnnounceAsync(Announcements.Moved(Label(item), target + 1, count));
            StateHasChanged();
        }
        else if (e.Key is " " or "Enter")
        {
            var item = _preview![_grabbedIndex];
            var from = _grabbedFrom;
            var to = _grabbedIndex;
            ResetGrab();
            await CommitAsync(from, to);
            await AnnounceAsync(Announcements.Dropped(Label(item), to + 1, count));
        }
        else if (e.Key is "Escape" or "Tab")
        {
            await CancelGrabAsync();
        }
    }

    internal async Task CancelGrabAsync()
    {
        if (_grabbedIndex < 0) return;
        var from = _grabbedFrom;
        var item = Items[from];
        ResetGrab();
        _focusAfterRender = HandleId(from);
        await AnnounceAsync(Announcements.Cancelled(Label(item), from + 1));
        StateHasChanged();
    }

    private void ResetGrab()
    {
        _preview = null;
        _grabbedFrom = _grabbedIndex = -1;
    }

    // ── Pointer reordering (reported by the JS runtime) ──────────────────────

    /// <summary>Invoked by the JS runtime when a pointer drag starts.</summary>
    [JSInvokable]
    public async Task OnPointerDragStart(int from)
    {
        if (Disabled || from < 0 || from >= Items.Count) return;
        _pointerDragIndex = from;
        await AnnounceAsync(Announcements.PickedUp(Label(Items[from]), from + 1, Items.Count));
        StateHasChanged();
    }

    /// <summary>Invoked by the JS runtime when a pointer drag ends.</summary>
    [JSInvokable]
    public async Task OnPointerDragEnd(int from, int to, bool cancelled)
    {
        _pointerDragIndex = -1;
        if (Disabled || from < 0 || from >= Items.Count) return;
        var item = Items[from];
        if (cancelled || from == to)
        {
            await AnnounceAsync(Announcements.Cancelled(Label(item), from + 1));
            StateHasChanged();
            return;
        }
        await CommitAsync(from, Math.Clamp(to, 0, Items.Count - 1));
        await AnnounceAsync(Announcements.Dropped(Label(item), to + 1, Items.Count));
    }

    private async Task CommitAsync(int from, int to)
    {
        if (from == to)
        {
            StateHasChanged();
            return;
        }
        var item = Items[from];
        if (ItemsChanged.HasDelegate)
        {
            var list = Items.ToList();
            list.RemoveAt(from);
            list.Insert(to, item);
            await ItemsChanged.InvokeAsync(list);
        }
        else if (!Items.IsReadOnly)
        {
            Items.RemoveAt(from);
            Items.Insert(to, item);
        }
        await OnReorder.InvokeAsync(new BhReorderEventArgs<TItem>(item, from, to));
        _focusAfterRender ??= HandleId(to);
        StateHasChanged();
    }

    private ValueTask AnnounceAsync(string message)
        => (GetService<IBhAnnouncer>() ?? new BhAnnouncer(Interop)).AnnounceAsync(message, BhPoliteness.Assertive);

    public async ValueTask DisposeAsync()
    {
        if (_handle > 0) await Interop.SortableDetachAsync(_handle);
        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
