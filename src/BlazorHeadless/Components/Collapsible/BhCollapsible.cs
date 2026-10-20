using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// An interactive region that expands and collapses a panel via a trigger
/// (WAI-ARIA Disclosure pattern, Radix-style API).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Binding</b> — <c>@bind-Open</c>, or uncontrolled with <see cref="DefaultOpen"/>.</item>
///   <item><b>ARIA</b> — <c>aria-expanded</c> / <c>aria-controls</c> on the trigger.</item>
///   <item><b>Animations</b> — the content keeps rendering while an exit animation runs and exposes
///     <c>--bh-content-height</c> / <c>--bh-content-width</c> for height animations.</item>
///   <item><b>Find-in-page</b> — <see cref="BhCollapsibleContent.HiddenUntilFound"/> lets the browser reveal matches.</item>
/// </list>
///
/// <code>
/// &lt;BhCollapsible @bind-Open="expanded"&gt;
///     &lt;BhCollapsibleTrigger Context="c"&gt;@(c.IsOpen ? "Hide" : "Show")&lt;/BhCollapsibleTrigger&gt;
///     &lt;BhCollapsibleContent&gt;Details…&lt;/BhCollapsibleContent&gt;
/// &lt;/BhCollapsible&gt;
/// </code>
/// </summary>
public class BhCollapsible : BhComponentBase
{
    private bool _open;

    /// <summary>Whether the content is shown. Supplying it makes the component controlled (use <c>@bind-Open</c>).</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>Initial open state when uncontrolled.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>Two-way binding callback for <see cref="Open"/>.</summary>
    [Parameter]
    public EventCallback<bool> OpenChanged { get; set; }

    /// <summary>Fires whenever the open state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Prevents the user from toggling.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>A <see cref="BhCollapsibleTrigger"/> and a <see cref="BhCollapsibleContent"/>.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => IsProvided(nameof(Open)) ? Open : _open;

    protected override void OnInitialized() => _open = DefaultOpen;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = new BhCollapsibleContext
        {
            IsOpen = IsOpen,
            Disabled = Disabled,
            TriggerId = $"{ComponentId}-trigger",
            ContentId = $"{ComponentId}-content",
            Toggle = () => _ = ToggleAsync(),
            Close = () => _ = CloseAsync(),
            ToggleAsync = ToggleAsync,
            CloseAsync = CloseAsync,
        };

        builder.OpenComponent<CascadingValue<IBhCloseContext>>(0);
        builder.AddComponentParameter(1, "Value", (IBhCloseContext)context);
        builder.AddComponentParameter(2, "ChildContent", (RenderFragment)(closeOuter =>
        {
            closeOuter.OpenComponent<CascadingValue<BhCollapsibleContext>>(0);
            closeOuter.AddComponentParameter(1, "Value", context);
            closeOuter.AddComponentParameter(2, "ChildContent", (RenderFragment)(inner =>
            {
                inner.OpenElement(0, Tag);
                inner.AddAttribute(10, "id", ComponentId);
                inner.AddMultipleAttributes(20, GetFinalAttributes());
                inner.AddElementReferenceCapture(30, CaptureElement);
                inner.AddContent(40, ChildContent);
                inner.CloseElement();
            }));
            closeOuter.CloseComponent();
        }));
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        SetDataState(attrs, IsOpen);
        SetDataFlag(attrs, "disabled", Disabled);
        return attrs;
    }

    private Task ToggleAsync() => Disabled ? Task.CompletedTask : SetOpenAsync(!IsOpen);

    private Task CloseAsync() => IsOpen ? SetOpenAsync(false) : Task.CompletedTask;

    private async Task SetOpenAsync(bool value)
    {
        if (!IsProvided(nameof(Open)))
            _open = value;
        await OpenChanged.InvokeAsync(value);
        await OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
