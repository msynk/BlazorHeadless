using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>A button that dismisses its <see cref="BhToast"/>. Give it an <c>aria-label</c> when it only shows an icon.</summary>
public class BhToastClose : BhComponentBase
{
    [CascadingParameter]
    private BhToast? Toast { get; set; }

    /// <summary>Button content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddAttribute(20, "onclick", EventCallback.Factory.Create<MouseEventArgs>(this, () => Toast?.DismissAsync() ?? Task.CompletedTask));
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Tag.Equals("button", StringComparison.OrdinalIgnoreCase)) attrs["type"] = "button";
        return attrs;
    }
}
