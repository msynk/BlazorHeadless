using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A node of a <see cref="BhTree{TValue}"/> (<c>&lt;li role="treeitem"&gt;</c>). Put the
/// node's label first and its children in a <see cref="BhTreeGroup{TValue}"/>.
/// <c>aria-level</c>, <c>aria-setsize</c>, <c>aria-posinset</c>, <c>aria-expanded</c>,
/// <c>aria-selected</c> / <c>aria-checked</c> and a roving <c>tabindex</c> are managed for you.
///
/// <para><b>Styling hooks:</b> <c>data-state="open|closed"</c> (expandable items),
/// <c>data-selected</c>, <c>data-focused</c>, <c>data-disabled</c>, <c>data-level</c>,
/// <c>data-checked="true|false|mixed"</c>, and <c>--bh-tree-level</c> for indentation.</para>
/// </summary>
/// <typeparam name="TValue">The item value type.</typeparam>
public class BhTreeItem<TValue> : BhComponentBase, IBhCollectionItem, IDisposable
{
    [CascadingParameter]
    private BhTreeContext<TValue>? TreeContext { get; set; }

    [CascadingParameter]
    private BhTreeItem<TValue>? Parent { get; set; }

    /// <summary>The item's value.</summary>
    [Parameter, EditorRequired]
    public TValue Value { get; set; } = default!;

    /// <summary>Plain text used for typeahead. Defaults to the rendered text.</summary>
    [Parameter]
    public string? Label { get; set; }

    /// <summary>Disables the item.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Mark the item expandable before its children are loaded (lazy loading).</summary>
    [Parameter]
    public bool HasChildren { get; set; }

    /// <summary>The content.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Content that receives the render state (<c>BhTreeItemRenderContext&lt;TValue&gt;</c>). Use it instead of
    /// <see cref="ChildContent"/> when the markup depends on that state:
    /// <c>&lt;Template Context="state"&gt;…&lt;/Template&gt;</c>.
    /// </summary>
    [Parameter]
    public RenderFragment<BhTreeItemRenderContext<TValue>>? Template { get; set; }

    protected override string DefaultTag => "li";

    internal BhTreeItem<TValue>? ParentItem => Parent;

    private BhTree<TValue>? Tree => TreeContext?.Tree;

    /// <summary>Depth, starting at 1.</summary>
    public int Level => Parent is null ? 1 : Parent.Level + 1;

    /// <inheritdoc />
    public string ItemId => ComponentId;

    /// <inheritdoc />
    public bool IsItemDisabled => Disabled || (Tree?.IsTreeDisabled ?? false);

    /// <inheritdoc />
    public string? ItemText => Label ?? TreeContext?.Items.GetRenderedText(ItemId) ?? Value?.ToString();

    /// <summary>Whether the item has registered children or <see cref="HasChildren"/>.</summary>
    public bool IsExpandable => HasChildren || (TreeContext?.ChildrenOf(this).Any() ?? false);

    /// <summary>Whether every ancestor is expanded (the item is reachable).</summary>
    public bool AncestorsExpanded
    {
        get
        {
            for (var p = Parent; p is not null; p = p.Parent)
                if (TreeContext is null || !TreeContext.IsExpanded(p.Value)) return false;
            return true;
        }
    }

    private bool IsExpanded => TreeContext?.IsExpanded(Value) ?? false;

    internal void Refresh() => StateHasChanged();

    protected override void OnInitialized()
    {
        TreeContext?.Items.Register(this);
        Parent?.Refresh();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && TreeContext is not null)
            await TreeContext.Items.EnsureOrderAsync(Interop);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        var context = TreeContext;
        var checkedState = context is { Checkboxes: true } ? context.CheckedState(this) : null;
        var render = new BhTreeItemRenderContext<TValue>
        {
            Value = Value,
            Level = Level,
            IsExpandable = IsExpandable,
            IsExpanded = IsExpanded,
            IsSelected = context?.IsSelected(Value) ?? false,
            Checked = checkedState,
            IsFocused = context is not null && ReferenceEquals(context.FocusedItem, this),
            Disabled = IsItemDisabled,
            IsLoading = Tree?.IsLoading(Value) ?? false,
            ToggleExpanded = () => Tree?.ToggleExpandedAsync(this) ?? Task.CompletedTask,
        };

        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ItemId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onkeydown",
            EventCallback.Factory.Create<KeyboardEventArgs>(this, HandleKeyDown));
        builder.AddEventStopPropagationAttribute(31, "onkeydown", true);
        builder.AddAttribute(32, "onclick",
            EventCallback.Factory.Create<MouseEventArgs>(this, HandleClick));
        builder.AddEventStopPropagationAttribute(33, "onclick", true);
        builder.AddAttribute(34, "onfocus",
            EventCallback.Factory.Create<FocusEventArgs>(this, () => Tree?.OnItemFocused(this)));
        builder.AddEventStopPropagationAttribute(35, "onfocus", true);
        builder.AddElementReferenceCapture(40, CaptureElement);

        builder.OpenComponent<CascadingValue<BhTreeItem<TValue>>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", (RenderFragment)(inner =>
        {
            // The state changes every render: parts such as <BhTreeItemToggle /> re-render with it.
            inner.OpenComponent<CascadingValue<BhTreeItemRenderContext<TValue>>>(0);
            inner.AddComponentParameter(1, "Value", render);
            inner.AddComponentParameter(2, "ChildContent", BhContent.Pick(Template, ChildContent, render));
            inner.CloseComponent();
        }));
        builder.CloseComponent();

        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        var context = TreeContext;
        attrs["role"] = "treeitem";
        attrs["aria-level"] = Level;

        if (context is not null)
        {
            var siblings = context.ChildrenOf(Parent).ToList();
            attrs["aria-setsize"] = siblings.Count;
            attrs["aria-posinset"] = siblings.IndexOf(this) + 1;
            attrs["data-bh-item"] = context.Items.Key;
            attrs["tabindex"] = Tree?.IsTabStop(this) == true ? 0 : -1;

            if (IsExpandable)
            {
                SetAriaBool(attrs, "aria-expanded", IsExpanded);
                SetDataState(attrs, IsExpanded);
            }

            var selected = context.IsSelected(Value);
            if (context.Checkboxes)
            {
                var state = context.CheckedState(this);
                attrs["aria-checked"] = state is null ? "mixed" : state.Value ? "true" : "false";
                attrs["data-checked"] = state is null ? "mixed" : state.Value ? "true" : "false";
            }
            else if (context.SelectionMode != BhTreeSelectionMode.None)
            {
                SetAriaBool(attrs, "aria-selected", selected);
            }
            SetDataFlag(attrs, "selected", selected);
            SetDataFlag(attrs, "focused", ReferenceEquals(context.FocusedItem, this));
            if (Tree?.IsLoading(Value) == true) attrs["aria-busy"] = "true";
        }

        if (Label is not null) attrs["data-bh-text"] = Label;
        if (IsItemDisabled) attrs["aria-disabled"] = "true";
        attrs[BhKeyboard.KeysAttribute] = "ArrowUp ArrowDown ArrowLeft ArrowRight Home End Space Enter Ctrl+a Meta+a";
        attrs["style"] = $"--bh-tree-level:{Level}";
        SetDataValue(attrs, "level", Level.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetDataFlag(attrs, "disabled", IsItemDisabled);
        return attrs;
    }

    private Task HandleKeyDown(KeyboardEventArgs e) => Tree?.HandleKeyDownAsync(this, e) ?? Task.CompletedTask;

    private Task HandleClick(MouseEventArgs e) => Tree?.HandleClickAsync(this, e) ?? Task.CompletedTask;

    public void Dispose()
    {
        if (TreeContext is null) return;
        TreeContext.Items.Unregister(this);
        if (ReferenceEquals(TreeContext.FocusedItem, this)) TreeContext.FocusedItem = Parent;
    }
}
