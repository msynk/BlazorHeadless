using Microsoft.AspNetCore.Components;

namespace BlazorHeadless;

/// <summary>
/// Cascading context provided by <see cref="BhTooltip"/> to its descendant parts
/// (<see cref="BhTooltipTrigger"/>, <see cref="BhTooltipContent"/>,
/// <see cref="BhTooltipArrow"/>). Carries open state, ARIA wiring ids, and the
/// open/close callbacks plus pointer-tracking hooks the trigger and content
/// share to coordinate hoverable-content behaviour.
/// </summary>
public sealed class BhTooltipContext
{
    private readonly Func<bool, Task> _setOpenAsync;
    private readonly Func<Task> _scheduleOpenAsync;
    private readonly Func<Task> _scheduleCloseAsync;
    private readonly Action<ElementReference> _registerTrigger;

    internal BhTooltipContext(
        bool isOpen,
        bool delayedOpen,
        string baseId,
        bool disableHoverableContent,
        Func<bool, Task> setOpenAsync,
        Func<Task> scheduleOpenAsync,
        Func<Task> scheduleCloseAsync,
        Action<ElementReference> registerTrigger)
    {
        IsOpen = isOpen;
        DelayedOpen = delayedOpen;
        BaseId = baseId;
        DisableHoverableContent = disableHoverableContent;
        _setOpenAsync = setOpenAsync;
        _scheduleOpenAsync = scheduleOpenAsync;
        _scheduleCloseAsync = scheduleCloseAsync;
        _registerTrigger = registerTrigger;
    }

    /// <summary>Whether the tooltip is currently visible.</summary>
    public bool IsOpen { get; }

    /// <summary>
    /// Whether the most recent open used the configured delay (<c>true</c>) or
    /// skipped it (<c>false</c>). Surfaced via <c>data-state</c> as
    /// <c>"delayed-open"</c> vs <c>"instant-open"</c>.
    /// </summary>
    public bool DelayedOpen { get; }

    /// <summary>Base id used to derive trigger and content ids.</summary>
    public string BaseId { get; }

    /// <summary>Deterministic id of the trigger element.</summary>
    public string TriggerId => $"{BaseId}-trigger";

    /// <summary>Deterministic id of the content element.</summary>
    public string ContentId => $"{BaseId}-content";

    /// <summary>
    /// Whether tooltip content should be considered non-hoverable. When
    /// <c>true</c>, leaving the trigger immediately schedules a close even if
    /// the pointer moves into the content.
    /// </summary>
    public bool DisableHoverableContent { get; }

    /// <summary>Forces the tooltip into the requested open state immediately, bypassing delays.</summary>
    public Task SetOpenAsync(bool open) => _setOpenAsync(open);

    /// <summary>Cancels any pending close and schedules the tooltip to open after the configured delay.</summary>
    public Task ScheduleOpenAsync() => _scheduleOpenAsync();

    /// <summary>Cancels any pending open and schedules the tooltip to close shortly.</summary>
    public Task ScheduleCloseAsync() => _scheduleCloseAsync();

    internal void RegisterTrigger(ElementReference trigger)
    {
        TriggerRef = trigger;
        _registerTrigger(trigger);
    }

    /// <summary>Element reference of the trigger, used by the content for anchor positioning.</summary>
    internal ElementReference TriggerRef { get; private set; }
}

/// <summary>
/// Render-fragment context exposed by <see cref="BhTooltipTrigger"/> and
/// <see cref="BhTooltipContent"/> for state-driven rendering.
/// </summary>
public sealed record BhTooltipRenderContext
{
    /// <summary>Whether the tooltip is currently visible.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether the open used the delay (<c>true</c>) or was instant (<c>false</c>).</summary>
    public required bool DelayedOpen { get; init; }
}
