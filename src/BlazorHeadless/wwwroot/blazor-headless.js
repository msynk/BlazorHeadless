// BlazorHeadless — JS runtime.
//
// The C# side imports this module on first use (BhInterop) and the JS
// initializer (BlazorHeadless.lib.module.js) imports it at startup to install
// the global, declarative behaviours (key handling, interaction attributes).
//
// Every exported namespace is a small, self-contained primitive:
//
//   keys        — declarative preventDefault for keys listed in [data-bh-keys]
//   interaction — data-hover / data-focus(-visible) / data-active on [data-bh-interactive]
//   anchor      — floating element positioning (flip, shift, size, arrow, hide)
//   layers      — dismissable layer stack (Escape, pointer-down outside, focus outside)
//   focus       — focus scopes (trap / loop / restore), tabbable discovery, helpers
//   modal       — hide-others (inert + aria-hidden) and stack-aware scroll lock
//   portal      — DOM teleportation that keeps Blazor's component tree intact
//   floating    — orchestrates the above for popovers, menus, listboxes, dialogs
//   presence    — wait for CSS exit animations / transitions
//   collection  — document-order resolution for registered collection items
//   announcer   — polite / assertive live-region announcements
//   transition  — class-based enter / leave transitions
//   sortable    — pointer-driven drag & drop reordering
//   slider      — pointer-driven slider thumbs
//   dom         — misc helpers (scrollIntoView, scroll to index, element rects)

const byId = (idOrEl) => (typeof idOrEl === 'string' ? document.getElementById(idOrEl) : idOrEl || null);
const isElement = (n) => n && n.nodeType === 1;

// ─── keys ────────────────────────────────────────────────────────────────────
//
// Blazor cannot conditionally call preventDefault() based on which key was
// pressed. Components declare the keys they own with a space-separated list:
//
//     <button data-bh-keys="ArrowDown ArrowUp Enter Space">
//
// Tokens may carry modifiers ("Ctrl+a", "Shift+Tab"). A token without Shift
// also matches the shifted key (so "ArrowDown" covers Shift+ArrowDown), while
// Ctrl / Meta / Alt must be listed explicitly.

function keyName(e) {
    if (e.key === ' ' || e.key === 'Spacebar') return 'Space';
    return e.key;
}

function matchesKeyToken(e, token) {
    const parts = token.split('+');
    const key = parts.pop();
    const mods = new Set(parts.map((p) => p.toLowerCase()));
    const name = keyName(e);
    if (key.length === 1 ? name.toLowerCase() !== key.toLowerCase() : name !== key) return false;
    if (e.ctrlKey !== mods.has('ctrl')) return false;
    if (e.metaKey !== mods.has('meta')) return false;
    if (e.altKey !== mods.has('alt')) return false;
    if (mods.has('shift') && !e.shiftKey) return false;
    return true;
}

function onGlobalKeyDown(e) {
    const target = e.target;
    if (!isElement(target)) return;
    const spec = target.getAttribute('data-bh-keys');
    if (!spec) return;
    for (const token of spec.split(/\s+/)) {
        if (token && matchesKeyToken(e, token)) {
            e.preventDefault();
            return;
        }
    }
}

export const keys = {
    matches: matchesKeyToken,
};

// ─── interaction ─────────────────────────────────────────────────────────────
//
// Tracks the last input modality (keyboard vs pointer) and maintains
// data-hover / data-active / data-focus / data-focus-visible on any element
// marked [data-bh-interactive]. Running in JS keeps hover feedback instant and
// avoids a server round-trip per pointer movement in Blazor Server.

let lastModality = 'keyboard';

function interactiveRoot(node) {
    return isElement(node) ? node.closest('[data-bh-interactive]') : null;
}

function isDisabledInteractive(el) {
    return el.hasAttribute('data-disabled') || el.getAttribute('aria-disabled') === 'true' || el.disabled === true;
}

function onPointerOverInteractive(e) {
    if (e.pointerType === 'touch') return;
    const el = interactiveRoot(e.target);
    if (el && !isDisabledInteractive(el)) el.setAttribute('data-hover', '');
}

function onPointerOutInteractive(e) {
    const el = interactiveRoot(e.target);
    if (!el) return;
    if (e.relatedTarget && el.contains(e.relatedTarget)) return;
    el.removeAttribute('data-hover');
    el.removeAttribute('data-active');
}

function onPointerDownInteractive(e) {
    lastModality = 'pointer';
    const el = interactiveRoot(e.target);
    if (el && !isDisabledInteractive(el)) el.setAttribute('data-active', '');
}

function clearActive() {
    for (const el of document.querySelectorAll('[data-bh-interactive][data-active]')) el.removeAttribute('data-active');
}

function onFocusInInteractive(e) {
    const el = interactiveRoot(e.target);
    if (!el || isDisabledInteractive(el)) return;
    el.setAttribute('data-focus', '');
    let visible = lastModality === 'keyboard';
    try { visible = e.target.matches(':focus-visible'); } catch { /* old browsers */ }
    if (visible) el.setAttribute('data-focus-visible', '');
}

function onFocusOutInteractive(e) {
    const el = interactiveRoot(e.target);
    if (!el) return;
    if (e.relatedTarget && el.contains(e.relatedTarget)) return;
    el.removeAttribute('data-focus');
    el.removeAttribute('data-focus-visible');
}

export const interaction = {
    get modality() { return lastModality; },
};

// ─── install ─────────────────────────────────────────────────────────────────

let installed = false;

/** Installs the global listeners. Idempotent; called by the JS initializer and on first import. */
export function install() {
    if (installed || typeof document === 'undefined') return;
    installed = true;

    document.addEventListener('keydown', (e) => {
        if (!e.metaKey && !e.altKey && !e.ctrlKey) lastModality = 'keyboard';
        onGlobalKeyDown(e);
    }, true);
    document.addEventListener('keydown', onLayerKeyDownCapture, true);
    document.addEventListener('keydown', onLayerKeyDown, false);
    document.addEventListener('pointerdown', onLayerPointerDown, true);
    document.addEventListener('click', onLayerClick, true);
    document.addEventListener('focusin', onLayerFocusIn, true);

    document.addEventListener('pointerover', onPointerOverInteractive, true);
    document.addEventListener('pointerout', onPointerOutInteractive, true);
    document.addEventListener('pointerdown', onPointerDownInteractive, true);
    document.addEventListener('pointerup', clearActive, true);
    document.addEventListener('pointercancel', clearActive, true);
    document.addEventListener('dragend', clearActive, true);
    document.addEventListener('focusin', onFocusInInteractive, true);
    document.addEventListener('focusout', onFocusOutInteractive, true);

    installPresence();

    // Create the shared live regions up front: screen readers often ignore a region
    // that is inserted at the same moment as its first message.
    if (document.body) liveRegion('polite');
    else document.addEventListener('DOMContentLoaded', () => liveRegion('polite'), { once: true });

    // <BhVisuallyHidden Focusable>: reveal while keyboard focus is inside (skip links).
    document.addEventListener('focusin', (e) => {
        const el = isElement(e.target) ? e.target.closest('[data-bh-visually-hidden="focusable"]') : null;
        if (el && el.hasAttribute('style')) {
            el.setAttribute('data-bh-clip', el.getAttribute('style'));
            el.removeAttribute('style');
        }
    }, true);
    document.addEventListener('focusout', (e) => {
        const el = isElement(e.target) ? e.target.closest('[data-bh-visually-hidden="focusable"]') : null;
        if (!el || (e.relatedTarget && el.contains(e.relatedTarget))) return;
        const clip = el.getAttribute('data-bh-clip');
        if (clip !== null) {
            el.setAttribute('style', clip);
            el.removeAttribute('data-bh-clip');
        }
    }, true);

    // hidden="until-found" panels: open them (by clicking their trigger) when
    // the browser's find-in-page reveals a match inside.
    document.addEventListener('beforematch', (e) => {
        const panel = isElement(e.target) ? e.target.closest('[data-bh-trigger]') : null;
        const trigger = panel && document.getElementById(panel.getAttribute('data-bh-trigger'));
        if (trigger && trigger.getAttribute('aria-expanded') !== 'true') trigger.click();
    }, true);
}

// ─── anchor ──────────────────────────────────────────────────────────────────
//
// A self-contained positioning engine in the spirit of Floating UI's
// computePosition + flip + shift + size + arrow + hide middleware. Uses
// position:fixed and compensates for transformed ancestors (which create a new
// containing block for fixed elements).

const anchorHandles = new Map();
let anchorSeq = 0;

const OPPOSITE = { top: 'bottom', bottom: 'top', left: 'right', right: 'left' };

function parsePlacement(to) {
    const parts = String(to || 'bottom').trim().toLowerCase().split(/\s+/);
    let side = parts[0] || 'bottom';
    let align = parts[1] || 'center';
    if (!OPPOSITE[side]) side = 'bottom';
    if (align !== 'start' && align !== 'end') align = 'center';
    return { side, align };
}

function viewportRect() {
    const vv = window.visualViewport;
    const width = vv ? vv.width : (window.innerWidth || document.documentElement.clientWidth);
    const height = vv ? vv.height : (window.innerHeight || document.documentElement.clientHeight);
    return { left: 0, top: 0, right: width, bottom: height, width, height };
}

function resolveReferenceRect(reference) {
    if (!reference) return null;
    if (typeof reference.x === 'number' && typeof reference.y === 'number' && !isElement(reference)) {
        return { left: reference.x, top: reference.y, right: reference.x, bottom: reference.y, width: 0, height: 0 };
    }
    const el = byId(reference);
    return el ? el.getBoundingClientRect() : null;
}

function isRtl(el) {
    try { return getComputedStyle(el).direction === 'rtl'; } catch { return false; }
}

function coordsFor(ref, w, h, side, align, gap, alignOffset, rtl) {
    let x = 0, y = 0;
    const vertical = side === 'top' || side === 'bottom';
    if (vertical) {
        y = side === 'bottom' ? ref.bottom + gap : ref.top - gap - h;
        const startX = rtl ? ref.right - w : ref.left;
        const endX = rtl ? ref.left : ref.right - w;
        x = align === 'start' ? startX : align === 'end' ? endX : ref.left + (ref.width - w) / 2;
        x += rtl && align !== 'center' ? -alignOffset : alignOffset;
    } else {
        x = side === 'right' ? ref.right + gap : ref.left - gap - w;
        y = align === 'start' ? ref.top : align === 'end' ? ref.bottom - h : ref.top + (ref.height - h) / 2;
        y += alignOffset;
    }
    return { x, y };
}

function mainAxisOverflow(side, pos, w, h, vp, padding) {
    switch (side) {
        case 'bottom': return pos.y + h - (vp.bottom - padding);
        case 'top': return padding - pos.y;
        case 'right': return pos.x + w - (vp.right - padding);
        default: return padding - pos.x;
    }
}

function computePlacement(state) {
    const { floating, options } = state;
    const ref = resolveReferenceRect(state.reference);
    if (!ref) return null;

    const vp = viewportRect();
    const padding = options.padding ?? 8;
    const gap = options.gap ?? 0;
    const alignOffset = options.offset ?? 0;
    const rtl = isRtl(floating);
    const { side: preferredSide, align } = parsePlacement(options.to);

    const rect = floating.getBoundingClientRect();
    const w = rect.width, h = rect.height;

    let side = preferredSide;
    let pos = coordsFor(ref, w, h, side, align, gap, alignOffset, rtl);

    if (options.flip !== false) {
        const overflow = mainAxisOverflow(side, pos, w, h, vp, padding);
        if (overflow > 0) {
            const alt = OPPOSITE[side];
            const altPos = coordsFor(ref, w, h, alt, align, gap, alignOffset, rtl);
            const altOverflow = mainAxisOverflow(alt, altPos, w, h, vp, padding);
            if (altOverflow < overflow) { side = alt; pos = altPos; }
        }
    }

    if (options.shift !== false) {
        if (side === 'top' || side === 'bottom') {
            pos.x = Math.max(vp.left + padding, Math.min(pos.x, vp.right - padding - w));
        } else {
            pos.y = Math.max(vp.top + padding, Math.min(pos.y, vp.bottom - padding - h));
        }
    }

    // Space left on the chosen side, exposed for max-height / max-width rules.
    const availableHeight = side === 'bottom' ? vp.bottom - padding - ref.bottom - gap
        : side === 'top' ? ref.top - gap - padding
        : vp.height - padding * 2;
    const availableWidth = side === 'right' ? vp.right - padding - ref.right - gap
        : side === 'left' ? ref.left - gap - padding
        : vp.width - padding * 2;

    const referenceHidden = ref.bottom < vp.top || ref.top > vp.bottom || ref.right < vp.left || ref.left > vp.right;

    return { ref, side, align, x: pos.x, y: pos.y, w, h, availableHeight, availableWidth, referenceHidden };
}

function applyArrow(state, p) {
    const arrow = state.options.arrowId ? document.getElementById(state.options.arrowId) : null;
    if (!arrow) return null;
    const arrowRect = arrow.getBoundingClientRect();
    const arrowPadding = state.options.arrowPadding ?? 0;
    const vertical = p.side === 'top' || p.side === 'bottom';

    arrow.style.position = 'absolute';
    arrow.style.left = '';
    arrow.style.top = '';
    arrow.style.right = '';
    arrow.style.bottom = '';
    arrow.setAttribute('data-side', p.side);

    let offset;
    if (vertical) {
        const center = p.ref.left + p.ref.width / 2 - p.x - arrowRect.width / 2;
        offset = Math.max(arrowPadding, Math.min(center, p.w - arrowRect.width - arrowPadding));
        arrow.style.left = Math.round(offset) + 'px';
        if (p.side === 'bottom') arrow.style.bottom = '100%';
        else arrow.style.top = '100%';
    } else {
        const center = p.ref.top + p.ref.height / 2 - p.y - arrowRect.height / 2;
        offset = Math.max(arrowPadding, Math.min(center, p.h - arrowRect.height - arrowPadding));
        arrow.style.top = Math.round(offset) + 'px';
        if (p.side === 'right') arrow.style.right = '100%';
        else arrow.style.left = '100%';
    }
    // Rotate the default (downward pointing) arrow to face the reference.
    const rotation = { top: 0, right: 90, bottom: 180, left: -90 }[p.side];
    arrow.style.transform = `rotate(${rotation}deg)`;
    arrow.style.transformOrigin = 'center';
    return offset + (vertical ? arrowRect.width / 2 : arrowRect.height / 2);
}

function transformOrigin(p, arrowCenter) {
    const cross = arrowCenter != null ? `${Math.round(arrowCenter)}px` : 'center';
    switch (p.side) {
        case 'bottom': return `${cross} top`;
        case 'top': return `${cross} bottom`;
        case 'right': return `left ${cross}`;
        default: return `right ${cross}`;
    }
}

function applyPosition(state) {
    const { floating, options } = state;
    const refRect = resolveReferenceRect(state.reference);
    if (refRect) {
        floating.style.setProperty('--bh-anchor-width', refRect.width + 'px');
        floating.style.setProperty('--bh-anchor-height', refRect.height + 'px');
        // Headless UI compatible alias.
        floating.style.setProperty('--button-width', refRect.width + 'px');
    }
    floating.style.setProperty('--anchor-gap', (options.gap ?? 0) + 'px');
    floating.style.setProperty('--anchor-offset', (options.offset ?? 0) + 'px');
    floating.style.setProperty('--anchor-padding', (options.padding ?? 8) + 'px');
    if (options.matchWidth && refRect) floating.style.minWidth = refRect.width + 'px';

    // Measure the containing-block origin: fixed elements are positioned
    // relative to a transformed ancestor (if any) rather than the viewport.
    floating.style.position = 'fixed';
    floating.style.left = '0px';
    floating.style.top = '0px';
    floating.style.margin = '0';
    const origin = floating.getBoundingClientRect();

    const p = computePlacement(state);
    if (!p) return;

    floating.style.left = Math.round(p.x - origin.left) + 'px';
    floating.style.top = Math.round(p.y - origin.top) + 'px';
    floating.style.setProperty('--bh-available-height', Math.max(0, Math.floor(p.availableHeight)) + 'px');
    floating.style.setProperty('--bh-available-width', Math.max(0, Math.floor(p.availableWidth)) + 'px');

    const arrowCenter = applyArrow(state, p);
    floating.style.setProperty('--bh-transform-origin', transformOrigin(p, arrowCenter));

    floating.setAttribute('data-side', p.side);
    floating.setAttribute('data-align', p.align);
    floating.setAttribute('data-anchor', p.side + (p.align !== 'center' ? ' ' + p.align : ''));

    if (p.referenceHidden) floating.setAttribute('data-reference-hidden', '');
    else floating.removeAttribute('data-reference-hidden');
    if (options.hideWhenDetached) floating.style.visibility = p.referenceHidden ? 'hidden' : '';
}

export const anchor = {
    /**
     * Starts positioning `floating` relative to `reference` (an element, an id, or
     * a virtual point {x, y}). Auto-updates on scroll, resize and size changes.
     */
    start(reference, floating, options) {
        floating = byId(floating);
        if (!floating) return 0;
        if (typeof reference === 'string') reference = document.getElementById(reference) || reference;
        const id = ++anchorSeq;
        const state = { reference, floating, options: options || {}, frame: 0 };

        const update = () => {
            if (state.frame) return;
            state.frame = requestAnimationFrame(() => {
                state.frame = 0;
                if (anchorHandles.has(id)) applyPosition(state);
            });
        };
        state.update = update;

        applyPosition(state);

        window.addEventListener('scroll', update, { capture: true, passive: true });
        window.addEventListener('resize', update, { passive: true });
        if (window.visualViewport) window.visualViewport.addEventListener('resize', update, { passive: true });
        if (typeof ResizeObserver !== 'undefined') {
            state.observer = new ResizeObserver(update);
            if (isElement(reference)) state.observer.observe(reference);
            state.observer.observe(floating);
        }
        anchorHandles.set(id, state);
        return id;
    },

    stop(handle, reset) {
        const state = anchorHandles.get(handle);
        if (!state) return;
        anchorHandles.delete(handle);
        if (state.frame) cancelAnimationFrame(state.frame);
        window.removeEventListener('scroll', state.update, { capture: true });
        window.removeEventListener('resize', state.update);
        if (window.visualViewport) window.visualViewport.removeEventListener('resize', state.update);
        state.observer?.disconnect();
        if (reset !== false) {
            const el = state.floating;
            for (const prop of ['position', 'left', 'top', 'margin', 'visibility', 'minWidth']) el.style[prop] = '';
            for (const v of ['--bh-anchor-width', '--bh-anchor-height', '--button-width', '--anchor-gap', '--anchor-offset',
                '--anchor-padding', '--bh-available-height', '--bh-available-width', '--bh-transform-origin']) {
                el.style.removeProperty(v);
            }
            el.removeAttribute('data-anchor');
            el.removeAttribute('data-reference-hidden');
        }
    },

    update(handle, options) {
        const state = anchorHandles.get(handle);
        if (!state) return;
        if (options) state.options = options;
        applyPosition(state);
    },

    /** Moves the virtual reference point of a running handle (context menus). */
    setPoint(handle, x, y) {
        const state = anchorHandles.get(handle);
        if (!state) return;
        state.reference = { x, y };
        applyPosition(state);
    },
};

// ─── layers ──────────────────────────────────────────────────────────────────
//
// A stack of dismissable layers. Each layer owns a set of elements (its
// "branch": content, trigger/reference, extra ids and every descendant layer).
//
//  - Pointer-down outside a layer's branch dismisses it ("outside"). Touch
//    input waits for the click so scrolling with a finger doesn't dismiss.
//  - Focus moving outside the branch dismisses it when focusOutside is on.
//  - Escape dismisses the top-most layer when focus is not inside any layer
//    (focused content handles Escape itself, on the C# side).
//  - A modal layer shields the layers below it from outside interaction.

const layerStack = [];
let layerSeq = 0;

function layerElements(layer) {
    const els = [];
    for (const id of layer.ids) {
        const el = document.getElementById(id);
        if (el) els.push(el);
    }
    return els;
}

function isInLayerBranch(layer, node) {
    if (!node) return false;
    if (isElement(node) && node.closest('[data-bh-dismiss-ignore]')) return true;
    for (const el of layerElements(layer)) if (el.contains(node)) return true;
    for (const child of layerStack) {
        if (child.parent === layer.id && isInLayerBranch(child, node)) return true;
    }
    return false;
}

function isInsideAnyLayer(node) {
    return layerStack.some((l) => isInLayerBranch(l, node));
}

function dismissLayer(layer, reason) {
    if (layer.dismissed) return;
    layer.dismissed = true;
    // Children go first so nested content never outlives its parent.
    for (const child of layerStack.filter((l) => l.parent === layer.id)) dismissLayer(child, reason);
    layer.dotnet?.invokeMethodAsync('OnLayerDismiss', reason).catch(() => { });
}

let pendingTouchDismiss = null;

function processOutside(target, reason) {
    for (let i = layerStack.length - 1; i >= 0; i--) {
        const layer = layerStack[i];
        if (!isInLayerBranch(layer, target) && layer.options[reason === 'focus-outside' ? 'focusOutside' : 'outside']) {
            dismissLayer(layer, reason);
        }
        if (layer.options.modal) break;
    }
}

function onLayerPointerDown(e) {
    if (!layerStack.length) return;
    if (e.pointerType === 'touch') {
        pendingTouchDismiss = e.target;
        return;
    }
    pendingTouchDismiss = null;
    processOutside(e.target, 'outside');
}

function onLayerClick(e) {
    if (pendingTouchDismiss && layerStack.length) {
        const target = pendingTouchDismiss;
        pendingTouchDismiss = null;
        if (target === e.target || (isElement(target) && target.contains(e.target))) processOutside(target, 'outside');
    }
}

function onLayerFocusIn(e) {
    if (!layerStack.length) return;
    processOutside(e.target, 'focus-outside');
}

// Hover-driven layers (tooltips, hover cards) must be dismissable with Escape
// without moving the pointer or focus (WCAG 1.4.13), even when focus sits inside
// another layer. They are handled first, in the capture phase.
function onLayerKeyDownCapture(e) {
    if (e.key !== 'Escape' || !layerStack.length) return;
    const top = layerStack[layerStack.length - 1];
    if (!top.options.hover || top.dismissed) return;
    e.preventDefault();
    e.stopPropagation();
    dismissLayer(top, 'escape');
}

function onLayerKeyDown(e) {
    if (e.key !== 'Escape' || e.defaultPrevented || !layerStack.length) return;
    if (isInsideAnyLayer(e.target)) return; // focused content handles Escape itself
    for (let i = layerStack.length - 1; i >= 0; i--) {
        const layer = layerStack[i];
        if (layer.dismissed) continue;
        if (layer.options.escape !== false) {
            e.preventDefault();
            dismissLayer(layer, 'escape');
        }
        return;
    }
}

export const layers = {
    /**
     * Pushes a layer. ids: element ids that belong to the layer's branch.
     * options: { escape, outside, focusOutside, modal, parent }.
     */
    push(dotnet, ids, options) {
        const id = ++layerSeq;
        layerStack.push({ id, dotnet, ids: (ids || []).filter(Boolean), options: options || {}, parent: options?.parent || 0 });
        return id;
    },
    remove(handle) {
        const i = layerStack.findIndex((l) => l.id === handle);
        if (i >= 0) layerStack.splice(i, 1);
    },
    setIds(handle, ids) {
        const layer = layerStack.find((l) => l.id === handle);
        if (layer) layer.ids = (ids || []).filter(Boolean);
    },
    isTop(handle) {
        return layerStack.length > 0 && layerStack[layerStack.length - 1].id === handle;
    },
    contains: isInsideAnyLayer,
};

// ─── focus ───────────────────────────────────────────────────────────────────

const CANDIDATE_SELECTOR = [
    'a[href]', 'area[href]', 'button', 'input', 'select', 'textarea', 'iframe', 'audio[controls]', 'video[controls]',
    'summary', '[contenteditable]:not([contenteditable="false"])', '[tabindex]',
].join(',');

function isVisible(el) {
    if (!el.isConnected) return false;
    if (el.getClientRects().length === 0) return false;
    const style = getComputedStyle(el);
    return style.visibility !== 'hidden' && style.visibility !== 'collapse';
}

function isFocusable(el) {
    if (!isElement(el)) return false;
    if (el.disabled) return false;
    if (el.closest('[inert]')) return false;
    if (el.tagName === 'INPUT' && el.type === 'hidden') return false;
    const details = el.closest('details');
    if (details && !details.open && el.tagName !== 'SUMMARY') return false;
    return isVisible(el);
}

function isTabbable(el) {
    if (!isFocusable(el)) return false;
    const tabindex = el.getAttribute('tabindex');
    if (tabindex !== null && parseInt(tabindex, 10) < 0) return false;
    if (el.tagName === 'INPUT' && el.type === 'radio' && el.name && !el.checked) {
        const form = el.form || document;
        const group = Array.from(form.querySelectorAll(`input[type="radio"][name="${CSS.escape(el.name)}"]`));
        if (group.some((r) => r.checked)) return false;
        return group[0] === el;
    }
    return true;
}

function tabbables(container) {
    if (!container) return [];
    const list = Array.from(container.querySelectorAll(CANDIDATE_SELECTOR)).filter(isTabbable);
    // Positive tabindex values come first (in order), then document order.
    const positive = list.filter((el) => el.tabIndex > 0).sort((a, b) => a.tabIndex - b.tabIndex);
    return positive.concat(list.filter((el) => el.tabIndex <= 0));
}

function focusElement(el, options) {
    if (!el || typeof el.focus !== 'function') return false;
    el.focus({ preventScroll: options?.preventScroll !== false });
    if (options?.select && typeof el.select === 'function' && el.tagName === 'INPUT') el.select();
    return document.activeElement === el;
}

const scopeStack = [];
let scopeSeq = 0;

function scopeContains(scope, node) {
    const container = byId(scope.containerId);
    if (container && container.contains(node)) return true;
    // Focus inside a layer opened from within this scope (e.g. a listbox
    // popup portalled to <body>) is still considered inside.
    if (scope.layer) {
        const layer = layerStack.find((l) => l.id === scope.layer);
        if (layer && isInLayerBranch(layer, node)) return true;
    }
    return !!(isElement(node) && node.closest('[data-bh-live-region]'));
}

function onScopeKeyDown(e) {
    if (e.key !== 'Tab' || e.altKey || e.ctrlKey || e.metaKey) return;
    const scope = scopeStack[scopeStack.length - 1];
    if (!scope || !scope.trap && !scope.loop) return;
    const container = byId(scope.containerId);
    if (!container) return;
    const items = tabbables(container);
    const active = document.activeElement;
    if (!container.contains(active) && !scope.trap) return;
    if (items.length === 0) {
        if (scope.trap) { e.preventDefault(); focusElement(container); }
        return;
    }
    const first = items[0];
    const last = items[items.length - 1];
    if (e.shiftKey && (active === first || active === container || !container.contains(active))) {
        e.preventDefault();
        focusElement(last);
    } else if (!e.shiftKey && (active === last || !container.contains(active))) {
        e.preventDefault();
        focusElement(first);
    }
}

function onScopeFocusIn(e) {
    const scope = scopeStack[scopeStack.length - 1];
    if (!scope || !scope.trap) return;
    if (scopeContains(scope, e.target)) {
        scope.lastFocused = e.target;
        return;
    }
    const container = byId(scope.containerId);
    if (!container) return;
    const fallback = scope.lastFocused && container.contains(scope.lastFocused) ? scope.lastFocused : tabbables(container)[0] || container;
    focusElement(fallback);
}

let scopeListenersInstalled = false;

function ensureScopeListeners() {
    if (scopeListenersInstalled) return;
    scopeListenersInstalled = true;
    document.addEventListener('keydown', onScopeKeyDown, true);
    document.addEventListener('focusin', onScopeFocusIn, true);
}

export const focus = {
    tabbables,
    isTabbable,

    /** Focuses the element with the given id. */
    focus(id, preventScroll, select) {
        return focusElement(byId(id), { preventScroll, select });
    },

    /** Focuses the first tabbable element inside the container (or the container). */
    focusFirst(id, preventScroll) {
        const container = byId(id);
        if (!container) return false;
        return focusElement(tabbables(container)[0] || container, { preventScroll });
    },

    focusLast(id, preventScroll) {
        const container = byId(id);
        if (!container) return false;
        const items = tabbables(container);
        return focusElement(items[items.length - 1] || container, { preventScroll });
    },

    /** Moves focus to the next / previous tabbable element after `id` in document order. */
    focusAdjacent(id, backwards) {
        const from = byId(id);
        if (!from) return false;
        const all = tabbables(document.body);
        let index = all.indexOf(from);
        if (index < 0) {
            // `from` is not tabbable (e.g. tabindex=-1); find its position by document order.
            index = all.findIndex((el) => from.compareDocumentPosition(el) & Node.DOCUMENT_POSITION_FOLLOWING);
            index = index < 0 ? all.length : index;
            if (!backwards) index -= 1;
        }
        const next = all[index + (backwards ? -1 : 1)];
        return next ? focusElement(next) : false;
    },

    /** Returns true when the focused element is inside the element with the given id. */
    contains(id) {
        const el = byId(id);
        return !!(el && document.activeElement && el.contains(document.activeElement));
    },

    activeElementId() {
        return document.activeElement?.id || null;
    },

    /**
     * Creates a focus scope. options: { trap, loop, autoFocus: 'first' | 'container' | 'none' | <id>,
     * restore, returnFocusId, layer }. Returns a handle for destroyScope.
     */
    createScope(containerId, options) {
        ensureScopeListeners();
        options = options || {};
        const container = byId(containerId);
        if (!container) return 0;
        const id = ++scopeSeq;
        const scope = {
            id,
            containerId: container.id || containerId,
            trap: !!options.trap,
            loop: !!options.loop,
            restore: options.restore !== false,
            returnFocusId: options.returnFocusId || null,
            previous: document.activeElement,
            layer: options.layer || 0,
            lastFocused: null,
        };
        scopeStack.push(scope);

        const autoFocus = options.autoFocus ?? 'first';
        if (autoFocus !== 'none' && !container.contains(document.activeElement)) {
            let target = null;
            if (isElement(autoFocus)) target = autoFocus;
            else if (autoFocus === 'container') target = container;
            else if (autoFocus === 'first') target = container.querySelector('[data-bh-autofocus]') || tabbables(container)[0] || container;
            else target = document.getElementById(autoFocus) || tabbables(container)[0] || container;
            focusElement(target, { select: target?.tagName === 'INPUT' });
        }
        return id;
    },

    /** Destroys a scope and (optionally) restores focus to where it was before. */
    destroyScope(handle, restore) {
        const index = scopeStack.findIndex((s) => s.id === handle);
        if (index < 0) return;
        const [scope] = scopeStack.splice(index, 1);
        if (restore === false || !scope.restore) return;
        const container = byId(scope.containerId);
        const active = document.activeElement;
        // Only restore when focus would otherwise be lost (inside the closing
        // scope, or on <body>). Pointer users who clicked elsewhere keep focus.
        const lost = !active || active === document.body || (container && container.contains(active));
        if (!lost) return;
        const explicit = isElement(scope.returnFocusId) ? scope.returnFocusId : (scope.returnFocusId && document.getElementById(scope.returnFocusId));
        const target = explicit || scope.previous;
        if (target && target.isConnected) focusElement(target);
    },
};

// ─── modal ───────────────────────────────────────────────────────────────────

const hiddenCounts = new WeakMap();
const originalAria = new WeakMap();
let scrollLockCount = 0;
let scrollSnapshot = null;

function hideElement(el) {
    const count = hiddenCounts.get(el) || 0;
    if (count === 0) {
        originalAria.set(el, { ariaHidden: el.getAttribute('aria-hidden'), inert: el.hasAttribute('inert') });
        el.setAttribute('aria-hidden', 'true');
        el.setAttribute('inert', '');
    }
    hiddenCounts.set(el, count + 1);
}

function unhideElement(el) {
    const count = hiddenCounts.get(el) || 0;
    if (count <= 1) {
        hiddenCounts.delete(el);
        const original = originalAria.get(el);
        originalAria.delete(el);
        if (!original || original.ariaHidden === null) el.removeAttribute('aria-hidden');
        else el.setAttribute('aria-hidden', original.ariaHidden);
        if (!original || !original.inert) el.removeAttribute('inert');
    } else {
        hiddenCounts.set(el, count - 1);
    }
}

const SKIP_TAGS = new Set(['SCRIPT', 'STYLE', 'TEMPLATE', 'LINK', 'META', 'NOSCRIPT']);

/** Hides everything except `keep` elements (and their ancestors) from AT and interaction. */
function hideOthers(keepEls) {
    const keep = keepEls.filter(Boolean);
    const keepAncestors = new Set();
    for (const el of keep) {
        let node = el;
        while (node && node !== document.body) { keepAncestors.add(node); node = node.parentElement; }
    }
    const hidden = [];
    const visit = (parent) => {
        for (const child of Array.from(parent.children)) {
            if (SKIP_TAGS.has(child.tagName)) continue;
            if (child.hasAttribute('data-bh-live-region')) continue;
            if (keep.includes(child)) continue;
            if (keepAncestors.has(child)) { visit(child); continue; }
            hideElement(child);
            hidden.push(child);
        }
    };
    visit(document.body);
    return hidden;
}

function lockScroll() {
    if (scrollLockCount++ > 0) return;
    const body = document.body;
    const html = document.documentElement;
    const scrollbar = window.innerWidth - html.clientWidth;
    scrollSnapshot = { overflow: body.style.overflow, paddingRight: body.style.paddingRight };
    body.style.overflow = 'hidden';
    if (scrollbar > 0) {
        const current = parseFloat(getComputedStyle(body).paddingRight) || 0;
        body.style.paddingRight = current + scrollbar + 'px';
    }
    body.setAttribute('data-bh-scroll-locked', '');
}

function unlockScroll() {
    if (scrollLockCount === 0) return;
    if (--scrollLockCount > 0) return;
    const body = document.body;
    if (scrollSnapshot) {
        body.style.overflow = scrollSnapshot.overflow;
        body.style.paddingRight = scrollSnapshot.paddingRight;
    }
    scrollSnapshot = null;
    body.removeAttribute('data-bh-scroll-locked');
}

export const modal = {
    hideOthers(ids) {
        return hideOthers((ids || []).map((id) => byId(id)));
    },
    unhide(elements) {
        for (const el of elements || []) unhideElement(el);
    },
    lockScroll,
    unlockScroll,
};

// ─── portal ──────────────────────────────────────────────────────────────────
//
// Moves a Blazor-rendered element to a container elsewhere in the document.
// Blazor keeps direct references to the nodes it renders and removes them via
// node.parentNode, so a component whose only root element is teleported keeps
// working: cascading values, events and re-renders are unaffected.

const portalHandles = new Map();
let portalSeq = 0;

function portalRoot(target) {
    if (target && target !== true) {
        const el = typeof target === 'string' ? (document.getElementById(target) || document.querySelector(target)) : target;
        if (el) return el;
    }
    let root = document.querySelector('[data-bh-portal-root]');
    if (!root) {
        root = document.createElement('div');
        root.setAttribute('data-bh-portal-root', '');
        root.id = 'bh-portal-root';
        document.body.appendChild(root);
    }
    return root;
}

export const portal = {
    move(element, target) {
        const el = byId(element);
        if (!el || !el.parentNode) return 0;
        const placeholder = document.createComment('bh-portal');
        el.parentNode.insertBefore(placeholder, el);
        portalRoot(target).appendChild(el);
        const id = ++portalSeq;
        portalHandles.set(id, { el, placeholder });
        return id;
    },
    restore(handle) {
        const state = portalHandles.get(handle);
        if (!state) return;
        portalHandles.delete(handle);
        const { el, placeholder } = state;
        if (placeholder.parentNode) {
            if (el.isConnected) placeholder.parentNode.insertBefore(el, placeholder);
            placeholder.parentNode.removeChild(placeholder);
        }
    },
};

// ─── presence ────────────────────────────────────────────────────────────────
//
// Exit animations without server round-trips. Components render `hidden` the
// moment they close (so closing is instant when there is nothing to animate).
// Elements marked [data-bh-presence] are watched: when `hidden` is added, the
// element is shown again for as long as a CSS animation / transition triggered
// by its closed state runs, then hidden for real. Reopening cancels the exit.
//
// While visible, --bh-content-height / --bh-content-width hold the element's
// natural size so collapsibles can animate height: 0 ↔ auto.

function parseDurations(value) {
    return String(value || '0s').split(',').map((s) => {
        s = s.trim();
        const n = parseFloat(s);
        if (isNaN(n)) return 0;
        return s.endsWith('ms') ? n : n * 1000;
    });
}

function longestMotion(el) {
    const cs = getComputedStyle(el);
    let max = 0;
    if (cs.animationName && cs.animationName !== 'none') {
        const d = parseDurations(cs.animationDuration);
        const delay = parseDurations(cs.animationDelay);
        d.forEach((v, i) => { max = Math.max(max, v + (delay[i % delay.length] || 0)); });
    }
    const td = parseDurations(cs.transitionDuration);
    const tdel = parseDurations(cs.transitionDelay);
    td.forEach((v, i) => { max = Math.max(max, v + (tdel[i % tdel.length] || 0)); });
    return max;
}

/** Finite animations / transitions currently running on the element itself. */
function runningAnimations(el) {
    if (typeof el.getAnimations !== 'function') return null;
    getComputedStyle(el).getPropertyValue('opacity'); // flush style so new animations start
    const subtree = el.getAttribute('data-bh-presence') === 'subtree';
    return el.getAnimations({ subtree }).filter((a) => {
        if (a.playState === 'finished' || a.playState === 'idle') return false;
        const end = a.effect?.getComputedTiming?.().endTime;
        return end !== Infinity;
    });
}

function waitForMotion(el) {
    if (!el || !el.isConnected) return Promise.resolve();
    const anims = runningAnimations(el);
    if (anims) {
        return anims.length === 0
            ? Promise.resolve()
            : Promise.allSettled(anims.map((a) => a.finished)).then(() => { });
    }
    // Fallback for browsers without the Web Animations API.
    return new Promise((resolve) => {
        const ms = longestMotion(el);
        if (ms <= 0) { resolve(); return; }
        const finish = () => {
            clearTimeout(timer);
            el.removeEventListener('animationend', onEnd);
            el.removeEventListener('transitionend', onEnd);
            resolve();
        };
        const onEnd = (e) => { if (e.target === el) finish(); };
        const timer = setTimeout(finish, ms + 50);
        el.addEventListener('animationend', onEnd);
        el.addEventListener('transitionend', onEnd);
    });
}

const presenceExits = new WeakMap();
let presenceObserver = null;

function setContentSize(el) {
    el.style.setProperty('--bh-content-height', el.scrollHeight + 'px');
    el.style.setProperty('--bh-content-width', el.scrollWidth + 'px');
}

function startExit(el) {
    if (presenceExits.has(el)) return;
    el.removeAttribute('hidden');
    const anims = runningAnimations(el);
    const hasMotion = anims ? anims.length > 0 : longestMotion(el) > 0;
    if (!hasMotion) {
        el.setAttribute('hidden', '');
        presenceObserver?.takeRecords();
        return;
    }
    el.setAttribute('data-exiting', '');
    const entry = {};
    entry.promise = waitForMotion(el).then(() => {
        if (presenceExits.get(el) !== entry) return;
        presenceExits.delete(el);
        el.removeAttribute('data-exiting');
        el.setAttribute('hidden', '');
        presenceObserver?.takeRecords();
    });
    presenceExits.set(el, entry);
    presenceObserver?.takeRecords();
}

function cancelExit(el) {
    if (!presenceExits.has(el)) return;
    presenceExits.delete(el);
    el.removeAttribute('data-exiting');
}

function onPresenceMutations(records) {
    for (const r of records) {
        const el = r.target;
        if (!isElement(el) || !el.hasAttribute('data-bh-presence')) continue;
        if (r.attributeName === 'hidden') {
            if (el.hasAttribute('hidden')) {
                if (r.oldValue === null) startExit(el);
            } else {
                cancelExit(el);
                setContentSize(el);
            }
        } else if (r.attributeName === 'data-state') {
            const state = el.getAttribute('data-state');
            if (state === 'open' || state === 'active' || state === 'checked') {
                cancelExit(el);
                if (!el.hasAttribute('hidden')) setContentSize(el);
            } else if (!el.hasAttribute('hidden') && !presenceExits.has(el)) {
                setContentSize(el);
            }
        }
    }
}

function installPresence() {
    if (typeof MutationObserver === 'undefined') return;
    presenceObserver = new MutationObserver(onPresenceMutations);
    presenceObserver.observe(document.documentElement, {
        subtree: true,
        attributes: true,
        attributeOldValue: true,
        attributeFilter: ['hidden', 'data-state'],
    });
}

/** Resolves when the element's presence exit (if any) has finished. */
function presenceDone(el) {
    if (!el) return Promise.resolve();
    const exit = presenceExits.get(el);
    if (exit) return exit.promise;
    return el.hasAttribute('hidden') ? Promise.resolve() : waitForMotion(el);
}

export const presence = {
    waitForExit(id) {
        return presenceDone(byId(id));
    },
    measure(id) {
        const el = byId(id);
        if (el) setContentSize(el);
    },
};

// ─── floating ────────────────────────────────────────────────────────────────
//
// One controller per open floating surface. open() performs, in order:
// portal → reveal → position → layer → modal → focus. close() undoes it, waits
// for the exit animation, and returns the element to its original place.

const floatingHandles = new Map();
let floatingSeq = 0;

export const floating = {
    /**
     * opts: {
     *   portalIds: [ids moved to the portal root, in order], portal (true | selector | id),
     *   reference (id | {x, y}), anchor (anchor options),
     *   layer: { escape, outside, focusOutside, modal, hover, parentFloating, ids: [] },
     *   modal: { hideOthers, scrollLock },
     *   focus: { trap, loop, autoFocus, restore, returnFocusId },
     * }
     */
    open(dotnet, contentId, opts) {
        const content = byId(contentId);
        if (!content) return 0;
        opts = opts || {};
        const id = ++floatingSeq;
        const surfaceIds = (opts.portalIds && opts.portalIds.length ? opts.portalIds : [contentId]).filter((x) => !!byId(x));
        const state = { id, contentId, opts, dotnet, surfaceIds, portals: [] };

        if (opts.portal) {
            for (const sid of surfaceIds) {
                const h = portal.move(sid, opts.portal);
                if (h) state.portals.push(h);
            }
        }

        for (const sid of surfaceIds) byId(sid)?.removeAttribute('hidden');
        content.removeAttribute('hidden');

        if (opts.anchor && opts.reference) {
            state.anchor = anchor.start(opts.reference, content, opts.anchor);
        }

        if (opts.layer) {
            const ids = [contentId, ...(opts.layer.ids || [])];
            const parent = opts.layer.parentFloating ? floatingHandles.get(opts.layer.parentFloating) : null;
            state.layer = layers.push(dotnet, ids, { ...opts.layer, parent: parent?.layer || 0 });
        }

        if (opts.modal) {
            if (opts.modal.hideOthers !== false) state.hidden = modal.hideOthers([contentId, ...surfaceIds]);
            if (opts.modal.scrollLock !== false) { lockScroll(); state.scrollLocked = true; }
        }

        if (opts.focus) {
            state.scope = focus.createScope(contentId, { ...opts.focus, layer: state.layer });
        }

        floatingHandles.set(id, state);
        return id;
    },

    update(handle, opts) {
        const state = floatingHandles.get(handle);
        if (!state) return;
        if (opts?.anchor && state.anchor) anchor.update(state.anchor, opts.anchor);
        else if (state.anchor) anchor.update(state.anchor);
        if (opts?.layerIds && state.layer) layers.setIds(state.layer, [state.contentId, ...opts.layerIds]);
    },

    setPoint(handle, x, y) {
        const state = floatingHandles.get(handle);
        if (state?.anchor) anchor.setPoint(state.anchor, x, y);
    },

    /** Closes the surface; resolves once the exit animation (if any) has finished. */
    async close(handle, opts) {
        const state = floatingHandles.get(handle);
        if (!state) return;
        floatingHandles.delete(handle);
        opts = opts || {};

        // Dismiss nested surfaces first (a submenu never outlives its menu).
        for (const [childHandle, child] of Array.from(floatingHandles.entries())) {
            if (child.opts.layer?.parentFloating === handle) floating.close(childHandle, { waitForExit: false, restoreFocus: false });
        }

        // Un-inert the page before restoring focus: inert elements can't take focus.
        if (state.hidden) modal.unhide(state.hidden);
        if (state.scrollLocked) unlockScroll();
        if (state.layer) layers.remove(state.layer);
        if (state.scope) focus.destroyScope(state.scope, opts.restoreFocus);

        if (opts.waitForExit !== false) {
            const els = [...new Set([...state.surfaceIds, state.contentId])].map((x) => byId(x)).filter(Boolean);
            await Promise.all(els.map(presenceDone));
        }

        if (state.anchor) anchor.stop(state.anchor, true);
        const content = byId(state.contentId);
        if (content && opts.hide !== false) content.setAttribute('hidden', '');
        for (const h of state.portals.reverse()) portal.restore(h);
    },

    /** Closes every open surface. Used when the circuit tears down. */
    closeAll() {
        for (const handle of Array.from(floatingHandles.keys())) floating.close(handle, { waitForExit: false });
    },
};

// ─── hotkeys ─────────────────────────────────────────────────────────────────
//
// Global keyboard shortcuts ("mod+k", "shift+?", "ctrl+alt+p"). "mod" is Cmd
// on Apple platforms and Ctrl elsewhere. Shortcuts are ignored while typing in
// editable fields unless allowInInputs is set.

const hotkeyHandles = new Map();
let hotkeySeq = 0;
const isApple = typeof navigator !== 'undefined' && /Mac|iPhone|iPad|iPod/i.test(navigator.platform || navigator.userAgent);

function parseHotkey(combo) {
    const parts = String(combo).toLowerCase().split('+').map((p) => p.trim()).filter(Boolean);
    const key = parts.pop() || '';
    const mods = new Set(parts.map((m) => (m === 'mod' ? (isApple ? 'meta' : 'ctrl') : m === 'cmd' ? 'meta' : m === 'option' ? 'alt' : m)));
    return { key: key === 'space' ? ' ' : key, mods };
}

function hotkeyMatches(e, hk) {
    if ((e.key || '').toLowerCase() !== hk.key && (e.code || '').toLowerCase() !== `key${hk.key}`) return false;
    return e.ctrlKey === hk.mods.has('ctrl') && e.metaKey === hk.mods.has('meta')
        && e.altKey === hk.mods.has('alt') && (hk.key.length > 1 || hk.mods.has('shift') ? e.shiftKey === hk.mods.has('shift') : true);
}

function isEditable(el) {
    return isElement(el) && (el.isContentEditable || /^(input|textarea|select)$/i.test(el.tagName));
}

export const hotkeys = {
    register(dotnet, combos, options) {
        const id = ++hotkeySeq;
        const parsed = (Array.isArray(combos) ? combos : [combos]).map(parseHotkey);
        const allowInInputs = !!options?.allowInInputs;
        const handler = (e) => {
            if (!allowInInputs && isEditable(e.target) && !parsed.some((h) => h.mods.has('ctrl') || h.mods.has('meta'))) return;
            if (parsed.some((h) => hotkeyMatches(e, h))) {
                e.preventDefault();
                dotnet.invokeMethodAsync('OnHotkey').catch(() => { });
            }
        };
        document.addEventListener('keydown', handler, true);
        hotkeyHandles.set(id, handler);
        return id;
    },
    unregister(handle) {
        const handler = hotkeyHandles.get(handle);
        if (!handler) return;
        hotkeyHandles.delete(handle);
        document.removeEventListener('keydown', handler, true);
    },
};

// ─── collection ──────────────────────────────────────────────────────────────

export const collection = {
    /**
     * Returns [id, text] for every element tagged [data-bh-item="key"], in document
     * order. The text (data-bh-text, else the item's own text — nested items of the
     * same collection, like child tree items, excluded) feeds typeahead.
     */
    order(key) {
        const selector = `[data-bh-item="${CSS.escape(key)}"]`;
        const ownText = (el) => {
            if (!el.querySelector(selector)) return el.textContent ?? '';
            const clone = el.cloneNode(true);
            clone.querySelectorAll(selector).forEach((nested) => nested.remove());
            return clone.textContent ?? '';
        };
        return Array.from(document.querySelectorAll(selector))
            .map((el) => [el.id, (el.getAttribute('data-bh-text') ?? ownText(el)).trim().replace(/\s+/g, ' ')]);
    },
};

// ─── dom ─────────────────────────────────────────────────────────────────────

export const dom = {
    scrollIntoView(id, block) {
        const el = byId(id);
        if (!el) return;
        const parent = scrollParent(el);
        if (parent) {
            const pr = parent.getBoundingClientRect();
            const er = el.getBoundingClientRect();
            if (er.top < pr.top) parent.scrollTop -= pr.top - er.top;
            else if (er.bottom > pr.bottom) parent.scrollTop += er.bottom - pr.bottom;
        } else {
            el.scrollIntoView({ block: block || 'nearest', inline: 'nearest' });
        }
    },
    /** Scrolls a virtualized container so the item at `index` (fixed `itemSize`) is visible. */
    scrollToIndex(containerId, index, itemSize, itemId) {
        const container = byId(containerId);
        if (!container) return;
        const el = itemId ? document.getElementById(itemId) : null;
        if (el) { dom.scrollIntoView(itemId); return; }
        const top = index * itemSize;
        if (top < container.scrollTop) container.scrollTop = top;
        else if (top + itemSize > container.scrollTop + container.clientHeight) container.scrollTop = top + itemSize - container.clientHeight;
    },
    click(id) {
        byId(id)?.click();
    },
    rect(id) {
        const el = byId(id);
        if (!el) return null;
        const r = el.getBoundingClientRect();
        return { x: r.x, y: r.y, width: r.width, height: r.height };
    },
    setAttribute(id, name, value) {
        const el = byId(id);
        if (!el) return;
        if (value === null || value === undefined) el.removeAttribute(name);
        else el.setAttribute(name, value);
    },
};

function scrollParent(el) {
    let node = el.parentElement;
    while (node && node !== document.body) {
        const style = getComputedStyle(node);
        if (/(auto|scroll|overlay)/.test(style.overflowY) && node.scrollHeight > node.clientHeight) return node;
        node = node.parentElement;
    }
    return null;
}

// ─── announcer ───────────────────────────────────────────────────────────────

const VISUALLY_HIDDEN = 'position:absolute;width:1px;height:1px;margin:-1px;padding:0;border:0;overflow:hidden;clip:rect(0 0 0 0);clip-path:inset(50%);white-space:nowrap;';
let liveRoot = null;

function liveRegion(politeness) {
    if (!liveRoot || !liveRoot.isConnected) {
        liveRoot = document.createElement('div');
        liveRoot.setAttribute('data-bh-live-region', '');
        liveRoot.style.cssText = VISUALLY_HIDDEN;
        for (const p of ['polite', 'assertive']) {
            const region = document.createElement('div');
            region.setAttribute('aria-live', p);
            region.setAttribute('aria-atomic', 'true');
            region.setAttribute('role', p === 'assertive' ? 'alert' : 'status');
            region.setAttribute('data-bh-politeness', p);
            liveRoot.appendChild(region);
        }
        document.body.appendChild(liveRoot);
    }
    return liveRoot.querySelector(`[data-bh-politeness="${politeness === 'assertive' ? 'assertive' : 'polite'}"]`);
}

export const announcer = {
    announce(message, politeness, clearAfterMs) {
        const region = liveRegion(politeness);
        region.textContent = '';
        // A short delay ensures screen readers notice the change even when the
        // same message is announced twice in a row.
        setTimeout(() => {
            region.textContent = message;
            if (clearAfterMs > 0) setTimeout(() => { if (region.textContent === message) region.textContent = ''; }, clearAfterMs);
        }, 50);
    },
    clear() {
        if (!liveRoot) return;
        for (const r of liveRoot.children) r.textContent = '';
    },
};

// ─── transition ──────────────────────────────────────────────────────────────

const runningTransitions = new WeakMap();

function splitClasses(str) {
    return str ? String(str).trim().split(/\s+/).filter(Boolean) : [];
}

function nextFrame() {
    return new Promise((resolve) => requestAnimationFrame(() => requestAnimationFrame(resolve)));
}

// Runs one phase of a transition. Besides the class lists, it maintains the
// Headless UI v2 data attributes so plain CSS can drive transitions:
//   [data-transition]   while either phase runs
//   [data-enter] / [data-leave]
//   [data-closed]       on the first frame of enter and the last frames of leave
async function runTransition(el, phase, base, from, to, final, clear) {
    const token = {};
    runningTransitions.set(el, token);
    el.classList.remove(...splitClasses(base), ...splitClasses(from), ...splitClasses(to), ...splitClasses(clear));
    el.removeAttribute(phase === 'enter' ? 'data-leave' : 'data-enter');
    el.setAttribute('data-transition', '');
    el.setAttribute(`data-${phase}`, '');
    if (phase === 'enter') el.setAttribute('data-closed', '');
    el.classList.add(...splitClasses(base), ...splitClasses(from));

    await nextFrame();
    if (runningTransitions.get(el) !== token) return false;

    el.classList.remove(...splitClasses(from));
    el.classList.add(...splitClasses(to));
    if (phase === 'enter') el.removeAttribute('data-closed');
    else el.setAttribute('data-closed', '');

    await waitForMotion(el);
    if (runningTransitions.get(el) !== token) return false;

    el.classList.remove(...splitClasses(base), ...splitClasses(to));
    el.classList.add(...splitClasses(final));
    el.removeAttribute('data-transition');
    el.removeAttribute(`data-${phase}`);
    runningTransitions.delete(el);
    return true;
}

export const transition = {
    /** Runs the enter transition. Resolves true when it completed, false when interrupted. */
    enter(el, c) {
        el = byId(el);
        if (!el) return Promise.resolve(true);
        c = c || {};
        return runTransition(el, 'enter', c.enter, c.enterFrom, c.enterTo, c.entered, `${c.leave || ''} ${c.leaveFrom || ''} ${c.leaveTo || ''}`);
    },
    leave(el, c) {
        el = byId(el);
        if (!el) return Promise.resolve(true);
        c = c || {};
        el.classList.remove(...splitClasses(c.entered));
        return runTransition(el, 'leave', c.leave, c.leaveFrom, c.leaveTo, '', `${c.enter || ''} ${c.enterFrom || ''} ${c.enterTo || ''}`);
    },
};

// ─── sortable ────────────────────────────────────────────────────────────────
//
// Pointer-driven reordering. While dragging, siblings are shifted with CSS
// transforms (no .NET round-trips); on drop the final indexes are reported to
// .NET, which reorders its data and re-renders.

const sortables = new Map();
let sortableSeq = 0;

function sortableItems(state) {
    const container = byId(state.containerId);
    if (!container) return [];
    return Array.from(container.querySelectorAll(`[data-bh-sortable-item="${CSS.escape(state.containerId)}"]`));
}

function startSortDrag(state, item, e) {
    const items = sortableItems(state);
    const from = items.indexOf(item);
    if (from < 0) return;
    const vertical = state.options.orientation !== 'horizontal';
    const rects = items.map((el) => el.getBoundingClientRect());
    const drag = { item, from, to: from, items, rects, vertical, startX: e.clientX, startY: e.clientY, pointerId: e.pointerId, started: false };
    state.drag = drag;

    const move = (ev) => {
        if (ev.pointerId !== drag.pointerId) return;
        const dx = ev.clientX - drag.startX;
        const dy = ev.clientY - drag.startY;
        if (!drag.started) {
            if (Math.hypot(dx, dy) < (state.options.threshold ?? 4)) return;
            drag.started = true;
            item.setAttribute('data-dragging', '');
            byId(state.containerId)?.setAttribute('data-dragging', '');
            item.style.zIndex = '1';
            item.style.position = item.style.position || 'relative';
            for (const el of items) if (el !== item) el.style.transition = 'transform 150ms ease';
            try { item.setPointerCapture(drag.pointerId); } catch { /* ignore */ }
            state.dotnet?.invokeMethodAsync('OnPointerDragStart', from).catch(() => { });
        }
        ev.preventDefault();
        item.style.transform = vertical ? `translateY(${dy}px)` : `translateX(${dx}px)`;

        const r = drag.rects[from];
        const center = vertical ? r.top + r.height / 2 + dy : r.left + r.width / 2 + dx;
        let to = from;
        for (let i = 0; i < drag.rects.length; i++) {
            const ri = drag.rects[i];
            const mid = vertical ? ri.top + ri.height / 2 : ri.left + ri.width / 2;
            if (i < from && center < mid) { to = i; break; }
            if (i > from && center > mid) to = i;
        }
        drag.to = to;
        const size = vertical ? r.height : r.width;
        const gapSize = items.length > 1 ? (vertical
            ? Math.abs((drag.rects[1].top - drag.rects[0].bottom))
            : Math.abs((drag.rects[1].left - drag.rects[0].right))) : 0;
        items.forEach((el, i) => {
            if (el === item) return;
            let shift = 0;
            if (from < to && i > from && i <= to) shift = -(size + gapSize);
            if (from > to && i >= to && i < from) shift = size + gapSize;
            el.style.transform = shift ? (vertical ? `translateY(${shift}px)` : `translateX(${shift}px)`) : '';
        });
    };

    const end = (ev, cancelled) => {
        if (ev && ev.pointerId !== drag.pointerId) return;
        document.removeEventListener('pointermove', move, true);
        document.removeEventListener('pointerup', up, true);
        document.removeEventListener('pointercancel', cancel, true);
        document.removeEventListener('keydown', esc, true);
        for (const el of items) { el.style.transform = ''; el.style.transition = ''; el.style.zIndex = ''; }
        item.removeAttribute('data-dragging');
        byId(state.containerId)?.removeAttribute('data-dragging');
        state.drag = null;
        if (!drag.started) return;
        // Swallow the click that follows a drag.
        const swallow = (ce) => { ce.stopPropagation(); ce.preventDefault(); };
        document.addEventListener('click', swallow, { capture: true, once: true });
        setTimeout(() => document.removeEventListener('click', swallow, true), 0);
        state.dotnet?.invokeMethodAsync('OnPointerDragEnd', drag.from, cancelled ? drag.from : drag.to, !!cancelled).catch(() => { });
    };
    const up = (ev) => end(ev, false);
    const cancel = (ev) => end(ev, true);
    const esc = (ev) => { if (ev.key === 'Escape' && drag.started) { ev.preventDefault(); ev.stopPropagation(); end(null, true); } };

    document.addEventListener('pointermove', move, true);
    document.addEventListener('pointerup', up, true);
    document.addEventListener('pointercancel', cancel, true);
    document.addEventListener('keydown', esc, true);
}

export const sortable = {
    /** options: { orientation, handleSelector, threshold } */
    attach(dotnet, containerId, options) {
        const container = byId(containerId);
        if (!container) return 0;
        const id = ++sortableSeq;
        const state = { id, dotnet, containerId: container.id, options: options || {}, drag: null };
        state.onPointerDown = (e) => {
            if (e.button !== 0 || state.drag) return;
            const item = e.target.closest(`[data-bh-sortable-item="${CSS.escape(state.containerId)}"]`);
            if (!item || item.hasAttribute('data-disabled')) return;
            const handle = item.querySelector('[data-bh-sortable-handle]');
            if (handle && !handle.contains(e.target)) return;
            if (!handle && e.target.closest('input, textarea, select, [contenteditable="true"]')) return;
            startSortDrag(state, item, e);
        };
        container.addEventListener('pointerdown', state.onPointerDown);
        sortables.set(id, state);
        return id;
    },
    update(handle, options) {
        const state = sortables.get(handle);
        if (state) state.options = options || {};
    },
    detach(handle) {
        const state = sortables.get(handle);
        if (!state) return;
        sortables.delete(handle);
        byId(state.containerId)?.removeEventListener('pointerdown', state.onPointerDown);
    },
};

// ─── slider ──────────────────────────────────────────────────────────────────

const sliders = new Map();
let sliderSeq = 0;

function sliderFraction(state, e) {
    const track = byId(state.trackId) || byId(state.rootId);
    if (!track) return 0;
    const r = track.getBoundingClientRect();
    const vertical = state.options.orientation === 'vertical';
    let f = vertical ? (r.bottom - e.clientY) / r.height : (e.clientX - r.left) / r.width;
    if (!vertical && isRtl(track)) f = 1 - f;
    if (state.options.inverted) f = 1 - f;
    return Math.max(0, Math.min(1, f));
}

export const slider = {
    /** options: { orientation, inverted }. Calls OnSliderPointer(phase, fraction) on .NET. */
    attach(dotnet, rootId, trackId, options) {
        const root = byId(rootId);
        if (!root) return 0;
        const id = ++sliderSeq;
        const state = { id, dotnet, rootId, trackId, options: options || {}, pending: null, busy: false };

        const send = (phase, fraction) => {
            // Coalesce pointer moves: only one call in flight at a time.
            if (phase === 'move' && state.busy) { state.pending = fraction; return; }
            state.busy = true;
            dotnet.invokeMethodAsync('OnSliderPointer', phase, fraction).catch(() => { }).finally(() => {
                state.busy = false;
                if (state.pending !== null) { const f = state.pending; state.pending = null; send('move', f); }
            });
        };

        state.onPointerDown = (e) => {
            if (e.button !== 0 || root.hasAttribute('data-disabled')) return;
            e.preventDefault();
            const thumb = e.target.closest('[role="slider"]');
            (thumb || root.querySelector('[role="slider"]'))?.focus({ preventScroll: true });
            try { root.setPointerCapture(e.pointerId); } catch { /* ignore */ }
            root.setAttribute('data-dragging', '');
            send('start', sliderFraction(state, e));
            const move = (ev) => send('move', sliderFraction(state, ev));
            const up = (ev) => {
                root.removeEventListener('pointermove', move);
                root.removeEventListener('pointerup', up);
                root.removeEventListener('pointercancel', up);
                root.removeAttribute('data-dragging');
                state.pending = null;
                send('end', sliderFraction(state, ev));
            };
            root.addEventListener('pointermove', move);
            root.addEventListener('pointerup', up);
            root.addEventListener('pointercancel', up);
        };
        root.addEventListener('pointerdown', state.onPointerDown);
        sliders.set(id, state);
        return id;
    },
    update(handle, options) {
        const state = sliders.get(handle);
        if (state) state.options = options || {};
    },
    detach(handle) {
        const state = sliders.get(handle);
        if (!state) return;
        sliders.delete(handle);
        byId(state.rootId)?.removeEventListener('pointerdown', state.onPointerDown);
    },
};

install();
