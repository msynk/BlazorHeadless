using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A single option inside an <see cref="HComboboxOptions{TValue}"/>. Renders as
/// <c>&lt;li role="option"&gt;</c> by default.
///
/// <para><b>Behaviour:</b></para>
/// <list type="bullet">
///   <item>Mouse-down (intentionally not click — see remarks) selects the option.</item>
///   <item>Mouse-enter sets this option as the "active" highlight.</item>
///   <item>Sets <c>aria-selected</c>; emits <c>data-active</c>, <c>data-selected</c>, <c>data-disabled</c>.</item>
/// </list>
///
/// <para>
/// <b>Why mouse-down instead of click?</b> The input has focus while the panel is
/// open. A normal click on an option would first fire <c>blur</c> on the input,
/// which closes the panel before the click registers. Using <c>mousedown</c>
/// (which fires before blur) plus <c>preventDefault</c> on the option's
/// click event prevents that focus-loss race.
/// </para>
/// </summary>
public class HComboboxOption<TValue> : HeadlessComponentBase, IDisposable
{
    [CascadingParameter]
    private ComboboxContext<TValue> ComboboxContext { get; set; } = default!;

    /// <summary>The option's value. Required.</summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    /// <summary>Disables this option.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Content template receiving <see cref="ComboboxOptionRenderContext{TValue}"/>.</summary>
    [Parameter]
    public RenderFragment<ComboboxOptionRenderContext<TValue>>? ChildContent { get; set; }

    private int _index = -1;

    protected override string DefaultTag => "li";

    /// <summary>Whether this option is in the current selection.</summary>
    public bool IsSelected => ComboboxContext?.IsSelected(Value) ?? false;

    /// <summary>Whether this option is the currently "active" (highlighted) option.</summary>
    public bool IsActive => ComboboxContext?.ActiveIndex == _index;

    /// <summary>Whether this option is disabled (own flag or root combobox).</summary>
    public bool IsOptionDisabled => Disabled || (ComboboxContext?.Disabled ?? false);

    private ComboboxOptionRenderContext<TValue> RenderContext => new()
    {
        Value = Value,
        IsSelected = IsSelected,
        IsActive = IsActive,
        Disabled = IsOptionDisabled,
    };

    protected override void OnInitialized()
    {
        if (ComboboxContext is not null)
            _index = ComboboxContext.RegisterOption(this);
    }

    public void Dispose()
    {
        ComboboxContext?.UnregisterOption(this);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComboboxContext is not null ? ComboboxContext.GetOptionId(_index) : ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());

        // Mouse-down so we beat the input's blur event. preventDefault on
        // mousedown also stops the input from losing focus when the option is clicked.
        builder.AddAttribute(30, "onmousedown",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseDown));
        builder.AddAttribute(31, "onmousedown:preventDefault", true);
        builder.AddAttribute(32, "onmouseenter",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleMouseEnter));

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

    private Task HandleMouseDown(MouseEventArgs _)
    {
        if (IsOptionDisabled) return Task.CompletedTask;
        return ComboboxContext?.SelectAsync(Value) ?? Task.CompletedTask;
    }

    private void HandleMouseEnter(MouseEventArgs _)
    {
        if (IsOptionDisabled) return;
        ComboboxContext?.SetActiveIndex(_index);
    }
}
