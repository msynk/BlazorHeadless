using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless, accessible two-state Switch (<c>role="switch"</c>).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Checked</c> with <c>EditForm</c> validation, or uncontrolled.</item>
///   <item><b>Keyboard</b> — Space and Enter toggle.</item>
///   <item><b>Forms</b> — <see cref="BhField"/> wiring (label / description) and a hidden input for plain posts.</item>
///   <item><b>Styling</b> — <c>data-state="checked|unchecked"</c> on the switch and its <see cref="BhSwitchThumb"/>.</item>
/// </list>
///
/// <code>
/// &lt;BhSwitch @bind-Checked="enabled" aria-label="Notifications" class="switch"&gt;
///     &lt;BhSwitchThumb class="thumb" /&gt;
/// &lt;/BhSwitch&gt;
/// </code>
/// </summary>
public class BhSwitch : BhCheckableBase
{
    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhSwitchRenderContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhSwitchRenderContext>? Template { get; set; }

    protected override string Role => "switch";

    protected override bool EnterToggles => true;

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        var context = new BhSwitchRenderContext { IsChecked = IsChecked, Disabled = IsDisabled };
        builder.OpenComponent<CascadingValue<BhSwitchRenderContext>>(sequence);
        builder.AddComponentParameter(sequence + 1, "Value", context);
        builder.AddComponentParameter(sequence + 2, "ChildContent", BhContent.Pick(Template, ChildContent, context));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetAriaBool(attrs, "aria-checked", IsChecked);
        SetDataState(attrs, IsChecked, "checked", "unchecked");
        return attrs;
    }
}
