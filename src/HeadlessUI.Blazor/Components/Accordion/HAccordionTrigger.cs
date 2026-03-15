using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The interactive trigger button for an <see cref="HAccordionItem"/>. Clicking it
/// toggles the associated <see cref="HAccordionContent"/> open or closed.
///
/// <para><b>Key behaviours:</b></para>
/// <list type="bullet">
///   <item>Renders as a native <c>&lt;button&gt;</c> with <c>type="button"</c>.</item>
///   <item>Sets <c>aria-expanded</c> and <c>aria-controls</c> pointing to the content panel.</item>
///   <item>Emits <c>data-state="open"|"closed"</c> and <c>data-disabled</c> for CSS hooks.</item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> receives an
///     <see cref="AccordionTriggerContext"/> so consumers can render a rotating chevron
///     or other state-driven indicator.
///   </item>
/// </list>
///
/// <para><b>Usage with context:</b></para>
/// <code>
/// &lt;HAccordionTrigger Context="t"&gt;
///     Section 1
///     &lt;span class="@(t.IsOpen ? "chevron-up" : "chevron-down")"&gt;▾&lt;/span&gt;
/// &lt;/HAccordionTrigger&gt;
/// </code>
/// </summary>
public class HAccordionTrigger : HeadlessComponentBase
{
    [CascadingParameter]
    private AccordionItemContext ItemContext { get; set; } = default!;

    /// <summary>
    /// Content template receiving <see cref="AccordionTriggerContext"/> for state-driven rendering.
    /// Plain content (without referencing context) works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<AccordionTriggerContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private AccordionTriggerContext TriggerContext => new()
    {
        IsOpen = ItemContext?.IsOpen ?? false,
        Disabled = ItemContext?.Disabled ?? false,
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ItemContext?.TriggerId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(TriggerContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var isOpen = ItemContext?.IsOpen ?? false;
        var isDisabled = ItemContext?.Disabled ?? false;

        attrs["type"] = "button";
        attrs["aria-expanded"] = isOpen ? "true" : "false";

        if (ItemContext?.ContentId is not null)
            attrs["aria-controls"] = ItemContext.ContentId;

        if (isDisabled)
            attrs["disabled"] = true;

        SetDataState(attrs, isOpen);
        SetDataFlag(attrs, "disabled", isDisabled);

        return attrs;
    }

    private Task HandleClickAsync(MouseEventArgs _)
    {
        if (ItemContext?.Disabled ?? false) return Task.CompletedTask;
        ItemContext?.Toggle();
        return Task.CompletedTask;
    }
}
