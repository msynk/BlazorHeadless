using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// An in-place ARIA live region (<c>role="status"</c> / <c>role="alert"</c>) whose
/// content changes are announced by screen readers — form status, search result
/// counts, save confirmations. Rendered from the first render (live regions must
/// exist before their content changes). Set <see cref="VisuallyHidden"/> to
/// announce without showing.
///
/// <para>For fire-and-forget announcements from code, inject <see cref="IBhAnnouncer"/> instead.</para>
///
/// <code>
/// &lt;BhLiveRegion&gt;@resultCount results&lt;/BhLiveRegion&gt;
/// </code>
/// </summary>
public class BhLiveRegion : BhComponentBase
{
    /// <summary>Polite (default, <c>role="status"</c>) or assertive (<c>role="alert"</c>).</summary>
    [Parameter]
    public BhPoliteness Politeness { get; set; } = BhPoliteness.Polite;

    /// <summary>Announce the whole region on change (default) instead of only the changed nodes.</summary>
    [Parameter]
    public bool Atomic { get; set; } = true;

    /// <summary>Hide the region visually.</summary>
    [Parameter]
    public bool VisuallyHidden { get; set; }

    /// <summary>The announced content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var assertive = Politeness == BhPoliteness.Assertive;
        attrs["role"] = assertive ? "alert" : "status";
        attrs["aria-live"] = assertive ? "assertive" : "polite";
        attrs["aria-atomic"] = Atomic ? "true" : "false";
        if (VisuallyHidden) attrs["style"] = BhVisuallyHidden.Style;
        return attrs;
    }
}
