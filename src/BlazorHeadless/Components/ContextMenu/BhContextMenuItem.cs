using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// An action item inside a <see cref="BhContextMenuContent"/> (<c>role="menuitem"</c>).
/// Equivalent to <see cref="BhMenuItem"/> (which also works inside context menus),
/// with a <see cref="BhContextMenuItemRenderContext"/> render context.
/// </summary>
public class BhContextMenuItem : BhMenuItemBase
{
    /// <summary>The action to invoke when this item is activated (click, Enter or Space).</summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Whether activating the item closes the menu. Default: true.</summary>
    [Parameter]
    public bool CloseOnSelect { get; set; } = true;

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhContextMenuItemRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhContextMenuItemRenderContext>? Template { get; set; }

    internal override async Task SelectAsync(bool viaKeyboard)
    {
        if (viaKeyboard && IsLink)
        {
            await ClickElementAsync();
            return;
        }
        if (CloseOnSelect)
            await CloseMenuAsync();
        await OnClick.InvokeAsync();
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is not null || ChildContent is not null)
            builder.AddContent(sequence, BhContent.Pick(Template, ChildContent, new BhContextMenuItemRenderContext { IsActive = IsActive, Disabled = IsItemDisabled }));
    }
}

/// <summary>Groups related context-menu items (<c>role="group"</c>). See <see cref="BhMenuGroup"/>.</summary>
public class BhContextMenuGroup : BhMenuGroup
{
}

/// <summary>A non-interactive heading inside a context menu. See <see cref="BhMenuLabel"/>.</summary>
public class BhContextMenuLabel : BhMenuLabel
{
}

/// <summary>A divider between context-menu items. See <see cref="BhMenuSeparator"/>.</summary>
public class BhContextMenuSeparator : BhMenuSeparator
{
}
