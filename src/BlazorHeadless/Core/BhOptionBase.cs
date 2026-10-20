using System.ComponentModel;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// Shared behaviour of options in a <c>role="listbox"</c> popup
/// (<see cref="BhListboxOption{TValue}"/>, <see cref="BhComboboxOption{TValue}"/>):
/// registration, highlight on hover, selection on click without stealing focus
/// from the control, and the common ARIA / data attributes.
/// </summary>
/// <typeparam name="TValue">The option value type.</typeparam>
public abstract class BhOptionBase<TValue> : BhComponentBase, IBhCollectionItem, IDisposable
{
    private string? _attachedId;

    [CascadingParameter]
    private BhListState<TValue>? ListState { get; set; }

    /// <summary>The option's value.</summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    /// <summary>Disables this option. Disabled options are skipped by keyboard navigation and typeahead.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Plain-text label used for typeahead and the button / input display. Defaults
    /// to the option's rendered text.
    /// </summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Position in a data-driven (virtualized) list. Set by the options panel; don't set it yourself.</summary>
    [Parameter]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int DataIndex { get; set; } = -1;

    protected override string DefaultTag => "li";

    /// <summary>The shared list state.</summary>
    protected BhListState<TValue>? State => ListState;

    /// <summary>Whether the owning control is disabled.</summary>
    protected abstract bool RootDisabled { get; }

    /// <inheritdoc />
    public string ItemId => DataIndex >= 0 && ListState is { IsDataMode: true } ? ListState.DataItemId(DataIndex) : ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || RootDisabled;

    /// <inheritdoc />
    public string? ItemText => Label ?? ListState?.Options.GetRenderedText(ItemId) ?? Value?.ToString();

    /// <summary>Whether this option is highlighted.</summary>
    public bool IsActive => ListState is not null && ListState.ActiveId == ItemId;

    /// <summary>Whether this option's value is selected.</summary>
    public bool IsSelected => ListState?.IsSelected(Value) ?? false;

    /// <summary>Selects this option.</summary>
    protected abstract Task SelectAsync();

    /// <summary>Renders the option's content.</summary>
    protected abstract void RenderContent(RenderTreeBuilder builder, int sequence);

    internal void Refresh() => StateHasChanged();

    protected override void OnParametersSet()
    {
        // Virtualized options are recycled for different indexes: re-attach under the new id.
        if (_attachedId != ItemId && ListState is not null)
        {
            if (_attachedId is not null) ListState.Detach(this, _attachedId);
            ListState.Attach(this);
            _attachedId = ItemId;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && ListState is not null)
            await ListState.EnsureOrderAsync();
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        // Keep focus on the button / input while clicking an option.
        builder.AddAttribute(31, "onmousedown", EventCallback.Factory.Create<MouseEventArgs>(this, static () => { }));
        builder.AddEventPreventDefaultAttribute(32, "onmousedown", true);
        builder.AddAttribute(33, "onpointerenter",
            EventCallback.Factory.Create<PointerEventArgs>(this, HandlePointerEnter));
        builder.AddElementReferenceCapture(40, CaptureElement);
        RenderContent(builder, 50);
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["role"] = "option";
        SetAriaBool(attrs, "aria-selected", IsSelected);
        if (IsItemDisabled)
            attrs["aria-disabled"] = "true";
        if (ListState is not null && !ListState.IsDataMode)
            attrs["data-bh-item"] = ListState.Key;
        if (ListState is { IsDataMode: true } && DataIndex >= 0)
        {
            attrs["aria-setsize"] = ListState.Count;
            attrs["aria-posinset"] = DataIndex + 1;
        }
        if (Label is not null)
            attrs["data-bh-text"] = Label;
        SetDataFlag(attrs, "active", IsActive);
        SetDataFlag(attrs, "highlighted", IsActive);
        SetDataFlag(attrs, "selected", IsSelected);
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    private Task HandleClick(MouseEventArgs _)
        => IsItemDisabled ? Task.CompletedTask : SelectAsync();

    private void HandlePointerEnter(PointerEventArgs args)
    {
        if (IsItemDisabled || ListState is null || args.PointerType == "touch") return;
        ListState.SetActiveId(ItemId, scroll: false);
    }

    public virtual void Dispose()
    {
        if (ListState is null || _attachedId is null) return;
        ListState.Detach(this, _attachedId);
        if (ListState.ActiveId == _attachedId && !ListState.IsDataMode)
            ListState.SetActiveId(null, scroll: false);
    }
}
