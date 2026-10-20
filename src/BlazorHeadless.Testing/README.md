# BlazorHeadless.Testing

Accessibility test tooling for Razor components, built on [bUnit](https://bunit.dev). Works with any Blazor components; first-class support for [BlazorHeadless](https://github.com/msynk/BlazorHeadless).

| API | What it does |
| --- | --- |
| `cut.ShouldBeAccessible()` / `cut.Audit()` | Static WAI-ARIA 1.2 rule engine over the rendered markup (see rules below). Throws `BhA11yException` listing every violation, or returns a `BhA11yReport`. |
| `element.AccessibleName()` / `AccessibleDescription()` | The W3C accessible name and description computation (accname 1.2): `aria-labelledby`, `aria-label`, `<label>`, `alt`, `title`, name from content, hidden content. |
| `element.Role()` | Explicit or implicit role. |
| `cut.TabSequence()` | The keyboard Tab order (positive `tabindex` first, disabled / hidden / `inert` skipped, one stop per native radio group). |
| `element.Press("ArrowDown", shift: true)`, `PressSequence(…)`, `Type("abc")` | Keyboard helpers. |
| `cut.ActiveDescendant(element)` | The element referenced by `aria-activedescendant`. |
| `context.LastFocusRequest()` | The id of the element the component last moved focus to (bUnit has no real focus). |
| `BhTestAnnouncer` | Records live-region announcements; `await announcer.WaitForAsync("2 results")` waits for debounced ones. |
| `BhA11yReport.ToMarkdown()` / `ToJson()` | CI-friendly reports (e.g. write to `$GITHUB_STEP_SUMMARY`). |

## Getting started

```bash
dotnet add package BlazorHeadless.Testing
```

Derive your test classes from **`BhTestContext`** (a `BunitContext`):

```razor
@* MenuTests.razor *@
@inherits BhTestContext

@code {
    [Fact]
    public void Menu_is_accessible_and_keyboard_operable()
    {
        var cut = Render(
            @<BhMenu>
                <BhMenuButton>Options</BhMenuButton>
                <BhMenuItems>
                    <BhMenuItem>Edit</BhMenuItem>
                    <BhMenuItem>Delete</BhMenuItem>
                </BhMenuItems>
            </BhMenu>);

        cut.Find("button").Press("ArrowDown");

        var menu = cut.Find("[role=menu]");
        Assert.Equal("Edit", cut.ActiveDescendant(menu)!.AccessibleName());
        cut.ShouldBeAccessible();
    }
}
```

`BhTestContext`:

- sets bUnit's JS interop to loose mode and registers the BlazorHeadless services;
- replaces the announcer with a recording `BhTestAnnouncer` (the `Announcer` property);
- **emulates the browser runtime's DOM queries**: item collections (options, menu items, tree items, roving items …) learn their document order and rendered text from the rendered markup, so arrow-key order, typeahead and text filtering behave as they do in a browser.

Already have your own base class? Call `this.AddBlazorHeadless()` in its constructor to get the services and the recording announcer (without the DOM emulation).

## Rules

| Rule | Checks |
| --- | --- |
| `duplicate-id` | ids are unique |
| `aria-valid-role` | `role` values exist in WAI-ARIA 1.2 |
| `aria-valid-attr-value` | ARIA attribute values have the right type (true/false, tristate, integer, id reference, token) |
| `aria-allowed-attr` | role-specific attributes are allowed on the element's role |
| `aria-required-attr` | required states and properties are present (e.g. `aria-valuenow` on `slider`) |
| `idref-exists` | `aria-labelledby`, `aria-describedby`, `aria-controls`, `aria-activedescendant`, `for` … point at existing ids |
| `aria-required-children` | composite widgets own only allowed roles (listbox → option / group, tree → treeitem / group …) |
| `aria-required-parent` | options, tabs, menu items, tree items, rows, cells … sit inside the right container |
| `accessible-name` | interactive elements and landmarks / dialogs have a name |
| `aria-hidden-focus` | nothing focusable inside `aria-hidden="true"` |
| `nested-interactive` | no interactive content inside buttons, links and similar |
| `tabindex` | no positive `tabindex` |
| `presentation-conflict` | `role="presentation"` isn't ignored because of focusability or global ARIA |
| `activedescendant-target` | `aria-activedescendant` targets are owned / controlled by the element |
| `form-label` | form controls are labelled |

Configure with `BhA11yOptions`: `.Without("rule-id")`, `MinimumImpact`, `IsFragment` (ignore references that may live elsewhere on the page).

## Reporting in CI

```csharp
var report = cut.Audit();
File.AppendAllText(Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") ?? "a11y.md", report.ToMarkdown("Checkout form"));
Assert.True(report.Passed, report.ToString());
```

## Scope

Static checks catch structural ARIA mistakes early and fast, in the same test run as your behaviour tests. They complement — they don't replace — in-browser scanning (e.g. axe-core via Playwright, which also checks colour contrast and layout) and testing with real assistive technology.
