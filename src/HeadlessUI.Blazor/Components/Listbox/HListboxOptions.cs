using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// The popup panel containing the listbox's options. Renders as
/// <c>&lt;ul role="listbox"&gt;</c> by default (override via <see cref="HeadlessComponentBase.As"/>).
///
/// <para>
/// Hidden via the HTML <c>hidden</c> attribute when the listbox is closed,
/// matching the behaviour of the other show/hide primitives in this library.
/// Consumers can drive open/close transitions via the <c>data-state</c> hook.
/// </para>
/// </summary>
public class HListboxOptions<TValue> : HeadlessComponentBase
{
    [CascadingParameter]
    private ListboxContext<TValue> ListboxContext { get; set; } = default!;

    /// <summary>Child content. Should contain one or more <see cref="HListboxOption{TValue}"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private ElementReference _elementRef;

    protected override string DefaultTag => "ul";

    private bool IsOpen => ListboxContext?.IsOpen ?? false;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ListboxContext?.OptionsId ?? ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        if (!IsOpen)
            builder.AddAttribute(30, "hidden", true);

        // Per ARIA, keyboard nav happens on the listbox panel itself when it has focus.
        builder.AddAttribute(35, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            Ref?.Invoke(e);
        });

        builder.AddContent(50, ChildContent);

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "listbox";

        if (ListboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";

        // The panel itself isn't focusable directly; assistive tech follows
        // aria-activedescendant on the button. We add tabindex=-1 so programmatic
        // focus calls work but the panel isn't in the tab order.
        attrs["tabindex"] = -1;

        SetDataState(attrs, IsOpen);

        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        // Forward to the listbox root, which handles keyboard nav holistically.
        return ListboxContext?.HandleOptionKeyDownAsync(ListboxContext.ActiveIndex, args)
            ?? Task.CompletedTask;
    }
}
