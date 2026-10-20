using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The trigger button for an <see cref="BhPopover"/>. Clicking it toggles the panel.
///
/// <para>
/// Can also be placed <em>inside</em> the <see cref="BhPopoverPanel"/> to act as a
/// "Close" button — it will close the popover when clicked from there too.
/// </para>
/// </summary>
public class BhPopoverButton : BhComponentBase
{
    [CascadingParameter]
    private BhPopoverContext? PopoverContext { get; set; }

    [CascadingParameter]
    private BhFloatingScope? Scope { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhPopoverRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhPopoverRenderContext>? Template { get; set; }

    /// <summary>Disables the button.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => PopoverContext?.IsOpen ?? false;

    // The first button (outside the panel) is the trigger and owns the
    // deterministic id; a button rendered inside the panel is a close button.
    private bool IsInsidePanel => Scope is not null && PopoverContext is not null && _insidePanel;
    private bool _insidePanel;

    private BhPopoverRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = () => _ = PopoverContext?.CloseAsync(),
    };

    protected override void OnInitialized()
    {
        // A button rendered inside the panel receives the panel's floating scope.
        _insidePanel = Scope is BhFloatingScope s && s.Owner == PopoverContext?.PanelId;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", IsInsidePanel || PopoverContext is null ? ComponentId : PopoverContext.ButtonId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddElementReferenceCapture(40, CaptureElement);
        if (Template is not null || ChildContent is not null)
            builder.AddContent(50, BhContent.Pick(Template, ChildContent, RenderContext));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isButton = Tag.Equals("button", StringComparison.OrdinalIgnoreCase);
        if (isButton)
            attrs["type"] = "button";
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = Disabled ? -1 : 0;
        }

        if (!IsInsidePanel)
        {
            SetAriaBool(attrs, "aria-expanded", IsOpen);
            if (IsOpen && PopoverContext is not null)
                attrs["aria-controls"] = PopoverContext.PanelId;
        }

        if (Disabled)
        {
            if (isButton) attrs["disabled"] = true;
            else attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
    {
        if (Disabled || PopoverContext is null) return Task.CompletedTask;
        return IsInsidePanel ? PopoverContext.CloseAsync() : PopoverContext.ToggleAsync();
    }
}
