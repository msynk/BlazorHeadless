using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// The popup panel containing the menu's items. Renders as
/// <c>&lt;ul role="menu"&gt;</c> by default. Hidden via the HTML <c>hidden</c>
/// attribute when the menu is closed.
/// </summary>
public class HMenuItems : HeadlessComponentBase
{
    [CascadingParameter]
    private MenuContext MenuContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="HMenuItem"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "ul";

    private bool IsOpen => MenuContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", MenuContext?.ItemsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "menu";
        attrs["tabindex"] = -1;
        SetDataState(attrs, IsOpen);
        return attrs;
    }
}
