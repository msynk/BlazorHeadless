using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless legend component that serves as the title for a <see cref="HFieldset"/>.
/// Renders as a <c>&lt;div&gt;</c> by default (since the native <c>&lt;legend&gt;</c>
/// element is notoriously difficult to style consistently across browsers).
///
/// <para>
/// The component automatically picks up the <see cref="FieldsetContext"/> to
/// wire its id for <c>aria-labelledby</c> on the parent fieldset and to
/// reflect the disabled state via <c>data-disabled</c>.
/// </para>
/// </summary>
public class HLegend : HeadlessComponentBase
{
    [CascadingParameter]
    private FieldsetContext? FieldsetContext { get; set; }

    /// <summary>Legend text or rich content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", FieldsetContext?.LegendId ?? ComponentId);
        builder.AddAttribute(11, "role", "legend");
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (Ref is not null)
            builder.AddElementReferenceCapture(30, Ref);

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", FieldsetContext?.Disabled ?? false);
        return attrs;
    }
}
