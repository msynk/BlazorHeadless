using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace BlazorHeadless;

/// <summary>
/// A headless Collapsible component — an interactive region that expands and
/// collapses a panel, toggled by a trigger. Mirrors the Radix UI Collapsible
/// primitive: <see cref="BhCollapsibleTrigger"/> toggles, and
/// <see cref="BhCollapsibleContent"/> shows/hides.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item>
///     <b>Uncontrolled and controlled</b> — seed state via <see cref="DefaultOpen"/>
///     or drive externally with <see cref="Open"/> + <see cref="OnOpenChange"/>.
///   </item>
///   <item>
///     <b>Compound API</b> — <see cref="BhCollapsibleTrigger"/> toggles,
///     <see cref="BhCollapsibleContent"/> shows/hides. Cascading context wires
///     <c>aria-expanded</c>, <c>aria-controls</c>, and <c>aria-labelledby</c>.
///   </item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> is a
///     <see cref="RenderFragment"/>; the trigger and content each expose a
///     <see cref="BhCollapsibleRenderContext"/>, exposing <c>IsOpen</c> and a
///     <c>Close()</c> action you can call from anywhere inside the collapsible.
///   </item>
///   <item>
///     <b>Data attributes</b> — emits <c>data-state="open"|"closed"</c> and
///     <c>data-disabled</c> for CSS hooks.
///   </item>
/// </list>
///
/// <para><b>Usage (uncontrolled):</b></para>
/// <code>
/// &lt;BhCollapsible&gt;
///     &lt;BhCollapsibleTrigger&gt;Toggle&lt;/BhCollapsibleTrigger&gt;
///     &lt;BhCollapsibleContent&gt;Hidden content&lt;/BhCollapsibleContent&gt;
/// &lt;/BhCollapsible&gt;
/// </code>
///
/// <para><b>Usage (with render context):</b></para>
/// <code>
/// &lt;BhCollapsible&gt;
///     &lt;BhCollapsibleTrigger Context="c"&gt;@(c.IsOpen ? "Hide" : "Show")&lt;/BhCollapsibleTrigger&gt;
///     &lt;BhCollapsibleContent Context="c"&gt;
///         Content here.
///         &lt;button @onclick="c.Close"&gt;Close&lt;/button&gt;
///     &lt;/BhCollapsibleContent&gt;
/// &lt;/BhCollapsible&gt;
/// </code>
/// </summary>
public class BhCollapsible : BhComponentBase
{
    private bool _isOpen;
    private bool _initialized;

    /// <summary>Whether the content is open by default (uncontrolled). Ignored when <see cref="Open"/> is supplied.</summary>
    [Parameter]
    public bool DefaultOpen { get; set; }

    /// <summary>
    /// Controlled open state. When non-null the component runs in controlled mode
    /// and <see cref="OnOpenChange"/> must update this value.
    /// </summary>
    [Parameter]
    public bool? Open { get; set; }

    /// <summary>Fires whenever the open state changes (controlled or uncontrolled).</summary>
    [Parameter]
    public EventCallback<bool> OnOpenChange { get; set; }

    /// <summary>Disables interaction with the collapsible.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Child content. Should contain a <see cref="BhCollapsibleTrigger"/> and a
    /// <see cref="BhCollapsibleContent"/>. The trigger and content each expose a
    /// <see cref="BhCollapsibleRenderContext"/> via <c>Context="c"</c> for state-driven UI.
    /// </summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "div";

    private bool IsOpen => Open ?? _isOpen;

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Open is null)
            _isOpen = DefaultOpen;
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        if (!_initialized) return;
        // In controlled mode the public Open property is the source of truth;
        // _isOpen is unused.
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = CreateContext();

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

                if (Ref is not null)
                    inner.AddElementReferenceCapture(30, Ref);

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

    // ── State and context ────────────────────────────────────────────────────

    private BhCollapsibleContext CreateContext() => new()
    {
        IsOpen = IsOpen,
        Disabled = Disabled,
        TriggerId = $"{ComponentId}-trigger",
        ContentId = $"{ComponentId}-content",
        Toggle = Toggle,
        Close = Close,
    };

    private void Toggle()
    {
        if (Disabled) return;
        SetOpen(!IsOpen);
    }

    private void Close()
    {
        if (!IsOpen) return;
        SetOpen(false);
    }

    private void SetOpen(bool value)
    {
        if (Open is null)
            _isOpen = value;

        _ = OnOpenChange.InvokeAsync(value);
        StateHasChanged();
    }
}
