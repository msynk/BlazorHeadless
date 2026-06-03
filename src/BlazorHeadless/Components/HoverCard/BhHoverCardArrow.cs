using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// An optional arrow rendered alongside <see cref="BhHoverCardContent"/> to
/// visually link the card with its trigger. Renders a small inline SVG triangle
/// by default, but the markup is fully replaceable via <see cref="ChildContent"/>
/// or by overriding <see cref="BhComponentBase.As"/>.
///
/// <para>
/// Place this component <em>inside</em> the <see cref="BhHoverCardContent"/> and
/// position it with CSS keyed off the resolved <c>data-anchor</c> side — for
/// example:
/// </para>
/// <code>
/// .hover-card-content[data-anchor^="bottom"] .hover-card-arrow {
///     position: absolute; top: -4px; left: 50%;
///     transform: translateX(-50%) rotate(180deg);
/// }
/// </code>
/// </summary>
public class BhHoverCardArrow : BhComponentBase
{
    [CascadingParameter]
    private BhHoverCardContext HoverCardContext { get; set; } = default!;

    /// <summary>Width of the default SVG arrow in pixels. Default: 10.</summary>
    [Parameter]
    public int Width { get; set; } = 10;

    /// <summary>Height of the default SVG arrow in pixels. Default: 5.</summary>
    [Parameter]
    public int Height { get; set; } = 5;

    /// <summary>
    /// Optional custom content. When provided, the default SVG triangle is
    /// replaced with this content (still wrapped in the polymorphic root tag).
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        if (ChildContent is not null)
        {
            builder.AddContent(40, ChildContent);
        }
        else
        {
            // Default: a tiny SVG triangle pointing down. Consumers replace via ChildContent.
            builder.OpenElement(50, "svg");
            builder.AddAttribute(51, "xmlns", "http://www.w3.org/2000/svg");
            builder.AddAttribute(52, "width", Width);
            builder.AddAttribute(53, "height", Height);
            builder.AddAttribute(54, "viewBox", $"0 0 {Width} {Height}");
            builder.AddAttribute(55, "preserveAspectRatio", "none");
            builder.AddAttribute(56, "aria-hidden", "true");
            builder.OpenElement(60, "polygon");
            builder.AddAttribute(61, "points", $"0,0 {Width},0 {Width / 2.0},{Height}");
            builder.CloseElement();
            builder.CloseElement();
        }

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        if (HoverCardContext is not null)
            SetDataState(attrs, HoverCardContext.IsOpen);
        return attrs;
    }
}
