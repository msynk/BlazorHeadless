using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The dialog's surface — the actual <c>role="dialog"</c> element that holds the
/// title, description, and content. Receives focus on open and is the focus-trap
/// boundary.
///
/// <para>
/// Captures its own <see cref="ElementReference"/> and registers it with the
/// parent <see cref="BhDialog"/> so the JS interop can lock focus to it.
/// </para>
/// </summary>
public class BhDialogPanel : BhComponentBase
{
    [CascadingParameter]
    private BhDialogContext BhDialogContext { get; set; } = default!;

    /// <summary>
    /// Optional element to receive focus when the dialog opens. When omitted the
    /// first focusable element inside the panel is focused. Reserved for a future
    /// pass — currently the JS focuses the first focusable element automatically.
    /// </summary>
    [Parameter]
    public ElementReference? InitialFocus { get; set; }

    /// <summary>
    /// When true (the default), Escape closes the dialog. Set to <c>false</c>
    /// to require explicit dismissal.
    /// </summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Child content. Title, description, and any other dialog markup go here.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ElementReference _elementRef;

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhDialogContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (DismissOnEscape)
        {
            builder.AddAttribute(30, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        }

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            BhDialogContext?.RegisterPanel(e);
            Ref?.Invoke(e);
        });

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "dialog";
        attrs["aria-modal"] = "true";
        attrs["aria-labelledby"] = BhDialogContext?.TitleId ?? string.Empty;
        attrs["aria-describedby"] = BhDialogContext?.DescriptionId ?? string.Empty;

        // tabindex=-1 so the panel itself is programmatically focusable when no
        // descendants are focusable, but isn't part of the tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, BhDialogContext?.IsOpen ?? false);

        // Default panel positioning — centred on screen, above the backdrop.
        // Consumers override via class=/style=.
        if (!attrs.ContainsKey("style"))
            attrs["style"] = "position:relative;z-index:1;";

        return attrs;
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && BhDialogContext is not null)
            await BhDialogContext.CloseAsync();
    }
}
