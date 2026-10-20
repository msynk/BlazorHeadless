namespace BlazorHeadless;

/// <summary>
/// Traps keyboard focus inside its content while <see cref="BhFocusScope.Enabled"/>:
/// a <see cref="BhFocusScope"/> with <see cref="BhFocusScope.Trapped"/> on by default.
///
/// <code>
/// &lt;BhFocusTrap Enabled="isActive"&gt;
///     &lt;input placeholder="First" /&gt;
///     &lt;button&gt;Action&lt;/button&gt;
/// &lt;/BhFocusTrap&gt;
/// </code>
/// </summary>
public class BhFocusTrap : BhFocusScope
{
    /// <summary>Creates a trap (Trapped = true).</summary>
    public BhFocusTrap() => Trapped = true;
}
