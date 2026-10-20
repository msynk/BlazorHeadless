using System.Globalization;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;

namespace BlazorHeadless.Testing;

/// <summary>Severity of an accessibility violation (mirrors axe-core impact levels).</summary>
public enum BhA11yImpact
{
    /// <summary>Best-practice issue.</summary>
    Minor,

    /// <summary>Degrades the experience for some users.</summary>
    Moderate,

    /// <summary>Severely degrades the experience.</summary>
    Serious,

    /// <summary>Blocks access for some users.</summary>
    Critical,
}

/// <summary>One accessibility problem found by <see cref="BhA11y.Audit(INode, BhA11yOptions?)"/>.</summary>
/// <param name="RuleId">The rule that failed (e.g. <c>aria-valid-attr-value</c>).</param>
/// <param name="Impact">The severity.</param>
/// <param name="Message">What is wrong and how to fix it.</param>
/// <param name="Selector">A CSS selector locating the element.</param>
/// <param name="Html">The element's opening tag.</param>
public sealed record BhA11yViolation(string RuleId, BhA11yImpact Impact, string Message, string Selector, string Html)
{
    /// <inheritdoc />
    public override string ToString() => $"[{Impact}] {RuleId}: {Message}\n    at {Selector}\n    {Html}";
}

/// <summary>Options for <see cref="BhA11y.Audit(INode, BhA11yOptions?)"/>.</summary>
public sealed class BhA11yOptions
{
    /// <summary>Rules to skip (by id).</summary>
    public ISet<string> DisabledRules { get; } = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Only report violations at or above this impact. Default: <see cref="BhA11yImpact.Minor"/>.</summary>
    public BhA11yImpact MinimumImpact { get; set; } = BhA11yImpact.Minor;

    /// <summary>
    /// Whether the markup is a fragment of a page. When true, ID references
    /// to elements outside the fragment are not reported — they may live elsewhere
    /// on the page. Default: false.
    /// </summary>
    public bool IsFragment { get; set; }

    /// <summary>Disables a rule (fluent).</summary>
    public BhA11yOptions Without(params string[] ruleIds)
    {
        foreach (var id in ruleIds) DisabledRules.Add(id);
        return this;
    }
}

/// <summary>The result of an accessibility audit.</summary>
public sealed class BhA11yReport
{
    internal BhA11yReport(IReadOnlyList<BhA11yViolation> violations, int elementCount)
    {
        Violations = violations;
        ElementCount = elementCount;
    }

    /// <summary>The violations found, most severe first.</summary>
    public IReadOnlyList<BhA11yViolation> Violations { get; }

    /// <summary>The number of elements inspected.</summary>
    public int ElementCount { get; }

    /// <summary>Whether no violations were found.</summary>
    public bool Passed => Violations.Count == 0;

    /// <summary>A human-readable summary.</summary>
    public override string ToString()
        => Passed
            ? $"No accessibility violations in {ElementCount} elements."
            : $"{Violations.Count} accessibility violation(s) in {ElementCount} elements:\n\n" + string.Join("\n\n", Violations);

    /// <summary>A Markdown table (for CI job summaries).</summary>
    public string ToMarkdown(string? title = null)
    {
        var sb = new StringBuilder();
        if (title is not null) sb.AppendLine($"### {title}").AppendLine();
        if (Passed)
        {
            sb.AppendLine($"✅ No accessibility violations ({ElementCount} elements checked).");
            return sb.ToString();
        }
        sb.AppendLine("| Impact | Rule | Element | Problem |").AppendLine("| --- | --- | --- | --- |");
        foreach (var v in Violations)
            sb.AppendLine($"| {v.Impact} | `{v.RuleId}` | `{v.Selector.Replace("|", "\\|")}` | {v.Message.Replace("|", "\\|")} |");
        return sb.ToString();
    }

    /// <summary>The violations as JSON (for tooling).</summary>
    public string ToJson() => JsonSerializer.Serialize(new
    {
        passed = Passed,
        elements = ElementCount,
        violations = Violations.Select(v => new { rule = v.RuleId, impact = v.Impact.ToString().ToLowerInvariant(), v.Message, v.Selector, v.Html }),
    }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
}

/// <summary>Thrown by <c>ShouldBeAccessible</c> assertions.</summary>
public sealed class BhA11yException(BhA11yReport report) : Exception(report.ToString())
{
    /// <summary>The failing report.</summary>
    public BhA11yReport Report { get; } = report;
}

/// <summary>
/// A static WAI-ARIA conformance checker for rendered Razor component markup — the
/// checks that can be made reliably without a browser: valid roles and states,
/// allowed attributes, value types, ID references, required owned elements and
/// context roles, accessible names, focusable content hidden from AT, nested
/// interactive controls, positive tabindex and duplicate ids.
///
/// <para>Static checks complement — they don't replace — browser-based scanning
/// (axe-core) and manual testing with assistive technology.</para>
/// </summary>
public static class BhA11y
{
    /// <summary>All rule ids.</summary>
    public static readonly IReadOnlyList<string> Rules =
    [
        "duplicate-id", "aria-valid-role", "aria-valid-attr-value", "aria-allowed-attr", "aria-required-attr",
        "idref-exists", "aria-required-children", "aria-required-parent", "accessible-name", "aria-hidden-focus",
        "nested-interactive", "tabindex", "presentation-conflict", "activedescendant-target", "form-label",
    ];

    /// <summary>Audits <paramref name="root"/> and everything below it.</summary>
    public static BhA11yReport Audit(INode root, BhA11yOptions? options = null)
        => Audit(new[] { root }, options);

    /// <summary>Audits several root nodes (e.g. <c>cut.Nodes</c>).</summary>
    public static BhA11yReport Audit(IEnumerable<INode> roots, BhA11yOptions? options = null)
    {
        options ??= new BhA11yOptions();
        var elements = new List<IElement>();
        foreach (var root in roots)
        {
            if (root is IElement e) elements.Add(e);
            if (root is IParentNode parent) elements.AddRange(parent.QuerySelectorAll("*"));
        }
        elements = elements.Distinct().ToList();

        var ids = elements.Where(e => !string.IsNullOrEmpty(e.Id)).GroupBy(e => e.Id!, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
        var violations = new List<BhA11yViolation>();
        void Report(string rule, BhA11yImpact impact, IElement el, string message)
        {
            if (options.DisabledRules.Contains(rule) || impact < options.MinimumImpact) return;
            violations.Add(new BhA11yViolation(rule, impact, message, Selector(el), OpeningTag(el)));
        }

        foreach (var (id, list) in ids)
            if (list.Count > 1)
                Report("duplicate-id", BhA11yImpact.Serious, list[1], $"The id \"{id}\" is used by {list.Count} elements; ID references become ambiguous.");

        foreach (var el in elements)
        {
            var hidden = BhAria.IsHidden(el);
            var role = BhAria.GetRole(el);
            CheckRoleAttribute(el, Report);
            CheckAttributeValues(el, role, Report);
            CheckIdRefs(el, ids, options, Report);

            if (hidden)
            {
                if (el.GetAttribute("aria-hidden") == "true" && !BhAria.IsInert(el))
                    foreach (var focusable in new[] { el }.Concat(el.QuerySelectorAll("*")).Where(IsTabbable2))
                        Report("aria-hidden-focus", BhA11yImpact.Serious, focusable, "Focusable element inside aria-hidden=\"true\": keyboard users reach content screen readers can't perceive. Make it inert or remove it from the tab order.");
                continue;
            }

            if (role is null) continue;
            CheckAllowedAttributes(el, role, Report);
            CheckRequiredAttributes(el, role, Report);
            CheckOwnedElements(el, role, Report);
            CheckContext(el, role, Report);
            CheckName(el, role, Report);
            CheckNestedInteractive(el, role, Report);
            CheckPresentation(el, role, Report);
            CheckActiveDescendant(el, ids, Report);
        }

        foreach (var el in elements)
        {
            if (int.TryParse(el.GetAttribute("tabindex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var t) && t > 0)
                Report("tabindex", BhA11yImpact.Moderate, el, $"tabindex=\"{t}\" changes the natural focus order; use 0 or -1.");
        }

        var ordered = violations.OrderByDescending(v => v.Impact).ThenBy(v => v.RuleId, StringComparer.Ordinal).ToList();
        return new BhA11yReport(ordered, elements.Count);
    }

    // ── Rules ────────────────────────────────────────────────────────────────

    private static void CheckRoleAttribute(IElement el, Action<string, BhA11yImpact, IElement, string> report)
    {
        var roleAttr = el.GetAttribute("role");
        if (roleAttr is null) return;
        if (string.IsNullOrWhiteSpace(roleAttr))
        {
            report("aria-valid-role", BhA11yImpact.Minor, el, "Empty role attribute.");
            return;
        }
        var tokens = roleAttr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (!tokens.Any(BhAria.ValidRoles.Contains))
            report("aria-valid-role", BhA11yImpact.Critical, el, $"role=\"{roleAttr}\" is not a valid WAI-ARIA role.");
    }

    private static void CheckAttributeValues(IElement el, string? role, Action<string, BhA11yImpact, IElement, string> report)
    {
        foreach (var attr in el.Attributes)
        {
            var name = attr.Name;
            if (!name.StartsWith("aria-", StringComparison.Ordinal)) continue;
            var value = attr.Value.Trim();
            if (BhAria.TokenValues.TryGetValue(name, out var tokens) && !tokens.Contains(value))
                report("aria-valid-attr-value", BhA11yImpact.Critical, el, $"{name}=\"{value}\" is invalid; expected one of: {string.Join(", ", tokens)}.");
            else if (BhAria.IntegerAttributes.Contains(name) && !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _))
                report("aria-valid-attr-value", BhA11yImpact.Critical, el, $"{name}=\"{value}\" must be an integer.");
            else if (BhAria.NumberAttributes.Contains(name) && !double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                report("aria-valid-attr-value", BhA11yImpact.Critical, el, $"{name}=\"{value}\" must be a number.");
            else if (name is "aria-label" or "aria-labelledby" or "aria-describedby" && value.Length == 0)
                report("aria-valid-attr-value", BhA11yImpact.Minor, el, $"{name} is empty.");
        }

        if (role is "slider" or "meter" or "progressbar" or "spinbutton"
            && double.TryParse(el.GetAttribute("aria-valuenow"), NumberStyles.Float, CultureInfo.InvariantCulture, out var now))
        {
            var min = double.TryParse(el.GetAttribute("aria-valuemin"), NumberStyles.Float, CultureInfo.InvariantCulture, out var mi) ? mi : (role is "slider" or "progressbar" or "meter" ? 0 : double.MinValue);
            var max = double.TryParse(el.GetAttribute("aria-valuemax"), NumberStyles.Float, CultureInfo.InvariantCulture, out var ma) ? ma : (role is "slider" or "progressbar" ? 100 : double.MaxValue);
            if (now < min || now > max)
                report("aria-valid-attr-value", BhA11yImpact.Serious, el, $"aria-valuenow ({now}) is outside [{min}, {max}].");
        }
    }

    private static void CheckIdRefs(IElement el, Dictionary<string, List<IElement>> ids, BhA11yOptions options, Action<string, BhA11yImpact, IElement, string> report)
    {
        foreach (var name in BhAria.IdRefAttributes.Append("for"))
        {
            var value = el.GetAttribute(name);
            if (string.IsNullOrWhiteSpace(value)) continue;
            if (name == "for" && el.LocalName != "label") continue;
            // A collapsed control may point at popup content that isn't rendered yet (APG / axe).
            if (name == "aria-controls" && el.GetAttribute("aria-expanded") == "false") continue;
            foreach (var id in value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (ids.ContainsKey(id)) continue;
                if (options.IsFragment) continue;
                report("idref-exists", name is "aria-activedescendant" or "aria-labelledby" or "for" ? BhA11yImpact.Serious : BhA11yImpact.Moderate, el,
                    $"{name} references \"{id}\", which doesn't exist.");
            }
        }
    }

    private static void CheckAllowedAttributes(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        foreach (var attr in el.Attributes)
        {
            if (!BhAria.RoleSpecificAttributes.TryGetValue(attr.Name, out var roles)) continue;
            if (roles.Contains(role)) continue;
            // Native controls may carry states the browser maps (e.g. aria-checked on input is redundant, not invalid).
            report("aria-allowed-attr", BhA11yImpact.Serious, el, $"{attr.Name} is not supported on role \"{role}\"; assistive tech will ignore it.");
        }
    }

    private static void CheckRequiredAttributes(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        if (!BhAria.RequiredAttributes.TryGetValue(role, out var required)) return;
        if (BhAria.IsNativeControl(el) && el.GetAttribute("role") is null) return;
        if (role == "heading" && el.LocalName is "h1" or "h2" or "h3" or "h4" or "h5" or "h6") return;
        if (role is "checkbox" or "radio" or "switch" && el.LocalName == "input") return;
        if (role == "slider" && el.LocalName == "input") return;
        foreach (var attr in required)
        {
            if (role == "scrollbar" && attr == "aria-controls") continue;
            if (!el.HasAttribute(attr))
                report("aria-required-attr", BhA11yImpact.Critical, el, $"role \"{role}\" requires {attr}.");
        }
        if (role == "combobox" && el.GetAttribute("aria-expanded") == "true" && !el.HasAttribute("aria-controls") && !el.HasAttribute("aria-owns"))
            report("aria-required-attr", BhA11yImpact.Serious, el, "An expanded combobox needs aria-controls pointing at its popup.");
    }

    private static readonly HashSet<string> Transparent = new(StringComparer.Ordinal) { "presentation", "none", "generic" };

    /// <summary>Owned elements: the first descendants with a non-generic role (through generic wrappers).</summary>
    private static IEnumerable<(IElement Element, string Role)> OwnedRoles(IElement el)
    {
        foreach (var child in el.Children)
        {
            if (BhAria.IsHidden(child)) continue;
            var childRole = BhAria.GetRole(child);
            if (childRole is null || Transparent.Contains(childRole))
            {
                foreach (var owned in OwnedRoles(child)) yield return owned;
            }
            else
            {
                yield return (child, childRole);
            }
        }
    }

    private static void CheckOwnedElements(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        if (!BhAria.RequiredOwned.TryGetValue(role, out var allowed)) return;
        // In a listbox / tree, a group may only contain the leaf role.
        foreach (var (owned, ownedRole) in OwnedRoles(el))
        {
            if (allowed.Contains(ownedRole)) continue;
            report("aria-required-children", BhA11yImpact.Critical, owned,
                $"role \"{ownedRole}\" is not allowed directly inside role \"{role}\" (allowed: {string.Join(", ", allowed)}).");
        }
    }

    private static void CheckContext(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        if (!BhAria.RequiredContext.TryGetValue(role, out var contexts)) return;
        if (role == "listitem" && el.LocalName == "li" && el.GetAttribute("role") is null)
        {
            var parentTag = el.ParentElement?.LocalName;
            if (parentTag is "ul" or "ol" or "menu" && el.ParentElement!.GetAttribute("role") is null) return;
        }
        if (role == "option" && el.LocalName == "option") return;

        for (var parent = el.ParentElement; parent is not null; parent = parent.ParentElement)
        {
            var parentRole = BhAria.GetRole(parent);
            if (parentRole is null || Transparent.Contains(parentRole)) continue;
            if (contexts.Contains(parentRole))
            {
                // A group only counts when it is itself in the right container.
                if (parentRole == "group" && role is "option" or "treeitem" or "menuitem" or "menuitemcheckbox" or "menuitemradio")
                {
                    var outer = parent.ParentElement;
                    while (outer is not null && (BhAria.GetRole(outer) is null || Transparent.Contains(BhAria.GetRole(outer)!) || BhAria.GetRole(outer) == "group" || BhAria.GetRole(outer) == "treeitem"))
                        outer = outer.ParentElement;
                    var outerRole = outer is null ? null : BhAria.GetRole(outer);
                    var ok = role switch
                    {
                        "option" => outerRole is "listbox",
                        "treeitem" => outerRole is "tree",
                        _ => outerRole is "menu" or "menubar",
                    };
                    if (!ok) break;
                }
                return;
            }
            break;
        }
        report("aria-required-parent", BhA11yImpact.Critical, el, $"role \"{role}\" must be contained by {string.Join(" / ", contexts)}.");
    }

    private static void CheckName(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        var needsName = BhAria.NameRequired.Contains(role)
            || (role is "textbox" or "searchbox" or "combobox" or "listbox" or "slider" or "spinbutton" or "checkbox" or "radio" or "switch" && BhAria.IsNativeControl(el));
        if (!needsName) return;
        if (role == "img" && el.LocalName == "svg" && el.GetAttribute("aria-hidden") == "true") return;
        if (role == "option" && el.LocalName == "option") return;
        if (role == "region" && el.LocalName == "section") return;
        // Hidden inputs and inputs inside a labelled group of radios are fine.
        if (el.LocalName == "input" && string.Equals(el.GetAttribute("type"), "hidden", StringComparison.OrdinalIgnoreCase)) return;

        var name = BhAccessibleName.Compute(el);
        if (name.Length > 0) return;
        var rule = BhAria.IsNativeControl(el) && el.LocalName is "input" or "select" or "textarea" ? "form-label" : "accessible-name";
        report(rule, role is "dialog" or "alertdialog" or "region" or "tabpanel" or "listbox" or "tree" or "grid" or "treegrid" or "table" or "radiogroup" or "meter" or "progressbar" ? BhA11yImpact.Serious : BhA11yImpact.Critical, el,
            $"Element with role \"{role}\" has no accessible name. Add text content, aria-label or aria-labelledby" + (rule == "form-label" ? ", or a <label>." : "."));
    }

    private static void CheckNestedInteractive(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        if (!BhAria.ChildrenPresentational.Contains(role) && role is not "link") return;
        foreach (var descendant in el.QuerySelectorAll("*"))
        {
            if (BhAria.IsHidden(descendant) || BhAria.IsInert(descendant)) continue;
            var descendantRole = BhAria.GetRole(descendant);
            if (BhAria.IsTabbable(descendant) || (descendantRole is not null && BhAria.Interactive.Contains(descendantRole) && BhAria.IsFocusable(descendant)))
            {
                report("nested-interactive", BhA11yImpact.Serious, descendant,
                    $"Interactive element nested inside role \"{role}\": screen readers don't announce it and keyboard behaviour is unpredictable.");
                return;
            }
        }
    }

    private static void CheckPresentation(IElement el, string role, Action<string, BhA11yImpact, IElement, string> report)
    {
        if (el.GetAttribute("role") is not ("presentation" or "none")) return;
        if (BhAria.IsFocusable(el))
            report("presentation-conflict", BhA11yImpact.Minor, el, "A focusable element can't be presentational; the role is ignored.");
        else if (el.Attributes.Any(a => a.Name is "aria-label" or "aria-labelledby" or "aria-describedby" or "aria-live" or "aria-controls" or "aria-owns"))
            report("presentation-conflict", BhA11yImpact.Minor, el, "Global ARIA attributes on a presentational element cause the role to be ignored.");
    }

    private static void CheckActiveDescendant(IElement el, Dictionary<string, List<IElement>> ids, Action<string, BhA11yImpact, IElement, string> report)
    {
        var id = el.GetAttribute("aria-activedescendant");
        if (string.IsNullOrWhiteSpace(id) || !ids.TryGetValue(id, out var targets)) return;
        var target = targets[0];
        if (el.Contains(target)) return;
        foreach (var owner in new[] { "aria-controls", "aria-owns" })
        {
            var refs = el.GetAttribute(owner);
            if (refs is null) continue;
            foreach (var r in refs.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (ids.TryGetValue(r, out var controlled) && controlled[0].Contains(target)) return;
        }
        report("activedescendant-target", BhA11yImpact.Serious, el,
            $"aria-activedescendant points at \"{id}\", which is neither inside the element nor inside the element it controls.");
        _ = target;
    }

    private static bool IsTabbable2(IElement element) => BhAria.IsFocusable(element)
        && !(int.TryParse(element.GetAttribute("tabindex"), out var t) && t < 0);

    // ── Formatting ───────────────────────────────────────────────────────────

    /// <summary>A short CSS selector locating <paramref name="element"/>.</summary>
    public static string Selector(IElement element)
    {
        if (!string.IsNullOrEmpty(element.Id)) return $"#{element.Id}";
        var parts = new List<string>();
        for (var e = element; e is not null && parts.Count < 4; e = e.ParentElement)
        {
            if (!string.IsNullOrEmpty(e.Id))
            {
                parts.Insert(0, $"#{e.Id}");
                break;
            }
            var part = e.LocalName;
            var role = e.GetAttribute("role");
            if (role is not null) part += $"[role=\"{role}\"]";
            else if (e.ClassList.Length > 0) part += "." + string.Join('.', e.ClassList.Take(2));
            parts.Insert(0, part);
        }
        return string.Join(" > ", parts);
    }

    private static string OpeningTag(IElement element)
    {
        var html = element.OuterHtml;
        var end = html.IndexOf('>');
        var tag = end > 0 ? html[..(end + 1)] : html;
        return tag.Length > 200 ? tag[..200] + "…" : tag;
    }
}
