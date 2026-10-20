using System.Text;
using AngleSharp.Dom;

namespace BlazorHeadless.Testing;

/// <summary>
/// Computes accessible names and descriptions following the W3C "Accessible Name
/// and Description Computation 1.2" algorithm, as far as it can be evaluated on
/// static markup (no CSS: <c>::before</c> content and visibility from stylesheets
/// are not considered).
/// </summary>
public static class BhAccessibleName
{
    /// <summary>Returns the accessible name of <paramref name="element"/> (empty when it has none).</summary>
    public static string Compute(IElement element) => Normalize(Name(element, new HashSet<IElement>(), traversing: false, viaLabelledBy: false));

    /// <summary>Returns the accessible description of <paramref name="element"/>.</summary>
    public static string ComputeDescription(IElement element)
    {
        var ids = element.GetAttribute("aria-describedby");
        if (!string.IsNullOrWhiteSpace(ids))
            return Normalize(string.Join(" ", ResolveIds(element, ids).Select(e => Name(e, new HashSet<IElement>(), traversing: true, viaLabelledBy: true))));
        return Normalize(element.GetAttribute("title") ?? string.Empty);
    }

    private static IEnumerable<IElement> ResolveIds(IElement element, string ids)
    {
        foreach (var id in ids.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var target = FindById(element, id) ?? element.Owner?.GetElementById(id);
            if (target is not null) yield return target;
        }
    }

    private static IElement? FindById(IElement element, string id)
    {
        var root = element;
        while (root.ParentElement is not null) root = root.ParentElement;
        return root.Id == id ? root : root.QuerySelector($"[id='{id.Replace("'", "\\'")}']");
    }

    /// <summary>Item roles whose nested sub-containers (child items) aren't part of their name, as browsers compute it.</summary>
    private static readonly HashSet<string> ItemRoles = new(StringComparer.Ordinal) { "treeitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "row" };

    private static readonly HashSet<string> NestedContainers = new(StringComparer.Ordinal) { "group", "tree", "menu", "listbox", "grid", "treegrid" };

    private static string Name(IElement element, HashSet<IElement> visited, bool traversing, bool viaLabelledBy, bool insideItem = false)
    {
        if (!visited.Add(element)) return string.Empty;

        // Step 2A: hidden nodes contribute nothing unless referenced directly.
        if (traversing && !viaLabelledBy && BhAria.IsHidden(element)) return string.Empty;

        // Step 2B: aria-labelledby (not when already following a labelledby chain).
        var labelledBy = element.GetAttribute("aria-labelledby");
        if (!viaLabelledBy && !string.IsNullOrWhiteSpace(labelledBy))
        {
            // Each reference is computed independently (self-references are allowed).
            var parts = ResolveIds(element, labelledBy).Select(e => Name(e, new HashSet<IElement>(), traversing: true, viaLabelledBy: true)).ToList();
            var joined = string.Join(" ", parts).Trim();
            if (joined.Length > 0) return joined;
        }

        var role = BhAria.GetRole(element);

        // Step 2C: embedded control inside a label: use its value.
        if (traversing && role is "textbox" or "searchbox" && element.LocalName is "input" or "textarea")
            return element.GetAttribute("value") ?? element.TextContent;

        // Step 2D: aria-label.
        var ariaLabel = element.GetAttribute("aria-label");
        if (!string.IsNullOrWhiteSpace(ariaLabel)) return ariaLabel;

        // Step 2E: native host-language label.
        if (role is not ("presentation" or "none"))
        {
            var native = NativeName(element, visited);
            if (!string.IsNullOrWhiteSpace(native)) return native;
        }

        // Step 2F: name from content.
        if (traversing || (role is not null && BhAria.NameFromContent.Contains(role)) || element.LocalName is "label" or "legend" or "caption")
        {
            var sb = new StringBuilder();
            // A tree item's name is its own row: its nested group of child items is excluded.
            var item = insideItem || (role is not null && ItemRoles.Contains(role));
            foreach (var child in element.ChildNodes)
            {
                if (child is IText text) sb.Append(text.Data);
                else if (child is IElement childElement)
                {
                    if (BhAria.IsHidden(childElement)) continue;
                    if (item && BhAria.GetRole(childElement) is { } childRole && NestedContainers.Contains(childRole)) continue;
                    var childName = Name(childElement, visited, traversing: true, viaLabelledBy: false, item);
                    // Block-level elements separate words.
                    sb.Append(' ').Append(childName).Append(' ');
                }
            }
            var content = sb.ToString();
            if (!string.IsNullOrWhiteSpace(content)) return content;
        }

        // Step 2I: tooltip attribute.
        return element.GetAttribute("title") ?? (element.LocalName is "input" or "textarea" ? element.GetAttribute("placeholder") ?? string.Empty : string.Empty);
    }

    private static string? NativeName(IElement element, HashSet<IElement> visited)
    {
        switch (element.LocalName)
        {
            case "input" or "select" or "textarea" or "meter" or "progress" or "output" or "button":
                var type = element.GetAttribute("type")?.ToLowerInvariant();
                if (element.LocalName == "input" && type is "button" or "submit" or "reset")
                    return element.GetAttribute("value") ?? (type == "submit" ? "Submit" : type == "reset" ? "Reset" : null);
                if (element.LocalName == "input" && type == "image")
                    return element.GetAttribute("alt");
                var labels = Labels(element).ToList();
                if (labels.Count > 0)
                    return string.Join(" ", labels.Select(l => Name(l, visited, traversing: true, viaLabelledBy: false)));
                return null;
            case "img" or "area":
                return element.GetAttribute("alt");
            case "fieldset":
                var legend = element.Children.FirstOrDefault(c => c.LocalName == "legend");
                return legend is null ? null : Name(legend, visited, traversing: true, viaLabelledBy: false);
            case "table":
                var caption = element.Children.FirstOrDefault(c => c.LocalName == "caption");
                return caption is null ? null : Name(caption, visited, traversing: true, viaLabelledBy: false);
            case "svg":
                return element.Children.FirstOrDefault(c => c.LocalName == "title")?.TextContent;
            default:
                return null;
        }
    }

    private static IEnumerable<IElement> Labels(IElement control)
    {
        var wrapping = control.Closest("label");
        if (wrapping is not null) yield return wrapping;
        if (!string.IsNullOrEmpty(control.Id))
        {
            var root = control;
            while (root.ParentElement is not null) root = root.ParentElement;
            foreach (var label in root.QuerySelectorAll("label[for]"))
                if (label.GetAttribute("for") == control.Id && !ReferenceEquals(label, wrapping)) yield return label;
        }
    }

    private static string Normalize(string value)
        => string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
