window.atlasIcons = (() => {
    const FRAME = 64;
    let atlas = null;

    // key -> objectURL
    const cache = new Map();

    // One offscreen canvas reused for all renders
    const c = document.createElement("canvas");
    const ctx = c.getContext("2d", { alpha: true });
    ctx.imageSmoothingEnabled = false;

    function init(atlasUrl) {
        atlas = new Image();
        atlas.decoding = "async";
        atlas.src = atlasUrl;

        return new Promise((resolve, reject) => {
            atlas.onload = () => resolve(true);
            atlas.onerror = reject;
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
    const tmp = document.createElement("canvas");
    const tctx = tmp.getContext("2d", { alpha: true });
    tctx.imageSmoothingEnabled = false;

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
        const scale = px / size;
        ctx.setTransform(scale, 0, 0, scale, 0, 0); 

        c.width = px;
        c.height = px;

        // Draw in CSS pixels but scale output to DPR
        ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
        ctx.clearRect(0, 0, size, size);

        if (l1) drawBase(l1, size);
        if (l2) drawTintedOverlay(l2, size);
        if (l3) drawTintedOverlay(l3, size);

        const blob = await new Promise(res => c.toBlob(res, "image/png"));
        return URL.createObjectURL(blob);
    }

    async function renderAllCached() {
        if (!atlas) throw new Error("atlasIcons not initialized");

        const icons = document.querySelectorAll("img.atlas-icon");

        for (const imgEl of icons) {
            const size = int(get(imgEl, "data-size")) || imgEl.width || 20;

            const l1 = layer(imgEl, "l1");
            const l2 = layer(imgEl, "l2");
            const l3 = layer(imgEl, "l3");

            if (!l1 && !l2 && !l3) continue;

            const key = makeKey(size, l1, l2, l3);

            // If this element already has the right rendered result, skip.
            if (imgEl.dataset.iconKey === key && imgEl.src) continue;

            let url = cache.get(key);
            if (!url) {
                url = await renderKeyToObjectUrl(size, l1, l2, l3);
                cache.set(key, url);
            }

            imgEl.dataset.iconKey = key;
            imgEl.src = url;
        }
    }

    // Optional: if you ever need to clear cache to free memory
    function clearCache() {
        for (const url of cache.values()) URL.revokeObjectURL(url);
        cache.clear();
    }

    return { init, renderAllCached, clearCache };
})();
