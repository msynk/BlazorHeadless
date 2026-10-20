# Changelog

All notable changes to BlazorHeadless and BlazorHeadless.Testing. The project follows [Semantic Versioning](https://semver.org/); while in `0.x`, minor versions may contain breaking changes, which are listed with migration steps.

## 0.2.0 — unreleased

A ground-up review of behaviour and accessibility, a new runtime, many new primitives and a testing package.

### Added

- **New components**
  - Menus: `BhMenubar`, submenus (`BhMenuSub`, `BhMenuSubTrigger`, `BhMenuSubContent`), `BhMenuCheckboxItem`, `BhMenuRadioGroup` / `BhMenuRadioItem`, `BhMenuItemIndicator`, `BhMenuGroup`, `BhMenuLabel`, `BhMenuSeparator`, `BhMenuArrow`.
  - Command palette: `BhCommand`, `BhCommandInput`, `BhCommandList`, `BhCommandGroup`, `BhCommandItem`, `BhCommandEmpty`, `BhCommandSeparator`, `BhCommandPage` (nested pages), `BhCommandDialog` (⌘K / Ctrl+K). Fuzzy ranking (`BhFilter.Score`), keyword aliases, announced result counts.
  - Hierarchies and data: `BhTree` (single / multi selection, tri-state checkboxes, lazy loading, typeahead), `BhGrid` (data grid and treegrid, cell navigation, row selection, sortable headers), `BhSortable` (pointer + keyboard reordering with live announcements).
  - Form controls: `BhSlider` (single / range, vertical, RTL), `BhToggle`, `BhToggleGroup`, `BhCheckboxGroup` (with `SelectAll`), `BhCheckboxIndicator`, `BhSwitchThumb`, `BhErrorMessage`.
  - Feedback and layout: `BhToaster` / `BhToastViewport` / `BhToast` (+ title, description, action, close), `BhProgress`, `BhMeter`, `BhToolbar`, `BhSeparator`.
  - Grid extras: `BhGridRowToggle` (treegrid chevron), `BhGridRowState`, `BhGridCell.InitialTabStop`, `BhGrid.SetExpandedAsync`.
  - Accessibility utilities: `BhFocusScope`, `BhRovingFocusGroup` / `BhRovingFocusItem`, `BhVisuallyHidden`, `BhLiveRegion`, `IBhAnnouncer`, `BhHotkey`, `BhDirectionProvider`, `BhAccordionHeader`, `BhComboboxEmpty`, `BhPopoverArrow`.
- **Runtime** (`blazor-headless.js`, loaded automatically): layered dismissal, focus scopes with restore, `inert` / `aria-hidden` for modals, scroll lock, portals that keep the component tree, presence (exit animations), anchor positioning (flip, shift, size, arrow, hide), hotkeys, typeahead support, sortable and slider pointer handling.
- **Large lists**: data-driven and virtualized options for `BhListbox` / `BhCombobox` (`Items`, `ItemTemplate`).
- **Forms**: `EditForm` validation for every bound control, `BhField` wiring of labels, descriptions and error messages, hidden inputs for plain posts, `Required` / `Invalid` / `Disabled` cascading from `BhField` / `BhFieldset`.
- **Right-to-left** support for every arrow-key interaction.
- `BhCommandDialog` forwards `Search`, `Value`, `Page` (+ change callbacks), `Filter`, `ResultsAnnouncement` and `CloseOnSelect`; choosing an item that opens a nested page keeps the palette open.
- Typeahead text of nested items (e.g. tree items) excludes their child items.
- The shared live regions are created at startup so the first announcement isn't lost.
- **Browser test suite** (`tests/e2e`): axe-core (WCAG 2.2 AA) on every demo page plus keyboard and focus flows in Chromium, run in CI.
- **BlazorHeadless.Testing** (new package): `BhTestContext`, the WAI-ARIA audit (`ShouldBeAccessible`, `Audit`, Markdown / JSON reports), accessible name / description, tab sequence, keyboard helpers, `LastFocusRequest`, recording announcer with `WaitForAsync`.

### Fixed (highlights)

- `aria-activedescendant` could point at the wrong or a missing option after filtering (items are now tracked by stable ids, not indexes).
- Focus was restored to `<body>` after closing a dialog (the page was still `inert`).
- `aria-describedby` could reference error messages that were never rendered; error messages now re-render when validation changes.
- `<label for>` pointed at non-labelable elements (radio groups, sliders); those controls are now labelled through `aria-labelledby` and focused when the label is clicked.
- Treegrid arrow keys could land on rows hidden under a collapsed parent.
- Toasts never rendered (invalid render-tree key).
- The command empty state flashed on first render.
- Tabs referenced unmounted panels with `aria-controls`.
- `BhDataInteractive` with `Disabled` didn't tell assistive tech (now `aria-disabled`).
- `BhCommandSeparator` used `role="separator"` inside a listbox, which only allows options and groups (now presentational).
- Parameterless parts (`<BhTreeItemToggle />`, `<BhSliderRange />`, `<BhSwitchThumb />`) didn't follow state changes.
- `BhSlider` now emits `data-dragging` on the root as documented.

### Breaking changes and migration

1. **`ChildContent` is a plain `RenderFragment` everywhere.** Templated child content made components fail to compile inside `EditForm` and other templated content (Razor error RZ9999). State-dependent content moves to the new `Template` parameter:

   ```razor
   @* before *@
   <BhDisclosureButton Context="d">@(d.IsOpen ? "Hide" : "Show")</BhDisclosureButton>

   @* after *@
   <BhDisclosureButton>
       <Template Context="d">@(d.IsOpen ? "Hide" : "Show")</Template>
   </BhDisclosureButton>
   ```

   Affected: accordion / collapsible / disclosure triggers and panels, tabs and tab panels, checkbox, checkbox group, checkbox indicator, switch, radio, toggle, toggle-group item, button, menu button and items, context-menu trigger and items, listbox button and options, combobox button and options, popover button and panel, tooltip and hover-card trigger and content, alert-dialog trigger, command item and page, tree item and toggle, slider thumb, progress, meter, error message, data-interactive. Item templates keep their context: `BhSortable`, `BhToastViewport`, and `ItemTemplate` on listbox / combobox options. Often CSS on `data-state` / `aria-*` attributes replaces the template altogether.

2. **Select-all checkboxes**: use `<BhCheckbox SelectAll="true">` inside a `BhCheckboxGroup` instead of wiring `AllChecked` / `SomeChecked` / `SetAll` by hand (still available through the group's `Template`).

3. **JavaScript**: `headlessui.js` was replaced by `blazor-headless.js`, which loads itself. Remove any `<script src="_content/BlazorHeadless/headlessui.js">` tag.

4. **`AddBlazorHeadless()`** is recommended rather than required; it now also registers `IBhAnnouncer` and `BhToaster`.

5. **Menu markup**: menu panels render `div`s with `role="menu"`; use `BhMenuLabel`, `BhMenuGroup` and `BhMenuSeparator` instead of `<li role="presentation">` / raw separators inside menus. The same parts exist for context menus (`BhContextMenuLabel`, `BhContextMenuGroup`, `BhContextMenuSeparator`).

6. **Disclosure-style panels stay mounted while closed** (`hidden`, or `hidden="until-found"` with `HiddenUntilFound`) so exit animations and find-in-page work; set `Unmount="true"` where available to remove them instead. Tooltip content also stays mounted (hidden) so the trigger is always described.

7. **Toast viewport**: the `region` landmark now wraps the toast list; your attributes go on the list (`<ol>`).

8. **Single toggle groups** are radio groups: arrow keys now select (APG), except inside a `BhToolbar` where arrows only move focus. Deselecting the pressed item is allowed by default (`AllowEmpty="true"`); set `AllowEmpty="false"` to forbid it.

9. **Listbox button** without content shows the selected option text (or `Placeholder`).

## 0.1.0

Initial preview release.
