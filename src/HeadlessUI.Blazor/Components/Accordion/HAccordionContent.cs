using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// The collapsible content panel for an <see cref="HAccordionItem"/>. Rendered
/// always but hidden via the HTML <c>hidden</c> attribute when the item is closed,
/// giving consumers full control over open/close transitions via CSS.
///
/// <para><b>Key behaviours:</b></para>
/// <list type="bullet">
///   <item>Sets <c>id</c> to the panel ID so the trigger's <c>aria-controls</c> can reference it.</item>
///   <item>Sets <c>role="region"</c> and <c>aria-labelledby</c> pointing to the trigger.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> for CSS-driven animations.</item>
///   <item>
///     Sets the HTML <c>hidden</c> attribute when closed (accessible hiding).
///     To animate, override with CSS: <c>[data-state="closed"] { display: block; … }</c>.
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HAccordionContent class="accordion-panel"&gt;
///     This content is hidden when the item is closed.
/// &lt;/HAccordionContent&gt;
/// </code>
/// </summary>
public class HAccordionContent : HeadlessComponentBase
{
    [CascadingParameter]
    private AccordionItemContext ItemContext { get; set; } = default!;

    /// <summary>The content to display inside the panel when the item is open.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => ItemContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ItemContext?.ContentId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        // Hidden attribute provides accessible hiding and is the default closed behaviour.
        // Consumers who want CSS animations should override display with:
        //   [data-state="closed"] { display: block; overflow: hidden; height: 0; }
        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        builder.AddContent(50, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);

        attrs["role"] = "region";

        if (ItemContext?.TriggerId is not null)
            attrs["aria-labelledby"] = ItemContext.TriggerId;

        return attrs;
    }
}
