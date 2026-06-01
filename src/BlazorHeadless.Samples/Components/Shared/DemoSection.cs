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
        new("accordion",        "Accordion"),
        new("aspect-ratio",     "Aspect Ratio"),
        new("avatar",           "Avatar"),
        new("button",           "Button"),
        new("checkbox",         "Checkbox"),
        new("close-button",     "CloseButton"),
        new("combobox",         "Combobox"),
        new("context-menu",     "Context Menu"),
        new("data-interactive", "DataInteractive"),
        new("dialog",           "Dialog"),
        new("disclosure",       "Disclosure"),
        new("field",            "Field / Input"),
        new("fieldset",         "Fieldset"),
        new("focus-trap",       "FocusTrap"),
        new("listbox",          "Listbox"),
        new("menu",             "Menu"),
        new("popover",          "Popover"),
        new("portal",           "Portal"),
        new("radiogroup",       "Radio Group"),
        new("switch",           "Switch"),
        new("tabs",             "Tabs"),
        new("tooltip",          "Tooltip"),
        new("transition",       "Transition"),
    ];

    public static string Href(DemoSection section, string slug) =>
        section == DemoSection.Tailwind ? $"tw/{slug}" : slug;

    public static string SectionRoot(DemoSection section) =>
        section == DemoSection.Tailwind ? "tw/" : "/";
}
