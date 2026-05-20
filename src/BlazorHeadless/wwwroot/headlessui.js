// BlazorHeadless — JS interop module
//
// Each exported namespace mirrors a component family. The C# side imports this
// module on first use (via IJSRuntime.InvokeAsync<IJSObjectReference>("import"))
// and keeps a single reference for the lifetime of the application.

// ─── Anchor: floating panel positioning ─────────────────────────────────────
//
// A self-contained positioning engine inspired by Floating UI's computePosition.
// Positions a floating element relative to a reference element with automatic
// flip/shift to stay within the viewport.

const anchorHandles = new Map();
let anchorHandleSeq = 0;

/**
 * Parses a placement string like "bottom start" into { side, alignment }.
 * side: "top" | "right" | "bottom" | "left"
 * alignment: "center" | "start" | "end"
 */
function parsePlacement(to) {
    const parts = (to || 'bottom').trim().split(/\s+/);
    const side = parts[0] || 'bottom';
    const alignment = parts[1] || 'center';
    return { side, alignment };
}

/**
 * Returns the opposite side for flip calculations.
 */
function oppositeSide(side) {
    switch (side) {
        case 'top': return 'bottom';
        case 'bottom': return 'top';
        case 'left': return 'right';
        case 'right': return 'left';
        default: return 'bottom';
    }
}

/**
 * Computes the position of the floating element relative to the reference.
 */
function computePosition(reference, floating, options) {
    const { side, alignment } = parsePlacement(options.to);
    const gap = options.gap || 0;
    const offset = options.offset || 0;
    const padding = options.padding || 8;

    const refRect = reference.getBoundingClientRect();
    const floatRect = floating.getBoundingClientRect();
    const viewport = {
        width: window.innerWidth || document.documentElement.clientWidth,
        height: window.innerHeight || document.documentElement.clientHeight,
    };

    let x = 0, y = 0;
    let finalSide = side;

    // Calculate position based on side
    switch (side) {
        case 'bottom':
            y = refRect.bottom + gap;
            x = computeAlignmentX(refRect, floatRect, alignment, offset);
            // Flip to top if not enough space below
            if (y + floatRect.height > viewport.height - padding &&
                refRect.top - gap - floatRect.height >= padding) {
                y = refRect.top - gap - floatRect.height;
                finalSide = 'top';
            }
            break;
        case 'top':
            y = refRect.top - gap - floatRect.height;
            x = computeAlignmentX(refRect, floatRect, alignment, offset);
            // Flip to bottom if not enough space above
            if (y < padding &&
                refRect.bottom + gap + floatRect.height <= viewport.height - padding) {
                y = refRect.bottom + gap;
                finalSide = 'bottom';
            }
            break;
        case 'left':
            x = refRect.left - gap - floatRect.width;
            y = computeAlignmentY(refRect, floatRect, alignment, offset);
            // Flip to right if not enough space on left
            if (x < padding &&
                refRect.right + gap + floatRect.width <= viewport.width - padding) {
                x = refRect.right + gap;
                finalSide = 'right';
            }
            break;
        case 'right':
            x = refRect.right + gap;
            y = computeAlignmentY(refRect, floatRect, alignment, offset);
            // Flip to left if not enough space on right
            if (x + floatRect.width > viewport.width - padding &&
                refRect.left - gap - floatRect.width >= padding) {
                x = refRect.left - gap - floatRect.width;
                finalSide = 'left';
            }
            break;
    }

    // Shift: clamp to viewport bounds
    if (finalSide === 'top' || finalSide === 'bottom') {
        x = Math.max(padding, Math.min(x, viewport.width - floatRect.width - padding));
    } else {
        y = Math.max(padding, Math.min(y, viewport.height - floatRect.height - padding));
    }

    return { x, y, side: finalSide, alignment };
}

function computeAlignmentX(refRect, floatRect, alignment, offset) {
    switch (alignment) {
        case 'start': return refRect.left + offset;
        case 'end': return refRect.right - floatRect.width - offset;
        default: return refRect.left + (refRect.width - floatRect.width) / 2 + offset;
    }
}

function computeAlignmentY(refRect, floatRect, alignment, offset) {
    switch (alignment) {
        case 'start': return refRect.top + offset;
        case 'end': return refRect.bottom - floatRect.height - offset;
        default: return refRect.top + (refRect.height - floatRect.height) / 2 + offset;
    }
}

/**
 * Applies computed position to the floating element.
 */
function applyPosition(floating, reference, options) {
    // Set CSS custom properties BEFORE measuring so that CSS rules like
    // `width: var(--button-width)` take effect during measurement.
    const refRect = reference.getBoundingClientRect();
    floating.style.setProperty('--button-width', refRect.width + 'px');
    floating.style.setProperty('--anchor-gap', (options.gap || 0) + 'px');
    floating.style.setProperty('--anchor-offset', (options.offset || 0) + 'px');
    floating.style.setProperty('--anchor-padding', (options.padding || 8) + 'px');

    // Force a reflow so CSS rules using the custom properties (e.g. width: var(--button-width))
    // are applied before we measure the floating element's dimensions.
    floating.offsetHeight; // eslint-disable-line no-unused-expressions

    const pos = computePosition(reference, floating, options);

    // Apply positioning styles
    floating.style.position = 'fixed';
    floating.style.top = '0';
    floating.style.left = '0';
    floating.style.transform = `translate(${Math.round(pos.x)}px, ${Math.round(pos.y)}px)`;
    floating.style.willChange = 'transform';

    // Set data attributes for the resolved placement
    floating.setAttribute('data-anchor', pos.side + (pos.alignment !== 'center' ? ' ' + pos.alignment : ''));
}

export const anchor = {
    /**
     * Starts positioning the floating element relative to the reference element.
     * Sets up auto-update via scroll/resize listeners and ResizeObserver.
     * Returns a handle id that must be passed to anchor.stop(...) to clean up.
     *
     * reference: can be an Element or an element ID string
     * floating: can be an Element or an element ID string
     * options: { to, gap, offset, padding }
     */
    start(reference, floating, options) {
        // Resolve elements if IDs were passed
        if (typeof reference === 'string') reference = document.getElementById(reference);
        if (typeof floating === 'string') floating = document.getElementById(floating);
        if (!reference || !floating) return -1;
        options = options || {};

        const id = ++anchorHandleSeq;

        // Remove hidden if still present (Blazor should have already removed it
        // when IsOpen=true, but ensure it's gone for measurement).
        if (floating.hasAttribute('hidden')) {
            floating.removeAttribute('hidden');
        }

        // Auto-update: reposition on scroll, resize, and DOM changes
        const update = () => {
            if (anchorHandles.has(id)) {
                applyPosition(floating, reference, options);
            }
        };

        // Try initial positioning immediately. If the floating element has no
        // dimensions yet (browser hasn't laid it out), retry on next frame.
        applyPosition(floating, reference, options);
        const floatRect = floating.getBoundingClientRect();
        if (floatRect.width === 0 || floatRect.height === 0) {
            requestAnimationFrame(() => {
                if (anchorHandles.has(id)) {
                    applyPosition(floating, reference, options);
                }
            });
        }

        // Listen to scroll on all ancestor scroll containers
        const scrollParents = getScrollParents(reference);
        for (const parent of scrollParents) {
            parent.addEventListener('scroll', update, { passive: true });
        }
        window.addEventListener('resize', update, { passive: true });

        // ResizeObserver for reference and floating element size changes
        let resizeObserver = null;
        if (typeof ResizeObserver !== 'undefined') {
            resizeObserver = new ResizeObserver(update);
            resizeObserver.observe(reference);
            resizeObserver.observe(floating);
        }

        anchorHandles.set(id, { reference, floating, options, update, scrollParents, resizeObserver });
        return id;
    },

    /**
     * Stops auto-updating and cleans up listeners for the given handle.
     */
    stop(handle) {
        const state = anchorHandles.get(handle);
        if (!state) return;
        anchorHandles.delete(handle);

        for (const parent of state.scrollParents) {
            parent.removeEventListener('scroll', state.update);
        }
        window.removeEventListener('resize', state.update);

        if (state.resizeObserver) {
            state.resizeObserver.disconnect();
        }

        // Reset inline styles applied by the positioning engine
        const el = state.floating;
        if (el) {
            el.style.position = '';
            el.style.top = '';
            el.style.left = '';
            el.style.transform = '';
            el.style.willChange = '';
            el.style.visibility = '';
            el.style.display = '';
            el.style.removeProperty('--anchor-gap');
            el.style.removeProperty('--anchor-offset');
            el.style.removeProperty('--anchor-padding');
            el.style.removeProperty('--button-width');
            el.removeAttribute('data-anchor');
            // Re-hide the element so it doesn't flash unstyled content.
            // Blazor will also set hidden on next render, but we do it
            // immediately to prevent a visible frame.
            el.setAttribute('hidden', '');
        }
    },

    /**
     * Forces an immediate reposition for the given handle.
     */
    update(handle) {
        const state = anchorHandles.get(handle);
        if (!state) return;
        applyPosition(state.floating, state.reference, state.options);
    }
};

/**
 * Collects all scrollable ancestor elements of the given element.
 */
function getScrollParents(element) {
    const parents = [];
    let current = element.parentElement;
    while (current) {
        const style = getComputedStyle(current);
        const overflow = style.overflow + style.overflowX + style.overflowY;
        if (/auto|scroll|overlay/.test(overflow)) {
            parents.push(current);
        }
        current = current.parentElement;
    }
    parents.push(window);
    return parents;
}

// ─── Focusable helpers ───────────────────────────────────────────────────────

const FOCUSABLE_SELECTOR = [
    'a[href]:not([disabled])',
    'button:not([disabled])',
    'textarea:not([disabled])',
    'input[type="text"]:not([disabled])',
    'input[type="email"]:not([disabled])',
    'input[type="password"]:not([disabled])',
    'input[type="search"]:not([disabled])',
    'input[type="number"]:not([disabled])',
    'input[type="checkbox"]:not([disabled])',
    'input[type="radio"]:not([disabled])',
    'input[type="file"]:not([disabled])',
    'select:not([disabled])',
    '[tabindex]:not([tabindex="-1"]):not([disabled])',
    '[contenteditable="true"]'
].join(',');

function focusableInside(root) {
    if (!root) return [];
    return Array.from(root.querySelectorAll(FOCUSABLE_SELECTOR))
        .filter(el => el.offsetParent !== null || el === document.activeElement);
}

// ─── Dialog: focus trap, scroll lock, inert siblings ────────────────────────

let dialogScrollLockCount = 0;
let dialogScrollSnapshot = null;

const dialogHandles = new Map();
let dialogHandleSeq = 0;

export const dialog = {
    /**
     * Locks focus, scroll, and inerts siblings around the given panel.
     * Returns a handle id that must be passed to dialog.unlock(...).
     *
     * options:
     *   initialFocus:  optional Element to focus first
     *   returnFocus:   optional Element to focus on unlock; default = activeElement at lock time
     */
    lock(panel, options) {
        if (!panel) return -1;
        options = options || {};

        const previousActive = options.returnFocus || document.activeElement;

        // Inert all siblings of the panel's ancestor chain so background content
        // is removed from the AT tree and tab order. We mark only the immediate
        // children of <body> that don't contain the panel.
        const inerted = [];
        for (const child of Array.from(document.body.children)) {
            if (child === panel || child.contains(panel)) continue;
            if (!child.hasAttribute('inert')) {
                child.setAttribute('inert', '');
                child.setAttribute('aria-hidden', 'true');
                inerted.push(child);
            }
        }

        // Lock body scroll. Stack-aware so multiple dialogs don't clobber each other.
        if (dialogScrollLockCount === 0) {
            dialogScrollSnapshot = {
                overflow: document.body.style.overflow,
                paddingRight: document.body.style.paddingRight
            };
            const scrollbar = window.innerWidth - document.documentElement.clientWidth;
            document.body.style.overflow = 'hidden';
            if (scrollbar > 0) document.body.style.paddingRight = scrollbar + 'px';
        }
        dialogScrollLockCount++;

        // Trap focus with a capture-phase keydown listener.
        const onKeyDown = (e) => {
            if (e.key !== 'Tab') return;
            const focusables = focusableInside(panel);
            if (focusables.length === 0) {
                e.preventDefault();
                panel.focus();
                return;
            }
            const first = focusables[0];
            const last = focusables[focusables.length - 1];
            const active = document.activeElement;

            if (e.shiftKey) {
                if (active === first || !panel.contains(active)) {
                    e.preventDefault();
                    last.focus();
                }
            } else {
                if (active === last || !panel.contains(active)) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };
        document.addEventListener('keydown', onKeyDown, true);

        // Initial focus.
        const initial = options.initialFocus || focusableInside(panel)[0] || panel;
        // The panel may have tabindex="-1" so it's programmatically focusable.
        if (initial) initial.focus({ preventScroll: true });

        // Save the handle.
        const id = ++dialogHandleSeq;
        dialogHandles.set(id, {
            panel,
            previousActive,
            inerted,
            onKeyDown
        });
        return id;
    },

    /**
     * Restores everything captured by the matching lock(...) call.
     */
    unlock(handle) {
        const state = dialogHandles.get(handle);
        if (!state) return;
        dialogHandles.delete(handle);

        document.removeEventListener('keydown', state.onKeyDown, true);

        for (const el of state.inerted) {
            el.removeAttribute('inert');
            el.removeAttribute('aria-hidden');
        }

        dialogScrollLockCount = Math.max(0, dialogScrollLockCount - 1);
        if (dialogScrollLockCount === 0 && dialogScrollSnapshot) {
            document.body.style.overflow = dialogScrollSnapshot.overflow;
            document.body.style.paddingRight = dialogScrollSnapshot.paddingRight;
            dialogScrollSnapshot = null;
        }

        // Restore focus.
        const target = state.previousActive;
        if (target && typeof target.focus === 'function') {
            target.focus({ preventScroll: true });
        }
    }
};

// ─── FocusTrap: standalone focus cycling (no scroll lock, no inert) ──────────

const focusTrapHandles = new Map();
let focusTrapHandleSeq = 0;

export const focusTrap = {
    /**
     * Traps focus inside the given container by intercepting Tab/Shift+Tab.
     * Unlike dialog.lock, this does NOT lock scroll or mark siblings inert.
     * Returns a handle id that must be passed to focusTrap.unlock(...).
     *
     * options:
     *   initialFocus:  optional Element to focus first
     *   returnFocus:   optional Element to focus on unlock; default = activeElement at lock time
     */
    lock(container, options) {
        if (!container) return -1;
        options = options || {};

        const previousActive = options.returnFocus || document.activeElement;

        // Trap focus with a capture-phase keydown listener.
        const onKeyDown = (e) => {
            if (e.key !== 'Tab') return;
            const focusables = focusableInside(container);
            if (focusables.length === 0) {
                e.preventDefault();
                container.focus();
                return;
            }
            const first = focusables[0];
            const last = focusables[focusables.length - 1];
            const active = document.activeElement;

            if (e.shiftKey) {
                if (active === first || !container.contains(active)) {
                    e.preventDefault();
                    last.focus();
                }
            } else {
                if (active === last || !container.contains(active)) {
                    e.preventDefault();
                    first.focus();
                }
            }
        };
        document.addEventListener('keydown', onKeyDown, true);

        // Initial focus.
        const initial = options.initialFocus || focusableInside(container)[0] || container;
        if (initial) initial.focus({ preventScroll: true });

        const id = ++focusTrapHandleSeq;
        focusTrapHandles.set(id, { container, previousActive, onKeyDown });
        return id;
    },

    /**
     * Releases the focus trap and restores focus to the previously active element.
     */
    unlock(handle) {
        const state = focusTrapHandles.get(handle);
        if (!state) return;
        focusTrapHandles.delete(handle);

        document.removeEventListener('keydown', state.onKeyDown, true);

        // Restore focus.
        const target = state.previousActive;
        if (target && typeof target.focus === 'function') {
            target.focus({ preventScroll: true });
        }
    }
};

// ─── Popover: focus management (no trap, no scroll lock) ────────────────────

export const popover = {
    /**
     * Moves focus to the first focusable element inside the panel, or the panel
     * itself if none exist. Returns the element that was focused before the call
     * so the caller can restore it on close.
     */
    focusPanel(panel) {
        if (!panel) return null;
        const previous = document.activeElement;
        const first = focusableInside(panel)[0] || panel;
        if (first) first.focus({ preventScroll: true });
        return previous;
    },

    /**
     * Restores focus to the element returned by focusPanel (or any element).
     */
    restoreFocus(element) {
        if (element && typeof element.focus === 'function')
            element.focus({ preventScroll: true });
    }
};

// ─── Transition: CSS class-based enter/leave animations ─────────────────────

const transitionState = new WeakMap();

function nextFrame() {
    return new Promise(resolve => {
        requestAnimationFrame(() => requestAnimationFrame(resolve));
    });
}

function afterTransition(el) {
    return new Promise(resolve => {
        // Compute the longest transition/animation duration on the element.
        const styles = getComputedStyle(el);
        const durations = (styles.transitionDuration || '0s').split(',');
        const delays = (styles.transitionDelay || '0s').split(',');
        const animDurations = (styles.animationDuration || '0s').split(',');
        const animDelays = (styles.animationDelay || '0s').split(',');

        function parseMs(s) { return parseFloat(s) * (s.includes('ms') ? 1 : 1000); }

        let maxMs = 0;
        for (let i = 0; i < Math.max(durations.length, animDurations.length); i++) {
            const td = parseMs(durations[i % durations.length] || '0s');
            const tDelay = parseMs(delays[i % delays.length] || '0s');
            const ad = parseMs(animDurations[i % animDurations.length] || '0s');
            const aDelay = parseMs(animDelays[i % animDelays.length] || '0s');
            maxMs = Math.max(maxMs, td + tDelay, ad + aDelay);
        }

        if (maxMs <= 0) { resolve(); return; }

        // Use a timeout as a fallback in case transitionend doesn't fire.
        const timer = setTimeout(resolve, maxMs + 50);
        const handler = (e) => {
            if (e.target !== el) return;
            clearTimeout(timer);
            el.removeEventListener('transitionend', handler);
            el.removeEventListener('animationend', handler);
            resolve();
        };
        el.addEventListener('transitionend', handler);
        el.addEventListener('animationend', handler);
    });
}

function addClasses(el, str) {
    if (!str) return;
    for (const c of str.trim().split(/\s+/)) if (c) el.classList.add(c);
}

function removeClasses(el, str) {
    if (!str) return;
    for (const c of str.trim().split(/\s+/)) if (c) el.classList.remove(c);
}

export const transition = {
    /**
     * Runs the enter transition on the element.
     * classes: { enter, enterFrom, enterTo, entered }
     */
    async enter(el, classes) {
        if (!el) return;
        // Cancel any in-progress leave.
        this.cancel(el);

        const { enter, enterFrom, enterTo, entered } = classes || {};

        addClasses(el, enter);
        addClasses(el, enterFrom);

        await nextFrame();

        removeClasses(el, enterFrom);
        addClasses(el, enterTo);

        await afterTransition(el);

        removeClasses(el, enter);
        removeClasses(el, enterTo);
        addClasses(el, entered);

        transitionState.set(el, 'entered');
    },

    /**
     * Runs the leave transition on the element.
     * classes: { leave, leaveFrom, leaveTo, entered }
     * Returns after the transition completes (caller should then hide/unmount).
     */
    async leave(el, classes) {
        if (!el) return;
        this.cancel(el);

        const { leave, leaveFrom, leaveTo, entered } = classes || {};

        removeClasses(el, entered);
        addClasses(el, leave);
        addClasses(el, leaveFrom);

        await nextFrame();

        removeClasses(el, leaveFrom);
        addClasses(el, leaveTo);

        await afterTransition(el);

        removeClasses(el, leave);
        removeClasses(el, leaveTo);

        transitionState.set(el, 'left');
    },

    /**
     * Cancels any in-progress transition by removing all transition classes.
     */
    cancel(el) {
        if (!el) return;
        // We can't know which classes were applied, so we just clear the state.
        transitionState.delete(el);
    }
};
