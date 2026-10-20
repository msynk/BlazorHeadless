using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// The rail of a <see cref="BhSlider"/>. Pointer positions are measured against it
/// (or the slider root when absent). Positioned <c>relative</c> by default so the
/// <see cref="BhSliderRange"/> can sit inside it.
/// </summary>
public class BhSliderTrack : BhComponentBase
{
    [CascadingParameter]
    private BhSlider? Slider { get; set; }

    /// <summary>Usually a <see cref="BhSliderRange"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    protected override void OnInitialized() => Slider?.RegisterTrack(ComponentId);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Slider is not null)
        {
            SetDataValue(attrs, "orientation", Slider.SliderOrientation.ToAttribute());
            SetDataFlag(attrs, "disabled", Slider.SliderDisabled);
        }
        attrs["style"] = "position:relative;flex-grow:1;";
        return attrs;
    }
}
