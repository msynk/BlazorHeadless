using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless form field wrapper that generates deterministic ids and cascades
/// them to its children (<see cref="BhLabel"/>, <see cref="BhInput"/>,
/// <see cref="BhTextarea"/>, <see cref="BhSelect"/>, <see cref="BhDescription"/>).
///
/// <para>
/// Eliminates manual id management — the label's <c>for</c>, the input's <c>id</c>,
/// and the description's <c>aria-describedby</c> are all wired automatically.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhField&gt;
///     &lt;BhLabel&gt;Email&lt;/BhLabel&gt;
///     &lt;BhInput type="email" placeholder="you@example.com" /&gt;
///     &lt;BhDescription&gt;We'll never share your email.&lt;/BhDescription&gt;
/// &lt;/BhField&gt;
/// </code>
/// </summary>
public class BhField : BhComponentBase
{
    [CascadingParameter]
    private BhFieldsetContext? BhFieldsetContext { get; set; }

    /// <summary>Disables all form controls inside this field.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the field as invalid. Cascades <c>aria-invalid</c> and <c>data-invalid</c> to the input.</summary>
    [Parameter]
    public bool Invalid { get; set; }

    /// <summary>Child content. Should contain an <see cref="BhLabel"/>, an input component, and optionally an <see cref="BhDescription"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    /// <summary>
    /// Resolved disabled state: true if explicitly disabled or inherited from a parent <see cref="BhFieldset"/>.
    /// </summary>
    private bool IsDisabled => Disabled || (BhFieldsetContext?.Disabled ?? false);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhFieldContext>>(0);
        builder.AddComponentParameter(1, "Value", new BhFieldContext(ComponentId, IsDisabled, Invalid));
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
