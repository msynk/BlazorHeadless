using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The button that toggles a <see cref="BhDisclosure"/>. Renders a native
/// <c>&lt;button type="button"&gt;</c> with <c>aria-expanded</c> and <c>aria-controls</c>,
/// and <c>data-state="open|closed"</c>.
/// </summary>
public class BhDisclosureButton : BhComponentBase
{
    [CascadingParameter]
    private BhDisclosureContext? DisclosureContext { get; set; }

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

    protected override string DefaultTag => "button";

    private bool IsOpen => DisclosureContext?.IsOpen ?? false;
    private bool IsDisabled => DisclosureContext?.Disabled ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DisclosureContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        if (!Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            builder.AddAttribute(31, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
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
        BhToggleAttributes.ApplyTrigger(attrs, Tag, IsOpen, IsDisabled, DisclosureContext?.PanelId);
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsDisabled ? Task.CompletedTask : DisclosureContext?.ToggleAsync() ?? Task.CompletedTask;

    private Task HandleKeyDown(KeyboardEventArgs e)
        => e.Key is "Enter" or " " ? HandleClick(new MouseEventArgs()) : Task.CompletedTask;
}
