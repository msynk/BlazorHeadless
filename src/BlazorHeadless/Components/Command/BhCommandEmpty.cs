using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>Rendered inside <see cref="BhCommandList"/> when no item matches the search.</summary>
public class BhCommandEmpty : BhComponentBase, IDisposable
{
    [CascadingParameter]
    private BhCommandContext? CommandContext { get; set; }

    /// <summary>The empty-state content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    protected override void OnInitialized() => CommandContext?.Subscribe(StateHasChanged);

    public void Dispose() => CommandContext?.Unsubscribe(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CommandContext is null || CommandContext.VisibleCount > 0) return;
        builder.OpenElement(0, Tag);
        builder.AddMultipleAttributes(10, GetFinalAttributes());
        builder.AddContent(20, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "presentation";
        attrs["data-bh-command-empty"] = "";
        return attrs;
    }
}
