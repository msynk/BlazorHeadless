using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A visual separator between groups of items inside a
/// <see cref="BhContextMenuContent"/>. Renders as <c>&lt;li role="separator"&gt;</c>
/// by default so it nests correctly inside the <c>&lt;ul&gt;</c> content panel.
///
/// <para>
/// It is purely decorative for screen readers (announced as a separator) and is
/// never focusable or matched by typeahead.
/// </para>
/// </summary>
public class BhContextMenuSeparator : BhComponentBase
{
    protected override string DefaultTag => "li";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "separator";
        attrs["aria-orientation"] = "horizontal";
        return attrs;
    }
}
