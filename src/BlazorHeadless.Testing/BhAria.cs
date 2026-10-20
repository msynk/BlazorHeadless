using AngleSharp.Dom;

namespace BlazorHeadless.Testing;

/// <summary>
/// WAI-ARIA 1.2 knowledge used by the audit: valid roles, implicit (HTML-AAM)
/// roles, which roles support which states / properties, value types, required
/// owned elements and required context roles.
/// </summary>
public static class BhAria
{
    /// <summary>Every non-abstract ARIA 1.2 role (plus DPUB / graphics roles commonly used).</summary>
    public static readonly IReadOnlySet<string> ValidRoles = new HashSet<string>(StringComparer.Ordinal)
    {
        "alert", "alertdialog", "application", "article", "banner", "blockquote", "button", "caption", "cell",
        "checkbox", "code", "columnheader", "combobox", "comment", "complementary", "contentinfo", "definition",
        "deletion", "dialog", "directory", "document", "emphasis", "feed", "figure", "form", "generic", "grid",
        "gridcell", "group", "heading", "img", "image", "insertion", "link", "list", "listbox", "listitem", "log", "main",
        "mark", "marquee", "math", "menu", "menubar", "menuitem", "menuitemcheckbox", "menuitemradio", "meter",
        "navigation", "none", "note", "option", "paragraph", "presentation", "progressbar", "radio", "radiogroup",
        "region", "row", "rowgroup", "rowheader", "scrollbar", "search", "searchbox", "separator", "slider",
        "spinbutton", "status", "strong", "subscript", "suggestion", "superscript", "switch", "tab", "table",
        "tablist", "tabpanel", "term", "textbox", "time", "timer", "toolbar", "tooltip", "tree", "treegrid",
        "treeitem", "graphics-document", "graphics-object", "graphics-symbol",
    };

    /// <summary>Roles whose accessible name comes from their content.</summary>
    public static readonly IReadOnlySet<string> NameFromContent = new HashSet<string>(StringComparer.Ordinal)
    {
        "button", "cell", "checkbox", "columnheader", "gridcell", "heading", "link", "menuitem", "menuitemcheckbox",
        "menuitemradio", "option", "radio", "row", "rowheader", "switch", "tab", "tooltip", "treeitem", "comment", "sectionhead",
    };

    /// <summary>Roles that must have an accessible name.</summary>
    public static readonly IReadOnlySet<string> NameRequired = new HashSet<string>(StringComparer.Ordinal)
    {
        "alertdialog", "button", "checkbox", "combobox", "dialog", "grid", "img", "link", "listbox", "menuitem",
        "menuitemcheckbox", "menuitemradio", "meter", "option", "progressbar", "radio", "radiogroup", "region",
        "searchbox", "slider", "spinbutton", "switch", "tab", "table", "textbox", "tree", "treegrid", "treeitem", "tabpanel",
    };

    /// <summary>Interactive widget roles (used by the nested-interactive and focus rules).</summary>
    public static readonly IReadOnlySet<string> Interactive = new HashSet<string>(StringComparer.Ordinal)
    {
        "button", "checkbox", "combobox", "link", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio",
        "searchbox", "slider", "spinbutton", "switch", "tab", "textbox", "treeitem", "gridcell",
    };

    /// <summary>Roles whose children are presentational (may not contain interactive content).</summary>
    public static readonly IReadOnlySet<string> ChildrenPresentational = new HashSet<string>(StringComparer.Ordinal)
    {
        "button", "checkbox", "img", "menuitemcheckbox", "menuitemradio", "meter", "option", "progressbar", "radio",
        "scrollbar", "separator", "slider", "switch", "tab",
    };

    private static readonly string[] Composite = ["combobox", "grid", "listbox", "menu", "menubar", "radiogroup", "tablist", "tree", "treegrid", "textbox", "searchbox", "group", "spinbutton", "application", "row"];
    private static readonly string[] RangeRoles = ["meter", "progressbar", "scrollbar", "separator", "slider", "spinbutton"];

    /// <summary>States / properties that are only allowed on specific roles (global ones are not listed).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RoleSpecificAttributes = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["aria-activedescendant"] = Composite,
        ["aria-autocomplete"] = ["combobox", "searchbox", "textbox"],
        ["aria-checked"] = ["checkbox", "menuitemcheckbox", "menuitemradio", "option", "radio", "switch", "treeitem"],
        ["aria-colcount"] = ["grid", "table", "treegrid"],
        ["aria-colindex"] = ["cell", "columnheader", "gridcell", "row", "rowheader"],
        ["aria-colspan"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["aria-expanded"] = ["application", "button", "checkbox", "combobox", "gridcell", "link", "listbox", "menuitem", "menuitemcheckbox", "menuitemradio", "row", "rowheader", "tab", "treeitem", "columnheader", "switch"],
        ["aria-level"] = ["heading", "listitem", "row", "treeitem", "comment"],
        ["aria-modal"] = ["alertdialog", "dialog"],
        ["aria-multiline"] = ["searchbox", "textbox"],
        ["aria-multiselectable"] = ["grid", "listbox", "tablist", "tree", "treegrid"],
        ["aria-orientation"] = ["scrollbar", "select", "separator", "slider", "tablist", "toolbar", "listbox", "menu", "menubar", "radiogroup", "tree", "treegrid"],
        ["aria-placeholder"] = ["searchbox", "textbox"],
        ["aria-posinset"] = ["article", "listitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio", "row", "tab", "treeitem", "comment"],
        ["aria-pressed"] = ["button"],
        ["aria-readonly"] = ["checkbox", "combobox", "grid", "gridcell", "listbox", "radiogroup", "slider", "spinbutton", "textbox", "searchbox", "columnheader", "rowheader", "treegrid", "menuitemcheckbox", "menuitemradio", "switch"],
        ["aria-required"] = ["checkbox", "combobox", "gridcell", "listbox", "radiogroup", "spinbutton", "textbox", "searchbox", "tree", "treegrid", "columnheader", "rowheader", "switch"],
        ["aria-rowcount"] = ["grid", "table", "treegrid"],
        ["aria-rowindex"] = ["cell", "columnheader", "gridcell", "row", "rowheader"],
        ["aria-rowspan"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["aria-selected"] = ["gridcell", "option", "row", "tab", "columnheader", "rowheader", "treeitem"],
        ["aria-setsize"] = ["article", "listitem", "menuitem", "menuitemcheckbox", "menuitemradio", "option", "radio", "row", "tab", "treeitem", "comment"],
        ["aria-sort"] = ["columnheader", "rowheader"],
        ["aria-valuemax"] = RangeRoles,
        ["aria-valuemin"] = RangeRoles,
        ["aria-valuenow"] = RangeRoles,
        ["aria-valuetext"] = RangeRoles,
    };

    /// <summary>Allowed values of enumerated / boolean attributes.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> TokenValues = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["aria-atomic"] = ["true", "false"],
        ["aria-autocomplete"] = ["inline", "list", "both", "none"],
        ["aria-busy"] = ["true", "false"],
        ["aria-checked"] = ["true", "false", "mixed", "undefined"],
        ["aria-current"] = ["page", "step", "location", "date", "time", "true", "false"],
        ["aria-disabled"] = ["true", "false"],
        ["aria-expanded"] = ["true", "false", "undefined"],
        ["aria-haspopup"] = ["true", "false", "menu", "listbox", "tree", "grid", "dialog"],
        ["aria-hidden"] = ["true", "false", "undefined"],
        ["aria-invalid"] = ["true", "false", "grammar", "spelling"],
        ["aria-live"] = ["off", "polite", "assertive"],
        ["aria-modal"] = ["true", "false"],
        ["aria-multiline"] = ["true", "false"],
        ["aria-multiselectable"] = ["true", "false"],
        ["aria-orientation"] = ["horizontal", "vertical", "undefined"],
        ["aria-pressed"] = ["true", "false", "mixed", "undefined"],
        ["aria-readonly"] = ["true", "false"],
        ["aria-required"] = ["true", "false"],
        ["aria-selected"] = ["true", "false", "undefined"],
        ["aria-sort"] = ["ascending", "descending", "none", "other"],
    };

    /// <summary>Attributes whose value must be an integer.</summary>
    public static readonly IReadOnlySet<string> IntegerAttributes = new HashSet<string>(StringComparer.Ordinal)
    {
        "aria-colcount", "aria-colindex", "aria-colspan", "aria-level", "aria-posinset", "aria-rowcount", "aria-rowindex", "aria-rowspan", "aria-setsize",
    };

    /// <summary>Attributes whose value must be a number.</summary>
    public static readonly IReadOnlySet<string> NumberAttributes = new HashSet<string>(StringComparer.Ordinal)
    {
        "aria-valuemax", "aria-valuemin", "aria-valuenow",
    };

    /// <summary>Attributes holding id references.</summary>
    public static readonly IReadOnlySet<string> IdRefAttributes = new HashSet<string>(StringComparer.Ordinal)
    {
        "aria-activedescendant", "aria-controls", "aria-describedby", "aria-details", "aria-errormessage", "aria-flowto",
        "aria-labelledby", "aria-owns",
    };

    /// <summary>Roles a container may own (directly, through generic wrappers).</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RequiredOwned = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["grid"] = ["row", "rowgroup"],
        ["treegrid"] = ["row", "rowgroup"],
        ["table"] = ["row", "rowgroup", "caption"],
        ["rowgroup"] = ["row"],
        ["row"] = ["cell", "columnheader", "gridcell", "rowheader"],
        ["list"] = ["listitem"],
        ["listbox"] = ["option", "group"],
        ["menu"] = ["menuitem", "menuitemcheckbox", "menuitemradio", "group", "separator", "menu"],
        ["menubar"] = ["menuitem", "menuitemcheckbox", "menuitemradio", "group", "separator", "menu"],
        ["tablist"] = ["tab"],
        ["tree"] = ["treeitem", "group"],
        ["feed"] = ["article"],
    };

    /// <summary>The context roles an element of a role must live in.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RequiredContext = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["option"] = ["listbox", "group"],
        ["menuitem"] = ["menu", "menubar", "group"],
        ["menuitemcheckbox"] = ["menu", "menubar", "group"],
        ["menuitemradio"] = ["menu", "menubar", "group"],
        ["tab"] = ["tablist"],
        ["treeitem"] = ["tree", "group"],
        ["row"] = ["grid", "treegrid", "table", "rowgroup"],
        ["cell"] = ["row"],
        ["gridcell"] = ["row"],
        ["columnheader"] = ["row"],
        ["rowheader"] = ["row"],
        ["listitem"] = ["list", "directory"],
    };

    /// <summary>States / properties a role must have.</summary>
    public static readonly IReadOnlyDictionary<string, string[]> RequiredAttributes = new Dictionary<string, string[]>(StringComparer.Ordinal)
    {
        ["checkbox"] = ["aria-checked"],
        ["combobox"] = ["aria-expanded"],
        ["heading"] = ["aria-level"],
        ["menuitemcheckbox"] = ["aria-checked"],
        ["menuitemradio"] = ["aria-checked"],
        ["radio"] = ["aria-checked"],
        ["scrollbar"] = ["aria-controls", "aria-valuenow"],
        ["slider"] = ["aria-valuenow"],
        ["switch"] = ["aria-checked"],
    };

    /// <summary>Returns the element's role: the first valid token of <c>role</c>, else its implicit role (null for generic).</summary>
    public static string? GetRole(IElement element)
    {
        var explicitRole = element.GetAttribute("role");
        if (!string.IsNullOrWhiteSpace(explicitRole))
        {
            foreach (var token in explicitRole.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                if (ValidRoles.Contains(token)) return token == "image" ? "img" : token;
        }
        return GetImplicitRole(element);
    }

    /// <summary>The HTML-AAM implicit role of an element (null when generic / none).</summary>
    public static string? GetImplicitRole(IElement element)
    {
        var tag = element.LocalName;
        switch (tag)
        {
            case "a" or "area": return element.HasAttribute("href") ? "link" : null;
            case "article": return "article";
            case "aside": return "complementary";
            case "button": return "button";
            case "datalist": return "listbox";
            case "details": return "group";
            case "dialog": return "dialog";
            case "fieldset": return "group";
            case "figure": return "figure";
            case "footer": return "contentinfo";
            case "form": return "form";
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6": return "heading";
            case "header": return "banner";
            case "hr": return "separator";
            case "img": return element.GetAttribute("alt") == "" ? "presentation" : "img";
            case "li": return "listitem";
            case "main": return "main";
            case "math": return "math";
            case "menu" or "ol" or "ul": return "list";
            case "meter": return "meter";
            case "nav": return "navigation";
            case "optgroup": return "group";
            case "option": return "option";
            case "output": return "status";
            case "progress": return "progressbar";
            case "section": return element.HasAttribute("aria-label") || element.HasAttribute("aria-labelledby") ? "region" : null;
            case "select": return element.HasAttribute("multiple") || (int.TryParse(element.GetAttribute("size"), out var size) && size > 1) ? "listbox" : "combobox";
            case "summary": return "button";
            case "table": return "table";
            case "tbody" or "thead" or "tfoot": return "rowgroup";
            case "td": return "cell";
            case "textarea": return "textbox";
            case "th": return element.GetAttribute("scope") == "row" ? "rowheader" : "columnheader";
            case "tr": return "row";
            case "input":
                return (element.GetAttribute("type") ?? "text").ToLowerInvariant() switch
                {
                    "button" or "image" or "reset" or "submit" => "button",
                    "checkbox" => "checkbox",
                    "radio" => "radio",
                    "range" => "slider",
                    "number" => "spinbutton",
                    "search" => element.HasAttribute("list") ? "combobox" : "searchbox",
                    "hidden" => null,
                    "email" or "tel" or "text" or "url" => element.HasAttribute("list") ? "combobox" : "textbox",
                    _ => "textbox",
                };
            default: return null;
        }
    }

    /// <summary>Whether the element is a native form control whose state the browser exposes.</summary>
    public static bool IsNativeControl(IElement element)
        => element.LocalName is "input" or "select" or "textarea" or "button" or "meter" or "progress" or "option";

    /// <summary>Whether the element (or an ancestor) is hidden from assistive tech.</summary>
    public static bool IsHidden(IElement element)
    {
        for (var e = element; e is not null; e = e.ParentElement)
        {
            if (e.HasAttribute("hidden") || e.GetAttribute("aria-hidden") == "true") return true;
            if (e.LocalName is "template" or "script" or "style") return true;
            var style = e.GetAttribute("style");
            if (style is not null && (style.Replace(" ", "").Contains("display:none", StringComparison.OrdinalIgnoreCase)
                || style.Replace(" ", "").Contains("visibility:hidden", StringComparison.OrdinalIgnoreCase))) return true;
        }
        return false;
    }

    /// <summary>Whether the element is inert (or inside an inert subtree).</summary>
    public static bool IsInert(IElement element) => element.Closest("[inert]") is not null;

    /// <summary>Whether the element can receive focus (natively or through <c>tabindex</c>).</summary>
    public static bool IsFocusable(IElement element)
    {
        if (element.HasAttribute("disabled") && element.LocalName is "button" or "input" or "select" or "textarea" or "fieldset") return false;
        if (element.LocalName == "input" && string.Equals(element.GetAttribute("type"), "hidden", StringComparison.OrdinalIgnoreCase)) return false;
        if (element.HasAttribute("tabindex")) return true;
        return element.LocalName switch
        {
            "a" or "area" => element.HasAttribute("href"),
            "button" or "input" or "select" or "textarea" or "iframe" or "summary" => true,
            "audio" or "video" => element.HasAttribute("controls"),
            _ => element.GetAttribute("contenteditable") is "" or "true",
        };
    }

    /// <summary>Whether the element is in the sequential (Tab) focus order.</summary>
    public static bool IsTabbable(IElement element)
    {
        if (!IsFocusable(element)) return false;
        if (int.TryParse(element.GetAttribute("tabindex"), out var tabindex) && tabindex < 0) return false;
        if (element.Closest("fieldset[disabled]") is { } fieldset && element.LocalName is "button" or "input" or "select" or "textarea"
            && fieldset.QuerySelector("legend")?.Contains(element) != true) return false;
        return !IsHidden(element) && !IsInert(element);
    }
}
