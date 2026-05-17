using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The interactive trigger button for an <see cref="HDisclosure"/>.
/// Clicking it toggles the associated <see cref="HDisclosurePanel"/>.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Renders as a native <c>&lt;button type="button"&gt;</c> by default.</item>
///   <item>Sets <c>aria-expanded</c> and <c>aria-controls</c> automatically.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> and <c>data-disabled</c> for CSS hooks.</item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> receives a
///     <see cref="DisclosureRenderContext"/> for state-driven rendering
///     (e.g. a rotating chevron).
///   </item>
/// </list>
/// </summary>
public class HDisclosureButton : HeadlessComponentBase
{
    [CascadingParameter]
    private DisclosureContext DisclosureContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="DisclosureRenderContext"/> for state-driven rendering.
    /// Plain content works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<DisclosureRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsOpen => DisclosureContext?.IsOpen ?? false;
    private bool IsDisabled => DisclosureContext?.Disabled ?? false;

    private DisclosureRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = DisclosureContext?.Close ?? (() => { }),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DisclosureContext?.ButtonId ?? ComponentId);
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

        if (DisclosureContext?.PanelId is not null)
            attrs["aria-controls"] = DisclosureContext.PanelId;

        if (IsDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", IsDisabled);

        return attrs;
    }

    private void HandleClick(MouseEventArgs _)
    {
        if (IsDisabled) return;
        DisclosureContext?.Toggle();
    }
}
