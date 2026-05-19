using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A utility button pre-wired to close the nearest enclosing
/// <see cref="HDialog"/>, <see cref="HPopover"/>, or <see cref="HDisclosure"/>.
///
/// <para>Saves boilerplate for dismiss actions — just drop it inside any closeable
/// component and it will find and invoke the appropriate close callback.</para>
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Polymorphic rendering</b> — renders as &lt;button&gt; by default;
///     set <see cref="HeadlessComponentBase.As"/> to render as any element.</item>
///   <item><b>Automatic context detection</b> — walks up the cascading parameter
///     chain to find the nearest <see cref="ICloseContext"/> (Dialog, Popover,
///     or Disclosure).</item>
///   <item><b>Keyboard accessible</b> — non-native elements receive
///     <c>role="button"</c>, <c>tabindex="0"</c>, and Enter/Space handling.</item>
///   <item><b>Disabled support</b> — respects the <see cref="Disabled"/> parameter.</item>
/// </list>
///
/// <para><b>Usage inside a Dialog:</b></para>
/// <code>
/// &lt;HDialog Open="@isOpen" OnClose="() =&gt; isOpen = false"&gt;
///     &lt;HDialogPanel&gt;
///         &lt;HDialogTitle&gt;Confirm&lt;/HDialogTitle&gt;
///         &lt;HCloseButton&gt;Cancel&lt;/HCloseButton&gt;
///     &lt;/HDialogPanel&gt;
/// &lt;/HDialog&gt;
/// </code>
///
/// <para><b>Usage inside a Popover:</b></para>
/// <code>
/// &lt;HPopover&gt;
///     &lt;HPopoverButton&gt;Menu&lt;/HPopoverButton&gt;
///     &lt;HPopoverPanel&gt;
///         &lt;a href="/home"&gt;Home&lt;/a&gt;
///         &lt;HCloseButton As="a" href="/about"&gt;About&lt;/HCloseButton&gt;
///     &lt;/HPopoverPanel&gt;
/// &lt;/HPopover&gt;
/// </code>
///
/// <para><b>Usage inside a Disclosure:</b></para>
/// <code>
/// &lt;HDisclosure&gt;
///     &lt;HDisclosureButton&gt;Details&lt;/HDisclosureButton&gt;
///     &lt;HDisclosurePanel&gt;
///         Some content.
///         &lt;HCloseButton&gt;Close&lt;/HCloseButton&gt;
///     &lt;/HDisclosurePanel&gt;
/// &lt;/HDisclosure&gt;
/// </code>
/// </summary>
public class HCloseButton : HeadlessComponentBase
{
    /// <summary>
    /// The nearest closeable context (Dialog, Popover, or Disclosure).
    /// Resolved automatically via cascading parameters.
    /// </summary>
    [CascadingParameter]
    private ICloseContext? CloseContext { get; set; }

    /// <summary>Whether the close button is disabled.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Child content rendered inside the button.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase)
        || Tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (!IsNativeButton)
        {
            builder.AddAttribute(40, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(50, Ref);

        if (ChildContent is not null)
            builder.AddContent(60, ChildContent);

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (Disabled)
                attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = Disabled ? -1 : 0;
            if (Disabled)
                attrs["aria-disabled"] = "true";
        }

        SetDataFlag(attrs, "disabled", Disabled);

        return attrs;
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (Disabled || CloseContext is null) return;
        await CloseContext.CloseAsync();
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (Disabled || CloseContext is null) return;

        if (args.Key is "Enter" or " ")
            await CloseContext.CloseAsync();
    }
}
