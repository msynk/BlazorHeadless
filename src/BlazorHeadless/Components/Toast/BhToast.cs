using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorHeadless;

/// <summary>
/// A single notification inside a <see cref="BhToastViewport"/>. Announced through the
/// shared live region (politely, or assertively for foreground toasts), auto-dismissed
/// after its duration — paused while hovered or focused — and dismissable with
/// Escape or a <see cref="BhToastClose"/>. Emits <c>data-state="open|closed"</c>
/// (exit animations supported).
/// </summary>
public class BhToast : BhComponentBase, IDisposable
{
    private BhDelayedAction? _timer;
    private bool _paused;
    private bool _closing;

    [CascadingParameter]
    private BhToastViewport? Viewport { get; set; }

    /// <summary>The toast to render (the viewport template's context).</summary>
    [Parameter, EditorRequired]
    public BhToastItem Item { get; set; } = default!;

    /// <summary>Title, description, action and close parts.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "li";

    private int Duration => Item.Options.Duration ?? Viewport?.Duration ?? 5000;

    protected override void OnInitialized() => StartTimer();

    private void StartTimer()
    {
        if (Duration <= 0 || _paused) return;
        _timer ??= new BhDelayedAction(a => InvokeAsync(a));
        _timer.Schedule(Duration, DismissAsync);
    }

    /// <summary>Dismisses this toast (after its exit animation).</summary>
    public async Task DismissAsync()
    {
        if (_closing) return;
        _closing = true;
        _timer?.Cancel();
        StateHasChanged();
        await Interop.WaitForExitAsync(ComponentId);
        Viewport?.EffectiveToaster.Dismiss(Item.Id);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddAttribute(30, "onpointerenter", EventCallback.Factory.Create<PointerEventArgs>(this, Pause));
        builder.AddAttribute(31, "onpointerleave", EventCallback.Factory.Create<PointerEventArgs>(this, Resume));
        builder.AddAttribute(32, "onfocusin", EventCallback.Factory.Create<FocusEventArgs>(this, Pause));
        builder.AddAttribute(33, "onfocusout", EventCallback.Factory.Create<FocusEventArgs>(this, Resume));
        builder.AddAttribute(34, "onkeydown", EventCallback.Factory.Create<KeyboardEventArgs>(this, e => e.Key == "Escape" ? DismissAsync() : Task.CompletedTask));
        builder.AddElementReferenceCapture(40, CaptureElement);
        builder.OpenComponent<CascadingValue<BhToast>>(50);
        builder.AddComponentParameter(51, "Value", this);
        builder.AddComponentParameter(52, "IsFixed", true);
        builder.AddComponentParameter(53, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        attrs["tabindex"] = 0;
        attrs["data-type"] = Item.Options.Foreground ? "foreground" : "background";
        SetDataState(attrs, !_closing);
        SetDataFlag(attrs, "paused", _paused);
        return attrs;
    }

    private void Pause()
    {
        _paused = true;
        _timer?.Cancel();
    }

    private void Resume()
    {
        _paused = false;
        if (!_closing) StartTimer();
    }

    internal async Task RunActionAsync()
    {
        if (Item.Options.OnAction is not null) await Item.Options.OnAction();
        await DismissAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Newly inserted live regions are unreliable across screen readers, so the
        // toast is announced through the shared announcer (which exists up front).
        if (!firstRender) return;
        var text = string.Join(". ", new[] { Item.Title, Item.Description }.Where(t => !string.IsNullOrWhiteSpace(t)));
        if (text.Length == 0) return;
        var announcer = GetService<IBhAnnouncer>() ?? new BhAnnouncer(Interop);
        await announcer.AnnounceAsync(text, Item.Options.Foreground ? BhPoliteness.Assertive : BhPoliteness.Polite);
    }

    public void Dispose() => _timer?.Dispose();
}
