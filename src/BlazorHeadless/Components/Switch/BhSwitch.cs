using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A headless two-state on/off Switch primitive. Behaviour and accessibility are built in;
/// styling is entirely yours.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item>
///     <b>Uncontrolled and controlled</b> — seed with <see cref="DefaultChecked"/>,
///     or drive externally with <see cref="Checked"/> + <see cref="OnCheckedChange"/>.
///   </item>
///   <item>
///     <b>Polymorphic</b> — renders as <c>&lt;button&gt;</c> by default. Set
///     <see cref="BhComponentBase.As"/> to render as <c>&lt;div&gt;</c>,
///     <c>&lt;span&gt;</c>, etc. — non-native elements receive
///     <c>tabindex</c>, <c>role="switch"</c>, and Space/Enter keyboard handling.
///   </item>
///   <item>
///     <b>Accessible</b> — emits <c>role="switch"</c> and <c>aria-checked</c>.
///     <c>aria-disabled</c> is set on non-native elements when disabled.
///   </item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> receives a
///     <see cref="BhSwitchRenderContext"/> for state-driven rendering of
///     thumb/track/icons.
///   </item>
///   <item>
///     <b>Data attributes</b> — emits <c>data-state="checked"|"unchecked"</c> and
///     <c>data-disabled</c> for CSS-driven styling.
///   </item>
///   <item>
///     <b>Form submission</b> — when <see cref="Name"/> is set the component renders
///     a sibling <c>&lt;input type="hidden"&gt;</c> carrying <see cref="Value"/>
///     when checked, so the switch participates in plain HTML form submits.
///   </item>
/// </list>
///
/// <para><b>Usage (uncontrolled):</b></para>
/// <code>
/// &lt;BhSwitch DefaultChecked="true" class="switch" Context="s"&gt;
///     &lt;span class="switch-thumb" data-state="@(s.IsChecked ? "checked" : "unchecked")"&gt;&lt;/span&gt;
/// &lt;/BhSwitch&gt;
/// </code>
/// </summary>
public class BhSwitch : BhComponentBase
{
    private bool _isChecked;
    private bool _initialized;

    /// <summary>Whether the switch is initially checked (uncontrolled). Ignored when <see cref="Checked"/> is supplied.</summary>
    [Parameter]
    public bool DefaultChecked { get; set; }

    /// <summary>
    /// Controlled checked state. When non-null the component runs in controlled mode
    /// and <see cref="OnCheckedChange"/> must update this value.
    /// </summary>
    [Parameter]
    public bool? Checked { get; set; }

    /// <summary>Fires whenever the checked state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnCheckedChange { get; set; }

    /// <summary>Disables the switch.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional form field name. When set the component renders an adjacent
    /// <c>&lt;input type="hidden"&gt;</c> with this name and <see cref="Value"/>
    /// while checked, so plain HTML form posts include the switch's state.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Value sent in the hidden form input when <see cref="Name"/> is set and the switch is checked.
    /// Defaults to <c>"on"</c>, matching native <c>&lt;input type="checkbox"&gt;</c>.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = "on";

    /// <summary>
    /// Content template receiving <see cref="BhSwitchRenderContext"/> for state-driven rendering.
    /// Plain content (without referencing context) works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<BhSwitchRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsChecked => Checked ?? _isChecked;

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    private BhSwitchRenderContext RenderContext => new()
    {
        IsChecked = IsChecked,
        Disabled = Disabled,
    };

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (Checked is null)
            _isChecked = DefaultChecked;
        _initialized = true;
    }

    protected override void OnParametersSet()
    {
        // In controlled mode the public Checked property is the source of truth;
        // _isChecked is unused.
        _ = _initialized;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));

        if (!IsNativeButton)
        {
            builder.AddAttribute(40, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(50, Ref);

        if (ChildContent is not null)
            builder.AddContent(60, ChildContent(RenderContext));

        builder.CloseElement();

        // Hidden input so the switch state participates in plain HTML form posts.
        if (!string.IsNullOrEmpty(Name) && IsChecked)
        {
            builder.OpenElement(70, "input");
            builder.AddAttribute(71, "type", "hidden");
            builder.AddAttribute(72, "name", Name);
            builder.AddAttribute(73, "value", Value);
            builder.CloseElement();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "switch";
        attrs["aria-checked"] = IsChecked ? "true" : "false";

        if (IsNativeButton)
        {
            attrs["type"] = "button";
            if (Disabled)
                attrs["disabled"] = true;
        }
        else
        {
            attrs["tabindex"] = Disabled ? -1 : 0;
            if (Disabled)
                attrs["aria-disabled"] = "true";
        }

        SetDataState(attrs, IsChecked, "checked", "unchecked");
        SetDataFlag(attrs, "disabled", Disabled);

        return attrs;
    }

    // ── State changes ────────────────────────────────────────────────────────

    private Task HandleClick(MouseEventArgs _)
    {
        if (Disabled) return Task.CompletedTask;
        return Toggle();
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        if (Disabled) return Task.CompletedTask;
        if (args.Key is " " or "Enter")
            return Toggle();
        return Task.CompletedTask;
    }

    private Task Toggle()
    {
        var next = !IsChecked;
        if (Checked is null)
            _isChecked = next;
        StateHasChanged();
        return OnCheckedChange.InvokeAsync(next);
    }
}
