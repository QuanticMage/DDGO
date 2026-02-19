window.atlasIcons = (() => {
    const FRAME = 64;
    let atlas = null;
    const cache = new Map();

    // Use OffscreenCanvas if available for better performance in Chrome
    // willReadFrequently: true is the "secret sauce" for Chrome tiny-canvas performance
    const c = window.OffscreenCanvas
        ? new OffscreenCanvas(FRAME, FRAME)
        : document.createElement("canvas");
    const ctx = c.getContext("2d", { alpha: true, willReadFrequently: true });
    ctx.imageSmoothingEnabled = false;

    // Reusable temp canvas for tinting
    const tmp = window.OffscreenCanvas
        ? new OffscreenCanvas(FRAME, FRAME)
        : document.createElement("canvas");
    const tctx = tmp.getContext("2d", { alpha: true, willReadFrequently: true });
    tctx.imageSmoothingEnabled = false;

    async function init(atlasUrl) {        
        atlas = new Image();
        atlas.src = atlasUrl;
        if (!atlas.complete) {
            await new Promise((res, rej) => {
                atlas.onload = res;
                atlas.onerror = rej;
            });
        }

        // decode AFTER load for best reliability
        if (atlas.decode) {
            try { await atlas.decode(); } catch { }
        }
        return true;
    }

    function setImageSrc(img, url) {
        return new Promise(res => {
            img.onload = () => res();
            img.onerror = () => res();
            img.src = url;
        });
    }

    function get(el, name) {
        const v = el.getAttribute(name);
        return (v === null || v === "") ? null : v;
    }

    function int(v) {
        if (v === null) return null;
        const n = Number.parseInt(v, 10);
        return Number.isFinite(n) ? n : null;
    }

    function layer(el, p) {
        const x = int(get(el, `data-${p}x`));
        const y = int(get(el, `data-${p}y`));
        if (x === null || y === null) return null;
        return { x, y, tint: get(el, `data-${p}t`) || "" };
    }
    function isNoTint(t) {
        if (!t) return true;
        const s = String(t).trim().toLowerCase();
        return s === "#000" || s === "#000000" || s === "rgb(0, 0, 0)" || s === "rgba(0, 0, 0, 1)";
    }

    function makeKey(size, l1, l2, l3) {
        return [
            size,
            l1 ? `${l1.x},${l1.y}` : "-",
            l2 ? `${l2.x},${l2.y},${l2.tint}` : "-",
            l3 ? `${l3.x},${l3.y},${l3.tint}` : "-"
        ].join("|");
    }
    
       
    function drawBase(L, size) {
        ctx.globalCompositeOperation = "source-over";
        ctx.drawImage(atlas, L.x, L.y, FRAME, FRAME, 0, 0, size, size);
    }

    function drawTintedOverlay(L, size) {
        if (!L || isNoTint(L.tint)) return;

        // Ensure temp canvas matches output size
        if (tmp.width !== c.width || tmp.height !== c.height) {
            tmp.width = c.width;
            tmp.height = c.height;
        }

        // IMPORTANT: match transforms (DPR scaling) so coordinates align
        tctx.setTransform(ctx.getTransform());

        // 1) Clear temp
        tctx.setTransform(1, 0, 0, 1, 0, 0);
        tctx.clearRect(0, 0, tmp.width, tmp.height);
        tctx.setTransform(ctx.getTransform());

        // 2) Draw mask into temp (only its alpha matters)
        tctx.globalCompositeOperation = "source-over";
        tctx.drawImage(atlas, L.x, L.y, FRAME, FRAME, 0, 0, size, size);

        // 3) Replace RGB with tint, keeping mask alpha
        tctx.globalCompositeOperation = "source-in";
        tctx.fillStyle = L.tint;
        tctx.fillRect(0, 0, size, size);

        // 4) Composite the overlay on top of the main canvas
        ctx.globalCompositeOperation = "source-over";
        ctx.drawImage(tmp, 0, 0, size, size);
    }
    async function renderKeyToObjectUrl(size, l1, l2, l3) {
        const dpr = window.devicePixelRatio || 1;
        const px = Math.max(1, Math.round(size * dpr));

        if (c.width !== px) {
            c.width = px;
            c.height = px;
            tmp.width = px;
            tmp.height = px;
        }

        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctx.clearRect(0, 0, size, size);

        // Draw layers
        if (l1) {
            ctx.globalCompositeOperation = "source-over";
            ctx.drawImage(atlas, l1.x, l1.y, FRAME, FRAME, 0, 0, size, size);
        }

        [l2, l3].forEach(L => {
            if (!L || isNoTint(L.tint)) return;
            tctx.setTransform(dpr, 0, 0, dpr, 0, 0);
            tctx.clearRect(0, 0, size, size);
            tctx.globalCompositeOperation = "source-over";
            tctx.drawImage(atlas, L.x, L.y, FRAME, FRAME, 0, 0, size, size);
            tctx.globalCompositeOperation = "source-in";
            tctx.fillStyle = L.tint;
            tctx.fillRect(0, 0, size, size);
            ctx.globalCompositeOperation = "source-over";
            ctx.drawImage(tmp, 0, 0, size, size);
        });

        // Convert to Blob and then ObjectURL
        // This is faster than DataURL for many small icons
        const blob = await (c.convertToBlob ? c.convertToBlob() : new Promise(r => c.toBlob(r)));
        return URL.createObjectURL(blob);
    }

    async function renderAllCached() {
        if (!atlas) throw new Error("atlasIcons not initialized");

        const icons = document.querySelectorAll("img.atlas-icon");

        ignoreMutations = true;
        try {
            for (const imgEl of icons) {
                await new Promise(r => setTimeout(r, 0));

                const size = int(get(imgEl, "data-size")) || imgEl.width || 20;
                const l1 = layer(imgEl, "l1");
                const l2 = layer(imgEl, "l2");
                const l3 = layer(imgEl, "l3");
                if (!l1 && !l2 && !l3) continue;

                const key = makeKey(size, l1, l2, l3);
                if (imgEl.dataset.iconKey === key && imgEl.src) continue;

                let url = cache.get(key);
                if (!url) {
                    url = await renderKeyToObjectUrl(size, l1, l2, l3);
                    cache.set(key, url);
                }

                imgEl.dataset.iconKey = key;
                await setImageSrc(imgEl, url);
            }
        } finally {
            ignoreMutations = false;
        }
    }

    // Optional: if you ever need to clear cache to free memory
    function clearCache() {
        for (const url of cache.values()) URL.revokeObjectURL(url);
        cache.clear();
    }
    let pending = false;
    let rendering = false;
    let rerun = false;
    let mo = null;

    // Simple Firefox detection (fine for this use-case)
    const IS_FIREFOX = typeof InstallTrigger !== "undefined";

    function scheduleRender() {
        if (rendering) { rerun = true; return; }
        if (pending) return;
        pending = true;

        const run = async () => {
            pending = false;
            rendering = true;
            try {
                await renderAllCached();
            } catch (e) {
                console.error(e);
            } finally {
                rendering = false;
                if (rerun) {
                    rerun = false;
                    scheduleRender();
                }
            }
        };

        if (IS_FIREFOX) {
            // Firefox: wait an extra frame for Blazor/DOM to settle
            requestAnimationFrame(() => requestAnimationFrame(run));
        } else {
            // Chrome: one frame is faster + less “mysterious delay”
            requestAnimationFrame(run);
        }
    }
    let ignoreMutations = false;

    function startAutoRender(root = document.body) {
        stopAutoRender();
        scheduleRender();

        mo = new MutationObserver((mutations) => {
            if (ignoreMutations) return;

            // Optional: only react if something relevant changed
            // (e.g. nodes added/removed or your layer attrs changed)
            scheduleRender();
        });

        mo.observe(root, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: [
                "class",
                "data-size",
                "data-l1x", "data-l1y",
                "data-l2x", "data-l2y", "data-l2t",
                "data-l3x", "data-l3y", "data-l3t"
                // IMPORTANT: do NOT include "src" or "data-icon-key"
            ]
        });
    }


    function stopAutoRender() {
        if (mo) { mo.disconnect(); mo = null; }
    }


    return { init, renderAllCached, clearCache, startAutoRender, stopAutoRender  };
})();
