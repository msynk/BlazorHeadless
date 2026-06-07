using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The optional dim overlay rendered behind the alert dialog content. Only
/// rendered while the alert dialog is open.
///
/// <para>
/// Unlike <see cref="BhDialogBackdrop"/>, clicking the overlay does <b>not</b>
/// dismiss the alert dialog — an alert dialog requires an explicit response via
/// <see cref="BhAlertDialogCancel"/> or <see cref="BhAlertDialogAction"/>.
/// </para>
/// </summary>
public class BhAlertDialogOverlay : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext BhAlertDialogContext { get; set; } = default!;

    /// <summary>Optional content to render inside the overlay (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => BhAlertDialogContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsOpen) return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);

        // Default styles cover the full viewport behind the content. Consumers
        // can override via class=/style=.
        if (!attrs.ContainsKey("style"))
            attrs["style"] = "position:fixed;inset:0;z-index:50;";

        // Marked aria-hidden so AT skips it.
        attrs["aria-hidden"] = "true";

        return attrs;
    }
}
