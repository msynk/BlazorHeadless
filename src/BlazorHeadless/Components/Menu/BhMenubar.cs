using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A horizontal application menu bar (<c>role="menubar"</c>) implementing the
/// WAI-ARIA Menubar pattern. Place <see cref="BhMenu"/> components inside it; their
/// <see cref="BhMenuButton"/>s become menubar items with a single tab stop.
///
/// <para><b>Keyboard:</b> ArrowLeft / ArrowRight move between menus (opening the
/// neighbour when a menu is already open), Home / End jump to the ends, Enter /
/// Space / ArrowDown open the focused menu, ArrowRight / ArrowLeft inside an open
/// menu move to the adjacent one (unless the item opens a submenu).</para>
/// <para><b>Pointer:</b> once a menu is open, hovering another trigger switches to it.</para>
///
/// <code>
/// &lt;BhMenubar&gt;
///     &lt;BhMenu&gt;
///         &lt;BhMenuButton&gt;File&lt;/BhMenuButton&gt;
///         &lt;BhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BhMenuItems&gt;
///     &lt;/BhMenu&gt;
///     &lt;BhMenu&gt;
///         &lt;BhMenuButton&gt;Edit&lt;/BhMenuButton&gt;
///         &lt;BhMenuItems Anchor="@("bottom start")"&gt;…&lt;/BhMenuItems&gt;
///     &lt;/BhMenu&gt;
/// &lt;/BhMenubar&gt;
/// </code>
/// </summary>
public class BhMenubar : BhComponentBase
{
    private BhMenubarContext? _context;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>Whether arrow keys wrap from the last menu to the first. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    /// <summary>Reading direction. Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>The menus.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnParametersSet()
    {
        _context ??= new BhMenubarContext(Interop);
        _context.Loop = Loop;
        _context.Dir = Dir ?? CascadedDir;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhMenubarContext>>(40);
        builder.AddComponentParameter(41, "Value", _context);
        builder.AddComponentParameter(42, "IsFixed", true);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menubar";
        attrs["aria-orientation"] = "horizontal";
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        return attrs;
    }
}

/// <summary>Coordinates the menus of a <see cref="BhMenubar"/>.</summary>
public sealed class BhMenubarContext
{
    private readonly List<BhMenu> _menus = new();
    private readonly BhInterop _interop;
    private BhMenu? _tabStop;

    internal BhMenubarContext(BhInterop interop) => _interop = interop;

    internal bool Loop { get; set; } = true;
    internal BhDirection Dir { get; set; }

    internal void Register(BhMenu menu)
    {
        if (!_menus.Contains(menu)) _menus.Add(menu);
    }

    internal void Unregister(BhMenu menu)
    {
        _menus.Remove(menu);
        if (_tabStop == menu) _tabStop = null;
    }

    internal bool IsTabStop(BhMenu menu)
        => _tabStop is not null ? _tabStop == menu : _menus.FirstOrDefault(m => !m.IsDisabled) == menu;

    internal void SetTabStop(BhMenu menu)
    {
        if (_tabStop == menu) return;
        var previous = _tabStop ?? _menus.FirstOrDefault(m => !m.IsDisabled);
        _tabStop = menu;
        previous?.Refresh();
        menu.Refresh();
    }

    private BhMenu? OpenMenu => _menus.FirstOrDefault(m => m.IsOpen);

    internal async Task OnMenuOpenedAsync(BhMenu menu)
    {
        SetTabStop(menu);
        foreach (var other in _menus.Where(m => m != menu && m.IsOpen).ToArray())
            await other.CloseAsync();
    }

    internal async Task OnTriggerPointerEnterAsync(BhMenu menu)
    {
        if (OpenMenu is { } open && open != menu && !menu.IsDisabled)
        {
            await menu.OpenAsync(BhMenuFocus.Container);
        }
    }

    internal async Task HandleTriggerKeyDownAsync(BhMenu menu, KeyboardEventArgs e)
    {
        var next = Dir == BhDirection.Rtl ? "ArrowLeft" : "ArrowRight";
        var prev = Dir == BhDirection.Rtl ? "ArrowRight" : "ArrowLeft";
        BhMenu? target = null;
        if (e.Key == next) target = Adjacent(menu, +1);
        else if (e.Key == prev) target = Adjacent(menu, -1);
        else if (e.Key == "Home") target = _menus.FirstOrDefault(m => !m.IsDisabled);
        else if (e.Key == "End") target = _menus.LastOrDefault(m => !m.IsDisabled);
        if (target is null) return;
        SetTabStop(target);
        await target.FocusButtonAsync();
    }

    internal async Task MoveFromMenuAsync(IBhMenuRoot root, int delta)
    {
        if (root is not BhMenu menu) return;
        var target = Adjacent(menu, delta);
        if (target is null || target == menu) return;
        await menu.CloseAsync();
        SetTabStop(target);
        await target.FocusButtonAsync();
        await target.OpenAsync(BhMenuFocus.First);
    }

    private BhMenu? Adjacent(BhMenu menu, int delta)
    {
        var index = _menus.IndexOf(menu);
        if (index < 0) return null;
        for (var i = 1; i <= _menus.Count; i++)
        {
            var j = index + delta * i;
            if (j < 0 || j >= _menus.Count)
            {
                if (!Loop) return null;
                j = ((j % _menus.Count) + _menus.Count) % _menus.Count;
            }
            if (!_menus[j].IsDisabled) return _menus[j];
        }
        return null;
    }
}
