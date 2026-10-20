using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Content shown inside <see cref="BhComboboxOptions{TValue}"/> when there are no
/// options (e.g. "No results"). Rendered as <c>role="presentation"</c> so it isn't
/// mistaken for an option.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BhComboboxEmpty<TValue> : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>The empty-state content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    protected override void OnInitialized()
    {
        if (ComboboxContext is not null) ComboboxContext.Combobox.Empty = this;
    }

    internal void Refresh() => StateHasChanged();

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (ComboboxContext is null || ComboboxContext.State.Count > 0) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddElementReferenceCapture(20, CaptureElement);
        builder.AddContent(30, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "presentation";
        return attrs;
    }

    public void Dispose()
    {
        if (ComboboxContext?.Combobox.Empty == this) ComboboxContext.Combobox.Empty = null;
    }
}
