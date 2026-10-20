using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// Registers a global keyboard shortcut while rendered. <c>mod</c> means ⌘ on Apple
/// platforms and Ctrl elsewhere. Renders nothing.
///
/// <code>
/// &lt;BhHotkey Keys="mod+k" OnTriggered="() => paletteOpen = true" /&gt;
/// &lt;BhHotkey Keys="shift+?" OnTriggered="ShowShortcuts" /&gt;
/// </code>
///
/// <para>Shortcuts without Ctrl / ⌘ are ignored while typing in a text field unless
/// <see cref="AllowInInputs"/> is set. Remember to document shortcuts for your users
/// and avoid overriding common browser / assistive-technology keys.</para>
/// </summary>
public sealed class BhHotkey : ComponentBase, IAsyncDisposable
{
    private int _handle;
    private string? _registered;
    private DotNetObjectReference<BhHotkey>? _selfRef;

    [Inject] private IServiceProvider Services { get; set; } = default!;
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>One or more comma-separated shortcuts, e.g. <c>"mod+k"</c> or <c>"mod+k, /"</c>.</summary>
    [Parameter, EditorRequired]
    public string Keys { get; set; } = string.Empty;

    /// <summary>Invoked when the shortcut is pressed.</summary>
    [Parameter]
    public EventCallback OnTriggered { get; set; }

    /// <summary>Whether the shortcut is active. Default: true.</summary>
    [Parameter]
    public bool Enabled { get; set; } = true;

    /// <summary>Also trigger while focus is in a text field.</summary>
    [Parameter]
    public bool AllowInInputs { get; set; }

    private BhInterop? _interop;
    private BhInterop Interop => _interop ??= BhInterop.Resolve(Services, JS);

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var key = Enabled ? $"{Keys}|{AllowInInputs}" : null;
        if (key == _registered) return;
        if (_handle > 0)
        {
            await Interop.HotkeyUnregisterAsync(_handle);
            _handle = 0;
        }
        _registered = key;
        if (key is null || string.IsNullOrWhiteSpace(Keys)) return;
        _selfRef ??= DotNetObjectReference.Create(this);
        var combos = Keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        _handle = await Interop.HotkeyRegisterAsync(_selfRef, combos, AllowInInputs);
    }

    /// <summary>Invoked by the JS runtime.</summary>
    [JSInvokable]
    public Task OnHotkey() => OnTriggered.InvokeAsync();

    public async ValueTask DisposeAsync()
    {
        if (_handle > 0) await Interop.HotkeyUnregisterAsync(_handle);
        _selfRef?.Dispose();
    }
}
