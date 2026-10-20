import { test, expect } from '@playwright/test';
import { axe, demoSlugs, describeViolations, openDemo } from './helpers.js';

// Every demo page, in both styling sections, must pass axe-core (WCAG 2.2 AA +
// best practices) in its initial state, and render without script errors.
for (const section of [{ name: 'css', prefix: '/' }, { name: 'tailwind', prefix: '/tw/' }]) {
    test.describe(`${section.name} demos`, () => {
        for (const slug of demoSlugs()) {
            test(`${slug} is accessible`, async ({ page }) => {
                const errors = await openDemo(page, section.prefix + slug);
                const violations = await axe(page);
                expect(violations, describeViolations(violations)).toEqual([]);
                expect(errors.filter((e) => !/favicon|404/.test(e)), errors.join('\n')).toEqual([]);
            });
        }
    });
}
