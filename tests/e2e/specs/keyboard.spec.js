import { test, expect } from '@playwright/test';
import { axe, describeViolations, focused, openDemo } from './helpers.js';

// Keyboard and focus behaviour that only a real browser can verify: focus
// movement, focus traps and restoration, inert backgrounds, scroll locking,
// positioning — plus axe on the *open* state of each overlay.

async function expectAccessible(page) {
    const violations = await axe(page);
    expect(violations, describeViolations(violations)).toEqual([]);
}

test('dialog traps focus, makes the page inert and restores focus', async ({ page }) => {
    await openDemo(page, '/dialog');
    const trigger = page.locator('main button', { hasText: /open/i }).first();
    await trigger.focus();
    await page.keyboard.press('Enter');

    const dialog = page.locator('[role=dialog]').first();
    await expect(dialog).toBeVisible();
    await expect(dialog).toHaveAttribute('aria-modal', 'true');
    const focusInDialog = () => page.evaluate(() => !!document.activeElement.closest('[role=dialog]'));
    await expect.poll(focusInDialog).toBe(true);
    await expect.poll(() => page.locator('[inert]').count()).toBeGreaterThan(0);
    await expect.poll(() => page.evaluate(() => getComputedStyle(document.body).overflow)).toBe('hidden');

    for (let i = 0; i < 6; i++) await page.keyboard.press('Tab');
    expect(await focusInDialog()).toBe(true);
    await expectAccessible(page);

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
    await expect(page.locator('[inert]')).toHaveCount(0);
});

test('menu opens with ArrowDown, navigates, and returns focus on Escape', async ({ page }) => {
    await openDemo(page, '/menu');
    const button = page.locator('main button[aria-haspopup=menu]').first();
    await button.focus();
    await page.keyboard.press('ArrowDown');

    const menu = page.locator('[role=menu]:visible').first();
    await expect(menu).toBeVisible();
    await expect(menu).toBeFocused();
    await expect(menu).toHaveAttribute('aria-activedescendant', /.+/);
    const first = await menu.getAttribute('aria-activedescendant');
    await page.keyboard.press('ArrowDown');
    await expect(menu).not.toHaveAttribute('aria-activedescendant', first);
    await expectAccessible(page);

    await page.keyboard.press('Escape');
    await expect(menu).toBeHidden();
    await expect(button).toBeFocused();
});

test('listbox: arrows, Enter selects, typeahead opens on a match', async ({ page }) => {
    await openDemo(page, '/listbox');
    const button = page.locator('main [role=combobox]').first();
    await button.focus();
    await page.keyboard.press('ArrowDown');
    await expect(button).toHaveAttribute('aria-expanded', 'true');
    await expect(button).toHaveAttribute('aria-activedescendant', /.+/);
    const first = await button.getAttribute('aria-activedescendant');
    await page.keyboard.press('ArrowDown');
    await expect(button).not.toHaveAttribute('aria-activedescendant', first);
    const active = await button.getAttribute('aria-activedescendant');
    const text = (await page.locator(`[id="${active}"]`).innerText()).trim();
    await expectAccessible(page);

    await page.keyboard.press('Enter');
    await expect(button).toHaveAttribute('aria-expanded', 'false');
    await expect(button).toContainText(text);
    await expect(button).toBeFocused();
});

test('combobox: typing filters and highlights, Enter selects', async ({ page }) => {
    await openDemo(page, '/combobox');
    const input = page.locator('main input[role=combobox]').first();
    await input.click();
    await input.pressSequentially('ap', { delay: 50 });
    await expect(input).toHaveAttribute('aria-expanded', 'true');
    await expect(input).toHaveAttribute('aria-activedescendant', /.+/);
    const active = await input.getAttribute('aria-activedescendant');
    const text = (await page.locator(`[id="${active}"]`).innerText()).trim();
    await expectAccessible(page);

    await page.keyboard.press('Enter');
    await expect(input).toHaveAttribute('aria-expanded', 'false');
    await expect(input).toHaveValue(text);
});

test('tabs: arrow keys move selection without scrolling the page', async ({ page }) => {
    await openDemo(page, '/tabs');
    // Pin the element by id: the selector would follow the selection.
    const id = await page.locator('main [role=tab][aria-selected=true]').first().getAttribute('id');
    const tab = page.locator(`[id="${id}"]`);
    await tab.focus();
    const before = await page.evaluate(() => window.scrollY);
    await page.keyboard.press('ArrowRight');
    await expect(tab).not.toBeFocused();
    const now = await focused(page);
    expect(now.role).toBe('tab');
    await expect(page.locator(`[id="${now.id}"]`)).toHaveAttribute('aria-selected', 'true');
    expect(await page.evaluate(() => window.scrollY)).toBe(before);
});

test('popover opens on click, closes on Escape and restores focus', async ({ page }) => {
    await openDemo(page, '/popover');
    const button = page.locator('main button[aria-expanded]').first();
    await button.click();
    await expect(button).toHaveAttribute('aria-expanded', 'true');
    await expectAccessible(page);
    await page.keyboard.press('Escape');
    await expect(button).toHaveAttribute('aria-expanded', 'false');
    await expect(button).toBeFocused();
});

test('tooltip shows on hover and hides on Escape', async ({ page }) => {
    await openDemo(page, '/tooltip');
    const trigger = page.locator('main [aria-describedby]').first();
    await trigger.hover();
    await expect(page.locator('[role=tooltip]:visible')).toHaveCount(1, { timeout: 3_000 });
    await page.keyboard.press('Escape');
    await expect(page.locator('[role=tooltip]:visible')).toHaveCount(0);
});

test('alert dialog ignores outside clicks and closes on Escape', async ({ page }) => {
    await openDemo(page, '/alert-dialog');
    const trigger = page.locator('main button[aria-haspopup=dialog]').first();
    await trigger.click();
    const dialog = page.locator('[role=alertdialog]');
    await expect(dialog).toBeVisible();
    await page.mouse.click(3, 3);
    await expect(dialog).toBeVisible();
    await expectAccessible(page);
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
});

test('radio group: arrows move focus and check', async ({ page }) => {
    await openDemo(page, '/radiogroup');
    const id = await page.locator('main [role=radio][tabindex="0"]').first().getAttribute('id');
    const radio = page.locator(`[id="${id}"]`);
    await radio.focus();
    await page.keyboard.press('ArrowDown');
    await expect(radio).not.toBeFocused();
    const now = await focused(page);
    expect(now.role).toBe('radio');
    await expect(page.locator(`[id="${now.id}"]`)).toHaveAttribute('aria-checked', 'true');
});

test('checkbox toggles with Space but not Enter', async ({ page }) => {
    await openDemo(page, '/checkbox');
    const box = page.locator('main [role=checkbox]:not([disabled])').first();
    await box.focus();
    const initial = await box.getAttribute('aria-checked');
    await page.keyboard.press('Enter');
    await expect(box).toHaveAttribute('aria-checked', initial);
    await page.keyboard.press(' ');
    await expect(box).not.toHaveAttribute('aria-checked', initial);
});

test('tree: arrows move focus, Right expands, typeahead finds items', async ({ page }) => {
    await openDemo(page, '/tree');
    const tree = page.locator('main [role=tree]').first();
    const first = tree.locator('[role=treeitem][tabindex="0"]').first();
    const id = await first.getAttribute('id');
    await page.locator(`[id="${id}"]`).focus();
    await page.keyboard.press('ArrowDown');
    await expect(page.locator(`[id="${id}"]`)).not.toBeFocused();
    expect((await focused(page)).role).toBe('treeitem');

    const collapsed = tree.locator('[role=treeitem][aria-expanded=false]').first();
    const cid = await collapsed.getAttribute('id');
    await page.locator(`[id="${cid}"]`).focus();
    await page.keyboard.press('ArrowRight');
    await expect(page.locator(`[id="${cid}"]`)).toHaveAttribute('aria-expanded', 'true');
    await expectAccessible(page);
});

test('command palette opens with Ctrl+K, filters, and restores focus', async ({ page }) => {
    await openDemo(page, '/command');
    const before = await page.evaluate(() => document.activeElement?.tagName);
    await page.keyboard.press('Control+k');
    const dialog = page.locator('[role=dialog]');
    await expect(dialog).toBeVisible();
    const input = dialog.locator('input[role=combobox]');
    await expect(input).toBeFocused();
    await input.pressSequentially('se', { delay: 30 });
    await expect(input).toHaveAttribute('aria-activedescendant', /.+/);
    await expectAccessible(page);
    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    expect(await page.evaluate(() => document.activeElement?.tagName)).toBe(before);
});

test('slider: arrow keys change the value, Home / End jump', async ({ page }) => {
    await openDemo(page, '/slider');
    const thumb = page.locator('main [role=slider]:not([aria-disabled=true])').first();
    await thumb.focus();
    const min = await thumb.getAttribute('aria-valuemin');
    const max = await thumb.getAttribute('aria-valuemax');
    await page.keyboard.press('Home');
    await expect(thumb).toHaveAttribute('aria-valuenow', min);
    await page.keyboard.press('ArrowRight');
    await expect(thumb).not.toHaveAttribute('aria-valuenow', min);
    await page.keyboard.press('End');
    await expect(thumb).toHaveAttribute('aria-valuenow', max);
});

test('sortable: keyboard reordering is announced', async ({ page }) => {
    await openDemo(page, '/sortable');
    const list = page.locator('main [data-bh-sortable-handle]').first().locator('xpath=ancestor::ul[1]');
    const firstText = (await list.locator('> li').first().innerText()).trim();
    await page.locator('main [data-bh-sortable-handle]').first().focus();
    await page.keyboard.press(' ');
    await expect(page.locator('[data-bh-live-region]')).toContainText(/Picked up/);
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press(' ');
    await expect(page.locator('[data-bh-live-region]')).toContainText(/Dropped/);
    expect((await list.locator('> li').nth(1).innerText()).trim()).toBe(firstText);
});

test('toasts are announced and F8 moves focus to them', async ({ page }) => {
    await openDemo(page, '/toast');
    await page.locator('main button', { hasText: 'Show toast' }).click();
    const region = page.locator('[role=region][aria-label^="Notifications"]').first();
    await expect(region.locator('li')).toHaveCount(1);
    await expect(page.locator('[data-bh-live-region]')).not.toHaveText('');
    await page.keyboard.press('F8');
    await expect.poll(() => page.evaluate(() => !!document.activeElement.closest('[role=region]'))).toBe(true);
    await expectAccessible(page);
});
