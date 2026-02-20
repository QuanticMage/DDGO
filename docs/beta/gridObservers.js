(function () {
    const state = new WeakMap();

    const isElement = (x) => x && typeof x === "object" && x.nodeType === 1;

    function debounce(fn, ms) {
        let t;
        return () => {
            clearTimeout(t);
            t = setTimeout(fn, ms);
        };
    }

    function measure(table) {
        const tbody = table?.tBodies?.[0];
        const rows = tbody?.rows ?? [];
        const data = [];

        for (const tr of rows) {
            data.push({
                key: tr.dataset.key,
                y: tr.offsetTop,
                h: tr.offsetHeight
            });
        }

        return { overlayHeight: table?.scrollHeight ?? 0, rows: data };
    }

    window.gridOverlayObservers = {
        start: (scrollEl, tableEl, dotnetRef) => {
            if (!isElement(scrollEl) || !isElement(tableEl) || !dotnetRef) return;

            window.gridOverlayObservers.stop(scrollEl);

            const run = debounce(() => {
                const payload = measure(tableEl);
                dotnetRef.invokeMethodAsync("OnGridLayoutChanged", payload);
            }, 50);

            // Resizes (includes window resizes affecting layout)
            const ro = new ResizeObserver(run);
            ro.observe(scrollEl);
            ro.observe(tableEl);

            // Reorder + hidden-row toggles
            const tbody = tableEl.tBodies[0];
            const mo = new MutationObserver((mutations) => {
                for (const m of mutations) {
                    if (m.type === "childList") {
                        run();
                        return;
                    }
                    if (m.type === "attributes" && m.attributeName === "class") {
                        // only rerun if this was a row (tr) AND hidden-row could have changed
                        const el = m.target;
                        if (el && el.nodeName === "TR") {
                            // Only care about hidden-row (prevents hover class spam)
                            const nowHidden = el.classList.contains("hidden-row");
                            const wasHidden = (m.oldValue || "").split(/\s+/).includes("hidden-row");
                            if (nowHidden !== wasHidden) {
                                run();
                                return;
                            }
                        }
                    }
                }
            });

            if (tbody) {
                mo.observe(tbody, {
                    subtree: true,
                    childList: true,
                    attributes: true,
                    attributeFilter: ["class"],
                    attributeOldValue: true
                });
            }

            run();
            state.set(scrollEl, { ro, mo, dotnetRef });
        },


        stop: (scrollEl) => {
            if (!isElement(scrollEl)) return;
            const s = state.get(scrollEl);
            if (!s) return;
            s.ro.disconnect();
            s.mo.disconnect();
            state.delete(scrollEl);
        }
    };
})();
