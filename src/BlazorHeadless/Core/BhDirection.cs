using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>Reading direction. Flips horizontal arrow-key navigation and start/end alignment.</summary>
public enum BhDirection
{
    /// <summary>Left-to-right.</summary>
    Ltr,

    /// <summary>Right-to-left.</summary>
    Rtl,
}

/// <summary>Layout orientation of a composite widget.</summary>
public enum BhOrientation
{
    /// <summary>Items flow horizontally; Left / Right arrows navigate.</summary>
    Horizontal,

    /// <summary>Items flow vertically; Up / Down arrows navigate.</summary>
    Vertical,
}

/// <summary>
/// Provides the reading direction to every BlazorHeadless component below it.
/// Components also accept their own <c>Dir</c> parameter which takes precedence.
///
/// <code>
/// &lt;BhDirectionProvider Dir="BhDirection.Rtl"&gt;
///     &lt;Router … /&gt;
/// &lt;/BhDirectionProvider&gt;
/// </code>
/// </summary>
public sealed class BhDirectionProvider : ComponentBase
{
    /// <summary>The reading direction. Default: <see cref="BhDirection.Ltr"/>.</summary>
    [Parameter]
    public BhDirection Dir { get; set; }

    /// <summary>Child content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<BhDirection>>(0);
        builder.AddComponentParameter(1, "Value", Dir);
        builder.AddComponentParameter(2, "ChildContent", ChildContent);
        builder.CloseComponent();
    }
}

/// <summary>Helpers for keyboard navigation that depends on orientation and direction.</summary>
public static class BhKeyboard
{
    /// <summary>The key that moves to the next item for the given orientation / direction.</summary>
    public static string NextKey(BhOrientation orientation, BhDirection dir)
        => orientation == BhOrientation.Vertical ? "ArrowDown" : dir == BhDirection.Rtl ? "ArrowLeft" : "ArrowRight";

    /// <summary>The key that moves to the previous item for the given orientation / direction.</summary>
    public static string PreviousKey(BhOrientation orientation, BhDirection dir)
        => orientation == BhOrientation.Vertical ? "ArrowUp" : dir == BhDirection.Rtl ? "ArrowRight" : "ArrowLeft";

    /// <summary>Converts a key string to the token used in <c>data-bh-keys</c>.</summary>
    public static string Token(string key) => key == " " ? "Space" : key;

    /// <summary>Builds a <c>data-bh-keys</c> attribute value from keys / tokens.</summary>
    public static string Keys(params string?[] keys)
        => string.Join(' ', keys.Where(k => !string.IsNullOrEmpty(k)).Select(k => Token(k!)).Distinct());

    /// <summary>The <c>data-bh-keys</c> attribute name.</summary>
    public const string KeysAttribute = "data-bh-keys";

    /// <summary>The data-orientation / aria-orientation value.</summary>
    public static string ToAttribute(this BhOrientation orientation)
        => orientation == BhOrientation.Vertical ? "vertical" : "horizontal";

    /// <summary>The dir attribute value.</summary>
    public static string ToAttribute(this BhDirection dir)
        => dir == BhDirection.Rtl ? "rtl" : "ltr";
}
