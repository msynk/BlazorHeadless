using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible dropdown Menu implementing the WAI-ARIA Menu Button pattern.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Rich items</b> — <see cref="BhMenuItem"/>, <see cref="BhMenuCheckboxItem"/>, <see cref="BhMenuRadioGroup"/> / <see cref="BhMenuRadioItem"/>, groups, labels, separators.</item>
///   <item><b>Nested submenus</b> — <see cref="BhMenuSub"/>, <see cref="BhMenuSubTrigger"/>, <see cref="BhMenuSubContent"/> with hover intent and keyboard support.</item>
///   <item><b>Full keyboard support</b> — Enter / Space / ArrowDown / ArrowUp open; arrows, Home / End, typeahead navigate; Enter / Space activate; ArrowRight / ArrowLeft enter and leave submenus; Escape and Tab close.</item>
///   <item><b>Focus management</b> — focus moves into the menu (items are announced via <c>aria-activedescendant</c>) and returns to the button on close.</item>
///   <item><b>Dismissal</b> — pointer-down outside or Escape closes it; no invisible overlays, so the first outside click still reaches its target.</item>
///   <item><b>Menubar ready</b> — place menus inside a <see cref="BhMenubar"/> for an application menu bar.</item>
///   <item><b>Data attributes</b> — <c>data-state="open|closed"</c>; <c>data-active</c> / <c>data-highlighted</c>, <c>data-disabled</c>, <c>data-state="checked|unchecked"</c> on items.</item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhMenu&gt;
///     &lt;BhMenuButton&gt;Options ▾&lt;/BhMenuButton&gt;
///     &lt;BhMenuItems Anchor="@("bottom start")"&gt;
///         &lt;BhMenuItem OnClick="Edit"&gt;Edit&lt;/BhMenuItem&gt;
///         &lt;BhMenuItem OnClick="Delete" Disabled="true"&gt;Delete&lt;/BhMenuItem&gt;
///     &lt;/BhMenuItems&gt;
/// &lt;/BhMenu&gt;
/// </code>
/// </summary>
public class BhMenu : BhComponentBase, IBhMenuRoot, IDisposable
{
    private bool _open;
    private BhMenuFocus _focusIntent;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BhMenubarContext? MenubarContext { get; set; }

    /// <summary>Whether the menu is open. Supplying it makes the menu controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Disables the entire menu.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Reading direction (flips submenu keys). Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>Child content. Should contain an <see cref="BhMenuButton"/> and an <see cref="BhMenuItems"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>Whether the menu is open.</summary>
    public bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    bool IBhMenuRoot.Disabled => Disabled;
    BhDirection IBhMenuRoot.Dir => Dir ?? CascadedDir;
    string? IBhMenuRoot.TriggerId => ButtonId;
    BhMenubarContext? IBhMenuRoot.Menubar => MenubarContext;

    private string ButtonId => $"{ComponentId}-button";
    private string ItemsId => $"{ComponentId}-items";

    protected override void OnInitialized()
    {
        _open = DefaultOpen;
        MenubarContext?.Register(this);
    }

    public void Dispose() => MenubarContext?.Unregister(this);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhMenuContext(this, IsOpen, Disabled, ComponentId, ButtonId, ItemsId, MenubarContext);

        builder.OpenComponent<CascadingValue<IBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BhMenuContext>>(0);
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
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    internal BhMenuFocus ConsumeFocusIntent()
    {
        var intent = _focusIntent;
        _focusIntent = BhMenuFocus.Container;
        return intent;
    }

    internal BhMenuFocus PeekFocusIntent() => _focusIntent;

    /// <summary>Opens the menu, highlighting the item selected by <paramref name="focus"/>.</summary>
    public async Task OpenAsync(BhMenuFocus focus = BhMenuFocus.Container)
    {
        if (Disabled) return;
        _focusIntent = focus;
        if (IsOpen) return;
        await SetOpenAsync(true);
        if (MenubarContext is not null)
            await MenubarContext.OnMenuOpenedAsync(this);
    }

    /// <summary>Closes the menu (and every open submenu).</summary>
    public Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    internal Task ToggleAsync(BhMenuFocus focus) => IsOpen ? CloseAsync() : OpenAsync(focus);

    private async Task SetOpenAsync(bool value)
    {
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }

    internal Task FocusButtonAsync() => Interop.FocusAsync(ButtonId).AsTask();

    internal bool IsDisabled => Disabled;

    internal void Refresh() => StateHasChanged();
}
