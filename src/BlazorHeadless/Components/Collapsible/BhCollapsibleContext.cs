namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhCollapsible"/> to its trigger and content.
/// </summary>
public sealed class BhCollapsibleContext : IBhCloseContext
{
    /// <summary>Whether the content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether toggling is disabled.</summary>
    public required bool Disabled { get; init; }

    /// <summary>The trigger id.</summary>
    public required string TriggerId { get; init; }

    /// <summary>The content id.</summary>
    public required string ContentId { get; init; }

    /// <summary>Toggles the content (fire-and-forget; prefer <see cref="ToggleAsync"/>).</summary>
    public required Action Toggle { get; init; }

    /// <summary>Closes the content (fire-and-forget; prefer <see cref="CloseAsync"/>).</summary>
    public required Action Close { get; init; }

    /// <summary>Toggles the content.</summary>
    public Func<Task> ToggleAsync { get; init; } = () => Task.CompletedTask;

    /// <summary>Closes the content.</summary>
    public Func<Task> CloseAsync { get; init; } = () => Task.CompletedTask;

    Task IBhCloseContext.CloseAsync() => CloseAsync();
}

/// <summary>Render-fragment context exposed by collapsible parts.</summary>
public sealed record BhCollapsibleRenderContext
{
    /// <summary>Whether the content is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Closes the content.</summary>
    public required Action Close { get; init; }
}
