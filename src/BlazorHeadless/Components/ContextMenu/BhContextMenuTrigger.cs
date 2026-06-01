using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The area that opens the context menu. Wrap it around the target you want the
/// context menu to open from when right-clicking. Renders as a <c>&lt;span&gt;</c>
/// by default.
///
/// <para>
/// Handles the <c>contextmenu</c> event, suppresses the native browser menu, and
/// opens the parent <see cref="BhContextMenu"/> at the pointer coordinates.
/// </para>
/// </summary>
public class BhContextMenuTrigger : BhComponentBase
{
    [CascadingParameter]
    private BhContextMenuContext BhContextMenuContext { get; set; } = default!;

    /// <summary>
    /// Disables this trigger. A disabled trigger does not open the menu and the
    /// native browser context menu is allowed through.
    /// </summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Content template receiving <see cref="BhContextMenuTriggerRenderContext"/>
    /// for state-driven rendering.
    /// </summary>
    [Parameter]
    public RenderFragment<BhContextMenuTriggerRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    private bool IsDisabled => Disabled || (BhContextMenuContext?.Disabled ?? false);

    private BhContextMenuTriggerRenderContext RenderContext => new()
    {
        IsOpen = BhContextMenuContext?.IsOpen ?? false,
        Disabled = IsDisabled,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhContextMenuContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "oncontextmenu",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleContextMenu));
        // Suppress the native browser menu unless this trigger is disabled.
        builder.AddEventPreventDefaultAttribute(31, "oncontextmenu", !IsDisabled);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        var isOpen = BhContextMenuContext?.IsOpen ?? false;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);

        return attrs;
    }

    private Task HandleContextMenu(MouseEventArgs args)
    {
        if (IsDisabled || BhContextMenuContext is null)
            return Task.CompletedTask;

        return BhContextMenuContext.OpenAtAsync(args.ClientX, args.ClientY);
    }
}
