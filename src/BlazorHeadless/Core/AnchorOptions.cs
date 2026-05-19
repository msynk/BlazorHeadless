namespace BlazorHeadless;

/// <summary>
/// Configuration for anchoring a floating panel (dropdown, popover, etc.)
/// relative to a reference element (button, trigger).
///
/// <para>
/// Mirrors the Headless UI <c>anchor</c> prop API. Supports placement strings
/// like <c>"bottom"</c>, <c>"bottom start"</c>, <c>"top end"</c>, etc., plus
/// gap, offset, and padding values.
/// </para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HMenuItems Anchor="@(new AnchorOptions { To = "bottom start", Gap = 4 })"&gt;
///     ...
/// &lt;/HMenuItems&gt;
/// </code>
///
/// Or using the shorthand string constructor:
/// <code>
/// &lt;HMenuItems Anchor="@AnchorOptions.Parse("bottom start")"&gt;
///     ...
/// &lt;/HMenuItems&gt;
/// </code>
/// </summary>
public sealed class AnchorOptions
{
    /// <summary>
    /// Where to position the floating panel relative to the reference element.
    /// Use <c>"top"</c>, <c>"right"</c>, <c>"bottom"</c>, or <c>"left"</c> to center
    /// along the appropriate edge. Combine with <c>"start"</c> or <c>"end"</c> for
    /// corner alignment, e.g. <c>"bottom start"</c>, <c>"top end"</c>.
    /// Default: <c>"bottom"</c>.
    /// </summary>
    public string To { get; set; } = "bottom";

    /// <summary>
    /// The space (in pixels) between the reference element and the floating panel.
    /// Equivalent to the <c>--anchor-gap</c> CSS variable in Headless UI.
    /// Default: 0.
    /// </summary>
    public int Gap { get; set; }

    /// <summary>
    /// The distance (in pixels) the floating panel should be nudged along the
    /// alignment axis from its natural position. Equivalent to the
    /// <c>--anchor-offset</c> CSS variable in Headless UI.
    /// Default: 0.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// The minimum space (in pixels) between the floating panel and the viewport
    /// edges. Equivalent to the <c>--anchor-padding</c> CSS variable in Headless UI.
    /// Default: 8.
    /// </summary>
    public int Padding { get; set; } = 8;

    /// <summary>
    /// Creates an <see cref="AnchorOptions"/> from a placement string.
    /// </summary>
    /// <param name="placement">
    /// A placement string like <c>"bottom"</c>, <c>"bottom start"</c>, <c>"top end"</c>.
    /// </param>
    public static AnchorOptions Parse(string placement) => new() { To = placement };

    /// <summary>
    /// Implicit conversion from string to <see cref="AnchorOptions"/> for ergonomic usage.
    /// </summary>
    public static implicit operator AnchorOptions(string placement) => Parse(placement);

    /// <summary>
    /// Converts to the anonymous object shape expected by the JS interop layer.
    /// </summary>
    internal object ToJsOptions() => new
    {
        to = To,
        gap = Gap,
        offset = Offset,
        padding = Padding,
    };
}
