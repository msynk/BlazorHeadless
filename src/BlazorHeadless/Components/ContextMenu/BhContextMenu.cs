using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible Context Menu implementing the WAI-ARIA Menu pattern.
/// Displays a menu at the pointer when its <see cref="BhContextMenuTrigger"/>
/// receives a <c>contextmenu</c> event (right click, the Menu key or Shift+F10).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Pointer-anchored</b> with collision handling (flips / shifts to stay in the viewport).</item>
///   <item><b>Same item model as <see cref="BhMenu"/></b> — <see cref="BhMenuItem"/>, checkbox / radio items,
///     groups, labels, separators and nested <see cref="BhMenuSub"/>menus all work inside <see cref="BhContextMenuContent"/>.</item>
///   <item><b>Keyboard</b> — arrows, Home / End, typeahead, Enter / Space, Escape, Tab, submenu arrows.</item>
///   <item><b>Dismissal</b> — pointer-down outside, Escape, or right-clicking elsewhere.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhContextMenu&gt;
///     &lt;BhContextMenuTrigger class="trigger"&gt;Right-click here&lt;/BhContextMenuTrigger&gt;
///     &lt;BhContextMenuContent class="menu"&gt;
///         &lt;BhContextMenuItem OnClick="Back"&gt;Back&lt;/BhContextMenuItem&gt;
///         &lt;BhContextMenuSeparator /&gt;
///         &lt;BhContextMenuItem OnClick="Reload"&gt;Reload&lt;/BhContextMenuItem&gt;
///     &lt;/BhContextMenuContent&gt;
/// &lt;/BhContextMenu&gt;
/// </code>
/// </summary>
public class BhContextMenu : BhComponentBase, IBhMenuRoot
{
    private bool _open;
    private double _x;
    private double _y;
    private bool _keyboardOpened;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    /// <summary>Disables the context menu (the native browser menu shows instead).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (flips submenu keys). Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>Fires when the menu opens or closes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Child content. Should contain a <see cref="BhContextMenuTrigger"/> and a <see cref="BhContextMenuContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen => _open;

    bool IBhMenuRoot.Disabled => Disabled;
    BhDirection IBhMenuRoot.Dir => Dir ?? CascadedDir;
    string? IBhMenuRoot.TriggerId => null;
    BhMenubarContext? IBhMenuRoot.Menubar => null;

    internal IBhPointAnchoredContent? Content { get; set; }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhContextMenuContext(this, _open, Disabled, _x, _y, ComponentId);

        builder.OpenComponent<CascadingValue<IBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BhContextMenuContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _open);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal BhMenuFocus ConsumeFocusIntent()
    {
        var intent = _keyboardOpened ? BhMenuFocus.First : BhMenuFocus.Container;
        _keyboardOpened = false;
        return intent;
    }

    internal BhMenuFocus PeekFocusIntent() => BhMenuFocus.Container;

    /// <summary>Opens the menu at viewport coordinates (<paramref name="x"/>, <paramref name="y"/>).</summary>
    public async Task OpenAtAsync(double x, double y, bool viaKeyboard = false)
    {
        if (Disabled) return;
        _x = x;
        _y = y;
        _keyboardOpened = viaKeyboard;

        if (_open)
        {
            // Right-clicking again while open moves the menu to the new point.
            if (Content is not null) await Content.MoveToAsync(x, y);
            StateHasChanged();
            return;
        }

        _open = true;
        StateHasChanged();
        await OnOpenChange.InvokeAsync(true);
    }

    /// <summary>Closes the menu.</summary>
    public async Task CloseAsync()
    {
        if (!_open) return;
        _open = false;
        StateHasChanged();
        await OnOpenChange.InvokeAsync(false);
    }

    internal ValueTask<BhRect?> GetRectAsync(string id) => Interop.GetRectAsync(id);
}

/// <summary>Implemented by floating content that can be moved to a new anchor point.</summary>
internal interface IBhPointAnchoredContent
{
    Task MoveToAsync(double x, double y);
}
