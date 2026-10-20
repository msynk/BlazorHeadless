using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// Content of a nested command page. Rendered only while <see cref="Name"/> is the
/// current page (<c>null</c> / empty = the root page). Navigate with
/// <see cref="BhCommandContext.PushPageAsync"/>; Backspace on an empty search goes back.
///
/// <code>
/// &lt;BhCommandPage&gt;
///     &lt;BhCommandItem OnSelect="@(() => ctx.PushPageAsync("projects"))"&gt;Search projects…&lt;/BhCommandItem&gt;
/// &lt;/BhCommandPage&gt;
/// &lt;BhCommandPage Name="projects"&gt; … &lt;/BhCommandPage&gt;
/// </code>
/// </summary>
public class BhCommandPage : ComponentBase, IDisposable
{
    [CascadingParameter]
    private BhCommandContext? CommandContext { get; set; }

    /// <summary>The page name; null / empty for the root page.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhCommandContext</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhCommandContext>? Template { get; set; }

    protected override void OnInitialized() => CommandContext?.Subscribe(StateHasChanged);

    public void Dispose() => CommandContext?.Unsubscribe(StateHasChanged);

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CommandContext is null) return;
        var current = CommandContext.Page;
        var mine = string.IsNullOrEmpty(Name) ? null : Name;
        if (current != mine) return;
        builder.AddContent(0, BhContent.Pick(Template, ChildContent, CommandContext));
    }
}
