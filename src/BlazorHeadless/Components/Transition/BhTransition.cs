using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless Transition component that manages CSS class-based enter/leave
/// animations. Keeps the element rendered during the leave phase so the exit
/// animation can play, then unmounts it.
///
/// <para><b>How it works:</b></para>
/// <list type="bullet">
///   <item><b>Enter</b>: element renders → <c>Enter</c> + <c>EnterFrom</c> applied → next frame: swap to <c>EnterTo</c> → after transition: apply <c>Entered</c>.</item>
///   <item><b>Leave</b>: <c>Leave</c> + <c>LeaveFrom</c> applied → next frame: swap to <c>LeaveTo</c> → after transition: element unmounts.</item>
/// </list>
///
/// <para><b>Requires <c>builder.Services.AddBlazorHeadless()</c> at startup.</b></para>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;BhTransition Show="@isOpen"
///              Enter="transition duration-200 ease-out"
///              EnterFrom="opacity-0 scale-95"
///              EnterTo="opacity-100 scale-100"
///              Leave="transition duration-150 ease-in"
///              LeaveFrom="opacity-100 scale-100"
///              LeaveTo="opacity-0 scale-95"&gt;
///     &lt;div class="panel"&gt;Content&lt;/div&gt;
/// &lt;/BhTransition&gt;
/// </code>
/// </summary>
public class BhTransition : BhComponentBase
{
    [Inject] private BhInterop Interop { get; set; } = default!;

    // ── Parameters ────────────────────────────────────────────────────────────

    /// <summary>Whether the content should be shown. Drives the enter/leave lifecycle.</summary>
    [Parameter]
    public bool Show { get; set; }

    /// <summary>Whether to run the enter transition on initial mount when <see cref="Show"/> is true. Defaults to false.</summary>
    [Parameter]
    public bool Appear { get; set; }

    /// <summary>CSS class(es) applied during the entire enter phase.</summary>
    [Parameter]
    public string? Enter { get; set; }

    /// <summary>CSS class(es) applied on the first frame of enter (the "from" state).</summary>
    [Parameter]
    public string? EnterFrom { get; set; }

    /// <summary>CSS class(es) applied on the second frame of enter (the "to" state, triggers the transition).</summary>
    [Parameter]
    public string? EnterTo { get; set; }

    /// <summary>CSS class(es) applied after the enter transition completes and while the element is shown.</summary>
    [Parameter]
    public string? Entered { get; set; }

    /// <summary>CSS class(es) applied during the entire leave phase.</summary>
    [Parameter]
    public string? Leave { get; set; }

    /// <summary>CSS class(es) applied on the first frame of leave (the "from" state).</summary>
    [Parameter]
    public string? LeaveFrom { get; set; }

    /// <summary>CSS class(es) applied on the second frame of leave (the "to" state, triggers the transition).</summary>
    [Parameter]
    public string? LeaveTo { get; set; }

    /// <summary>Fires before the enter transition starts.</summary>
    [Parameter]
    public EventCallback BeforeEnter { get; set; }

    /// <summary>Fires after the enter transition completes.</summary>
    [Parameter]
    public EventCallback AfterEnter { get; set; }

    /// <summary>Fires before the leave transition starts.</summary>
    [Parameter]
    public EventCallback BeforeLeave { get; set; }

    /// <summary>Fires after the leave transition completes (element is about to unmount).</summary>
    [Parameter]
    public EventCallback AfterLeave { get; set; }

    /// <summary>Child content to transition.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    // ── State ─────────────────────────────────────────────────────────────────

    private enum Phase { Hidden, Entering, Shown, Leaving }

    private Phase _phase = Phase.Hidden;
    private bool _previousShow;
    private bool _firstRender = true;
    private ElementReference _elementRef;
    private bool _hasElementRef;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        _previousShow = Show;
        if (Show)
        {
            _phase = Appear ? Phase.Entering : Phase.Shown;
        }
    }

    protected override void OnParametersSet()
    {
        if (Show && !_previousShow)
        {
            // Transition from hidden → entering.
            _phase = Phase.Entering;
        }
        else if (!Show && _previousShow)
        {
            // Transition from shown → leaving.
            _phase = Phase.Leaving;
        }

        _previousShow = Show;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        _firstRender = firstRender;

        if (_phase == Phase.Entering && _hasElementRef)
        {
            await BeforeEnter.InvokeAsync();
            await Interop.TransitionEnterAsync(_elementRef, Enter, EnterFrom, EnterTo, Entered);
            _phase = Phase.Shown;
            await AfterEnter.InvokeAsync();
            StateHasChanged();
        }
        else if (_phase == Phase.Leaving && _hasElementRef)
        {
            await BeforeLeave.InvokeAsync();
            await Interop.TransitionLeaveAsync(_elementRef, Leave, LeaveFrom, LeaveTo, Entered);
            _phase = Phase.Hidden;
            await AfterLeave.InvokeAsync();
            StateHasChanged();
        }
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Don't render anything when fully hidden.
        if (_phase == Phase.Hidden)
            return;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddElementReferenceCapture(30, e =>
        {
            _elementRef = e;
            _hasElementRef = true;
            Ref?.Invoke(e);
        });

        builder.AddContent(40, ChildContent);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, _phase == Phase.Shown || _phase == Phase.Entering);
        return attrs;
    }
}
