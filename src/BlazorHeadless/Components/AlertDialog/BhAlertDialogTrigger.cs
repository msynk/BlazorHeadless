using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The button that opens the <see cref="BhAlertDialog"/>. Renders as a native
/// <c>&lt;button&gt;</c> by default. On close, focus returns to this element.
/// </summary>
public class BhAlertDialogTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhAlertDialogRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhAlertDialogRenderContext>? Template { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => AlertDialogContext?.IsOpen ?? false;

    private BhAlertDialogRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = AlertDialogContext?.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, RenderContext));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";
        attrs["aria-haspopup"] = "dialog";
        SetAriaBool(attrs, "aria-expanded", IsOpen);
        if (AlertDialogContext is { IsPresent: true })
            attrs["aria-controls"] = AlertDialogContext.ContentId;
        SetDataState(attrs, IsOpen);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => AlertDialogContext?.OpenAsync() ?? Task.CompletedTask;
}
