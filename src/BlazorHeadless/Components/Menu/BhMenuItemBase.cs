using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Shared behaviour of every item in a menu panel (<see cref="BhMenuItem"/>,
/// <see cref="BhMenuCheckboxItem"/>, <see cref="BhMenuRadioItem"/>,
/// <see cref="BhMenuSubTrigger"/>, and the context-menu equivalents):
/// registration in the level's collection, highlight on hover, activation on
/// click / Enter / Space, typeahead text, and the common data attributes.
///
/// <para>Items use "virtual focus": the panel keeps DOM focus and points at the
/// highlighted item with <c>aria-activedescendant</c>.</para>
/// </summary>
public abstract class BhMenuItemBase : BhComponentBase, IBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BhMenuLevel? MenuLevel { get; set; }

    /// <summary>Disables the item. Disabled items are skipped by keyboard navigation and typeahead.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Plain-text label used for typeahead. Defaults to the item's rendered text,
    /// so it is only needed when the content isn't plain text.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>The menu level this item belongs to.</summary>
    protected BhMenuLevel? Level => MenuLevel;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || (MenuLevel?.Root.Disabled ?? false);

    /// <inheritdoc />
    public string? ItemText => Label ?? MenuLevel?.Items.GetRenderedText(ItemId);

    /// <summary>Whether this item is currently highlighted.</summary>
    public bool IsActive => MenuLevel is not null && ReferenceEquals(MenuLevel.ActiveItem, this);

    /// <summary>The ARIA role (<c>menuitem</c>, <c>menuitemcheckbox</c>, <c>menuitemradio</c>).</summary>
    protected virtual string Role => "menuitem";

    internal virtual bool IsSubTrigger => false;

    /// <summary>Runs the item's action. <paramref name="viaKeyboard"/> is true for Enter / Space.</summary>
    internal abstract Task SelectAsync(bool viaKeyboard);

    /// <summary>Renders the item's content.</summary>
    protected abstract void RenderContent(RenderTreeBuilder builder, int sequence);

    internal void Refresh() => StateHasChanged();

    /// <summary>Closes the whole menu tree.</summary>
    protected Task CloseMenuAsync() => MenuLevel?.Root.CloseAsync() ?? Task.CompletedTask;

    /// <summary>Clicks the underlying element (keyboard activation of link items).</summary>
    protected ValueTask ClickElementAsync() => Interop.ClickAsync(ItemId);

    /// <summary>Whether the item renders as a link (<c>As="a"</c>).</summary>
    protected bool IsLink => Tag.Equals("a", StringComparison.OrdinalIgnoreCase);

    protected override void OnInitialized() => MenuLevel?.Items.Register(this);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && MenuLevel is not null)
            await MenuLevel.Items.EnsureOrderAsync(Interop);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnter));
        builder.AddAttribute(32, "onpointerleave",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerLeave));
        builder.AddElementReferenceCapture(40, CaptureElement);
        RenderContent(builder, 50);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = Role;
        attrs["tabindex"] = -1;
        if (MenuLevel is not null)
            attrs["data-bh-item"] = MenuLevel.Items.Key;
        if (Label is not null)
            attrs["data-bh-text"] = Label;
        if (IsItemDisabled)
            attrs["aria-disabled"] = "true";
        SetDataFlag(attrs, "active", IsActive);
        SetDataFlag(attrs, "highlighted", IsActive);
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    private async Task HandleClick(MouseEventArgs args)
    {
        if (IsItemDisabled || MenuLevel is null) return;
        await MenuLevel.ActivateAsync(this, viaKeyboard: false);
    }

    private Task HandlePointerEnter(PointerEventArgs args)
    {
        MenuLevel?.PointerEnter(this);
        return OnPointerEnterAsync(args);
    }

    private Task HandlePointerLeave(PointerEventArgs args)
    {
        MenuLevel?.PointerLeave(this);
        return OnPointerLeaveAsync(args);
    }

    /// <summary>Hook for subclasses (submenu triggers).</summary>
    protected virtual Task OnPointerEnterAsync(PointerEventArgs args) => Task.CompletedTask;

    /// <summary>Hook for subclasses (submenu triggers).</summary>
    protected virtual Task OnPointerLeaveAsync(PointerEventArgs args) => Task.CompletedTask;

    public virtual void Dispose()
    {
        if (MenuLevel is null) return;
        if (IsActive) MenuLevel.SetActive(null, viaPointer: true);
        MenuLevel.Items.Unregister(this);
    }
}
