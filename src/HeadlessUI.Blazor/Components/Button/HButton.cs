using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace HeadlessUI.Blazor;

/// <summary>
/// A headless Button component that provides behaviour, accessibility, and
/// state management without any visual opinion.
///
/// <para><b>Key features (inspired by Radix UI / Ark UI patterns):</b></para>
/// <list type="bullet">
///   <item>
///     <b>Polymorphic rendering</b> — renders as &lt;button&gt; by default;
///     set <see cref="HeadlessComponentBase.As"/> to "a", "div", etc.
///     Non-native elements automatically receive <c>role="button"</c>,
///     <c>tabindex</c>, and keyboard handling (Enter / Space).
///   </item>
///   <item>
///     <b>Data-attribute styling hooks</b> — emits <c>data-disabled</c> and
///     <c>data-loading</c> so consumers can style with pure CSS selectors
///     like <c>[data-disabled]</c> instead of toggling class names.
///   </item>
///   <item>
///     <b>Accessible by default</b> — native disabled, aria-disabled,
///     aria-busy are managed internally based on component state.
///   </item>
///   <item>
///     <b>Render-prop context</b> — <see cref="ChildContent"/> receives a
///     <see cref="ButtonContext"/> enabling conditional UI (spinners, icons)
///     driven by the button's own state.
///   </item>
/// </list>
///
/// <para><b>Usage:</b></para>
/// <code>
/// &lt;HButton OnClick="Save" Loading="@isSaving" Context="btn"&gt;
///     @if (btn.Loading) { &lt;span&gt;Saving…&lt;/span&gt; }
///     else              { &lt;span&gt;Save&lt;/span&gt; }
/// &lt;/HButton&gt;
/// </code>
/// </summary>
public class HButton : HeadlessComponentBase
{
    /// <summary>Whether the button is disabled. Prevents clicks and keyboard activation.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether the button is in a loading state. Implies disabled behaviour and sets aria-busy.</summary>
    [Parameter]
    public bool Loading { get; set; }

    /// <summary>HTML button type attribute. Ignored when <see cref="HeadlessComponentBase.As"/> is not "button" or "input".</summary>
    [Parameter]
    public string Type { get; set; } = "button";

    /// <summary>Click handler. Not invoked when <see cref="Disabled"/> or <see cref="Loading"/> is true.</summary>
    [Parameter]
    public EventCallback<MouseEventArgs> OnClick { get; set; }

    /// <summary>
    /// Content template receiving <see cref="ButtonContext"/> for state-driven rendering.
    /// Plain content (without referencing context) works equally well.
    /// </summary>
    [Parameter]
    public RenderFragment<ButtonContext>? ChildContent { get; set; }

    protected override string DefaultTag => "button";

    private ButtonContext Context => new()
    {
        Disabled = Disabled,
        Loading = Loading,
        Type = Type
    };

    private bool IsNativeButton =>
        Tag.Equals("button", StringComparison.OrdinalIgnoreCase)
        || Tag.Equals("input", StringComparison.OrdinalIgnoreCase);

    private bool IsInteractive => !Disabled && !Loading;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);

        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, BuildAttributes());

        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClickAsync));

        if (!IsNativeButton)
        {
            builder.AddAttribute(40, "onkeydown",
                EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDownAsync));
        }

        if (Ref is not null)
            builder.AddElementReferenceCapture(50, Ref);

        if (ChildContent is not null)
            builder.AddContent(60, ChildContent(Context));

        builder.CloseElement();
    }

    private Dictionary<string, object> BuildAttributes()
    {
        var attrs = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        if (IsNativeButton)
        {
            attrs["type"] = Type;
            if (!IsInteractive)
                attrs["disabled"] = true;
        }
        else
        {
            attrs["role"] = "button";
            attrs["tabindex"] = IsInteractive ? 0 : -1;
            if (Disabled)
                attrs["aria-disabled"] = "true";
        }

        if (Loading)
            attrs["aria-busy"] = "true";

        AttributeUtilities.SetDataAttribute(attrs, "disabled", Disabled);
        AttributeUtilities.SetDataAttribute(attrs, "loading", Loading);

        return MergeAttributes(attrs);
    }

    private async Task HandleClickAsync(MouseEventArgs args)
    {
        if (!IsInteractive) return;
        await OnClick.InvokeAsync(args);
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs args)
    {
        if (!IsInteractive) return;

        if (args.Key is "Enter" or " ")
            await OnClick.InvokeAsync(new MouseEventArgs());
    }
}
