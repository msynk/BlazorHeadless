using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless, accessible Checkbox primitive supporting the standard
/// checked / unchecked / indeterminate tri-state.
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Tri-state</b> — checked, unchecked, or <see cref="Indeterminate"/> (mixed). Clicking an indeterminate checkbox advances it to checked.</item>
///   <item><b>Uncontrolled and controlled</b> — seed with <see cref="DefaultChecked"/> or drive with <see cref="Checked"/> + <see cref="OnCheckedChange"/>.</item>
///   <item><b>Polymorphic</b> — renders as <c>&lt;button&gt;</c> by default; override with <see cref="HeadlessComponentBase.As"/>.</item>
///   <item><b>Accessible</b> — emits <c>role="checkbox"</c> and <c>aria-checked="true|false|mixed"</c>. Space toggles (per ARIA spec; Enter does not).</item>
///   <item><b>Form submission</b> — when <see cref="Name"/> is set the component renders a sibling <c>&lt;input type="hidden"&gt;</c> while checked, so the value participates in plain HTML form posts.</item>
///   <item><b>Data attributes</b> — emits <c>data-state="checked"|"unchecked"|"indeterminate"</c> and <c>data-disabled</c>.</item>
/// </list>
///
/// <para><b>Usage (uncontrolled):</b></para>
/// <code>
/// &lt;HCheckbox class="checkbox" Context="c"&gt;
///     @if (c.IsIndeterminate) { &lt;span class="dash"&gt;–&lt;/span&gt; }
///     else if (c.IsChecked)   { &lt;span class="tick"&gt;✓&lt;/span&gt; }
/// &lt;/HCheckbox&gt;
/// </code>
/// </summary>
public class HCheckbox : HeadlessComponentBase
{
    private bool _isChecked;
    private bool _initialized;

    /// <summary>Initial checked value when uncontrolled. Ignored when <see cref="Checked"/> is supplied.</summary>
    [Parameter]
    public bool DefaultChecked { get; set; }

    /// <summary>
    /// Controlled checked value. When non-null the component runs in controlled mode
    /// and <see cref="OnCheckedChange"/> must update this value.
    /// </summary>
    [Parameter]
    public bool? Checked { get; set; }

    /// <summary>Fires whenever the checked state changes.</summary>
    [Parameter]
    public EventCallback<bool> OnCheckedChange { get; set; }

    /// <summary>
    /// Whether the checkbox is in the mixed (indeterminate) state. Display-only — clicking
    /// an indeterminate checkbox advances it to <c>checked = true</c> and clears indeterminate.
    /// Indeterminate takes visual precedence over <see cref="Checked"/>; <c>aria-checked="mixed"</c> is emitted.
    /// </summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>Disables the checkbox.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Marks the checkbox as required (sets <c>aria-required="true"</c> for assistive tech).</summary>
    [Parameter]
    public bool Required { get; set; }

    /// <summary>
    /// Optional form field name. When set the component renders an adjacent
    /// <c>&lt;input type="hidden"&gt;</c> with this name and <see cref="Value"/>
    /// while checked, so plain HTML form posts include the checkbox's state.
    /// </summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>
    /// Value sent in the hidden form input when checked. Defaults to <c>"on"</c>,
    /// matching native <c>&lt;input type="checkbox"&gt;</c>.
    /// </summary>
    [Parameter]
    public string Value { get; set; } = "on";

    /// <summary>
    /// Content template receiving <see cref="CheckboxRenderContext"/> for state-driven rendering
    /// of check marks, indeterminate dashes, etc.
    /// </summary>
    [Parameter]
    public RenderFragment<CheckboxRenderContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private bool IsChecked => Checked ?? _isChecked;

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase);

    private CheckboxRenderContext RenderContext => new()
    {
        IsChecked = IsChecked,
        IsIndeterminate = Indeterminate,
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
            // Native buttons handle Space natively — only attach a keydown handler
            // for non-native elements. (Enter does not toggle a checkbox per ARIA spec.)
            builder.AddAttribute(40, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(50, Ref);

        if (ChildContent is not null)
            builder.AddContent(60, ChildContent(RenderContext));

        builder.CloseElement();

        // Hidden input so checkbox state participates in plain HTML form posts.
        // Per native <input type="checkbox"> behaviour, indeterminate values still
        // submit (they're effectively "checked" until interacted with).
        if (!string.IsNullOrEmpty(Name) && (IsChecked || Indeterminate))
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

        attrs["role"] = "checkbox";
        attrs["aria-checked"] = Indeterminate ? "mixed" : (IsChecked ? "true" : "false");

        if (Required)
            attrs["aria-required"] = "true";

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

        if (Indeterminate)
            SetDataValue(attrs, "state", "indeterminate");
        else
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
        // ARIA spec: Space toggles role="checkbox". Enter does not.
        if (args.Key == " ")
            return Toggle();
        return Task.CompletedTask;
    }

    private Task Toggle()
    {
        // From indeterminate → checked, otherwise flip current value.
        var next = Indeterminate ? true : !IsChecked;

        if (Checked is null)
            _isChecked = next;

        StateHasChanged();
        return OnCheckedChange.InvokeAsync(next);
    }
}
