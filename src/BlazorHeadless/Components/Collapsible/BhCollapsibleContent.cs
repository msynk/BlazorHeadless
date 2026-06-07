using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The collapsible content panel for a <see cref="BhCollapsible"/>. Always rendered
/// but hidden via the HTML <c>hidden</c> attribute when the collapsible is closed
/// (matching the Radix UI <c>forceMount</c> behaviour: content stays mounted so
/// consumers retain full control over open/close transitions via CSS).
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Sets its <c>id</c> to match <c>aria-controls</c> on the trigger.</item>
///   <item>Sets <c>aria-labelledby</c> pointing at the trigger.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> for CSS-driven transitions.</item>
///   <item>
///     Receives the <see cref="BhCollapsibleRenderContext"/> so consumers can render
///     a "Close" link or any state-driven UI from inside the content.
///   </item>
/// </list>
///
/// <para>
/// To animate height transitions, override the default <c>hidden</c> behaviour
/// with CSS like
/// <c>[data-state="closed"] { display: block; height: 0; overflow: hidden; }</c>.
/// </para>
/// </summary>
public class BhCollapsibleContent : BhComponentBase
{
    [CascadingParameter]
    private BhCollapsibleContext BhCollapsibleContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="BhCollapsibleRenderContext"/>.
    /// Plain content works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<BhCollapsibleRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => BhCollapsibleContext?.IsOpen ?? false;

    private BhCollapsibleRenderContext RenderContext => new()
    {
        IsOpen = IsOpen,
        Close = BhCollapsibleContext?.Close ?? (() => { }),
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", BhCollapsibleContext?.ContentId ?? ComponentId);
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

        if (BhCollapsibleContext?.Disabled == true)
            SetDataFlag(attrs, "disabled", true);

        if (BhCollapsibleContext?.TriggerId is not null)
            attrs["aria-labelledby"] = BhCollapsibleContext.TriggerId;

        return attrs;
    }
}
