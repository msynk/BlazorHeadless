import { readFileSync } from 'node:fs';
import { createRequire } from 'node:module';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);

/** The demo catalogue, read from the sample app so new pages are tested automatically. */
export function demoSlugs() {
    const source = readFileSync(resolve(here, '../../../src/BlazorHeadless.Samples/Components/Shared/DemoSection.cs'), 'utf8');
    return [...source.matchAll(/new\("([^"]+)",/g)].map((m) => m[1]);
}

const axeSource = readFileSync(require.resolve('axe-core/axe.min.js'), 'utf8');

/** Opens a demo page and waits until the interactive circuit is up. */
export async function openDemo(page, path) {
    const errors = [];
    page.on('pageerror', (e) => errors.push(e.message));
    page.on('console', (m) => {
        // Network failures of remote demo assets (avatars …) are not script errors.
        if (m.type() === 'error' && !/Failed to load resource/.test(m.text())) errors.push(m.text());
    });
    const response = await page.goto(path);
    if (!response || !response.ok()) throw new Error(`${path} responded ${response?.status()}`);
    // Blazor Server: wait for the circuit, then for the first interactive render.
    await page.waitForFunction(() => !!window.Blazor, null, { timeout: 30_000 });
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(300);
    return errors;
}

/** WCAG 2.2 A / AA + best practices over the demo content (the app chrome is out of scope). */
export async function axe(page, { include = 'main', disable = [] } = {}) {
    await page.addScriptTag({ content: axeSource });
    return page.evaluate(
        async ({ include, disable }) => {
            const rules = Object.fromEntries(disable.map((id) => [id, { enabled: false }]));
            const result = await window.axe.run(
                { include: [include] },
                {
                    runOnly: { type: 'tag', values: ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa', 'best-practice'] },
                    rules,
                }
            );
            return result.violations.map((v) => ({
                id: v.id,
                impact: v.impact,
                help: v.help,
                helpUrl: v.helpUrl,
                nodes: v.nodes.map((n) => ({ target: n.target.join(' '), summary: n.failureSummary })),
            }));
        },
        { include, disable }
    );
}

/** A readable listing of axe violations for assertion messages. */
export function describeViolations(violations) {
    return violations
        .map(
            (v) =>
                `[${v.impact}] ${v.id}: ${v.help} (${v.helpUrl})\n` +
                v.nodes
                    .slice(0, 5)
                    .map((n) => `    ${n.target}\n      ${n.summary.replace(/\n/g, '\n      ')}`)
                    .join('\n')
        )
        .join('\n\n');
}

/** Describes the focused element. */
export function focused(page) {
    return page.evaluate(() => {
        const a = document.activeElement;
        return a
            ? { id: a.id, role: a.getAttribute('role') ?? a.tagName.toLowerCase(), text: (a.textContent ?? '').trim() }
            : null;
    });
}
