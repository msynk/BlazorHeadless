using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless form field wrapper that generates deterministic ids and cascades
/// them to its children (<see cref="HLabel"/>, <see cref="HInput"/>,
/// <see cref="HTextarea"/>, <see cref="HSelect"/>, <see cref="HDescription"/>).
///
/// <para>
/// Eliminates manual id management — the label's <c>for</c>, the input's <c>id</c>,
/// and the description's <c>aria-describedby</c> are all wired automatically.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HField&gt;
///     &lt;HLabel&gt;Email&lt;/HLabel&gt;
///     &lt;HInput type="email" placeholder="you@example.com" /&gt;
///     &lt;HDescription&gt;We'll never share your email.&lt;/HDescription&gt;
/// &lt;/HField&gt;
/// </code>
/// </summary>
public class HField : HeadlessComponentBase
{
    [CascadingParameter]
    private FieldsetContext? FieldsetContext { get; set; }

    /// <summary>Disables all form controls inside this field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the field as invalid. Cascades <c>aria-invalid</c> and <c>data-invalid</c> to the input.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Child content. Should contain an <see cref="HLabel"/>, an input component, and optionally an <see cref="HDescription"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>
    /// Resolved disabled state: true if explicitly disabled or inherited from a parent <see cref="HFieldset"/>.
    /// </summary>
    private bool IsDisabled => Disabled || (FieldsetContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<FieldContext>>(0);
        builder.AddComponentParameter(1, "Value", new FieldContext(ComponentId, IsDisabled, Invalid));
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
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
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "invalid", Invalid);
        return attrs;
    }
}
