namespace BlazorHeadless.Samples.Components.Shared;

/// <summary>
/// Identifies which styling section a demo page belongs to.
/// Used to drive section-aware navigation and the section switcher.
/// </summary>
public enum DemoSection
{
    /// <summary>Hand-written CSS samples (lives at <c>/&lt;component&gt;</c>).</summary>
    Css,

    /// <summary>Tailwind CSS samples (lives at <c>/tw/&lt;component&gt;</c>).</summary>
    Tailwind,
}

/// <summary>
/// Static catalogue of demo components shared between both styling sections.
/// </summary>
public static class DemoCatalog
{
    public sealed record Entry(string Slug, string Label);

    public static readonly IReadOnlyList<Entry> Components =
    [
        new("accessibility",   "Accessibility Utilities"),
        new("accordion",       "Accordion"),
        new("alert-dialog",    "Alert Dialog"),
        new("aspect-ratio",    "Aspect Ratio"),
        new("avatar",          "Avatar"),
        new("button",          "Button"),
        new("checkbox",        "Checkbox"),
        new("close-button",    "CloseButton"),
        new("collapsible",     "Collapsible"),
        new("combobox",        "Combobox"),
        new("command",         "Command Palette"),
        new("context-menu",    "Context Menu"),
        new("data-interactive", "DataInteractive"),
        new("dialog",          "Dialog"),
        new("disclosure",      "Disclosure"),
        new("field",           "Field / Input"),
        new("fieldset",        "Fieldset"),
        new("focus-trap",      "FocusTrap"),
        new("grid",            "Grid / TreeGrid"),
        new("hover-card",      "Hover Card"),
        new("listbox",         "Listbox"),
        new("menu",            "Menu"),
        new("menubar",         "Menubar"),
        new("popover",         "Popover"),
        new("portal",          "Portal"),
        new("progress",        "Progress / Meter"),
        new("radiogroup",      "Radio Group"),
        new("slider",          "Slider"),
        new("sortable",        "Sortable"),
        new("switch",          "Switch"),
        new("tabs",            "Tabs"),
        new("toast",           "Toast"),
        new("toggle-group",    "Toggle / Toggle Group"),
        new("toolbar",         "Toolbar"),
        new("tooltip",         "Tooltip"),
        new("transition",      "Transition"),
        new("tree",            "Tree View"),
    ];

    public static string Href(DemoSection section, string slug) =>
        section == DemoSection.Tailwind ? $"tw/{slug}" : slug;

    public static string SectionRoot(DemoSection section) =>
        section == DemoSection.Tailwind ? "tw/" : "/";
}
