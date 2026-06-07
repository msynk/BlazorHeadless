using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The button that opens the <see cref="BhAlertDialog"/>. Renders as a native
/// <c>&lt;button&gt;</c> by default. Always present in the DOM regardless of open state.
///
/// <para>
/// On close, focus returns to this element automatically.
/// </para>
/// </summary>
public class BhAlertDialogTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhAlertDialogContext BhAlertDialogContext { get; set; } = default!;

    /// <summary>Content template receiving <see cref="BhAlertDialogRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<BhAlertDialogRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => BhAlertDialogContext?.IsOpen ?? false;

    private BhAlertDialogRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = BhAlertDialogContext?.CloseAsync(),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhAlertDialogContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        builder.AddElementReferenceCapture(40, e =>
        {
            BhAlertDialogContext?.RegisterTrigger(e);
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

        attrs["aria-haspopup"] = "dialog";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (BhAlertDialogContext is not null)
            attrs["aria-controls"] = BhAlertDialogContext.ContentId;

        SetDataState(attrs, isOpen);

        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => BhAlertDialogContext?.OpenAsync() ?? Task.CompletedTask;
}
