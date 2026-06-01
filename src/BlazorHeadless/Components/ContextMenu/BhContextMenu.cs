using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Context Menu implementing the WAI-ARIA Menu design pattern.
/// Displays a menu located at the pointer, triggered by a right click on its
/// <see cref="BhContextMenuTrigger"/>.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Pointer-anchored</b> — opens at the cursor position on <c>contextmenu</c> (right-click).</item>
///   <item><b>Action-based</b> — each item runs a callback and closes the menu. No selection model.</item>
///   <item><b>Virtual focus</b> — focus moves to the content panel; <c>aria-activedescendant</c> drives screen-reader announcements.</item>
///   <item><b>Full keyboard support</b> — ArrowDown/Up, Home, End, Enter/Space (activate), Escape, Tab, typeahead.</item>
///   <item><b>Click-outside closes</b> via a transparent full-viewport overlay.</item>
///   <item><b>Collision-aware</b> — the content panel is clamped/flipped to stay within the viewport.</item>
///   <item><b>Compound API</b> — <see cref="BhContextMenuTrigger"/>, <see cref="BhContextMenuContent"/>,
///     <see cref="BhContextMenuItem"/>, <see cref="BhContextMenuGroup"/>, <see cref="BhContextMenuLabel"/>,
///     <see cref="BhContextMenuSeparator"/>.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c> on root, trigger and content;
///     <c>data-active</c>, <c>data-disabled</c> on items.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhContextMenu&gt;
///     &lt;BhContextMenuTrigger class="trigger"&gt;Right-click here&lt;/BhContextMenuTrigger&gt;
///     &lt;BhContextMenuContent class="menu"&gt;
///         &lt;BhContextMenuItem OnClick="Back"&gt;Back&lt;/BhContextMenuItem&gt;
///         &lt;BhContextMenuItem OnClick="Forward" Disabled="true"&gt;Forward&lt;/BhContextMenuItem&gt;
///         &lt;BhContextMenuSeparator /&gt;
///         &lt;BhContextMenuItem OnClick="Reload"&gt;Reload&lt;/BhContextMenuItem&gt;
///     &lt;/BhContextMenuContent&gt;
/// &lt;/BhContextMenu&gt;
/// </code>
/// </summary>
public class BhContextMenu : BhComponentBase
{
    private readonly List<BhContextMenuItem> _items = new();

    private bool _isOpen;
    private int _activeIndex = -1;
    private double _x;
    private double _y;

    private string _typeaheadBuffer = string.Empty;
    private DateTime _typeaheadResetAt = DateTime.MinValue;
    private static readonly TimeSpan TypeaheadWindow = TimeSpan.FromMilliseconds(500);

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Disables the entire context menu. The trigger will not open it.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Invoked whenever the open state changes, with the new value.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>
    /// Child content. Should contain a <see cref="BhContextMenuTrigger"/> and a
    /// <see cref="BhContextMenuContent"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhContextMenuContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);

            // Click-outside overlay — only present while open.
            if (_isOpen)
            {
                inner.OpenElement(50, "div");
                inner.AddAttribute(51, "data-blazor-headless-overlay", true);
                inner.AddAttribute(52, "style",
                    "position:fixed;inset:0;z-index:30;background:transparent;");
                inner.AddAttribute(53, "onclick",
                    EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await CloseAsync()));
                // Right-clicking the overlay should also dismiss (and not re-trigger the OS menu).
                inner.AddAttribute(54, "oncontextmenu",
                    EventCallback.Factory.Create<MouseEventArgs>(this, async _ => await CloseAsync()));
                inner.AddEventPreventDefaultAttribute(55, "oncontextmenu", true);
                inner.CloseElement();
            }

            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _isOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    // ── Context ───────────────────────────────────────────────────────────────

    private BhContextMenuContext CreateContext()
        => new(
            isOpen: _isOpen,
            disabled: Disabled,
            activeIndex: _activeIndex,
            x: _x,
            y: _y,
            baseId: ComponentId,
            registerItem: RegisterItem,
            unregisterItem: UnregisterItem,
            setActiveIndex: SetActiveIndex,
            activateItemAsync: ActivateItemAsync,
            handleContentKeyDownAsync: HandleContentKeyDownAsync,
            openAtAsync: OpenAtAsync,
            closeAsync: CloseAsync);

    // ── Item registration ─────────────────────────────────────────────────────

    internal int RegisterItem(BhContextMenuItem item)
    {
        if (!_items.Contains(item))
            _items.Add(item);
        return _items.IndexOf(item);
    }

    internal void UnregisterItem(BhContextMenuItem item) => _items.Remove(item);

    private void SetActiveIndex(int index)
    {
        if (index == _activeIndex) return;
        _activeIndex = index;
        StateHasChanged();
    }

    // ── Activation ────────────────────────────────────────────────────────────

    private async Task ActivateItemAsync(int index)
    {
        if (Disabled) return;
        if (index < 0 || index >= _items.Count) return;
        if (_items[index].IsItemDisabled) return;

        await CloseAsync();
        await _items[index].InvokeClickAsync();
    }

    // ── Open / Close ───────────────────────────────────────────────────────────

    private async Task OpenAtAsync(double x, double y)
    {
        if (Disabled) return;

        _x = x;
        _y = y;
        _activeIndex = -1;

        var wasOpen = _isOpen;
        _isOpen = true;
        StateHasChanged();

        if (!wasOpen)
            await OnOpenChange.InvokeAsync(true);
    }

    private async Task CloseAsync()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _activeIndex = -1;
        StateHasChanged();
        await OnOpenChange.InvokeAsync(false);
    }

    // ── Keyboard handling ─────────────────────────────────────────────────────

    private async Task HandleContentKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;

        switch (args.Key)
        {
            case "ArrowDown":
                _activeIndex = FindEnabledIndex(_activeIndex + 1, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "ArrowUp":
                _activeIndex = FindEnabledIndex(
                    (_activeIndex < 0 ? _items.Count : _activeIndex) - 1, step: -1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "Home":
                _activeIndex = FindEnabledIndex(0, step: +1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "End":
                _activeIndex = FindEnabledIndex(_items.Count - 1, step: -1) ?? _activeIndex;
                StateHasChanged();
                break;

            case "Enter":
            case " ":
                if (_activeIndex >= 0 && _activeIndex < _items.Count)
                    await ActivateItemAsync(_activeIndex);
                break;

            case "Escape":
            case "Tab":
                await CloseAsync();
                break;

            default:
                if (args.Key.Length == 1 && !string.IsNullOrWhiteSpace(args.Key))
                    HandleTypeahead(args.Key);
                break;
        }
    }

    private void HandleTypeahead(string key)
    {
        var now = DateTime.UtcNow;
        if (now > _typeaheadResetAt)
            _typeaheadBuffer = string.Empty;

        _typeaheadBuffer += key;
        _typeaheadResetAt = now + TypeaheadWindow;

        var match = _items.FirstOrDefault(item =>
            !item.IsItemDisabled
            && (item.GetTextLabel() ?? string.Empty)
                .StartsWith(_typeaheadBuffer, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
        {
            _activeIndex = _items.IndexOf(match);
            StateHasChanged();
        }
    }

    private int? FindEnabledIndex(int start, int step)
    {
        if (_items.Count == 0) return null;
        var i = ((start % _items.Count) + _items.Count) % _items.Count;
        for (var attempts = 0; attempts < _items.Count; attempts++)
        {
            if (!_items[i].IsItemDisabled) return i;
            i = ((i + step) % _items.Count + _items.Count) % _items.Count;
        }
        return null;
    }
}
