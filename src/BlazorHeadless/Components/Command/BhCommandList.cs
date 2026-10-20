using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The scrollable result list of a <see cref="BhCommand"/> (<c>role="listbox"</c>).
/// Exposes <c>--bh-command-list-height</c> — the height of its content — so the
/// palette can animate its height as results change.
/// </summary>
public class BhCommandList : BhComponentBase
{
    [CascadingParameter]
    private BhCommandContext? CommandContext { get; set; }

    /// <summary>Groups, items, separators, the empty state.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", CommandContext?.ListId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "listbox";
        if (CommandContext is not null && GetAdditionalAttribute("aria-label") is null)
            attrs["aria-labelledby"] = CommandContext.LabelId;
        attrs["data-bh-command-list"] = "";
        return attrs;
    }
}
