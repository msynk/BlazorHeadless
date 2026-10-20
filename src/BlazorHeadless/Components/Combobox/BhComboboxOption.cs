using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A single option of a <see cref="BhCombobox{TValue}"/> (<c>&lt;li role="option"&gt;</c>).
/// Clicking selects it without taking focus out of the input; hovering highlights it.
/// Emits <c>aria-selected</c>, <c>data-active</c>, <c>data-selected</c> and <c>data-disabled</c>.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BhComboboxOption<TValue> : BhOptionBase<TValue>
{
    [CascadingParameter]
    private BhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhComboboxOptionRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhComboboxOptionRenderContext<TValue>>? Template { get; set; }

    protected override bool RootDisabled => ComboboxContext?.Disabled ?? false;

    protected override Task SelectAsync() => ComboboxContext?.SelectAsync(Value) ?? Task.CompletedTask;

    protected override void RenderContent(RenderTreeBuilder builder, int sequence)
    {
        if (Template is null && ChildContent is null) return;
        builder.AddContent(sequence, BhContent.Pick(Template, ChildContent, new BhComboboxOptionRenderContext<TValue>
        {
            Value = Value,
            IsSelected = IsSelected,
            IsActive = IsActive,
            Disabled = IsItemDisabled,
        }));
    }
}
