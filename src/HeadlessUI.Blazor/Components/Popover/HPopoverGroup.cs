using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace HeadlessUI.Blazor;

/// <summary>
/// An optional wrapper that groups multiple <see cref="HPopover"/> components so
/// that opening one automatically closes the others (mutual exclusion).
///
/// <para>
/// When popovers are not inside a group they operate independently — any number
/// can be open at the same time.
/// </para>
/// </summary>
public class HPopoverGroup : HeadlessComponentBase
{
    private readonly List<HPopover> _popovers = new();

    /// <summary>Child content. Should contain one or more <see cref="HPopover"/> components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<PopoverGroupContext>>(0);
        builder.AddComponentParameter(1, "Value", CreateGroupContext());
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
        {
            inner.OpenElement(0, Tag);
            inner.AddAttribute(10, "id", ComponentId);
            inner.AddMultipleAttributes(20, GetFinalAttributes());

            if (Ref is not null)
                inner.AddElementReferenceCapture(30, Ref);

            inner.AddContent(40, ChildContent);
            inner.CloseElement();
        }));
        builder.CloseComponent();
    }

    private PopoverGroupContext CreateGroupContext() => new(
        register: p => { if (!_popovers.Contains(p)) _popovers.Add(p); },
        unregister: p => _popovers.Remove(p),
        closeOthersAsync: async except =>
        {
            foreach (var p in _popovers.Where(p => p != except))
                await p.CloseFromGroupAsync();
        });
}
