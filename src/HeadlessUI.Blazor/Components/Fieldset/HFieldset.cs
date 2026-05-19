using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless fieldset component that groups a set of form controls together.
/// Renders as a native <c>&lt;fieldset&gt;</c> by default, providing built-in
/// browser semantics for grouping and disabling form controls.
///
/// <para>
/// Use with <see cref="HLegend"/> to provide a title for the group.
/// The <see cref="Disabled"/> prop cascades to all nested form controls
/// via both the native <c>disabled</c> attribute and a <see cref="FieldsetContext"/>.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HFieldset&gt;
///     &lt;HLegend&gt;Shipping details&lt;/HLegend&gt;
///     &lt;HField&gt;
///         &lt;HLabel&gt;Street address&lt;/HLabel&gt;
///         &lt;HInput name="address" /&gt;
///     &lt;/HField&gt;
/// &lt;/HFieldset&gt;
/// </code>
/// </summary>
public class HFieldset : HeadlessComponentBase
{
    /// <summary>Disables all form controls inside this fieldset.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Child content. Should contain an <see cref="HLegend"/> and one or more <see cref="HField"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "fieldset";

    private string LegendId => $"{ComponentId}-legend";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new FieldsetContext(LegendId, Disabled);

        builder.OpenComponent<CascadingValue<FieldsetContext>>(0);
        builder.AddComponentParameter(1, "Value", context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(5, "id", ComponentId);
            inner.AddAttribute(6, "aria-labelledby", LegendId);

            if (Disabled)
                inner.AddAttribute(7, "disabled", true);

            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }
}
