// ------------------------------------------------------------------
// BlazorHeadless documentation site — small client script
// Handles: code-sample tabs, copy buttons, mobile sidebar.
// ------------------------------------------------------------------

(function () {
    'use strict';

    // ---------- Tabs (Razor / CSS) for each example block ----------
    document.querySelectorAll('.example').forEach(function (example) {
        const tabs = example.querySelectorAll('.example-tab');
        const panels = example.querySelectorAll('.example-panel');

        tabs.forEach(function (tab) {
            tab.addEventListener('click', function () {
                const target = tab.getAttribute('data-tab');
                tabs.forEach(t => t.classList.toggle('active', t === tab));
                panels.forEach(p =>
                    p.classList.toggle('active', p.getAttribute('data-tab') === target)
                );
            });
        });
    });

    // ---------- Copy buttons ----------
    document.querySelectorAll('.copy-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const wrap = btn.closest('.code-wrap');
            if (!wrap) return;
            const code = wrap.querySelector('pre code');
            if (!code) return;

            const text = code.textContent || '';
            const done = function () {
                const original = btn.dataset.label || btn.textContent;
                btn.dataset.label = original;
                btn.textContent = 'Copied';
                btn.classList.add('copied');
                setTimeout(function () {
                    btn.textContent = original;
                    btn.classList.remove('copied');
                }, 1400);
            };

            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(done).catch(fallback);
            } else {
                fallback();
            }

            function fallback() {
                const ta = document.createElement('textarea');
                ta.value = text;
                ta.setAttribute('readonly', '');
                ta.style.position = 'absolute';
                ta.style.left = '-9999px';
                document.body.appendChild(ta);
                ta.select();
                try { document.execCommand('copy'); } catch (e) { /* ignore */ }
                document.body.removeChild(ta);
                done();
            }
        });
    });

    // ---------- Mobile sidebar ----------
    const toggle = document.getElementById('menu-toggle');
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.querySelector('.sidebar-overlay');

    function setOpen(open) {
        if (sidebar) sidebar.classList.toggle('open', open);
        if (overlay) overlay.classList.toggle('open', open);
    }

    if (toggle) {
        toggle.addEventListener('click', function () {
            setOpen(!sidebar.classList.contains('open'));
        });
    }
    if (overlay) overlay.addEventListener('click', () => setOpen(false));

    // Close sidebar after clicking a link on mobile
    if (sidebar) {
        sidebar.querySelectorAll('a').forEach(function (a) {
            a.addEventListener('click', function () { setOpen(false); });
        });
    }

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') setOpen(false);
    });
})();
