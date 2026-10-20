using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A container grouping controls such as buttons, toggles and links
/// (<c>role="toolbar"</c>, WAI-ARIA Toolbar pattern). The toolbar is a single tab
/// stop; arrow keys move between <see cref="BhToolbarButton"/>s,
/// <see cref="BhToolbarLink"/>s and <see cref="BhToggleGroup"/> items. Give it an
/// accessible name with <c>aria-label</c>.
///
/// <code>
/// &lt;BhToolbar aria-label="Formatting"&gt;
///     &lt;BhToggleGroup Type="BhToggleGroupType.Multiple" aria-label="Text style"&gt;
///         &lt;BhToggleGroupItem Value="bold" aria-label="Bold"&gt;B&lt;/BhToggleGroupItem&gt;
///     &lt;/BhToggleGroup&gt;
///     &lt;BhToolbarSeparator /&gt;
///     &lt;BhToolbarButton OnClick="Share"&gt;Share&lt;/BhToolbarButton&gt;
/// &lt;/BhToolbar&gt;
/// </code>
/// </summary>
public class BhToolbar : BhRovingFocusGroup
{
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        Context.IsToolbar = true;
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var orientation = Orientation == BhRovingOrientation.Vertical ? "vertical" : "horizontal";
        attrs["role"] = "toolbar";
        attrs["aria-orientation"] = orientation;
        SetDataValue(attrs, "orientation", orientation);
        return attrs;
    }
}
