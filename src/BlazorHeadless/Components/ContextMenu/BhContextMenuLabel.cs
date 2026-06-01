using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A non-interactive label used to title a section of a
/// <see cref="BhContextMenuContent"/>. Renders as <c>&lt;li&gt;</c> by default and
/// is not focusable via arrow keys nor matched by typeahead.
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhContextMenuContent&gt;
///     &lt;BhContextMenuLabel&gt;People&lt;/BhContextMenuLabel&gt;
///     &lt;BhContextMenuItem OnClick="..."&gt;Pedro&lt;/BhContextMenuItem&gt;
/// &lt;/BhContextMenuContent&gt;
/// </code>
/// </summary>
public class BhContextMenuLabel : BhComponentBase
{
    /// <summary>Content rendered inside the label.</summary>
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
        attrs["role"] = "presentation";
        return attrs;
    }
}
