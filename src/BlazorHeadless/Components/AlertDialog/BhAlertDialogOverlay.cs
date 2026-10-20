using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The optional dim overlay rendered behind the alert dialog content while it is
/// open (and while its exit animation runs).
///
/// <para>
/// Unlike <see cref="BhDialogBackdrop"/>, clicking the overlay does <b>not</b>
/// dismiss the alert dialog — an alert dialog requires an explicit response via
/// <see cref="BhAlertDialogCancel"/> or <see cref="BhAlertDialogAction"/>.
/// </para>
/// </summary>
public class BhAlertDialogOverlay : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhAlertDialogContext? AlertDialogContext { get; set; }

    /// <summary>Optional content to render inside the overlay (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => AlertDialogContext?.Register(BhAlertDialogPart.Overlay, true);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (AlertDialogContext is { IsPresent: false }) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", AlertDialogContext?.OverlayId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (AlertDialogContext?.IsHidden == true)
            builder.AddAttribute(25, "hidden", true);
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, AlertDialogContext?.IsOpen ?? false);
        attrs["data-bh-presence"] = "";
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = "position:fixed;inset:0;z-index:50;";
        attrs["aria-hidden"] = "true";
        return attrs;
    }

    public void Dispose() => AlertDialogContext?.Register(BhAlertDialogPart.Overlay, false);
}
