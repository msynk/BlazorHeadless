namespace BlazorHeadless;

/// <summary>
/// Announces messages to screen-reader users through a visually hidden ARIA live
/// region — for async results ("12 results"), drag-and-drop progress, toasts, form
/// submission outcomes and any other change that isn't conveyed by focus.
///
/// <para>
/// Inject <see cref="IBhAnnouncer"/> anywhere. In tests, replace it with a fake
/// that records messages (see <c>BlazorHeadless.Testing</c>).
/// </para>
/// <code>
/// @inject IBhAnnouncer Announcer
/// await Announcer.AnnounceAsync($"{results.Count} results available");
/// </code>
/// </summary>
public interface IBhAnnouncer
{
    /// <summary>Announces <paramref name="message"/> with the given politeness.</summary>
    ValueTask AnnounceAsync(string message, BhPoliteness politeness = BhPoliteness.Polite);
}

/// <summary>Default <see cref="IBhAnnouncer"/> backed by a live region the JS runtime appends to &lt;body&gt;.</summary>
public sealed class BhAnnouncer : IBhAnnouncer
{
    private readonly BhInterop _interop;

    /// <summary>Creates the announcer.</summary>
    public BhAnnouncer(BhInterop interop)
    {
        _interop = interop;
    }

    /// <summary>How long (ms) a message stays in the region before it is cleared. Default: 7000.</summary>
    public int ClearAfterMilliseconds { get; set; } = 7000;

    /// <inheritdoc />
    public ValueTask AnnounceAsync(string message, BhPoliteness politeness = BhPoliteness.Polite)
        => string.IsNullOrWhiteSpace(message)
            ? ValueTask.CompletedTask
            : _interop.AnnounceAsync(message, politeness, ClearAfterMilliseconds);
}
