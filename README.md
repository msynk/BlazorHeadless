# BlazorHeadless

**Accessible, unstyled UI primitives for Blazor — with accessibility testing built in.**

BlazorHeadless gives you the hard parts of interactive UI — keyboard interaction, focus management, ARIA semantics, positioning, dismissal, announcements — as plain Razor components that render semantic HTML and nothing else. You bring the styles: plain CSS, Tailwind, Bootstrap, Fluent tokens or your own design system.

It ships with **BlazorHeadless.Testing**, a bUnit-based accessibility toolkit (WAI-ARIA rule engine, accessible-name and focus-order inspection, keyboard helpers, live-region assertions) used to test every component in this repository — and usable for testing *your* components too.

Inspired by [Radix UI](https://www.radix-ui.com/), [React Aria](https://react-spectrum.adobe.com/react-aria/), [Headless UI](https://headlessui.com/), [Ark UI](https://ark-ui.com/) and [cmdk](https://cmdk.paco.me/), implemented natively for Blazor (Server, WebAssembly, Auto and static SSR prerendering).

---

## Contents

- [Why BlazorHeadless](#why-blazorheadless)
- [Installation](#installation)
- [Components](#components)
- [Concepts](#concepts)
- [Styling](#styling)
- [Testing accessibility](#testing-accessibility)
- [Sample app and docs](#sample-app-and-docs)
- [Contributing](#contributing)

## Why BlazorHeadless

- **Zero visual opinion.** No CSS ships with the library. Components render semantic elements and expose their state through `data-*` and ARIA attributes.
- **Accessibility is the feature.** Every widget follows the [WAI-ARIA Authoring Practices](https://www.w3.org/WAI/ARIA/apg/): roles, states, properties, keyboard interaction, focus management, typeahead and screen-reader announcements.
- **Composable.** Compound components (`BhMenu` → `BhMenuButton` + `BhMenuItems` + `BhMenuItem`) share state through cascading contexts, so you control the markup and nesting.
- **Controlled or uncontrolled.** Every stateful component works with `@bind-…`, with `Default…` initial values, or with change callbacks (`On…Change`).
- **Forms built in.** `EditForm` validation, `BhField` label / description / error wiring, hidden inputs for plain form posts.
- **Polymorphic.** Render any element with `As="…"`; `class`, `style` and any attribute flow through.
- **Robust overlays.** Layered dismissal (Escape, outside click, focus out) that respects nesting, focus scopes with restore, `inert` / `aria-hidden` for modals, scroll locking, portals that keep the component tree intact, and exit animations.
- **Positioning engine included.** Anchor panels with flip, shift, size and arrow support — no Popper / Floating UI dependency.
- **Large data.** Option, item and tree collections use stable ids (never indexes) and support virtualization.
- **Tested.** Every component is covered by keyboard and ARIA tests, plus automated accessibility audits.

## Installation

```bash
dotnet add package BlazorHeadless
```

Register the services (recommended — components fall back to sensible defaults without it):

```csharp
// Program.cs (server and, for WebAssembly / Auto, the client project too)
builder.Services.AddBlazorHeadless();
```

Import the namespace:

```razor
@* _Imports.razor *@
@using BlazorHeadless
```

That's it. The small JavaScript runtime (`blazor-headless.js`) is loaded automatically through a Razor class library initializer; there is no script tag to add. Components must run in an **interactive** render mode to respond to input; during prerendering they render their initial, accessible markup.

Requirements: .NET 10.

## Components

| Area | Components |
| --- | --- |
| **Overlays** | `BhDialog`, `BhAlertDialog`, `BhPopover`, `BhTooltip` (+ `BhTooltipProvider`), `BhHoverCard`, `BhPortal` |
| **Menus** | `BhMenu` (submenus, checkbox / radio items, groups, labels), `BhMenubar`, `BhContextMenu` |
| **Selection** | `BhListbox` (single / multi, typeahead, virtualized), `BhCombobox` (autocomplete, multi, virtualized), `BhCommand` / `BhCommandDialog` (command palette with fuzzy ranking and nested pages) |
| **Hierarchies & data** | `BhTree` (single / multi / tri-state checkboxes, lazy loading), `BhGrid` (data grid and treegrid with cell navigation and row selection), `BhSortable` (pointer and keyboard reordering with announcements) |
| **Disclosure** | `BhTabGroup`, `BhAccordion`, `BhDisclosure`, `BhCollapsible` |
| **Form controls** | `BhField` (`BhLabel`, `BhDescription`, `BhErrorMessage`, `BhInput`, `BhSelect`, `BhTextarea`), `BhFieldset`, `BhCheckbox` (+ `BhCheckboxGroup`, select-all), `BhSwitch`, `BhRadioGroup`, `BhSlider` (range, vertical), `BhToggle`, `BhToggleGroup` |
| **Layout & feedback** | `BhToolbar`, `BhSeparator`, `BhProgress`, `BhMeter`, `BhToaster` / `BhToastViewport`, `BhAvatar`, `BhAspectRatio`, `BhButton`, `BhCloseButton` |
| **Accessibility utilities** | `BhFocusScope` / `BhFocusTrap`, `BhRovingFocusGroup`, `BhVisuallyHidden`, `BhLiveRegion`, `IBhAnnouncer`, `BhHotkey`, `BhDirectionProvider`, `BhDataInteractive`, `BhTransition` |

### A taste

```razor
<BhMenu>
    <BhMenuButton class="btn">Options</BhMenuButton>
    <BhMenuItems class="menu" Anchor="@("bottom start")">
        <BhMenuItem OnClick="Edit">Edit</BhMenuItem>
        <BhMenuItem OnClick="Duplicate" Disabled="true">Duplicate</BhMenuItem>
        <BhMenuSeparator />
        <BhMenuSub>
            <BhMenuSubTrigger>Share</BhMenuSubTrigger>
            <BhMenuSubContent class="menu">
                <BhMenuItem OnClick="CopyLink">Copy link</BhMenuItem>
            </BhMenuSubContent>
        </BhMenuSub>
    </BhMenuItems>
</BhMenu>
```

```razor
<EditForm Model="model" OnValidSubmit="Save">
    <DataAnnotationsValidator />
    <BhField>
        <BhLabel>Country</BhLabel>
        <BhCombobox TValue="string" @bind-Value="model.Country" OnQueryChange="q => query = q">
            <BhComboboxInput TValue="string" class="input" />
            <BhComboboxOptions TValue="string" class="options" Anchor="@("bottom start")">
                @foreach (var c in Countries.Where(c => BhFilter.Contains(c, query)))
                {
                    <BhComboboxOption TValue="string" Value="@c">@c</BhComboboxOption>
                }
                <BhComboboxEmpty TValue="string">No matches</BhComboboxEmpty>
            </BhComboboxOptions>
        </BhCombobox>
        <BhErrorMessage class="error" />
    </BhField>
</EditForm>
```

```razor
<BhCommandDialog @bind-Open="paletteOpen" Shortcut="mod+k" Label="Command palette">
    <BhCommandInput placeholder="Type a command…" />
    <BhCommandList>
        <BhCommandEmpty>No results.</BhCommandEmpty>
        <BhCommandGroup Heading="Navigation">
            <BhCommandItem OnSelect="@(() => Nav.NavigateTo("/settings"))" Keywords="@(["preferences"])">Settings</BhCommandItem>
        </BhCommandGroup>
    </BhCommandList>
</BhCommandDialog>
```

## Concepts

### Compound components and state

Parent components own state and cascade it to their parts. Every stateful part supports three modes:

```razor
<BhTabGroup @bind-Value="tab">…</BhTabGroup>                  @* controlled *@
<BhTabGroup DefaultValue="profile">…</BhTabGroup>             @* uncontrolled *@
<BhTabGroup OnValueChange="v => Log(v)">…</BhTabGroup>        @* uncontrolled + notified *@
```

### Rendering state in content: `Template`

`ChildContent` is always a plain `RenderFragment`, so components nest freely inside `EditForm`, panels and each other. When markup depends on a component's state, use its `Template` instead:

```razor
<BhDisclosureButton>
    <Template Context="d">@(d.IsOpen ? "Hide" : "Show") details</Template>
</BhDisclosureButton>
```

Most of the time CSS is enough: style `[data-state="open"]`, `[aria-expanded="true"]`, `[data-active]` … directly.

### Common parameters

Every component derives from `BhComponentBase`:

| Parameter | Purpose |
| --- | --- |
| `As` | Render a different element (`As="a"`, `As="li"`, `As="table"` …) |
| `Id` | Explicit id; generated otherwise. ARIA references (`aria-controls`, `aria-labelledby` …) are wired for you |
| `Ref` | `Action<ElementReference>` to get the element |
| any attribute | `class`, `style`, `data-*`, `aria-*` … are forwarded; `class` / `style` merge with internal values |

### Positioning

Floating parts (`BhMenuItems`, `BhListboxOptions`, `BhComboboxOptions`, `BhPopoverPanel`, `BhTooltipContent`, `BhHoverCardContent`, submenus …) take an `Anchor`:

```razor
<BhPopoverPanel Anchor="@(new BhAnchorOptions { To = "bottom end", Gap = 6, MatchReferenceWidth = true })" />
<BhMenuItems Anchor="@("right start")" />   @* a placement string converts implicitly *@
```

Placements: `top | right | bottom | left`, optionally with `start` / `end`. Options: `Gap`, `Offset`, `Padding`, `Flip`, `Shift`, `MatchReferenceWidth`, `HideWhenDetached`, `ArrowPadding`. Panels receive `data-anchor="bottom start"` (the resolved side) and the CSS variables `--bh-anchor-width`, `--bh-anchor-height`, `--bh-available-width`, `--bh-available-height`, `--bh-transform-origin`. Arrows (`BhPopoverArrow`, `BhTooltipArrow`, `BhMenuArrow`) are positioned automatically.

### Overlays, dismissal and focus

- Escape and outside interaction dismiss the **topmost** layer only; `OnDismissRequest` lets you cancel (`r.Cancel = true`).
- Modals (`BhDialog`, `BhAlertDialog`, `Modal` popovers) trap focus, make the rest of the page `inert`, lock scrolling and restore focus on close.
- `BhPortal` moves the DOM node but keeps the component tree, so cascading values and events keep working.
- Exit animations: closing parts stay mounted until their CSS animations / transitions finish (`data-state="closed"`).

### Announcements

`IBhAnnouncer` speaks through a shared polite / assertive live region. Components use it for result counts (command palette), toasts and drag-and-drop steps; you can inject it too:

```razor
@inject IBhAnnouncer Announcer
<button @onclick='() => Announcer.AnnounceAsync("Saved")'>Save</button>
```

### Right-to-left

Wrap content in `<BhDirectionProvider Dir="BhDirection.Rtl">` (or set `Dir` per component) and horizontal arrow keys mirror.

## Styling

Every component exposes state as attributes. The most common hooks:

```css
[data-state="open"], [data-state="closed"]            /* disclosure, menus, popovers, dialogs */
[data-state="checked"], [data-state="indeterminate"]  /* checkbox, switch, radio */
[data-state="active"]                                 /* selected tab */
[data-state="on"]                                     /* pressed toggle */
[data-active], [data-highlighted]                     /* keyboard / pointer highlight in menus, listboxes */
[data-selected], [data-disabled], [data-invalid]
[data-orientation="vertical"], [data-anchor^="top"]
[aria-expanded="true"], [aria-selected="true"], [aria-pressed="true"]
```

**Plain CSS**

```css
.menu-item[data-highlighted] { background: #eff6ff; }
.dialog[data-state="closed"] { animation: fade-out 150ms ease-in; }
```

**Tailwind v4** — arbitrary data / aria variants work out of the box:

```razor
<BhMenuItem class="rounded px-2 py-1 data-highlighted:bg-blue-50 data-disabled:opacity-50">Edit</BhMenuItem>
<BhTab class="border-b-2 border-transparent data-[state=active]:border-blue-600">Profile</BhTab>
```

**Bootstrap** — reuse its classes and map states:

```razor
<BhMenuItems class="dropdown-menu show">
    <BhMenuItem class="dropdown-item">Edit</BhMenuItem>
</BhMenuItems>
```
```css
.dropdown-item[data-highlighted] { background-color: var(--bs-dropdown-link-hover-bg); }
```

**Design tokens (Fluent or your own)** — headless components consume whatever custom properties your theme defines:

```css
.menu { background: var(--colorNeutralBackground1); border-radius: var(--borderRadiusMedium); }
```

> If you give a hideable part an explicit `display` (e.g. `display: flex`), keep `[hidden] { display: none }` working: `.panel[hidden] { display: none; }`, or in Tailwind `@layer base { [hidden] { display: none !important; } }`.

## Testing accessibility

```bash
dotnet add package BlazorHeadless.Testing
```

```csharp
public class SaveMenuTests : BhTestContext
{
    [Fact]
    public void Menu_is_accessible_and_keyboard_operable()
    {
        var cut = Render<SaveMenu>();               // .razor test files can use Render(@<SaveMenu />)
        cut.Find("button").Press("ArrowDown");

        var menu = cut.Find("[role=menu]");
        Assert.Equal("Save as…", cut.ActiveDescendant(menu)!.AccessibleName());
        cut.ShouldBeAccessible();                   // WAI-ARIA rule engine
        Assert.Single(cut.TabSequence());           // one tab stop
    }
}
```

`BhTestContext` emulates the browser behaviour the components rely on (document order and text of items, so typeahead and filtering work like in a browser) and records announcements (`await Announcer.WaitForAsync("3 results")`). The audit (`ShouldBeAccessible`, `Audit`) checks roles, required states, allowed attributes, ID references, required owned elements / context, accessible names, focusable content inside `aria-hidden`, nested interactive controls, positive `tabindex`, presentational conflicts, `aria-activedescendant` targets and form labels, and can render Markdown / JSON reports for CI. See [the Testing README](src/BlazorHeadless.Testing/README.md).

The audit complements — it does not replace — browser-based checks (e.g. axe-core with Playwright) and testing with real assistive technology.

## Sample app and docs

```bash
dotnet run --project src/BlazorHeadless.Samples
```

Every component is demonstrated twice — hand-written CSS at `/<component>` and Tailwind at `/tw/<component>` — with the Razor and CSS for each demo one click away. The static documentation site in [`docs/`](docs/README.md) is generated from the same pages (`node docs/build.mjs`).

```
src/BlazorHeadless/            the library (components, Core, wwwroot/blazor-headless.js)
src/BlazorHeadless.Testing/    accessibility test toolkit (bUnit)
src/BlazorHeadless.Samples/    demo app (CSS + Tailwind)
tests/BlazorHeadless.Tests/    component and engine tests
tests/e2e/                     real-browser tests: axe-core + keyboard (Playwright)
docs/                          static docs generator
```

## Contributing

```bash
dotnet build src/BlazorHeadless.slnx
dotnet test src/BlazorHeadless.slnx          # component, keyboard and ARIA tests (bUnit)

cd tests/e2e && npm ci && npx playwright install chromium
npx playwright test                            # axe-core on every demo page + keyboard flows in Chromium
```

CI runs all of the above on every pull request; a demo page that fails axe (WCAG 2.2 AA) fails the build.

New components should follow the APG pattern for their role, cover keyboard interaction and ARIA in tests (`ShouldBeAccessible()` on every state), and come with CSS and Tailwind demos. See [CHANGELOG.md](CHANGELOG.md) for release notes and migration guidance.

## License

[MIT](LICENSE)
