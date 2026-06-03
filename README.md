# BlazorHeadless

A headless UI component library for Blazor: behaviour, accessibility, and state management without any visual opinion. Style it your way with plain CSS, Tailwind, or any design system.

Inspired by [Headless UI](https://headlessui.com/), [Radix UI](https://www.radix-ui.com/), and [Ark UI](https://ark-ui.com/) patterns, ported natively to Blazor.

## Why headless?

- **Zero visual opinion** — the library ships no CSS, only semantic HTML and `data-*` hooks.
- **Accessibility built in** — ARIA roles, states, and keyboard interactions are handled for you.
- **Polymorphic rendering** — every component renders as any HTML element via the `As` parameter.
- **Styling hooks via data attributes** — `[data-state]`, `[data-disabled]`, `[data-active]`, and friends.
- **Attribute merging** — `class` and `style` concatenate; everything else lets the consumer win.
- **Controlled and uncontrolled** — every stateful component supports both modes.
- **Render-prop context** — child content receives a typed render context for state-driven UI.
- **Anchor positioning** — automatic floating panel positioning with flip/shift via JS interop.

## Requirements

- .NET 10
- `Microsoft.AspNetCore.Components.Web` 10.x

## Setup

```csharp
// Program.cs
builder.Services.AddBlazorHeadless();
```

This registers the JS interop service used by Dialog, Popover, Transition, Portal, FocusTrap, and anchor positioning.

If you use `BhPortal`, also add a portal outlet to your root layout:

```razor
<BhPortalOutlet />
```

## Components

| Component | Description |
| --- | --- |
| **BhAccordion** | Single or multiple expandable sections |
| **BhAspectRatio** | Constrain content to a desired width-to-height ratio |
| **BhAvatar** | Image with a graceful fallback for representing a user or entity |
| **BhButton** | Polymorphic button with disabled and loading states |
| **BhCheckbox** | Custom checkbox with indeterminate state support |
| **BhCloseButton** | Pre-wired button that closes the nearest Dialog, Popover, or Disclosure |
| **BhCombobox** | Typeable autocomplete with consumer-driven filtering |
| **BhDataInteractive** | Forward `data-hover` / `data-active` / `data-focus` attributes for unified state styling |
| **BhDialog** | Modal with focus trap, scroll lock, and inert background |
| **BhDisclosure** | Single show/hide region |
| **BhField** | Form field grouping with `BhLabel`, `BhDescription`, `BhInput`, `BhSelect`, `BhTextarea` |
| **BhFieldset** | Group form controls under a `BhLegend`, with cascading disabled state |
| **BhFocusTrap** | Trap keyboard focus inside a container |
| **BhHoverCard** | Preview content behind a link on hover or focus |
| **BhListbox** | Custom select with single/multi-select, typeahead, and form integration |
| **BhMenu** | Dropdown menu with keyboard nav, typeahead, and virtual focus |
| **BhContextMenu** | Right-click menu opened at the pointer, with keyboard nav and typeahead |
| **BhPopover** | Non-modal floating panel with focus management and group coordination |
| **BhPortal** | Render children into a different part of the DOM tree |
| **BhRadioGroup** | Single-select radio group |
| **BhSwitch** | Two-state toggle with optional hidden form input |
| **BhTabGroup** | Tabbed interface with keyboard navigation |
| **BhTooltip** | Pop-up label that appears on hover or focus, with delay coordination via provider |
| **BhTransition** | CSS class-based enter/leave animations with lifecycle callbacks |

## Anchor Positioning

Dropdown panels (`BhMenuItems`, `BhListboxOptions`, `BhComboboxOptions`, `BhPopoverPanel`) support automatic positioning relative to their trigger via the `Anchor` parameter:

```razor
<BhMenu>
    <BhMenuButton>Options ▾</BhMenuButton>
    <BhMenuItems Anchor="@(new BhAnchorOptions { To = "bottom start", Gap = 4 })">
        <BhMenuItem OnClick="Edit">Edit</BhMenuItem>
        <BhMenuItem OnClick="Delete">Delete</BhMenuItem>
    </BhMenuItems>
</BhMenu>
```

### Placement options

Use `top`, `right`, `bottom`, or `left` to center along an edge. Combine with `start` or `end` for corner alignment:

```
top start    |  top     |  top end
left start   |  left    |  left end
right start  |  right   |  right end
bottom start |  bottom  |  bottom end
```

### BhAnchorOptions

| Property | Default | Description |
| --- | --- | --- |
| `To` | `"bottom"` | Placement string |
| `Gap` | `0` | Space (px) between trigger and panel |
| `Offset` | `0` | Nudge along the alignment axis |
| `Padding` | `8` | Minimum space from viewport edges |

### Features

- **Auto-flip** — flips to the opposite side when there's not enough space.
- **Auto-shift** — clamps the panel within viewport bounds.
- **Auto-update** — repositions on scroll, resize, and element size changes via `ResizeObserver`.
- **CSS variables** — exposes `--button-width`, `--anchor-gap`, `--anchor-offset`, `--anchor-padding` on the panel.
- **Zero dependencies** — self-contained positioning engine, no Floating UI or Popper needed.

### Matching trigger width

```css
.my-dropdown {
    width: var(--button-width);
}
```

## Quick examples

### Accordion

```razor
<BhAccordion DefaultValue="item-1">
    <BhAccordionItem Value="item-1">
        <BhAccordionTrigger Context="t">
            Section 1 <span>@(t.IsOpen ? "−" : "+")</span>
        </BhAccordionTrigger>
        <BhAccordionContent>Content for section 1.</BhAccordionContent>
    </BhAccordionItem>
</BhAccordion>
```

### Combobox

```razor
<BhCombobox TValue="string" Value="@fruit" OnValueChange="v => fruit = v"
            OnQueryChange="Filter" DisplayValue="v => v ?? string.Empty">
    <BhComboboxInput TValue="string" Placeholder="Search…" />
    <BhComboboxOptions TValue="string">
        @foreach (var f in filtered)
        {
            <BhComboboxOption TValue="string" Value="@f">@f</BhComboboxOption>
        }
    </BhComboboxOptions>
</BhCombobox>
```

### Dialog

```razor
<BhDialog Open="@showDialog" OnClose="() => showDialog = false">
    <BhDialogBackdrop class="backdrop" />
    <BhDialogPanel class="dialog-panel">
        <BhDialogTitle>Confirm</BhDialogTitle>
        <BhDialogDescription>Are you sure?</BhDialogDescription>
        <BhCloseButton>OK</BhCloseButton>
    </BhDialogPanel>
</BhDialog>
```

### Disclosure

```razor
<BhDisclosure>
    <BhDisclosureButton Context="d">@(d.IsOpen ? "Hide" : "Show") details</BhDisclosureButton>
    <BhDisclosurePanel>Hidden content here.</BhDisclosurePanel>
</BhDisclosure>
```

### Listbox

```razor
<BhListbox TValue="string" Value="@person" OnValueChange="v => person = v">
    <BhListboxButton TValue="string" Context="b">
        @(b.Value ?? "Select…")
    </BhListboxButton>
    <BhListboxOptions TValue="string">
        <BhListboxOption TValue="string" Value="@("alice")">Alice</BhListboxOption>
        <BhListboxOption TValue="string" Value="@("bob")">Bob</BhListboxOption>
    </BhListboxOptions>
</BhListbox>
```

### Menu

```razor
<BhMenu>
    <BhMenuButton class="btn" Context="b">
        Options
        <span class="chevron @(b.IsOpen ? "open" : "")">▾</span>
    </BhMenuButton>
    <BhMenuItems class="dropdown">
        <BhMenuItem OnClick="Edit"   Label="Edit">Edit</BhMenuItem>
        <BhMenuItem OnClick="Delete" Label="Delete" Disabled="true">Delete</BhMenuItem>
    </BhMenuItems>
</BhMenu>
```

### Context Menu

```razor
<BhContextMenu>
    <BhContextMenuTrigger class="trigger">Right-click here</BhContextMenuTrigger>
    <BhContextMenuContent class="menu">
        <BhContextMenuItem OnClick="Back"    Label="Back">Back</BhContextMenuItem>
        <BhContextMenuItem OnClick="Forward" Label="Forward" Disabled="true">Forward</BhContextMenuItem>
        <BhContextMenuSeparator />
        <BhContextMenuLabel>Danger zone</BhContextMenuLabel>
        <BhContextMenuItem OnClick="Delete"  Label="Delete">Delete</BhContextMenuItem>
    </BhContextMenuContent>
</BhContextMenu>
```

### Popover

```razor
<BhPopover>
    <BhPopoverButton Context="b">Info ▾</BhPopoverButton>
    <BhPopoverPanel>
        <p>Panel content here.</p>
        <BhPopoverButton Context="closeBtn">Close</BhPopoverButton>
    </BhPopoverPanel>
</BhPopover>
```

### Switch

```razor
<BhSwitch Checked="@enabled" OnCheckedChange="v => enabled = v" class="switch" Context="s">
    <span class="switch-thumb"
          data-state="@(s.IsChecked ? "checked" : "unchecked")"></span>
</BhSwitch>
```

### Tabs

```razor
<BhTabGroup>
    <BhTabList>
        <BhTab>Account</BhTab>
        <BhTab>Profile</BhTab>
    </BhTabList>
    <BhTabPanels>
        <BhTabPanel>Account settings</BhTabPanel>
        <BhTabPanel>Profile settings</BhTabPanel>
    </BhTabPanels>
</BhTabGroup>
```

## Common parameters

Every component inherits from `BhComponentBase` and supports:

| Parameter | Purpose |
| --- | --- |
| `As` | Override the rendered HTML tag (e.g. `As="a"`) |
| `Id` | Explicit HTML `id`; auto-generated when omitted |
| `Ref` | `Action<ElementReference>` for DOM access and focus management |
| `AdditionalAttributes` | Captured unmatched attributes — `class`, `style`, `data-*`, `aria-*`, anything HTML |

## Styling with data attributes

Components emit data attributes that mirror their state:

```css
[data-state="open"]          { /* expanded / open */ }
[data-state="closed"]        { /* collapsed / closed */ }
[data-state="checked"]       { /* switch/checkbox on */ }
[data-state="unchecked"]     { /* switch/checkbox off */ }
[data-state="indeterminate"] { /* checkbox mixed state */ }
[data-state="active"]        { /* selected tab */ }
[data-active]                { background: #eff6ff; }   /* highlighted menu/listbox option */
[data-selected]              { font-weight: 600; }      /* selected listbox/combobox option */
[data-disabled]              { opacity: 0.5; pointer-events: none; }
[data-loading]               { cursor: progress; }
[data-orientation="horizontal"] { /* tablists, radio groups */ }
[data-orientation="vertical"]   { /* tablists, radio groups */ }
```

## Important CSS note

When your panel CSS sets an explicit `display` value (e.g. `display: flex`), you must add a `[hidden]` override so the `hidden` attribute works correctly:

```css
.my-panel {
    display: flex;
    /* ... */
}

.my-panel[hidden] {
    display: none;
}
```

Tailwind users can register a single global rule once in their input file:

```css
@layer utilities {
    [hidden] { display: none !important; }
}
```

## Sample app

The repo ships with a sample project that demonstrates every component twice — once with hand-written CSS and once with Tailwind utilities, side by side. Both versions render the same headless components; only the styling layer changes.

```
src/BlazorHeadless.Samples/
├── Components/Pages/Css/        # Plain CSS demos     (routes: /<slug>)
├── Components/Pages/Tailwind/   # Tailwind v4 demos   (routes: /tw/<slug>)
├── tailwind/input.css           # Tailwind entry point + custom data-* variants
└── wwwroot/app.css              # Hand-written CSS for the CSS section
```

Run it with:

```bash
dotnet run --project src/BlazorHeadless.Samples
```

The Tailwind section uses Tailwind v4's standalone CLI, downloaded automatically on first build by an MSBuild target — no Node.js required. Custom data-attribute variants (`data-state-open:`, `data-state-checked:`, `data-active:`, `data-disabled:`, etc.) are registered in `tailwind/input.css` so you can style states the Tailwind way:

```html
<button class="bg-white data-state-open:bg-zinc-100 data-disabled:opacity-50">…</button>
```

A header switcher on every demo page lets you flip between the CSS and Tailwind implementations of the same component.

## Project status

Active development. APIs may evolve as the library approaches feature parity with Headless UI v2.

## License

See [LICENSE](LICENSE).
