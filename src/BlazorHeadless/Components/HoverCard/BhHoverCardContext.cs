using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhHoverCard"/> to its descendant
/// parts (<see cref="BhHoverCardTrigger"/>, <see cref="BhHoverCardContent"/>,
/// <see cref="BhHoverCardArrow"/>). Carries open state, ARIA wiring ids, and the
/// open/close scheduling callbacks the trigger and content share to coordinate
/// the hover-open / hover-close behaviour with delays.
/// </summary>
public sealed class BhHoverCardContext
{
    private readonly Func<Task> _scheduleOpenAsync;
    private readonly Func<Task> _scheduleCloseAsync;
    private readonly Func<bool, Task> _setOpenAsync;
    private readonly Action<ElementReference> _registerTrigger;

    internal BhHoverCardContext(
        bool isOpen,
        string baseId,
        Func<Task> scheduleOpenAsync,
        Func<Task> scheduleCloseAsync,
        Func<bool, Task> setOpenAsync,
        Action<ElementReference> registerTrigger)
    {
        IsOpen = isOpen;
        BaseId = baseId;
        _scheduleOpenAsync = scheduleOpenAsync;
        _scheduleCloseAsync = scheduleCloseAsync;
        _setOpenAsync = setOpenAsync;
        _registerTrigger = registerTrigger;
    }

    /// <summary>Whether the hover card content is currently visible.</summary>
    public bool IsOpen { get; }

    /// <summary>Base id used to derive trigger and content ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the trigger element.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the content element.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>Cancels any pending close and schedules the card to open after <c>OpenDelay</c>.</summary>
    public Task ScheduleOpenAsync() => _scheduleOpenAsync();

    /// <summary>Cancels any pending open and schedules the card to close after <c>CloseDelay</c>.</summary>
    public Task ScheduleCloseAsync() => _scheduleCloseAsync();

    /// <summary>Forces the card into the requested open state immediately, bypassing delays.</summary>
    public Task SetOpenAsync(bool open) => _setOpenAsync(open);

    internal void RegisterTrigger(ElementReference trigger)
    {
        TriggerRef = trigger;
        _registerTrigger(trigger);
    }

    /// <summary>Element reference of the trigger, used by the content for anchor positioning.</summary>
    internal ElementReference TriggerRef { get; private set; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhHoverCardTrigger"/> and
/// <see cref="BhHoverCardContent"/> for state-driven rendering.
/// </summary>
public sealed record BhHoverCardRenderContext
{
    /// <summary>Whether the hover card content is currently visible.</summary>
    public required bool IsOpen { get; init; }
}
