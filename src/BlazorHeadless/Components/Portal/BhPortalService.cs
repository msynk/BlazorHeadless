using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// A scoped service that coordinates between <see cref="BhPortal"/> components
/// and the <see cref="BhPortalOutlet"/>. Portals register their content here;
/// the outlet subscribes and renders it.
/// </summary>
public sealed class BhPortalService
{
    private readonly List<PortalEntry> _entries = new();
    private Action? _outletCallback;

    /// <summary>
    /// Registers a portal's content. Returns an id that must be passed to
    /// <see cref="Unregister"/> when the portal disposes.
    /// </summary>
    internal string Register(RenderFragment content)
    {
        var id = Guid.NewGuid().ToString("N")[..8];
        _entries.Add(new PortalEntry(id, content));
        NotifyOutlet();
        return id;
    }

    /// <summary>
    /// Updates the content of an existing portal entry.
    /// </summary>
    internal void Update(string entryId, RenderFragment content)
    {
        var index = _entries.FindIndex(e => e.Id == entryId);
        if (index >= 0)
        {
            _entries[index] = new PortalEntry(entryId, content);
            NotifyOutlet();
        }
    }

    /// <summary>
    /// Removes a portal entry by id.
    /// </summary>
    internal void Unregister(string entryId)
    {
        var index = _entries.FindIndex(e => e.Id == entryId);
        if (index >= 0)
        {
            _entries.RemoveAt(index);
            NotifyOutlet();
        }
    }

    /// <summary>
    /// Gets all registered portal entries.
    /// </summary>
    internal IReadOnlyList<PortalEntry> GetEntries()
    {
        return _entries;
    }

    /// <summary>
    /// Subscribes the outlet to be notified when entries change.
    /// </summary>
    internal void Subscribe(Action callback)
    {
        _outletCallback = callback;
    }

    /// <summary>
    /// Unsubscribes the outlet.
    /// </summary>
    internal void Unsubscribe()
    {
        _outletCallback = null;
    }

    private void NotifyOutlet()
    {
        _outletCallback?.Invoke();
    }

    internal sealed record PortalEntry(string Id, RenderFragment Content);
}
