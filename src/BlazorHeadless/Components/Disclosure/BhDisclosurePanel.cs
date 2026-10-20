using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The panel of a <see cref="BhDisclosure"/>. Hidden while closed (visible during
/// exit animations) and exposes <c>--bh-content-height</c> / <c>--bh-content-width</c>
/// for size animations. Receives <see cref="BhDisclosureRenderContext"/> so it can
/// render a "Close" link.
/// </summary>
public class BhDisclosurePanel : BhComponentBase
{
    [CascadingParameter]
    private BhDisclosureContext? DisclosureContext { get; set; }

    /// <summary>
    /// Use <c>hidden="until-found"</c> while closed so the browser's find-in-page can
    /// search the content and open the disclosure when it finds a match.
    /// </summary>
    [Parameter]
    public bool HiddenUntilFound { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhDisclosureRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhDisclosureRenderContext>? Template { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => DisclosureContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DisclosureContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, new BhDisclosureRenderContext
            {
                IsOpen = IsOpen,
                Close = DisclosureContext?.Close ?? (() => { }),
            }));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        BhToggleAttributes.ApplyPanel(attrs, IsOpen, HiddenUntilFound, DisclosureContext?.ButtonId);
        return attrs;
    }
}
