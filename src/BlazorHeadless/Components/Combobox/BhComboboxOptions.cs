using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

namespace BlazorHeadless;

/// <summary>
/// The popup containing a <see cref="BhCombobox{TValue}"/>'s options
/// (<c>role="listbox"</c>). Hidden while closed.
///
/// <para>Options come from <see cref="BhComboboxOption{TValue}"/> children, or from
/// <see cref="Items"/> + <see cref="ItemTemplate"/> (optionally <see cref="Virtualize"/>d —
/// give the panel a fixed height with <c>overflow:auto</c> and options a fixed
/// <see cref="ItemSize"/>).</para>
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public class BhComboboxOptions<TValue> : BhFloatingContentBase
{
    private int _contentVersion;

    [CascadingParameter]
    private BhComboboxContext<TValue>? ComboboxContext { get; set; }

    /// <summary>Option components.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Data-driven options (alternative to child components).</summary>
    [Parameter]
    public IReadOnlyList<TValue>? Items { get; set; }

    /// <summary>Renders the content of each data-driven option.</summary>
    [Parameter]
    public RenderFragment<BhComboboxOptionRenderContext<TValue>>? ItemTemplate { get; set; }

    /// <summary>Returns whether a data-driven option is disabled.</summary>
    [Parameter]
    public Func<TValue, bool>? ItemDisabled { get; set; }

    /// <summary>Returns the text of a data-driven option. Defaults to the combobox's display value.</summary>
    [Parameter]
    public Func<TValue, string?>? ItemText { get; set; }

    /// <summary>Virtualize data-driven options.</summary>
    [Parameter]
    public bool Virtualize { get; set; }

    /// <summary>Height (px) of each option when virtualized. Default: 36.</summary>
    [Parameter]
    public float ItemSize { get; set; } = 36;

    /// <summary>Extra options rendered above / below the viewport when virtualized. Default: 5.</summary>
    [Parameter]
    public int OverscanCount { get; set; } = 5;

    /// <summary>Keep the panel open (rendered) even when there are no options, e.g. to show <see cref="BhComboboxEmpty{TValue}"/>. Default: true.</summary>
    [Parameter]
    public bool ShowWhenEmpty { get; set; } = true;

    protected override string DefaultTag => "ul";

    protected override bool IsOpen => (ComboboxContext?.IsOpen ?? false) && (ShowWhenEmpty || (ComboboxContext?.State.Count ?? 0) > 0);

    private string PanelId => ComboboxContext?.OptionsId ?? ComponentId;

    protected override void OnParametersSet()
    {
        _contentVersion++;
        var context = ComboboxContext;
        ComboboxContext?.State.SetItems(Items, ItemDisabled, ItemText ?? (context is null ? null : v => context.DisplayValue(v)), ItemSize);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (ComboboxContext?.IsOpen == true)
        {
            await ComboboxContext.State.EnsureOrderAsync();
            ComboboxContext.Combobox.ReconcileHighlight();
        }
    }

    protected override BhFloatingRequest CreateFloatingRequest()
    {
        var layer = new BhLayerOptions { ParentHandle = ParentLayerHandle };
        layer.BranchIds.Add(ComboboxContext?.InputId);
        layer.BranchIds.Add(ComboboxContext?.ButtonId);
        return new BhFloatingRequest
        {
            ContentId = PanelId,
            ReferenceId = ComboboxContext?.InputId,
            Anchor = EffectiveAnchor,
            ArrowId = ArrowId,
            Portal = EffectivePortal,
            PortalTarget = PortalTarget,
            Layer = layer,
        };
    }

    protected override Task OnDismissAsync(BhDismissReason reason)
        => ComboboxContext?.CloseAsync() ?? Task.CompletedTask;

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (!ShouldRenderContent) return;

        Scope.Owner = PanelId;

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", PanelId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        if (IsHidden)
            builder.AddAttribute(30, "hidden", true);
        builder.AddElementReferenceCapture(40, CaptureElement);
        BhFloatingScope.Render(builder, 50, Scope, (RenderFragment)(inner =>
            BhRenderBoundary.Render(inner, 0, _contentVersion, RenderOptions)));
        builder.CloseElement();
    }

    private void RenderOptions(RenderTreeBuilder builder)
    {
        if (Items is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        if (Virtualize)
        {
            builder.OpenComponent<Virtualize<BhIndexed<TValue>>>(1);
            builder.AddComponentParameter(2, nameof(Virtualize<BhIndexed<TValue>>.Items), new BhIndexedList<TValue>(Items));
            builder.AddComponentParameter(3, nameof(Virtualize<BhIndexed<TValue>>.ItemSize), ItemSize);
            builder.AddComponentParameter(4, nameof(Virtualize<BhIndexed<TValue>>.OverscanCount), OverscanCount);
            builder.AddComponentParameter(5, nameof(Virtualize<BhIndexed<TValue>>.SpacerElement), Tag.Equals("ul", StringComparison.OrdinalIgnoreCase) ? "li" : "div");
            builder.AddComponentParameter(6, nameof(Virtualize<BhIndexed<TValue>>.ItemContent), (RenderFragment<BhIndexed<TValue>>)(entry => b => RenderDataOption(b, entry.Index, entry.Value)));
            builder.CloseComponent();
        }
        else
        {
            for (var i = 0; i < Items.Count; i++)
                RenderDataOption(builder, i, Items[i]);
        }

        // Child content (e.g. BhComboboxEmpty) still renders after data options.
        builder.AddContent(20, ChildContent);
    }

    private void RenderDataOption(RenderTreeBuilder builder, int index, TValue value)
    {
        builder.OpenComponent<BhComboboxOption<TValue>>(10);
        builder.SetKey(index);
        builder.AddComponentParameter(11, nameof(BhComboboxOption<TValue>.Value), value);
        builder.AddComponentParameter(12, nameof(BhComboboxOption<TValue>.DataIndex), index);
        if (ItemDisabled is not null)
            builder.AddComponentParameter(13, nameof(BhComboboxOption<TValue>.Disabled), ItemDisabled(value));
        if (ItemTemplate is not null)
            builder.AddComponentParameter(14, nameof(BhComboboxOption<TValue>.Template), ItemTemplate);
        builder.CloseComponent();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "listbox";
        if (ComboboxContext is not null)
            attrs["aria-labelledby"] = ComboboxContext.Field?.LabelledBy ?? ComboboxContext.InputId;
        if (ComboboxContext?.Multiple == true)
            attrs["aria-multiselectable"] = "true";
        SetDataState(attrs, IsOpen);
        return attrs;
    }
}
