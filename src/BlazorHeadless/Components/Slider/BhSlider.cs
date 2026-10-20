using System.Globalization;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorHeadless;

/// <summary>
/// A headless Slider for picking a value — or a range, with several thumbs —
/// within a min / max (WAI-ARIA Slider and Multi-Thumb Slider patterns).
///
/// <para><b>Key features:</b></para>
/// <list type="bullet">
///   <item><b>Single or range</b> — <c>@bind-Value</c>, or <c>@bind-Values</c> with one <see cref="BhSliderThumb"/> per value.</item>
///   <item><b>Keyboard</b> — arrows (± <see cref="Step"/>), Shift+arrows / PageUp / PageDown (± <see cref="LargeStep"/>), Home / End; mirrored in RTL.</item>
///   <item><b>Pointer</b> — click or drag anywhere on the track (pointer capture; works on touch).</item>
///   <item><b>Forms</b> — <c>EditForm</c> validation, <see cref="BhField"/> labels, hidden inputs (<see cref="Name"/>).</item>
///   <item><b>Styling</b> — thumbs and the range get positioning styles (<c>left</c> / <c>right</c> / <c>bottom</c> / <c>top</c> in %) plus <c>data-orientation</c>, <c>data-disabled</c>, <c>data-dragging</c>.</item>
/// </list>
///
/// <code>
/// &lt;BhSlider @bind-Value="volume" Max="100" class="slider"&gt;
///     &lt;BhSliderTrack class="track"&gt;&lt;BhSliderRange class="range" /&gt;&lt;/BhSliderTrack&gt;
///     &lt;BhSliderThumb class="thumb" aria-label="Volume" /&gt;
/// &lt;/BhSlider&gt;
/// </code>
/// </summary>
public class BhSlider : BhComponentBase, IAsyncDisposable
{
    private readonly List<BhSliderThumb> _thumbs = new();
    private double[] _values = [];
    private int _activeThumb = -1;
    private int _handle;
    private DotNetObjectReference<BhSlider>? _selfRef;
    private BhFieldBinding? _binding;
    private string? _trackId;

    [CascadingParameter]
    private BhDirection CascadedDir { get; set; }

    [CascadingParameter]
    private BhFieldContext? FieldContext { get; set; }

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    /// <summary>The value (single thumb). Supplying it makes the slider controlled (use <c>@bind-Value</c>).</summary>
    [Parameter]
    public double Value { get; set; }

    /// <summary>Two-way binding callback for <see cref="Value"/>.</summary>
    [Parameter]
    public EventCallback<double> ValueChanged { get; set; }

    /// <summary>Identifies the bound field for <c>EditForm</c> validation.</summary>
    [Parameter]
    public Expression<Func<double>>? ValueExpression { get; set; }

    /// <summary>The values (one per thumb). Supplying it makes the slider controlled (use <c>@bind-Values</c>).</summary>
    [Parameter]
    public IReadOnlyList<double>? Values { get; set; }

    /// <summary>Two-way binding callback for <see cref="Values"/>.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> ValuesChanged { get; set; }

    /// <summary>Initial value when uncontrolled (single thumb).</summary>
    [Parameter]
    public double? DefaultValue { get; set; }

    /// <summary>Initial values when uncontrolled (range).</summary>
    [Parameter]
    public IReadOnlyList<double>? DefaultValues { get; set; }

    /// <summary>Fires when the user finishes changing the value (pointer up, key press).</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<double>> OnValueCommit { get; set; }

    /// <summary>The minimum value. Default: 0.</summary>
    [Parameter]
    public double Min { get; set; }

    /// <summary>The maximum value. Default: 100.</summary>
    [Parameter]
    public double Max { get; set; } = 100;

    /// <summary>The step. Default: 1.</summary>
    [Parameter]
    public double Step { get; set; } = 1;

    /// <summary>The step for PageUp / PageDown / Shift+arrows. Default: 10 steps.</summary>
    [Parameter]
    public double? LargeStep { get; set; }

    /// <summary>Minimum number of steps between thumbs of a range. Default: 0.</summary>
    [Parameter]
    public int MinStepsBetweenThumbs { get; set; }

    /// <summary>Horizontal (default) or vertical.</summary>
    [Parameter]
    public BhOrientation Orientation { get; set; } = BhOrientation.Horizontal;

    /// <summary>Reverses the direction (max on the left / bottom).</summary>
    [Parameter]
    public bool Inverted { get; set; }

    /// <summary>Reading direction. Inherits from <see cref="BhDirectionProvider"/>.</summary>
    [Parameter]
    public BhDirection? Dir { get; set; }

    /// <summary>Disables the slider.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Form field name: one hidden input per value.</summary>
    [Parameter]
    public string? Name { get; set; }

    /// <summary>Formats <c>aria-valuetext</c> for a thumb value (e.g. "$40").</summary>
    [Parameter]
    public Func<double, string>? ValueText { get; set; }

    /// <summary>Track, range and thumbs.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    protected override string DefaultTag => "span";

    private bool IsDisabled => Disabled || (FieldContext?.Disabled ?? false);
    private BhDirection EffectiveDir => Dir ?? CascadedDir;
    private bool IsRange => IsProvided(nameof(Values)) || DefaultValues is not null;
    private double EffectiveLargeStep => LargeStep ?? Math.Max(Step, (Max - Min) / 10);

    /// <summary>The current values (one per thumb).</summary>
    public IReadOnlyList<double> CurrentValues
    {
        get
        {
            if (IsProvided(nameof(Values))) return Values ?? [];
            if (IsProvided(nameof(Value))) return [Value];
            return _values;
        }
    }

    protected override void OnInitialized()
    {
        _values = DefaultValues?.ToArray() ?? [DefaultValue ?? Min];
        FieldContext?.SetControlFocus(FocusAsync);
    }

    /// <summary>Focuses the first thumb.</summary>
    public Task FocusAsync()
        => _thumbs.Count == 0 ? Task.CompletedTask : Interop.FocusAsync(_thumbs[0].ThumbId).AsTask();

    protected override void OnParametersSet()
    {
        if (ValueExpression is not null || _binding is not null)
        {
            _binding ??= new BhFieldBinding(() => InvokeAsync(StateHasChanged));
            _binding.Update(EditContext, ValueExpression);
            FieldContext?.ReportValidation(_binding.IsInvalid, _binding.Messages);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var options = new Dictionary<string, object?>
        {
            ["orientation"] = Orientation.ToAttribute(),
            ["inverted"] = Inverted,
        };
        if (_handle == 0 && firstRender)
        {
            _selfRef ??= DotNetObjectReference.Create(this);
            _handle = await Interop.SliderAttachAsync(_selfRef, ComponentId, _trackId, options);
        }
        else if (_handle > 0)
        {
            await Interop.SliderUpdateAsync(_handle, options);
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, Tag);
        builder.AddAttribute(10, "id", ComponentId);
        builder.AddMultipleAttributes(20, GetFinalAttributes());
        builder.AddElementReferenceCapture(30, CaptureElement);
        builder.OpenComponent<CascadingValue<BhSlider>>(40);
        builder.AddComponentParameter(41, "Value", this);
        // Not fixed: parameterless parts (<BhSliderRange />, <BhSliderTrack />) follow the value.
        builder.AddComponentParameter(42, "IsFixed", false);
        builder.AddComponentParameter(43, "ChildContent", ChildContent);
        builder.CloseComponent();
        builder.CloseElement();

        if (!string.IsNullOrEmpty(Name))
        {
            builder.OpenRegion(60);
            foreach (var v in CurrentValues)
            {
                builder.OpenElement(0, "input");
                builder.AddAttribute(1, "type", "hidden");
                builder.AddAttribute(2, "name", IsRange ? $"{Name}[]" : Name);
                builder.AddAttribute(3, "value", v.ToString(CultureInfo.InvariantCulture));
                builder.CloseElement();
            }
            builder.CloseRegion();
        }
    }

    protected override Dictionary<string, object> BuildComponentAttributes()
    {
        var attrs = base.BuildComponentAttributes();
        if (Dir is { } dir) attrs["dir"] = dir.ToAttribute();
        SetDataValue(attrs, "orientation", Orientation.ToAttribute());
        SetDataFlag(attrs, "disabled", IsDisabled);
        SetDataFlag(attrs, "dragging", _activeThumb >= 0);
        if (GetAdditionalAttribute("style") is null && GetAdditionalAttribute("class") is null)
            attrs["style"] = Orientation == BhOrientation.Vertical
                ? "position:relative;display:inline-flex;flex-direction:column;touch-action:none;user-select:none;"
                : "position:relative;display:flex;align-items:center;touch-action:none;user-select:none;";
        return attrs;
    }

    // ── Geometry ─────────────────────────────────────────────────────────────

    internal double Percent(double value) => Max > Min ? Math.Clamp((value - Min) / (Max - Min) * 100, 0, 100) : 0;

    /// <summary>CSS for absolutely positioning something at <paramref name="percent"/> along the slider.</summary>
    internal string PositionStyle(double percent)
    {
        var p = percent.ToString("0.###", CultureInfo.InvariantCulture);
        if (Orientation == BhOrientation.Vertical)
            return Inverted ? $"position:absolute;top:{p}%;transform:translateY(-50%)" : $"position:absolute;bottom:{p}%;transform:translateY(50%)";
        var fromRight = (EffectiveDir == BhDirection.Rtl) != Inverted;
        return fromRight ? $"position:absolute;right:{p}%;transform:translateX(50%)" : $"position:absolute;left:{p}%;transform:translateX(-50%)";
    }

    internal string RangeStyle()
    {
        var values = CurrentValues;
        var start = values.Count > 1 ? Percent(values.Min()) : 0;
        var end = 100 - Percent(values.Count > 0 ? values.Max() : Min);
        string F(double d) => d.ToString("0.###", CultureInfo.InvariantCulture);
        if (Orientation == BhOrientation.Vertical)
            return Inverted ? $"position:absolute;top:{F(start)}%;bottom:{F(end)}%" : $"position:absolute;bottom:{F(start)}%;top:{F(end)}%";
        var fromRight = (EffectiveDir == BhDirection.Rtl) != Inverted;
        return fromRight ? $"position:absolute;right:{F(start)}%;left:{F(end)}%" : $"position:absolute;left:{F(start)}%;right:{F(end)}%";
    }

    internal BhOrientation SliderOrientation => Orientation;
    internal bool SliderDisabled => IsDisabled;
    internal BhFieldContext? Field => FieldContext;
    internal string FormatValueText(double value) => ValueText?.Invoke(value) ?? value.ToString(CultureInfo.CurrentCulture);
    internal double MinValue => Min;
    internal double MaxValue => Max;

    internal void RegisterTrack(string id) => _trackId = id;

    internal int RegisterThumb(BhSliderThumb thumb)
    {
        if (!_thumbs.Contains(thumb)) _thumbs.Add(thumb);
        return _thumbs.IndexOf(thumb);
    }

    internal void UnregisterThumb(BhSliderThumb thumb) => _thumbs.Remove(thumb);

    internal int IndexOf(BhSliderThumb thumb) => _thumbs.IndexOf(thumb);

    internal double ValueAt(int index) => index >= 0 && index < CurrentValues.Count ? CurrentValues[index] : Min;

    internal bool IsActiveThumb(BhSliderThumb thumb) => _activeThumb >= 0 && _activeThumb == _thumbs.IndexOf(thumb);

    // ── Value updates ────────────────────────────────────────────────────────

    private double Snap(double value)
    {
        var snapped = Math.Round((value - Min) / Step) * Step + Min;
        var decimals = Math.Max(0, (int)Math.Ceiling(-Math.Log10(Step)) + 1);
        return Math.Clamp(Math.Round(snapped, Math.Min(decimals, 10)), Min, Max);
    }

    internal async Task SetThumbValueAsync(int index, double value, bool commit)
    {
        if (IsDisabled || index < 0) return;
        var values = CurrentValues.ToArray();
        if (index >= values.Length) return;

        var next = Snap(value);
        var gap = MinStepsBetweenThumbs * Step;
        if (index > 0) next = Math.Max(next, values[index - 1] + gap);
        if (index < values.Length - 1) next = Math.Min(next, values[index + 1] - gap);
        next = Math.Clamp(next, Min, Max);

        if (Math.Abs(values[index] - next) > double.Epsilon)
        {
            values[index] = next;
            if (!IsProvided(nameof(Values)) && !IsProvided(nameof(Value))) _values = values;
            if (IsRange) await ValuesChanged.InvokeAsync(values);
            else await ValueChanged.InvokeAsync(next);
            _binding?.NotifyChanged();
            StateHasChanged();
        }

        if (commit) await OnValueCommit.InvokeAsync(CurrentValues);
    }

    internal async Task HandleThumbKeyDownAsync(BhSliderThumb thumb, KeyboardEventArgs e)
    {
        var index = _thumbs.IndexOf(thumb);
        if (index < 0 || IsDisabled) return;
        var current = ValueAt(index);
        var step = e.ShiftKey ? EffectiveLargeStep : Step;
        var rtl = EffectiveDir == BhDirection.Rtl && Orientation == BhOrientation.Horizontal;
        var sign = Inverted ? -1 : 1;

        double? target = e.Key switch
        {
            "ArrowRight" => current + (rtl ? -step : step) * sign,
            "ArrowUp" => current + step * sign,
            "ArrowLeft" => current - (rtl ? -step : step) * sign,
            "ArrowDown" => current - step * sign,
            "PageUp" => current + EffectiveLargeStep,
            "PageDown" => current - EffectiveLargeStep,
            "Home" => Min,
            "End" => Max,
            _ => null,
        };
        if (target is { } t) await SetThumbValueAsync(index, t, commit: true);
    }

    /// <summary>Pointer interaction reported by the JS runtime (fraction 0–1 along the track).</summary>
    [JSInvokable]
    public async Task OnSliderPointer(string phase, double fraction)
    {
        if (IsDisabled || _thumbs.Count == 0) return;
        var value = Min + fraction * (Max - Min);
        if (phase == "start")
        {
            // Pick the closest thumb (the one that can move in that direction on ties).
            var values = CurrentValues;
            var best = 0;
            for (var i = 1; i < values.Count; i++)
                if (Math.Abs(values[i] - value) < Math.Abs(values[best] - value) ||
                    (Math.Abs(values[i] - value) == Math.Abs(values[best] - value) && value > values[i]))
                    best = i;
            _activeThumb = best;
        }
        await SetThumbValueAsync(_activeThumb, value, commit: phase == "end");
        if (phase == "end")
        {
            _activeThumb = -1;
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        _binding?.Dispose();
        FieldContext?.SetControlFocus(null);
        if (_handle > 0) await Interop.SliderDetachAsync(_handle);
        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
