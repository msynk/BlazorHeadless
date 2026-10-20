using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorHeadless.Testing;

/// <summary>bUnit integration: context setup, accessibility assertions and keyboard helpers.</summary>
public static class BhTestingExtensions
{
    /// <summary>
    /// Prepares a bUnit context for BlazorHeadless components: loose JS interop (the
    /// browser runtime is a progressive enhancement, so components work without it),
    /// the library services and a recording announcer, which is returned.
    /// </summary>
    public static BhTestAnnouncer AddBlazorHeadless(this BunitContext context)
    {
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        var announcer = new BhTestAnnouncer();
        context.Services.AddSingleton<IBhAnnouncer>(announcer);
        context.Services.AddSingleton(announcer);
        context.Services.AddBlazorHeadless();
        return announcer;
    }

    /// <summary>Audits the rendered markup of <paramref name="cut"/>.</summary>
    public static BhA11yReport Audit<TComponent>(this IRenderedComponent<TComponent> cut, BhA11yOptions? options = null)
        where TComponent : IComponent
        => BhA11y.Audit(cut.Nodes, options);

    /// <summary>Throws a <see cref="BhA11yException"/> listing every violation when the markup isn't accessible.</summary>
    public static IRenderedComponent<TComponent> ShouldBeAccessible<TComponent>(this IRenderedComponent<TComponent> cut, BhA11yOptions? options = null)
        where TComponent : IComponent
    {
        var report = cut.Audit(options);
        if (!report.Passed) throw new BhA11yException(report);
        return cut;
    }

    /// <summary>The Tab sequence of the rendered markup.</summary>
    public static IReadOnlyList<BhTabStop> TabSequence<TComponent>(this IRenderedComponent<TComponent> cut)
        where TComponent : IComponent
        => BhFocusOrder.TabSequence(cut.Nodes);

    /// <summary>The accessible name of an element.</summary>
    public static string AccessibleName(this IElement element) => BhAccessibleName.Compute(element);

    /// <summary>The accessible description of an element.</summary>
    public static string AccessibleDescription(this IElement element) => BhAccessibleName.ComputeDescription(element);

    /// <summary>The element's role (explicit or implicit).</summary>
    public static string? Role(this IElement element) => BhAria.GetRole(element);

    /// <summary>The element referenced by <c>aria-activedescendant</c>, or null.</summary>
    public static IElement? ActiveDescendant<TComponent>(this IRenderedComponent<TComponent> cut, IElement element)
        where TComponent : IComponent
    {
        var id = element.GetAttribute("aria-activedescendant");
        return string.IsNullOrEmpty(id) ? null : cut.FindAll($"[id='{id}']").FirstOrDefault();
    }

    /// <summary>
    /// The id of the element BlazorHeadless last asked the browser to focus, or null.
    /// bUnit has no real focus, so keyboard tests assert on the focus request instead.
    /// </summary>
    public static string? LastFocusRequest(this BunitContext context)
        => context.JSInterop.Invocations
            .Where(i => i.Identifier == "focus.focus")
            .Select(i => i.Arguments.Count > 0 ? i.Arguments[0] as string : null)
            .LastOrDefault();

    /// <summary>Dispatches a keydown for <paramref name="key"/> (a <c>KeyboardEvent.key</c> value like "ArrowDown", " ", "a").</summary>
    public static void Press(this IElement element, string key, bool shift = false, bool ctrl = false, bool alt = false, bool meta = false)
        => element.KeyDown(new KeyboardEventArgs { Key = key, Code = key, ShiftKey = shift, CtrlKey = ctrl, AltKey = alt, MetaKey = meta, Type = "keydown" });

    /// <summary>Dispatches keydowns for a sequence of keys.</summary>
    public static void PressSequence(this IElement element, params string[] keys)
    {
        foreach (var key in keys) element.Press(key);
    }

    /// <summary>Types text: one keydown per character (typeahead).</summary>
    public static void Type(this IElement element, string text)
    {
        foreach (var c in text) element.Press(c.ToString());
    }
}
