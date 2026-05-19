using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The trigger button for an <see cref="HPopover"/>. Clicking it toggles the panel.
///
/// <para>
/// Can also be placed <em>inside</em> the <see cref="HPopoverPanel"/> to act as a
/// "Close" button — it will close the popover when clicked from there too.
/// </para>
/// </summary>
public class HPopoverButton : HeadlessComponentBase
{
    [CascadingParameter]
    private PopoverContext PopoverContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="PopoverRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<PopoverRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => PopoverContext?.IsOpen ?? false;

    private PopoverRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = PopoverContext?.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", PopoverContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        builder.AddElementReferenceCapture(40, e =>
        {
            PopoverContext?.RegisterButton(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = IsOpen;

        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";

        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (PopoverContext is not null)
            attrs["aria-controls"] = PopoverContext.PanelId;

        SetDataState(attrs, isOpen);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => PopoverContext?.ToggleAsync() ?? Task.CompletedTask;
}
