using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Renders its content only while the enclosing <see cref="BhCheckbox"/> is checked
/// or indeterminate — the check mark / dash. Hidden from assistive tech.
/// Emits <c>data-state="checked|unchecked|indeterminate"</c>.
/// </summary>
public class BhCheckboxIndicator : BhComponentBase
{
    [CascadingParameter]
    private BhCheckboxRenderContext? CheckboxContext { get; set; }

    /// <summary>Always render (useful for CSS animations); content still reflects the state through <c>data-state</c>.</summary>
    [Parameter]
    public bool ForceMount { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhCheckboxRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhCheckboxRenderContext>? Template { get; set; }

    protected override string DefaultTag => "span";

    private bool IsVisible => CheckboxContext is { IsChecked: true } or { IsIndeterminate: true };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!IsVisible && !ForceMount) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        if (CheckboxContext is not null)
            builder.AddContent(30, BhContent.Pick(Template, ChildContent, CheckboxContext));
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["aria-hidden"] = "true";
        attrs["data-state"] = CheckboxContext switch
        {
            { IsIndeterminate: true } => "indeterminate",
            { IsChecked: true } => "checked",
            _ => "unchecked",
        };
        return attrs;
    }
}
