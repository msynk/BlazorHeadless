using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible dropdown Menu implementing the WAI-ARIA Menu Button pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Action-based</b> — each item runs a callback and closes the menu. No selection model.</item>
///   <item><b>Virtual focus</b> — focus stays on the button; <c>aria-activedescendant</c> drives screen-reader announcements.</item>
///   <item><b>Full keyboard support</b> — ArrowDown/Up (with auto-open), Home, End, Enter/Space (activate), Escape, Tab, typeahead.</item>
///   <item><b>Click-outside closes</b> via a transparent full-viewport overlay.</item>
///   <item><b>Compound API</b> — <see cref="BhMenuButton"/>, <see cref="BhMenuItems"/>, <see cref="BhMenuItem"/>.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c> on root and panel; <c>data-active</c>, <c>data-disabled</c> on items.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhMenu class="menu"&gt;
///     &lt;BhMenuButton class="menu-button"&gt;Options ▾&lt;/BhMenuButton&gt;
///     &lt;BhMenuItems class="menu-items"&gt;
///         &lt;BhMenuItem OnClick="Edit"   class="menu-item"&gt;Edit&lt;/BhMenuItem&gt;
///         &lt;BhMenuItem OnClick="Delete" class="menu-item" Disabled="true"&gt;Delete&lt;/BhMenuItem&gt;
///     &lt;/BhMenuItems&gt;
/// &lt;/BhMenu&gt;
/// </code>
/// </summary>
public class BhMenu : BhComponentBase
{
    private readonly List<BhMenuItem> _items = new();

    private bool _isOpen;
    private int _activeIndex = -1;
    private ElementReference _buttonRef;

    private string _typeaheadBuffer = string.Empty;
    private DateTime _typeaheadResetAt = DateTime.MinValue;
    private static readonly TimeSpan TypeaheadWindow = TimeSpan.FromMilliseconds(500);

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Disables the entire menu.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Child content. Should contain an <see cref="BhMenuButton"/> and an <see cref="BhMenuItems"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhMenuContext>>(0);
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

    private BhMenuContext CreateContext()
    {
        var ctx = new BhMenuContext(
            isOpen: _isOpen,
            disabled: Disabled,
            activeIndex: _activeIndex,
            baseId: ComponentId,
            registerItem: RegisterItem,
            unregisterItem: UnregisterItem,
            setActiveIndex: SetActiveIndex,
            activateItemAsync: ActivateItemAsync,
            handleButtonKeyDownAsync: HandleButtonKeyDownAsync,
            handleMenuKeyDownAsync: HandleMenuKeyDownAsync,
            toggleAsync: ToggleAsync,
            closeAsync: CloseAsync,
            registerButton: RegisterButton);
        ctx.SetButtonRef(_buttonRef);
        return ctx;
    }

    // ── Button registration ───────────────────────────────────────────────────

    private void RegisterButton(ElementReference button) => _buttonRef = button;

    /// <summary>Gets the button element reference for anchor positioning.</summary>
    internal ElementReference ButtonRef => _buttonRef;

    // ── Item registration ─────────────────────────────────────────────────────

    internal int RegisterItem(BhMenuItem item)
    {
        if (!_items.Contains(item))
            _items.Add(item);
        return _items.IndexOf(item);
    }

    internal void UnregisterItem(BhMenuItem item) => _items.Remove(item);

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

    // ── Open / Close / Toggle ─────────────────────────────────────────────────

    private Task ToggleAsync()
    {
        if (Disabled) return Task.CompletedTask;
        if (_isOpen) return CloseAsync();
        return OpenAsync(activateLast: false);
    }

    private Task OpenAsync(bool activateLast = false)
    {
        if (Disabled) return Task.CompletedTask;
        _isOpen = true;
        _activeIndex = activateLast
            ? FindEnabledIndex(_items.Count - 1, step: -1) ?? -1
            : FindEnabledIndex(0, step: +1) ?? -1;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task CloseAsync()
    {
        if (!_isOpen) return Task.CompletedTask;
        _isOpen = false;
        _activeIndex = -1;
        StateHasChanged();
        return Task.CompletedTask;
    }

    // ── Keyboard handling ─────────────────────────────────────────────────────

    private async Task HandleButtonKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled) return;

        switch (args.Key)
        {
            case " ":
            case "Enter":
            case "ArrowDown":
                await OpenAsync(activateLast: false);
                break;

            case "ArrowUp":
                await OpenAsync(activateLast: true);
                break;
        }
    }

    private async Task HandleMenuKeyDownAsync(KeyboardEventArgs args)
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
