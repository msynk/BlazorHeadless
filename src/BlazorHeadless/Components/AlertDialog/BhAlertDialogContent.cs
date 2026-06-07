using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The alert dialog's surface — the actual <c>role="alertdialog"</c> element that
/// holds the title, description, and the Cancel/Action buttons. Receives focus on
/// open (the <see cref="BhAlertDialogCancel"/> button when present) and is the
/// focus-trap boundary.
///
/// <para>
/// Only rendered while the alert dialog is open. Captures its own
/// <see cref="ElementReference"/> and registers it with the parent
/// <see cref="BhAlertDialog"/> so the JS interop can lock focus to it.
/// </para>
///
/// <para>
/// Unlike <see cref="BhDialog"/>, an alert dialog is <b>not</b> dismissed by
/// clicking the overlay — the user must explicitly choose Cancel or Action.
/// </para>
/// </summary>
public class BhAlertDialogContent : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext BhAlertDialogContext { get; set; } = default!;

    /// <summary>
    /// When true (the default), Escape closes the alert dialog. Set to <c>false</c>
    /// to require explicit dismissal via Cancel or Action.
    /// </summary>
    [Parameter]
    public bool DismissOnEscape { get; set; } = true;

    /// <summary>Child content. Title, description, Cancel/Action, and any other markup go here.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => BhAlertDialogContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsOpen) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhAlertDialogContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (DismissOnEscape)
        {
            builder.AddAttribute(30, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        }

        builder.AddElementReferenceCapture(40, e =>
        {
            BhAlertDialogContext?.RegisterContent(e);
            Ref?.Invoke(e);
        });

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "alertdialog";
        attrs["aria-modal"] = "true";
        attrs["aria-labelledby"] = BhAlertDialogContext?.TitleId ?? string.Empty;
        attrs["aria-describedby"] = BhAlertDialogContext?.DescriptionId ?? string.Empty;

        // tabindex=-1 so the content itself is programmatically focusable when no
        // descendants are focusable, but isn't part of the tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, IsOpen);

        // Default positioning — centred in the viewport, above the overlay.
        // Consumers override entirely via class=/style=.
        if (!attrs.ContainsKey("style"))
            attrs["style"] = "position:fixed;left:50%;top:50%;transform:translate(-50%,-50%);z-index:51;";

        return attrs;
    }

    private async Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape" && BhAlertDialogContext is not null)
            await BhAlertDialogContext.CloseAsync();
    }
}
