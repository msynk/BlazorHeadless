using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Shared implementation of a menu panel (<c>role="menu"</c>) — used by
/// <see cref="BhMenuItems"/>, <see cref="BhMenuSubContent"/> and
/// <see cref="BhContextMenuContent"/>. Owns a <see cref="BhMenuLevel"/>, keeps DOM
/// focus while open and exposes the highlighted item through
/// <c>aria-activedescendant</c>.
/// </summary>
public abstract class BhMenuContentBase : BhFloatingContentBase
{
    private BhMenuLevel? _level;
    private int _contentVersion;
    private BhMenuFocus _pendingFocus;
    private bool _wasOpen;

    /// <summary>Child content: items, groups, labels, separators and submenus.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether arrow-key navigation wraps from the last item to the first. Default: true.</summary>
    [Parameter]
    public bool Loop { get; set; } = true;

    protected override string DefaultTag => "div";

    /// <summary>The level owned by this panel.</summary>
    protected BhMenuLevel Level => _level ??= CreateLevel(StateHasChanged);

    /// <summary>The panel's element id.</summary>
    protected abstract string ContentId { get; }

    /// <summary>Creates this panel's level.</summary>
    private protected abstract BhMenuLevel CreateLevel(Action refresh);

    /// <summary>Returns (and clears) how the highlight should be placed now that the panel opened.</summary>
    private protected abstract BhMenuFocus ConsumeFocusIntent();

    /// <summary>Peeks the focus intent without clearing it (used to configure the JS focus scope).</summary>
    private protected abstract BhMenuFocus PeekFocusIntent();

    /// <summary>The id of the element that labels the menu (trigger), if any.</summary>
    protected virtual string? LabelledById => null;

    protected override void OnParametersSet()
    {
        Level.Loop = Loop;
        _contentVersion++;
    }

    /// <summary>Builds the focus options shared by all menu panels.</summary>
    private protected BhFocusScopeOptions CreateFocusOptions(string? returnFocusId) => new()
    {
        AutoFocus = PeekFocusIntent() == BhMenuFocus.None ? "none" : "container",
        RestoreFocus = true,
        ReturnFocusId = returnFocusId,
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (IsOpen && !_wasOpen)
        {
            _wasOpen = true;
            _pendingFocus = ConsumeFocusIntent();
            await Level.Items.EnsureOrderAsync(Interop);
            switch (_pendingFocus)
            {
                case BhMenuFocus.First: Level.SetActive(Level.Items.First()); break;
                case BhMenuFocus.Last: Level.SetActive(Level.Items.Last()); break;
            }
        }
        else if (!IsOpen && _wasOpen)
        {
            _wasOpen = false;
            Level.Reset();
            Level.HasFocus = false;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = ContentId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ContentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(30, "hidden", true);

        builder.AddAttribute(40, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, e => Level.HandleKeyDownAsync(e)));
        builder.AddEventStopPropagationAttribute(41, "onkeydown", IsOpen);
        builder.AddAttribute(42, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Level.HasFocus = true));
        builder.AddAttribute(43, "onblur",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Level.HasFocus = false));
        builder.AddAttribute(44, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, OnPointerEnterContentAsync));

        builder.AddElementReferenceCapture(50, CaptureElement);

        builder.OpenComponent<CascadingValue<BhMenuLevel>>(60);
        builder.AddComponentParameter(61, "Value", Level);
        builder.AddComponentParameter(62, "IsFixed", true);
        builder.AddComponentParameter(63, "ChildContent", (RenderFragment)(inner =>
            BhFloatingScope.Render(inner, 0, Scope, (RenderFragment)(scoped =>
                BhRenderBoundary.Render(scoped, 0, _contentVersion, ChildContent)))));
        builder.CloseComponent();

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menu";
        attrs["tabindex"] = -1;
        attrs["aria-orientation"] = "vertical";
        if (LabelledById is { } labelledBy)
            attrs["aria-labelledby"] = labelledBy;
        if (IsOpen && Level.ActiveItem is { } active)
            attrs["aria-activedescendant"] = active.ItemId;
        if (IsOpen)
            attrs[BhKeyboard.KeysAttribute] = Level.KeysAttribute;
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    /// <summary>Hook: pointer entered the panel (submenus cancel their pending close).</summary>
    protected virtual Task OnPointerEnterContentAsync(PointerEventArgs args) => Task.CompletedTask;
}
