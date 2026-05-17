# HeadlessUI.Blazor

A headless UI component library for Blazor: behaviour, accessibility, and state management without any visual opinion. Style it your way with CSS, Tailwind, or any design system.

Inspired by [Radix UI](https://www.radix-ui.com/), [Headless UI](https://headlessui.com/), and [Ark UI](https://ark-ui.com/) patterns, ported natively to Blazor.

## Why headless?

- **Zero visual opinion** — no CSS, no styles, only semantic HTML and `data-*` hooks.
- **Accessibility built in** — ARIA roles, states, and keyboard interactions are handled for you.
- **Polymorphic rendering** — every component renders as any HTML element via the `As` parameter.
- **Styling hooks via data attributes** — `[data-state]`, `[data-disabled]`, `[data-loading]`, and friends, so you can style with pure CSS selectors.
- **Attribute merging** — `class` and `style` concatenate; everything else lets the consumer win.
- **Controlled and uncontrolled** — every stateful component supports both modes.
- **Render-prop context** — child content receives a typed render context for state-driven UI (spinners, chevrons, icons).

## Requirements

- .NET 10
- `Microsoft.AspNetCore.Components.Web` 10.x

## Components

| Component | Description |
| --- | --- |
| `HButton` | Polymorphic button with disabled and loading states |
| `HSwitch` | Two-state on/off toggle with optional hidden form input |
| `HDisclosure` + `HDisclosureButton` + `HDisclosurePanel` | Single show/hide region |
| `HAccordion` + `HAccordionItem` + `HAccordionTrigger` + `HAccordionContent` | Single or multiple expandable sections |

## Quick examples

### Button

```razor
<HButton OnClick="Save" Loading="@isSaving" Context="btn">
    @if (btn.Loading) { <span>Saving…</span> }
    else              { <span>Save</span> }
</HButton>
```

### Switch

```razor
<HSwitch DefaultChecked="true" class="switch" Context="s">
    <span class="switch-thumb" data-state="@(s.IsChecked ? "checked" : "unchecked")"></span>
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
            Section 1
            <span class="@(t.IsOpen ? "chevron-up" : "chevron-down")">▾</span>
        </HAccordionTrigger>
        <HAccordionContent>Content for section 1.</HAccordionContent>
    </HAccordionItem>
    <HAccordionItem Value="item-2">
        <HAccordionTrigger>Section 2</HAccordionTrigger>
        <HAccordionContent>Content for section 2.</HAccordionContent>
    </HAccordionItem>
</HAccordion>
```

Switch to multi-open mode with `Type="AccordionType.Multiple"` and `DefaultValues="@(new[] { "item-1", "item-2" })"`.

## Common parameters

Every component inherits from `HeadlessComponentBase` and supports:

| Parameter | Purpose |
| --- | --- |
| `As` | Override the rendered HTML tag (e.g. `As="a"`) |
| `Id` | Explicit HTML `id`; auto-generated when omitted |
| `Ref` | `Action<ElementReference>` for DOM access and focus management |
| `AdditionalAttributes` | Captured unmatched attributes — `class`, `style`, `data-*`, `aria-*`, anything HTML |

## Styling with data attributes

Components emit data attributes that mirror their state, so you can drive styles with pure CSS:

```css
[data-state="open"]      { /* expanded */ }
[data-state="closed"]    { /* collapsed */ }
[data-state="checked"]   { /* switch on */ }
[data-state="unchecked"] { /* switch off */ }
[data-disabled]          { opacity: 0.5; pointer-events: none; }
[data-loading]           { cursor: progress; }
```

## Project status

Early-stage exploration of a native headless UI library for Blazor. APIs may evolve.

## License

See [LICENSE](LICENSE).
