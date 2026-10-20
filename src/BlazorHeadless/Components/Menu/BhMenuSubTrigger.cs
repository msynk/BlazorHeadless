using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The item of the parent menu that opens a <see cref="BhMenuSub"/>. Exposes
/// <c>aria-haspopup="menu"</c>, <c>aria-expanded</c> and <c>data-state="open|closed"</c>.
/// </summary>
public class BhMenuSubTrigger : BhMenuItemBase
{
    [CascadingParameter]
    private BhMenuSub? Sub { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhMenuItemRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhMenuItemRenderContext>? Template { get; set; }

    internal override bool IsSubTrigger => true;

    private bool IsSubOpen => Sub?.IsOpen ?? false;

    protected override void OnInitialized()
    {
        if (Sub is not null) Sub.TriggerItem = this;
        base.OnInitialized();
    }

    internal override Task SelectAsync(bool viaKeyboard)
    {
        if (Sub is null) return Task.CompletedTask;
        if (!viaKeyboard && IsSubOpen) return Task.CompletedTask;
        return Sub.OpenAsync(viaKeyboard ? BhMenuFocus.First : BhMenuFocus.Container);
    }

    protected override Task OnPointerEnterAsync(PointerEventArgs args)
    {
        if (!IsItemDisabled && args.PointerType != "touch")
            Sub?.ScheduleOpen();
        return Task.CompletedTask;
    }

    protected override Task OnPointerLeaveAsync(PointerEventArgs args)
    {
        Sub?.CancelOpen();
        if (IsSubOpen) Sub?.ScheduleClose();
        return Task.CompletedTask;
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-haspopup"] = "menu";
        SetAriaBool(attrs, "aria-expanded", IsSubOpen);
        if (IsSubOpen && Sub is not null)
            attrs["aria-controls"] = Sub.ContentId;
        SetDataState(attrs, IsSubOpen);
        return attrs;
    }

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is not null || ChildContent is not null)
            builder.AddContent(sequence, BhContent.Pick(Template, ChildContent, new BhMenuItemRenderContext
            {
                IsActive = IsActive,
                Disabled = IsItemDisabled,
                IsOpen = IsSubOpen,
            }));
    }

    public override void Dispose()
    {
        if (Sub?.TriggerItem == this) Sub.TriggerItem = null;
        base.Dispose();
    }
}
