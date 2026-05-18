using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The floating content panel for an <see cref="HPopover"/>. Hidden via the HTML
/// <c>hidden</c> attribute when the popover is closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Receives focus on open (first focusable element, or the panel itself).</item>
///   <item>Escape closes the popover from anywhere inside the panel.</item>
///   <item>Emits <c>data-state="open|closed"</c> for CSS-driven transitions.</item>
/// </list>
/// </summary>
public class HPopoverPanel : HeadlessComponentBase
{
    [CascadingParameter]
    private PopoverContext PopoverContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="PopoverRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<PopoverRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => PopoverContext?.IsOpen ?? false;

    private PopoverRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = PopoverContext?.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", PopoverContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        builder.AddAttribute(35, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            PopoverContext?.RegisterPanel(e);
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        // tabindex=-1 so the panel is programmatically focusable when it has no
        // focusable descendants, but isn't in the natural tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, IsOpen);

        return attrs;
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && PopoverContext is not null)
            await PopoverContext.CloseAsync();
    }
}
