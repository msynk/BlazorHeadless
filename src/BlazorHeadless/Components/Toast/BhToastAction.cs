using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The action button of a <see cref="BhToast"/> (e.g. "Undo"); runs the toast's
/// <see cref="BhToastOptions.OnAction"/> and dismisses it. Because toasts disappear,
/// make sure the action is also reachable elsewhere in your UI.
/// </summary>
public class BhToastAction : BhComponentBase
{
    [CascadingParameter]
    private BhToast? Toast { get; set; }

    /// <summary>Button content (defaults to the toast's <see cref="BhToastOptions.ActionLabel"/>).</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => Toast?.RunActionAsync() ?? Task.CompletedTask));
        if (ChildContent is not null) builder.AddContent(30, ChildContent);
        else builder.AddContent(31, Toast?.Item.Options.ActionLabel);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        return attrs;
    }
}
