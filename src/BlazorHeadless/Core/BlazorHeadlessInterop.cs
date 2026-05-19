using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// Lazy wrapper around the library's JS interop module. Imported once per
/// circuit and reused for the lifetime of the application.
///
/// <para>
/// Components don't typically use this directly. Each component family (Dialog,
/// Popover, future Combobox positioning) exposes a higher-level method that
/// internally calls into this service.
/// </para>
/// </summary>
public sealed class BlazorHeadlessInterop : IAsyncDisposable
{
    private const string ModulePath = "./_content/BlazorHeadless/headlessui.js";

    private readonly Lazy<Task<IJSObjectReference>> _module;

    public BlazorHeadlessInterop(IJSRuntime js)
    {
        _module = new Lazy<Task<IJSObjectReference>>(
            () => js.InvokeAsync<IJSObjectReference>("import", ModulePath).AsTask());
    }

    /// <summary>
    /// Locks focus inside <paramref name="panel"/>, locks body scroll, and marks
    /// background siblings <c>inert</c>. Returns a handle that must be passed to
    /// <see cref="DialogUnlockAsync"/> to undo.
    /// </summary>
    /// <param name="panel">The dialog panel element (the <c>role="dialog"</c> surface).</param>
    /// <param name="initialFocus">Optional element that should receive focus on lock.</param>
    /// <param name="returnFocus">Optional element to receive focus on unlock; defaults to whatever was active at lock time.</param>
    public async Task<int> DialogLockAsync(
        ElementReference panel,
        ElementReference? initialFocus = null,
        ElementReference? returnFocus = null)
    {
        var module = await _module.Value;
        var options = new
        {
            initialFocus = (object?)initialFocus,
            returnFocus = (object?)returnFocus,
        };
        return await module.InvokeAsync<int>("dialog.lock", panel, options);
    }

    /// <summary>Releases the locks and restores the captured focus target.</summary>
    public async ValueTask DialogUnlockAsync(int handle)
    {
        if (handle <= 0) return;
        var module = await _module.Value;
        await module.InvokeVoidAsync("dialog.unlock", handle);
    }

    // ── Popover ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Moves focus to the first focusable element inside <paramref name="panel"/>.
    /// Returns a reference to the element that had focus before the call, so the
    /// caller can restore it when the popover closes.
    /// </summary>
    public async Task<IJSObjectReference?> PopoverFocusPanelAsync(ElementReference panel)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<IJSObjectReference?>("popover.focusPanel", panel);
    }

    /// <summary>Restores focus to the element captured by <see cref="PopoverFocusPanelAsync"/>.</summary>
    public async ValueTask PopoverRestoreFocusAsync(IJSObjectReference? element)
    {
        if (element is null) return;
        var module = await _module.Value;
        await module.InvokeVoidAsync("popover.restoreFocus", element);
    }

    // ── Anchor / Positioning ─────────────────────────────────────────────────

    /// <summary>
    /// Starts positioning the <paramref name="floating"/> element relative to the
    /// <paramref name="reference"/> element. Sets up auto-update listeners for
    /// scroll, resize, and element size changes.
    /// Returns a handle that must be passed to <see cref="AnchorStopAsync"/> to clean up.
    /// </summary>
    /// <param name="reference">The trigger/button element to anchor against.</param>
    /// <param name="floating">The floating panel element to position.</param>
    /// <param name="options">Positioning options (placement, gap, offset, padding).</param>
    public async Task<int> AnchorStartAsync(
        ElementReference reference,
        ElementReference floating,
        AnchorOptions options)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<int>("anchor.start", reference, floating, options.ToJsOptions());
    }

    /// <summary>
    /// Starts positioning using element IDs instead of ElementReferences.
    /// More reliable when element refs may not be captured yet.
    /// </summary>
    public async Task<int> AnchorStartByIdAsync(
        string referenceId,
        string floatingId,
        AnchorOptions options)
    {
        var module = await _module.Value;
        return await module.InvokeAsync<int>("anchor.start", referenceId, floatingId, options.ToJsOptions());
    }

    /// <summary>
    /// Stops auto-updating and cleans up listeners for the given anchor handle.
    /// </summary>
    public async ValueTask AnchorStopAsync(int handle)
    {
        if (handle <= 0) return;
        try
        {
            var module = await _module.Value;
            await module.InvokeVoidAsync("anchor.stop", handle);
        }
        catch (JSDisconnectedException) { /* circuit gone */ }
    }

    /// <summary>
    /// Forces an immediate reposition for the given anchor handle.
    /// </summary>
    public async ValueTask AnchorUpdateAsync(int handle)
    {
        if (handle <= 0) return;
        var module = await _module.Value;
        await module.InvokeVoidAsync("anchor.update", handle);
    }

    // ── Transition ───────────────────────────────────────────────────────────

    /// <summary>
    /// Runs the CSS enter transition on <paramref name="element"/>.
    /// Applies the class sequence: enter+enterFrom → (next frame) enterTo → (after transition) entered.
    /// </summary>
    public async ValueTask TransitionEnterAsync(
        ElementReference element,
        string? enter,
        string? enterFrom,
        string? enterTo,
        string? entered)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("transition.enter", element, new { enter, enterFrom, enterTo, entered });
    }

    /// <summary>
    /// Runs the CSS leave transition on <paramref name="element"/>.
    /// Applies the class sequence: leave+leaveFrom → (next frame) leaveTo → (after transition) done.
    /// </summary>
    public async ValueTask TransitionLeaveAsync(
        ElementReference element,
        string? leave,
        string? leaveFrom,
        string? leaveTo,
        string? entered)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("transition.leave", element, new { leave, leaveFrom, leaveTo, entered });
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            try
            {
                var module = await _module.Value;
                await module.DisposeAsync();
            }
            catch (JSDisconnectedException) { /* circuit gone */ }
            catch (Exception) { /* best effort */ }
        }
    }
}
