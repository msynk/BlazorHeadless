// HeadlessUI.Blazor — JS interop module
//
// Each exported namespace mirrors a component family. The C# side imports this
// module on first use (via IJSRuntime.InvokeAsync<IJSObjectReference>("import"))
// and keeps a single reference for the lifetime of the application.

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
