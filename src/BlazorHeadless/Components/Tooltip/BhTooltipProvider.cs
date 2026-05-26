using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Provides global timing configuration for all enclosed <see cref="BhTooltip"/>
/// instances. Mirrors Radix UI's <c>Tooltip.Provider</c>: lets you tune the open
/// delay, the "skip" window during which the next tooltip opens instantly, and
/// whether tooltip content is itself hoverable.
///
/// <para>
/// You can either wrap your whole app in a single provider or wrap a subtree.
/// Each <see cref="BhTooltip"/> may also override the <see cref="DelayDuration"/>
/// individually.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhTooltipProvider DelayDuration="200" SkipDelayDuration="0"&gt;
///     &lt;BhTooltip&gt;
///         &lt;BhTooltipTrigger&gt;Hover me&lt;/BhTooltipTrigger&gt;
///         &lt;BhTooltipContent&gt;Tooltip text&lt;/BhTooltipContent&gt;
///     &lt;/BhTooltip&gt;
/// &lt;/BhTooltipProvider&gt;
/// </code>
/// </summary>
public class BhTooltipProvider : ComponentBase
{
    /// <summary>
    /// Duration (in milliseconds) that a user must hover or focus the trigger
    /// before the tooltip opens. Default: 700ms.
    /// </summary>
    [Parameter]
    public int DelayDuration { get; set; } = 700;

    /// <summary>
    /// How quickly (in milliseconds) the user has to move between triggers for
    /// the next tooltip to skip its open delay. Default: 300ms.
    /// </summary>
    [Parameter]
    public int SkipDelayDuration { get; set; } = 300;

    /// <summary>
    /// When <c>true</c>, the tooltip closes the moment the pointer leaves the
    /// trigger — even if it moves over the content. Default: <c>false</c>.
    /// </summary>
    [Parameter]
    public bool DisableHoverableContent { get; set; }

    /// <summary>Child content. Should contain one or more <see cref="BhTooltip"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private readonly BhTooltipProviderContext _context = new();

    protected override void OnParametersSet()
    {
        _context.DelayDuration = DelayDuration;
        _context.SkipDelayDuration = SkipDelayDuration;
        _context.DisableHoverableContent = DisableHoverableContent;
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhTooltipProviderContext>>(0);
        builder.AddComponentParameter(1, "Value", _context);
        builder.AddComponentParameter(2, "IsFixed", true);
        builder.AddComponentParameter(3, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}
