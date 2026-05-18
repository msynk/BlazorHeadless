using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A single option inside an <see cref="HListboxOptions{TValue}"/>. Renders as
/// <c>&lt;li role="option"&gt;</c> by default.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Click selects the option (and closes the panel in single-select mode).</item>
///   <item>Mouse-enter sets this option as the "active" highlight.</item>
///   <item>Sets <c>aria-selected</c> based on whether this option's value is in the current selection.</item>
///   <item>Emits <c>data-active</c>, <c>data-selected</c>, <c>data-disabled</c> for CSS hooks.</item>
///   <item>Render-prop via <see cref="ChildContent"/> exposes <see cref="ListboxOptionRenderContext{TValue}"/>.</item>
/// </list>
/// </summary>
/// <typeparam name="TValue">The option value type. Must match the parent <see cref="HListbox{TValue}"/>.</typeparam>
public class HListboxOption<TValue> : HeadlessComponentBase, IDisposable
{
    [CascadingParameter]
    private ListboxContext<TValue> ListboxContext { get; set; } = default!;

    /// <summary>The option's value. Required.</summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    /// <summary>Disables this option. Disabled options are skipped by arrow-key nav and typeahead.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Optional plain-text label used by the typeahead matcher. When omitted the
    /// matcher falls back to the option's <see cref="Value"/>.<c>ToString()</c>.
    /// Useful when <see cref="ChildContent"/> contains rich markup.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Content template receiving <see cref="ListboxOptionRenderContext{TValue}"/>.</summary>
    [Parameter]
    public RenderFragment<ListboxOptionRenderContext<TValue>>? ChildContent { get; set; }

    private int _index = -1;

    protected override string DefaultTag => "li";

    /// <summary>Whether this option's value is part of the current selection.</summary>
    public bool IsSelected => ListboxContext?.IsSelected(Value) ?? false;

    /// <summary>Whether this option is the currently "active" (highlighted) option.</summary>
    public bool IsActive => ListboxContext?.ActiveIndex == _index;

    /// <summary>Whether this option is disabled (own flag or root listbox).</summary>
    public bool IsOptionDisabled => Disabled || (ListboxContext?.Disabled ?? false);

    private ListboxOptionRenderContext<TValue> RenderContext => new()
    {
        Value = Value,
        IsSelected = IsSelected,
        IsActive = IsActive,
        Disabled = IsOptionDisabled,
    };

    /// <summary>Returns the textual label used by the typeahead matcher.</summary>
    internal string? GetTextLabel() => Label ?? Value?.ToString();

    // ── Lifecycle ────────────────────────────────────────────────────────────

    protected override void OnInitialized()
    {
        if (ListboxContext is not null)
            _index = ListboxContext.RegisterOption(this);
    }

    public void Dispose()
    {
        ListboxContext?.UnregisterOption(this);
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ListboxContext is not null ? ListboxContext.GetOptionId(_index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddAttribute(31, "onmouseenter",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));
        builder.AddAttribute(32, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));

        if (Ref is not null)
            builder.AddElementReferenceCapture(40, Ref);

        if (ChildContent is not null)
            builder.AddContent(50, ChildContent(RenderContext));

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();

        attrs["role"] = "option";
        attrs["aria-selected"] = IsSelected ? "true" : "false";

        if (IsOptionDisabled)
            attrs["aria-disabled"] = "true";

        SetDataFlag(attrs, "active", IsActive);
        SetDataFlag(attrs, "selected", IsSelected);
        SetDataFlag(attrs, "disabled", IsOptionDisabled);

        return attrs;
    }

    // ── Event handling ───────────────────────────────────────────────────────

    private Task HandleClick(MouseEventArgs _)
    {
        if (IsOptionDisabled) return Task.CompletedTask;
        return ListboxContext?.SelectAsync(Value) ?? Task.CompletedTask;
    }

    private void HandleMouseEnter(MouseEventArgs _)
    {
        if (IsOptionDisabled) return;
        ListboxContext?.SetActiveIndex(_index);
    }

    private Task HandleKeyDown(KeyboardEventArgs args)
    {
        return ListboxContext?.HandleOptionKeyDownAsync(_index, args) ?? Task.CompletedTask;
    }
}
