using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The button that closes the <see cref="BhAlertDialog"/> without taking the
/// destructive action — the safe choice. Receives focus by default when the
/// alert dialog opens, in line with the WAI-ARIA <c>alertdialog</c> pattern.
///
/// <para>
/// Registers its <see cref="ElementReference"/> with the parent so the JS interop
/// focuses it on open. Renders as a native <c>&lt;button&gt;</c> by default.
/// </para>
/// </summary>
public class BhAlertDialogCancel : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext BhAlertDialogContext { get; set; } = default!;

    /// <summary>
    /// Optional callback invoked when the cancel button is clicked, before the
    /// alert dialog closes.
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

        builder.AddElementReferenceCapture(40, e =>
        {
            BhAlertDialogContext?.RegisterCancel(e);
            Ref?.Invoke(e);
        });

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
