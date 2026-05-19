# BlazorHeadless

A headless UI component library for Blazor: behaviour, accessibility, and state management without any visual opinion. Style it your way with CSS, Tailwind, or any design system.

Inspired by [Headless UI](https://headlessui.com/), [Radix UI](https://www.radix-ui.com/), and [Ark UI](https://ark-ui.com/) patterns, ported natively to Blazor.

## Why headless?

- **Zero visual opinion** — no CSS, no styles, only semantic HTML and `data-*` hooks.
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

This registers the JS interop service used by Dialog, Popover, Transition, and anchor positioning.

## Components

| Component | Description |
| --- | --- |
| **Menu** | Dropdown menu with keyboard nav, typeahead, and virtual focus |
| **Listbox** | Custom select with single/multi-select, typeahead, and form integration |
| **Combobox** | Typeable autocomplete with consumer-driven filtering |
| **Dialog** | Modal with focus trap, scroll lock, and inert background |
| **Popover** | Non-modal floating panel with focus management and group coordination |
| **Disclosure** | Single show/hide region |
| **Accordion** | Single or multiple expandable sections |
| **Tabs** | Tabbed interface with keyboard navigation |
| **Switch** | Two-state toggle with optional hidden form input |
| **Checkbox** | Custom checkbox with indeterminate state support |
| **Radio Group** | Single-select radio group |
| **Button** | Polymorphic button with disabled and loading states |
| **Field** | Form field grouping with Label, Description, Input, Select, Textarea |
| **Transition** | CSS class-based enter/leave animations |

## Anchor Positioning

Dropdown panels (`HMenuItems`, `HListboxOptions`, `HComboboxOptions`, `HPopoverPanel`) support automatic positioning relative to their trigger via the `Anchor` parameter:

```razor
<HMenu>
    <HMenuButton>Options ▾</HMenuButton>
    <HMenuItems Anchor="@(new AnchorOptions { To = "bottom start", Gap = 4 })">
        <HMenuItem OnClick="Edit">Edit</HMenuItem>
        <HMenuItem OnClick="Delete">Delete</HMenuItem>
    </HMenuItems>
</HMenu>
```

### Placement options

Use `top`, `right`, `bottom`, or `left` to center along an edge. Combine with `start` or `end` for corner alignment:

```
top start    |  top     |  top end
left start   |  left    |  left end
right start  |  right   |  right end
bottom start |  bottom  |  bottom end
```

### AnchorOptions

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

### Menu

```razor
<HMenu>
    <HMenuButton class="btn" Context="b">
        Options
        <span class="chevron @(b.IsOpen ? "open" : "")">▾</span>
    </HMenuButton>
    <HMenuItems class="dropdown">
        <HMenuItem OnClick="Edit" Label="Edit">Edit</HMenuItem>
        <HMenuItem OnClick="Delete" Label="Delete" Disabled="true">Delete</HMenuItem>
    </HMenuItems>
</HMenu>
```

### Listbox

```razor
<HListbox TValue="string" Value="@person" OnValueChange="v => person = v">
    <HListboxButton TValue="string" Context="b">
        @(b.Value ?? "Select…")
    </HListboxButton>
    <HListboxOptions TValue="string">
        <HListboxOption TValue="string" Value="alice">Alice</HListboxOption>
        <HListboxOption TValue="string" Value="bob">Bob</HListboxOption>
    </HListboxOptions>
</HListbox>
```

### Combobox

```razor
<HCombobox TValue="string" Value="@fruit" OnValueChange="v => fruit = v"
           OnQueryChange="Filter" DisplayValue="v => v ?? string.Empty">
    <HComboboxInput TValue="string" Placeholder="Search…" />
    <HComboboxOptions TValue="string">
        @foreach (var f in filtered)
        {
            <HComboboxOption TValue="string" Value="@f">@f</HComboboxOption>
        }
    </HComboboxOptions>
</HCombobox>
```

### Popover

```razor
<HPopover>
    <HPopoverButton Context="b">Info ▾</HPopoverButton>
    <HPopoverPanel Context="p">
        <p>Panel content here.</p>
        <HPopoverButton>Close</HPopoverButton>
    </HPopoverPanel>
</HPopover>
```

### Dialog

```razor
<HDialog Open="showDialog" OnOpenChange="v => showDialog = v">
    <HDialogBackdrop class="backdrop" />
    <HDialogPanel class="dialog-panel">
        <HDialogTitle>Confirm</HDialogTitle>
        <HDialogDescription>Are you sure?</HDialogDescription>
        <button @onclick="() => showDialog = false">OK</button>
    </HDialogPanel>
</HDialog>
```

### Switch

```razor
<HSwitch Checked="@enabled" OnCheckedChange="v => enabled = v" class="switch" Context="s">
    <span class="switch-thumb"></span>
</HSwitch>
```

### Disclosure

```razor
<HDisclosure>
    <HDisclosureButton Context="d">@(d.IsOpen ? "Hide" : "Show") details</HDisclosureButton>
    <HDisclosurePanel>Hidden content here.</HDisclosurePanel>
</HDisclosure>
```

### Accordion

```razor
<HAccordion DefaultValue="item-1">
    <HAccordionItem Value="item-1">
        <HAccordionTrigger Context="t">
            Section 1 <span>@(t.IsOpen ? "−" : "+")</span>
        </HAccordionTrigger>
        <HAccordionContent>Content for section 1.</HAccordionContent>
    </HAccordionItem>
</HAccordion>
```

## Common parameters

Every component inherits from `HeadlessComponentBase` and supports:

| Parameter | Purpose |
| --- | --- |
| `As` | Override the rendered HTML tag (e.g. `As="a"`) |
| `Id` | Explicit HTML `id`; auto-generated when omitted |
| `Ref` | `Action<ElementReference>` for DOM access and focus management |
| `AdditionalAttributes` | Captured unmatched attributes — `class`, `style`, `data-*`, `aria-*`, anything HTML |

## Styling with data attributes

Components emit data attributes that mirror their state:

```css
[data-state="open"]      { /* expanded / open */ }
[data-state="closed"]    { /* collapsed / closed */ }
[data-state="checked"]   { /* switch/checkbox on */ }
[data-state="unchecked"] { /* switch/checkbox off */ }
[data-active]            { background: #eff6ff; }
[data-selected]          { font-weight: 600; }
[data-disabled]          { opacity: 0.5; pointer-events: none; }
[data-loading]           { cursor: progress; }
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

## Project status

Active development. APIs may evolve as the library approaches feature parity with Headless UI v2.

## License

See [LICENSE](LICENSE).
