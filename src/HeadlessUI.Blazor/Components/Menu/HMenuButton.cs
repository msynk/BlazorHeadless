using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The trigger button that opens the menu. Renders as
/// <c>&lt;button&gt;</c> with <c>aria-haspopup="menu"</c>,
/// <c>aria-expanded</c>, <c>aria-controls</c>, and <c>aria-activedescendant</c>
/// (while the menu is open).
///
/// <para>
/// Focus stays on this button while the menu is open; keyboard navigation
/// is handled here and forwarded to the parent <see cref="HMenu"/>.
/// </para>
/// </summary>
public class HMenuButton : HeadlessComponentBase
{
    [CascadingParameter]
    private MenuContext MenuContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="MenuButtonRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<MenuButtonRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private MenuButtonRenderContext RenderContext => new()
    {
        IsOpen = MenuContext?.IsOpen ?? false,
        Disabled = MenuContext?.Disabled ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", MenuContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = MenuContext?.IsOpen ?? false;
        var isDisabled = MenuContext?.Disabled ?? false;

        attrs["type"] = "button";
        attrs["aria-haspopup"] = "menu";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (MenuContext is not null)
        {
            attrs["aria-controls"] = MenuContext.ItemsId;

            if (isOpen && MenuContext.ActiveIndex >= 0)
                attrs["aria-activedescendant"] = MenuContext.GetItemId(MenuContext.ActiveIndex);
        }

        if (isDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => MenuContext?.ToggleAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // While open, forward all nav keys to the menu handler.
        if (MenuContext?.IsOpen == true)
            return MenuContext.HandleMenuKeyDownAsync(args);

        return MenuContext?.HandleButtonKeyDownAsync(args) ?? Task.CompletedTask;
    }
}
