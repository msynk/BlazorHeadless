using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// The dialog's title. Renders as <c>&lt;h2&gt;</c> by default and is referenced
/// by the panel's <c>aria-labelledby</c> via the deterministic id from <see cref="DialogContext"/>.
/// </summary>
public class HDialogTitle : HeadlessComponentBase
{
    [CascadingParameter]
    private DialogContext DialogContext { get; set; } = default!;

    /// <summary>Title text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "h2";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DialogContext?.TitleId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }
}
