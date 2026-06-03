// build.mjs — Generate the BlazorHeadless static documentation site.
//
// Reads the live sample pages in src/BlazorHeadless.Samples/Components/Pages
// and emits one HTML file per component plus the Overview and Getting started
// pages. Pure Node (>=18) — no npm dependencies.

import { fileURLToPath } from 'node:url';
import { dirname, join, resolve } from 'node:path';
import { readFile, writeFile, mkdir } from 'node:fs/promises';

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = resolve(__dirname, '..');
const samplesDir = join(
    repoRoot,
    'src',
    'BlazorHeadless.Samples',
    'Components',
    'Pages'
);
const outComponents = join(__dirname, 'components');

// ---------------------------------------------------------------------------
// Catalogue (mirror of DemoCatalog in DemoSection.cs).
// `cls` is the {Class}Page.razor file name; `label` is the display name.
// ---------------------------------------------------------------------------
const components = [
    { slug: 'accordion',        cls: 'AccordionPage',       label: 'Accordion' },
    { slug: 'aspect-ratio',     cls: 'AspectRatioPage',     label: 'Aspect Ratio' },
    { slug: 'avatar',           cls: 'AvatarPage',          label: 'Avatar' },
    { slug: 'button',           cls: 'ButtonPage',          label: 'Button' },
    { slug: 'checkbox',         cls: 'CheckboxPage',        label: 'Checkbox' },
    { slug: 'close-button',     cls: 'CloseButtonPage',     label: 'CloseButton' },
    { slug: 'combobox',         cls: 'ComboboxPage',        label: 'Combobox' },
    { slug: 'data-interactive', cls: 'DataInteractivePage', label: 'DataInteractive' },
    { slug: 'dialog',           cls: 'DialogPage',          label: 'Dialog' },
    { slug: 'disclosure',       cls: 'DisclosurePage',      label: 'Disclosure' },
    { slug: 'field',            cls: 'FieldPage',           label: 'Field / Input' },
    { slug: 'fieldset',         cls: 'FieldsetPage',        label: 'Fieldset' },
    { slug: 'focus-trap',       cls: 'FocusTrapPage',       label: 'FocusTrap' },
    { slug: 'hover-card',       cls: 'HoverCardPage',       label: 'Hover Card' },
    { slug: 'listbox',          cls: 'ListboxPage',         label: 'Listbox' },
    { slug: 'menu',             cls: 'MenuPage',            label: 'Menu' },
    { slug: 'popover',          cls: 'PopoverPage',         label: 'Popover' },
    { slug: 'portal',           cls: 'PortalPage',          label: 'Portal' },
    { slug: 'radiogroup',       cls: 'RadioGroupPage',      label: 'Radio Group' },
    { slug: 'switch',           cls: 'SwitchPage',          label: 'Switch' },
    { slug: 'tabs',             cls: 'TabsPage',            label: 'Tabs' },
    { slug: 'tooltip',          cls: 'TooltipPage',         label: 'Tooltip' },
    { slug: 'transition',       cls: 'TransitionPage',      label: 'Transition' },
];

// Pager order: Overview → Getting started → all components.
// `kind` is one of: 'index', 'getting-started', 'component'.
const pageOrder = [
    { kind: 'index',           label: 'Overview' },
    { kind: 'getting-started', label: 'Getting started' },
    ...components.map((c) => ({ kind: 'component', slug: c.slug, label: c.label })),
];

// ---------------------------------------------------------------------------
// HTML helpers
// ---------------------------------------------------------------------------
function escapeHtml(s) {
    return s
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;');
}

function escapeAttr(s) {
    return escapeHtml(s).replace(/"/g, '&quot;');
}

// Strip class="..." attributes from <code> tags so docs CSS controls appearance.
function cleanCodeClasses(html) {
    return html.replace(/<code\s+class="[^"]*"\s*>/g, '<code>');
}

// ---------------------------------------------------------------------------
// Razor file parsing
// ---------------------------------------------------------------------------
function parsePage(source) {
    // Page header: first <h1>...</h1> and following <p>...</p>.
    const titleMatch = source.match(/<h1[^>]*>([\s\S]*?)<\/h1>/);
    const descMatch = source.match(/<h1[^>]*>[\s\S]*?<\/h1>\s*<p[^>]*>([\s\S]*?)<\/p>/);
    const pageTitle = titleMatch ? titleMatch[1].trim() : '';
    const pageDescription = descMatch ? descMatch[1].trim() : '';

    // Raw string literals: private const string _xxx = """\n ... \n""";
    const consts = {};
    const constRe = /private\s+const\s+string\s+(\w+)\s*=\s*"""\s*\r?\n([\s\S]*?)\r?\n\s*""";/g;
    let m;
    while ((m = constRe.exec(source)) !== null) {
        consts[m[1]] = m[2];
    }

    // Each demo block: <DemoBlock|TwDemoBlock ...> ... </DemoBlock|TwDemoBlock>
    const blocks = [];
    const blockRe = /<(DemoBlock|TwDemoBlock)([\s\S]*?)>([\s\S]*?)<\/\1>/g;
    while ((m = blockRe.exec(source)) !== null) {
        const attrs = m[2];
        const inner = m[3];

        const titleAttrMatch = attrs.match(/Title\s*=\s*"([^"]*)"/);
        const codeAttrMatch = attrs.match(/Code\s*=\s*"@(\w+)"/);
        const cssAttrMatch = attrs.match(/Css\s*=\s*"@(\w+)"/);
        const descMatch = inner.match(/<Description>([\s\S]*?)<\/Description>/);

        if (!titleAttrMatch || !codeAttrMatch) continue;

        const codeKey = codeAttrMatch[1];
        const cssKey = cssAttrMatch ? cssAttrMatch[1] : null;

        blocks.push({
            title: titleAttrMatch[1].trim(),
            description: descMatch
                ? cleanCodeClasses(descMatch[1].trim()).replace(/\s+/g, ' ')
                : '',
            code: consts[codeKey] ?? '',
            css: cssKey ? consts[cssKey] ?? '' : null,
        });
    }

    return { pageTitle, pageDescription, blocks };
}

// ---------------------------------------------------------------------------
// Layout shell — used by every page so navigation stays consistent.
// ---------------------------------------------------------------------------
function shell({ title, currentPath, isRoot, currentSlug, contentHtml }) {
    // currentPath is "components/accordion.html" relative to docs root, etc.
    // isRoot = true when the page is at docs/ (index, getting-started); we
    // need to fix relative URLs accordingly.
    const prefix = isRoot ? '' : '../';

    const sidebarItems = [];
    sidebarItems.push(`<h3>Documentation</h3><ul>
        <li><a href="${prefix}index.html"${
        currentPath === 'index.html' ? ' class="active"' : ''
    }>Overview</a></li>
        <li><a href="${prefix}getting-started.html"${
        currentPath === 'getting-started.html' ? ' class="active"' : ''
    }>Getting started</a></li>
    </ul>`);

    sidebarItems.push(`<h3>Components</h3><ul>`);
    for (const c of components) {
        const isActive = currentSlug === c.slug;
        sidebarItems.push(
            `<li><a href="${prefix}components/${c.slug}.html"${
                isActive ? ' class="active"' : ''
            }>${c.label}</a></li>`
        );
    }
    sidebarItems.push(`</ul>`);

    return `<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>${escapeHtml(title)}</title>
    <link rel="stylesheet" href="${prefix}assets/main.css">
    <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/themes/prism-tomorrow.min.css">
</head>
<body>
    <header class="site-header">
        <button id="menu-toggle" class="menu-toggle" aria-label="Toggle navigation">☰</button>
        <a class="brand" href="${prefix}index.html">
            <span class="brand-logo">Bh</span>
            <span>BlazorHeadless</span>
        </a>
        <span class="header-spacer"></span>
        <a class="header-link" href="https://github.com/msynk/BlazorHeadless" target="_blank" rel="noreferrer">GitHub</a>
    </header>
    <div class="sidebar-overlay"></div>
    <div class="site">
        <aside class="sidebar">
            ${sidebarItems.join('\n            ')}
        </aside>
        <main class="content">
${contentHtml}
        </main>
    </div>
    <script src="${prefix}assets/main.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-core.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-markup.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-css.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-clike.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-csharp.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/prismjs@1.29.0/components/prism-bash.min.js"></script>
</body>
</html>`;
}

// Build the pager block for a given current page index in pageOrder.
// `fromKind` is the kind of the current page; we use it to compute relative
// URLs (component pages live one folder deep; index/getting-started are at
// the docs root).
function pager(currentIndex, fromKind) {
    const prev = pageOrder[currentIndex - 1];
    const next = pageOrder[currentIndex + 1];

    const hrefFor = (entry) => {
        if (!entry) return null;
        const fromRoot = fromKind !== 'component';
        switch (entry.kind) {
            case 'index':
                return fromRoot ? 'index.html' : '../index.html';
            case 'getting-started':
                return fromRoot ? 'getting-started.html' : '../getting-started.html';
            case 'component':
                return fromRoot
                    ? `components/${entry.slug}.html`
                    : `${entry.slug}.html`;
        }
        return null;
    };

    const link = (entry, dir) => {
        const href = hrefFor(entry);
        if (!href) return '';
        const arrow = dir === 'prev' ? '← Previous' : 'Next →';
        return `<a class="${dir}" href="${href}">
            <span class="pager-label">${arrow}</span>
            <span class="pager-title">${escapeHtml(entry.label)}</span>
        </a>`;
    };

    return `        <nav class="pager">
            ${link(prev, 'prev')}
            ${link(next, 'next')}
        </nav>`;
}

// ---------------------------------------------------------------------------
// Component page rendering
// ---------------------------------------------------------------------------
function renderExample(block, blockIdx) {
    const hasCss = block.css !== null && block.css.trim().length > 0;
    const hasTw = block.twCode !== undefined && block.twCode.trim().length > 0;
    const titleId = block.title
        .toLowerCase()
        .replace(/[^a-z0-9]+/g, '-')
        .replace(/(^-|-$)/g, '');

    const tabs = [];
    if (hasCss) tabs.push({ key: 'css', label: 'CSS' });
    if (hasTw) tabs.push({ key: 'tw', label: 'Tailwind' });
    if (tabs.length === 0) tabs.push({ key: 'css', label: 'Code' });

    const tabsHtml = tabs
        .map(
            (t, i) =>
                `<button class="example-tab${i === 0 ? ' active' : ''}" data-tab="${t.key}">${t.label}</button>`
        )
        .join('');

    function codeBlock(code, lang) {
        const esc = escapeHtml(code.trim());
        return `<div class="code-wrap">
                    <button class="copy-btn" type="button">Copy</button>
                    <pre class="code-block"><code class="language-${lang}">${esc}</code></pre>
                </div>`;
    }

    const panels = [];
    if (hasCss) {
        let panel = `<div class="example-panel${tabs[0].key === 'css' ? ' active' : ''}" data-tab="css">
                <h4 class="panel-label">Razor</h4>
                ${codeBlock(block.code, 'markup')}
                <h4 class="panel-label">CSS</h4>
                ${codeBlock(block.css, 'css')}
            </div>`;
        panels.push(panel);
    }
    if (hasTw) {
        const isActive = !hasCss; // first one is active
        let panel = `<div class="example-panel${isActive ? ' active' : ''}" data-tab="tw">
                <h4 class="panel-label">Razor + Tailwind</h4>
                ${codeBlock(block.twCode, 'markup')}
            </div>`;
        panels.push(panel);
    }
    if (!hasCss && !hasTw) {
        panels.push(`<div class="example-panel active" data-tab="css">
                <h4 class="panel-label">Razor</h4>
                ${codeBlock(block.code, 'markup')}
            </div>`);
    }

    return `        <h2 id="${titleId}">${escapeHtml(block.title)}</h2>
        ${block.description ? `<p>${block.description}</p>` : ''}
        <div class="example">
            <div class="example-header">
                <span class="example-title">Example</span>
                <div class="example-tabs">${tabsHtml}</div>
            </div>
            ${panels.join('\n            ')}
        </div>`;
}

async function buildComponentPage(component, idx) {
    const cssPath = join(samplesDir, 'Css', `${component.cls}.razor`);
    const twPath = join(samplesDir, 'Tailwind', `${component.cls}.razor`);

    const cssSrc = await readFile(cssPath, 'utf8');
    let twSrc = '';
    try {
        twSrc = await readFile(twPath, 'utf8');
    } catch {
        // No tailwind variant available.
    }

    const cssParsed = parsePage(cssSrc);
    const twParsed = twSrc ? parsePage(twSrc) : { blocks: [] };

    // Merge: for each block from the CSS page, pair it with the same-titled
    // block in the Tailwind page (matched case-insensitively, normalised).
    const norm = (s) => s.toLowerCase().replace(/[^a-z0-9]+/g, '');
    const twByTitle = new Map(twParsed.blocks.map((b) => [norm(b.title), b]));
    const merged = cssParsed.blocks.map((b) => ({
        ...b,
        twCode: twByTitle.get(norm(b.title))?.code ?? '',
    }));

    const pageOrderIdx = idx + 2; // +2 for Overview & Getting started
    const examples = merged.map((b, i) => renderExample(b, i)).join('\n');

    const content = `        <h1>${escapeHtml(cssParsed.pageTitle || component.label)}</h1>
        <p class="lead">${cssParsed.pageDescription}</p>
${examples}
${pager(pageOrderIdx, 'component')}`;

    const html = shell({
        title: `${component.label} — BlazorHeadless`,
        currentPath: `components/${component.slug}.html`,
        isRoot: false,
        currentSlug: component.slug,
        contentHtml: content,
    });

    await writeFile(join(outComponents, `${component.slug}.html`), html);
}

// ---------------------------------------------------------------------------
// Static pages: Overview & Getting started
// ---------------------------------------------------------------------------
const overviewContent = `        <h1>BlazorHeadless</h1>
        <p class="lead">A headless UI component library for Blazor: behaviour, accessibility, and state management without any visual opinion. Style it your way with plain CSS, Tailwind, or any design system.</p>

        <div class="hero-cards">
            <a class="hero-card" href="getting-started.html">
                <strong>Getting started →</strong>
                <span>Install the package, register services, and build your first component.</span>
            </a>
            <a class="hero-card" href="components/menu.html">
                <strong>Browse components →</strong>
                <span>23 unstyled, accessible primitives ready to wire into any design.</span>
            </a>
            <a class="hero-card" href="https://github.com/msynk/BlazorHeadless" target="_blank" rel="noreferrer">
                <strong>GitHub →</strong>
                <span>Source code, issues, and contributions.</span>
            </a>
        </div>

        <h2 id="why-headless">Why headless?</h2>
        <ul>
            <li><strong>Zero visual opinion.</strong> The library ships no CSS, only semantic HTML and <code>data-*</code> hooks.</li>
            <li><strong>Accessibility built in.</strong> ARIA roles, states, and keyboard interactions are handled for you.</li>
            <li><strong>Polymorphic rendering.</strong> Every component renders as any HTML element via the <code>As</code> parameter.</li>
            <li><strong>Styling hooks via data attributes.</strong> <code>[data-state]</code>, <code>[data-disabled]</code>, <code>[data-active]</code>, and friends.</li>
            <li><strong>Attribute merging.</strong> <code>class</code> and <code>style</code> concatenate; everything else lets the consumer win.</li>
            <li><strong>Controlled and uncontrolled.</strong> Every stateful component supports both modes.</li>
            <li><strong>Render-prop context.</strong> Child content receives a typed render context for state-driven UI.</li>
            <li><strong>Anchor positioning.</strong> Automatic floating panel positioning with flip/shift via JS interop.</li>
        </ul>

        <h2 id="components">Components</h2>
        <p>BlazorHeadless ships 23 primitives. Browse the docs for examples styled with both plain CSS and Tailwind utilities.</p>

        <div class="component-grid">
${components
    .map(
        (c) => `            <a href="components/${c.slug}.html">${c.label}</a>`
    )
    .join('\n')}
        </div>

        <h2 id="styling">Styling philosophy</h2>
        <p>Every component emits <code>data-*</code> attributes that mirror its state. Style with attribute selectors and stay framework-agnostic.</p>

        <pre class="code-block"><code class="language-css">[data-state="open"]          { /* expanded / open */ }
[data-state="closed"]        { /* collapsed / closed */ }
[data-state="checked"]       { /* switch/checkbox on */ }
[data-state="unchecked"]     { /* switch/checkbox off */ }
[data-state="indeterminate"] { /* checkbox mixed state */ }
[data-state="active"]        { /* selected tab */ }
[data-active]                { background: #eff6ff; }
[data-selected]              { font-weight: 600; }
[data-disabled]              { opacity: 0.5; pointer-events: none; }
[data-loading]               { cursor: progress; }
[data-orientation="horizontal"] { /* tablists, radio groups */ }
[data-orientation="vertical"]   { /* tablists, radio groups */ }
</code></pre>

        <h2 id="requirements">Requirements</h2>
        <ul>
            <li>.NET 10</li>
            <li><code>Microsoft.AspNetCore.Components.Web</code> 10.x</li>
        </ul>

        <h2 id="next">Next steps</h2>
        <p>Continue to <a href="getting-started.html">Getting started</a> to install the package and wire up your first component.</p>
${pager(0, 'index')}`;

const gettingStartedContent = `        <h1>Getting started</h1>
        <p class="lead">Install BlazorHeadless, register the JS interop service, and build your first component in under five minutes.</p>

        <h2 id="install">1. Install the package</h2>
        <p>Add the package reference to your Blazor project:</p>
        <pre class="code-block"><code class="language-bash">dotnet add package BlazorHeadless</code></pre>

        <p>Or in <code>.csproj</code>:</p>
        <pre class="code-block"><code class="language-markup">&lt;ItemGroup&gt;
    &lt;PackageReference Include="BlazorHeadless" Version="*" /&gt;
&lt;/ItemGroup&gt;</code></pre>

        <h2 id="register">2. Register services</h2>
        <p>In <code>Program.cs</code>, call <code>AddBlazorHeadless()</code> on the service collection. This registers the JS interop service used by Dialog, Popover, Transition, Portal, FocusTrap, and anchor positioning.</p>

        <pre class="code-block"><code class="language-csharp">var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddBlazorHeadless();

var app = builder.Build();
// ...</code></pre>

        <h2 id="imports">3. Add usings</h2>
        <p>Add the namespace to your <code>_Imports.razor</code> so components are available everywhere:</p>
        <pre class="code-block"><code class="language-markup">@using BlazorHeadless</code></pre>

        <h2 id="portal-outlet">4. Add a portal outlet (optional)</h2>
        <p>If you plan to use <a href="components/portal.html"><code>BhPortal</code></a>, add an outlet to your root layout so portalled content has somewhere to land:</p>
        <pre class="code-block"><code class="language-markup">&lt;BhPortalOutlet /&gt;</code></pre>

        <h2 id="first-component">5. Use your first component</h2>
        <p>Drop a Menu into any page. The library handles ARIA wiring, keyboard navigation, click-outside, and focus management — you control every pixel of the visuals.</p>

        <pre class="code-block"><code class="language-markup">&lt;BhMenu&gt;
    &lt;BhMenuButton class="btn"&gt;Options ▾&lt;/BhMenuButton&gt;
    &lt;BhMenuItems class="menu-items"&gt;
        &lt;BhMenuItem OnClick="Edit"   class="menu-item"&gt;Edit&lt;/BhMenuItem&gt;
        &lt;BhMenuItem OnClick="Delete" class="menu-item" Disabled="true"&gt;Delete&lt;/BhMenuItem&gt;
    &lt;/BhMenuItems&gt;
&lt;/BhMenu&gt;</code></pre>

        <h2 id="styling-approaches">6. Pick a styling approach</h2>
        <p>Components emit <code>data-*</code> attributes that mirror their internal state. Style with whichever system you prefer.</p>

        <h3>Plain CSS</h3>
        <pre class="code-block"><code class="language-css">.menu-item {
    padding: 0.45rem 0.65rem;
    border-radius: 0.35rem;
    cursor: pointer;
}

.menu-item[data-active]   { background: #eff6ff; color: #1d4ed8; }
.menu-item[data-disabled] { opacity: 0.4; cursor: not-allowed; }</code></pre>

        <h3>Tailwind</h3>
        <p>Register custom data-attribute variants once in your input file, then use them like any other Tailwind utility:</p>
        <pre class="code-block"><code class="language-css">@import "tailwindcss";

@custom-variant data-active (&amp;[data-active]);
@custom-variant data-disabled (&amp;[data-disabled]);
@custom-variant data-state-open (&amp;[data-state="open"]);
@custom-variant data-state-checked (&amp;[data-state="checked"]);

@layer utilities {
    [hidden] { display: none !important; }
}</code></pre>

        <pre class="code-block"><code class="language-markup">&lt;BhMenuItem class="rounded-md px-3 py-2 cursor-pointer
                    data-active:bg-blue-50 data-active:text-blue-700
                    data-disabled:opacity-40 data-disabled:cursor-not-allowed"&gt;
    Edit
&lt;/BhMenuItem&gt;</code></pre>

        <h2 id="display-hidden">A note on <code>display</code> and <code>hidden</code></h2>
        <p>When your panel CSS sets an explicit <code>display</code> value (for example <code>display: flex</code>), add a <code>[hidden]</code> override so the <code>hidden</code> attribute keeps working:</p>
        <pre class="code-block"><code class="language-css">.my-panel        { display: flex; }
.my-panel[hidden]{ display: none;  }</code></pre>

        <h2 id="next-steps">Next steps</h2>
        <ul>
            <li>Browse the <a href="components/accordion.html">component reference</a> for live examples and code snippets in both CSS and Tailwind.</li>
            <li>Learn about the <a href="components/menu.html#anchor-positioning">anchor positioning</a> system used by dropdowns and popovers.</li>
            <li>Read the source on <a href="https://github.com/msynk/BlazorHeadless" target="_blank" rel="noreferrer">GitHub</a>.</li>
        </ul>
${pager(1, 'getting-started')}`;

// ---------------------------------------------------------------------------
// Main
// ---------------------------------------------------------------------------
async function main() {
    await mkdir(outComponents, { recursive: true });

    // Component pages
    for (let i = 0; i < components.length; i++) {
        await buildComponentPage(components[i], i);
        process.stdout.write(`✓ ${components[i].slug}\n`);
    }

    // Overview / index
    await writeFile(
        join(__dirname, 'index.html'),
        shell({
            title: 'BlazorHeadless — Headless UI components for Blazor',
            currentPath: 'index.html',
            isRoot: true,
            currentSlug: null,
            contentHtml: overviewContent,
        })
    );
    process.stdout.write('✓ index.html\n');

    // Getting started
    await writeFile(
        join(__dirname, 'getting-started.html'),
        shell({
            title: 'Getting started — BlazorHeadless',
            currentPath: 'getting-started.html',
            isRoot: true,
            currentSlug: null,
            contentHtml: gettingStartedContent,
        })
    );
    process.stdout.write('✓ getting-started.html\n');
}

main().catch((err) => {
    console.error(err);
    process.exit(1);
});
