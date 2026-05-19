using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The collapsible content panel for an <see cref="HDisclosure"/>. Always rendered
/// but hidden via the HTML <c>hidden</c> attribute when the disclosure is closed.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Sets its <c>id</c> to match <c>aria-controls</c> on the button.</item>
///   <item>Sets <c>aria-labelledby</c> pointing at the button.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> for CSS-driven transitions.</item>
///   <item>
///     Receives the <see cref="DisclosureRenderContext"/> so consumers can render
///     a "Close" link or any state-driven UI from inside the panel.
///   </item>
/// </list>
///
/// <para>
/// To animate height transitions, override the default <c>hidden</c> behaviour
/// with CSS like
/// <c>[data-state="closed"] { display: block; height: 0; overflow: hidden; }</c>.
/// </para>
/// </summary>
public class HDisclosurePanel : HeadlessComponentBase
{
    [CascadingParameter]
    private DisclosureContext DisclosureContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="DisclosureRenderContext"/>.
    /// Plain content works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<DisclosureRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => DisclosureContext?.IsOpen ?? false;

    private DisclosureRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = DisclosureContext?.Close ?? (() => { }),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", DisclosureContext?.PanelId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);

        if (DisclosureContext?.ButtonId is not null)
            attrs["aria-labelledby"] = DisclosureContext.ButtonId;

        return attrs;
    }
}
