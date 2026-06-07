using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The button that confirms the alert dialog's action — typically the
/// destructive or consequential choice (e.g. "Delete account"). Invokes
/// <see cref="OnClick"/> and then closes the <see cref="BhAlertDialog"/>.
///
/// <para>Renders as a native <c>&lt;button&gt;</c> by default.</para>
/// </summary>
public class BhAlertDialogAction : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext BhAlertDialogContext { get; set; } = default!;

    /// <summary>
    /// Callback invoked when the action button is clicked, before the alert
    /// dialog closes. Put your confirm/destructive logic here.
    /// </summary>
    [Parameter]
    public EventCallback OnClick { get; set; }

    /// <summary>Child content rendered inside the button.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase))
            attrs["type"] = "button";

        return attrs;
    }

    private async Task HandleClick(MouseEventArgs _)
    {
        if (OnClick.HasDelegate)
            await OnClick.InvokeAsync();

        if (BhAlertDialogContext is not null)
            await BhAlertDialogContext.CloseAsync();
    }
}
