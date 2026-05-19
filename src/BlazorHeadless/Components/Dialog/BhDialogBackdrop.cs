using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The optional dim overlay rendered behind the dialog panel. Clicking the
/// backdrop dismisses the dialog by calling <see cref="BhDialog.OnClose"/>.
///
/// <para>
/// Set <see cref="DismissOnClick"/> to <c>false</c> to disable dismiss-on-click
/// (useful for confirmation flows where the user must explicitly choose).
/// </para>
/// </summary>
public class BhDialogBackdrop : BhComponentBase
{
    [CascadingParameter]
    private BhDialogContext BhDialogContext { get; set; } = default!;

    /// <summary>Whether clicking the backdrop should close the dialog. Defaults to <c>true</c>.</summary>
    [Parameter]
    public bool DismissOnClick { get; set; } = true;

    /// <summary>Optional content to render inside the backdrop (rare; typically empty).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (DismissOnClick)
        {
            builder.AddAttribute(30, "onclick",
                EventCallback.Factory.Create<MouseEventArgs>(this, async _ =>
                {
                    if (BhDialogContext is not null)
                        await BhDialogContext.CloseAsync();
                }));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, BhDialogContext?.IsOpen ?? false);

        // Default styles position the backdrop behind the panel inside the
        // dialog's positioning container. Consumers can override via class=/style=.
        if (!attrs.ContainsKey("style"))
            attrs["style"] = "position:absolute;inset:0;z-index:0;";

        // Marked aria-hidden so AT skips it.
        attrs["aria-hidden"] = "true";

        return attrs;
    }
}
