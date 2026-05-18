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
