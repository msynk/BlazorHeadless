using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A single radio inside an <see cref="HRadioGroup"/>. Renders as a native
/// <c>&lt;button role="radio"&gt;</c> by default and participates in the group's
/// roving-tabindex keyboard navigation.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Click selects this radio.</item>
///   <item>Arrow / Space keys delegate to the parent group.</item>
///   <item>Sets <c>role="radio"</c>, <c>aria-checked</c>, and <c>tabindex</c> (0 when this is the active tab stop, -1 otherwise).</item>
///   <item>Emits <c>data-state="checked"|"unchecked"</c> and <c>data-disabled</c>.</item>
///   <item>Render-prop via <see cref="ChildContent"/> exposes <see cref="RadioRenderContext"/>.</item>
/// </list>
/// </summary>
public class HRadio : HeadlessComponentBase, IDisposable
{
    [CascadingParameter]
    private RadioGroupContext GroupContext { get; set; } = default!;

    /// <summary>The unique value identifying this radio within the group.</summary>
    [Parameter, EditorRequired]
    public string Value { get; set; } = string.Empty;

    /// <summary>Disables this radio independently of the group's disabled state.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Content template receiving <see cref="RadioRenderContext"/> for state-driven rendering.</summary>
    [Parameter]
    public RenderFragment<RadioRenderContext>? ChildContent { get; set; }

    private int _index = -1;
    private ElementReference _elementRef;

    protected override string DefaultTag => "button";

    /// <summary>Whether this radio is currently selected.</summary>
    public bool IsChecked => GroupContext?.SelectedValue == Value;

    /// <summary>Whether this radio is disabled (own flag or group).</summary>
    public bool IsRadioDisabled => Disabled || (GroupContext?.Disabled ?? false);

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    private RadioRenderContext RenderContext => new()
    {
        IsChecked = IsChecked,
        Disabled = IsRadioDisabled,
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (GroupContext is not null)
            _index = GroupContext.RegisterRadio(this);
    }

    public void Dispose()
    {
        GroupContext?.UnregisterRadio(this);
    }

    /// <summary>Moves keyboard focus to this radio. Invoked by the group during arrow-key nav.</summary>
    internal ValueTask FocusAsync() => _elementRef.FocusAsync();

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        builder.AddElementReferenceCapture(40, e =>
        {
            _elementRef = e;
            Ref?.Invoke(e);
        });

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "radio";
        attrs["aria-checked"] = IsChecked ? "true" : "false";

        // Roving tabindex: only the active tab stop is in the document tab order.
        var isTabStop = GroupContext?.IsTabStop(_index) ?? false;
        attrs["tabindex"] = isTabStop ? 0 : -1;

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (IsRadioDisabled)
                attrs["disabled"] = true;
        }
        else
        {
            if (IsRadioDisabled)
                attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsChecked, "checked", "unchecked");
        SetDataFlag(attrs, "disabled", IsRadioDisabled);

        return attrs;
    }

    // ── Event handling ───────────────────────────────────────────────────────

    private Task HandleClick(MouseEventArgs _)
    {
        if (IsRadioDisabled) return Task.CompletedTask;
        return GroupContext?.SelectAsync(Value) ?? Task.CompletedTask;
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        return GroupContext?.HandleKeyDownAsync(_index, args) ?? Task.CompletedTask;
    }
}
