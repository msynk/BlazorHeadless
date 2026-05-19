using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// The interactive trigger button for an <see cref="BhDisclosure"/>.
/// Clicking it toggles the associated <see cref="BhDisclosurePanel"/>.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Renders as a native <c>&lt;button type="button"&gt;</c> by default.</item>
///   <item>Sets <c>aria-expanded</c> and <c>aria-controls</c> automatically.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> and <c>data-disabled</c> for CSS hooks.</item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> receives a
///     <see cref="BhDisclosureRenderContext"/> for state-driven rendering
///     (e.g. a rotating chevron).
///   </item>
/// </list>
/// </summary>
public class BhDisclosureButton : BhComponentBase
{
    [CascadingParameter]
    private BhDisclosureContext BhDisclosureContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="BhDisclosureRenderContext"/> for state-driven rendering.
    /// Plain content works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<BhDisclosureRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => BhDisclosureContext?.IsOpen ?? false;
    private bool IsDisabled => BhDisclosureContext?.Disabled ?? false;

    private BhDisclosureRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = BhDisclosureContext?.Close ?? (() => { }),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhDisclosureContext?.ButtonId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["type"] = "button";
        attrs["aria-expanded"] = IsOpen ? "true" : "false";

        if (BhDisclosureContext?.PanelId is not null)
            attrs["aria-controls"] = BhDisclosureContext.PanelId;

        if (IsDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);

        return attrs;
    }

    private void HandleClick(MouseEventArgs _)
    {
        if (IsDisabled) return;
        BhDisclosureContext?.Toggle();
    }
}
