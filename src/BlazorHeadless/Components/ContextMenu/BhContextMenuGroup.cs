using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Groups multiple <see cref="BhContextMenuItem"/>s together inside a
/// <see cref="BhContextMenuContent"/>. Renders as <c>&lt;li role="group"&gt;</c> by
/// default, wrapping its children so they are announced as a related set.
///
/// <para>
/// Pair with a <see cref="BhContextMenuLabel"/> (via <c>aria-labelledby</c> on the
/// group, supplied through <c>AdditionalAttributes</c>) to give the group an
/// accessible name.
/// </para>
/// </summary>
public class BhContextMenuGroup : BhComponentBase
{
    /// <summary>
    /// Child content. Should contain one or more <see cref="BhContextMenuItem"/>
    /// components and, optionally, a <see cref="BhContextMenuLabel"/>.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "group";
        return attrs;
    }
}
