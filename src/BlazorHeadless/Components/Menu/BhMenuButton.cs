using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The trigger button that opens the menu. Renders as
/// <c>&lt;button&gt;</c> with <c>aria-haspopup="menu"</c>,
/// <c>aria-expanded</c>, <c>aria-controls</c>, and <c>aria-activedescendant</c>
/// (while the menu is open).
///
/// <para>
/// Focus stays on this button while the menu is open; keyboard navigation
/// is handled here and forwarded to the parent <see cref="BhMenu"/>.
/// </para>
/// </summary>
public class BhMenuButton : BhComponentBase
{
    [CascadingParameter]
    private BhMenuContext BhMenuContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhMenuButtonRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhMenuButtonRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private BhMenuButtonRenderContext RenderContext => new()
    {
        IsOpen = BhMenuContext?.IsOpen ?? false,
        Disabled = BhMenuContext?.Disabled ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhMenuContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            BhMenuContext?.RegisterButton(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = BhMenuContext?.IsOpen ?? false;
        var isDisabled = BhMenuContext?.Disabled ?? false;

        attrs["type"] = "button";
        attrs["aria-haspopup"] = "menu";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (BhMenuContext is not null)
        {
            attrs["aria-controls"] = BhMenuContext.ItemsId;

            if (isOpen && BhMenuContext.ActiveIndex >= 0)
                attrs["aria-activedescendant"] = BhMenuContext.GetItemId(BhMenuContext.ActiveIndex);
        }

        if (isDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => BhMenuContext?.ToggleAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // While open, forward all nav keys to the menu handler.
        if (BhMenuContext?.IsOpen == true)
            return BhMenuContext.HandleMenuKeyDownAsync(args);

        return BhMenuContext?.HandleButtonKeyDownAsync(args) ?? Task.CompletedTask;
    }
}
