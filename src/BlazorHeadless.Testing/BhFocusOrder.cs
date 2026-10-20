using System.Globalization;
using AngleSharp.Dom;

namespace BlazorHeadless.Testing;

/// <summary>One stop of the sequential (Tab) focus order.</summary>
/// <param name="Element">The focusable element.</param>
/// <param name="Role">Its role.</param>
/// <param name="Name">Its accessible name.</param>
public sealed record BhTabStop(IElement Element, string? Role, string Name)
{
    /// <inheritdoc />
    public override string ToString() => $"{Role ?? Element.LocalName} \"{Name}\"";
}

/// <summary>
/// Inspects the keyboard focus order of rendered markup: which elements Tab
/// reaches and in which order (positive <c>tabindex</c> first, then document order;
/// disabled, hidden, inert and <c>tabindex="-1"</c> elements excluded).
/// </summary>
public static class BhFocusOrder
{
    /// <summary>The Tab sequence inside <paramref name="roots"/>.</summary>
    public static IReadOnlyList<BhTabStop> TabSequence(IEnumerable<INode> roots)
    {
        var elements = new List<IElement>();
        foreach (var root in roots)
        {
            if (root is IElement e) elements.Add(e);
            if (root is IParentNode parent) elements.AddRange(parent.QuerySelectorAll("*"));
        }
        var tabbable = RadioGroupStops(elements.Distinct().Where(BhAria.IsTabbable).ToList());
        int Index(IElement el) => int.TryParse(el.GetAttribute("tabindex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) ? t : 0;
        return tabbable.Where(e => Index(e) > 0).OrderBy(Index)
            .Concat(tabbable.Where(e => Index(e) <= 0))
            .Select(e => new BhTabStop(e, BhAria.GetRole(e), BhAccessibleName.Compute(e)))
            .ToList();
    }

    /// <summary>
    /// Native radio buttons sharing a name form one tab stop: the checked one, or the
    /// first of the group when none is checked.
    /// </summary>
    private static List<IElement> RadioGroupStops(List<IElement> tabbable)
    {
        static bool IsNamedRadio(IElement e)
            => e.LocalName == "input" && string.Equals(e.GetAttribute("type"), "radio", StringComparison.OrdinalIgnoreCase)
               && !string.IsNullOrEmpty(e.GetAttribute("name"));
        static (IElement? Form, string Name) Group(IElement e) => (e.Closest("form"), e.GetAttribute("name")!);

        var keep = new HashSet<IElement>();
        foreach (var group in tabbable.Where(IsNamedRadio).GroupBy(Group))
        {
            var stop = group.FirstOrDefault(e => e.HasAttribute("checked")) ?? group.First();
            keep.Add(stop);
        }
        return tabbable.Where(e => !IsNamedRadio(e) || keep.Contains(e)).ToList();
    }

    /// <summary>The Tab sequence inside <paramref name="root"/>.</summary>
    public static IReadOnlyList<BhTabStop> TabSequence(INode root) => TabSequence(new[] { root });

    /// <summary>A readable listing of the sequence (one "role \"name\"" per line).</summary>
    public static string Describe(IEnumerable<BhTabStop> stops) => string.Join(Environment.NewLine, stops.Select((s, i) => $"{i + 1}. {s}"));
}
