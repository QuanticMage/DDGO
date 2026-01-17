window.dropzone = (function () {
    let bound = false;

    function arrayBufferToBase64(buffer) {
        const bytes = new Uint8Array(buffer);
        let binary = "";
        const chunkSize = 0x8000;

        for (let i = 0; i < bytes.length; i += chunkSize) {
            const chunk = bytes.subarray(i, i + chunkSize);
            binary += String.fromCharCode.apply(null, chunk);
        }

        return btoa(binary);
    }

    return {
        init: function (_elementIdIgnored, dotNetRef) {
            if (bound) return;
            bound = true;

            const stop = (e) => {
                e.preventDefault();
                e.stopPropagation();
            };

            // Firefox-proof: prevent default navigation for file drops anywhere
            // Capture phase means overlays/tooltips can't steal it.
            document.addEventListener("dragenter", (e) => {
                stop(e);
                dotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragover", (e) => {
                stop(e);
                if (e.dataTransfer) e.dataTransfer.dropEffect = "copy";
                dotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragleave", (e) => {
                // dragleave fires a lot; only clear when leaving the document
                // relatedTarget is null when leaving the window in many browsers
                if (!e.relatedTarget) {
                    stop(e);
                    dotNetRef.invokeMethodAsync("SetDragOver", false);
                }
            }, true);

            document.addEventListener("drop", async (e) => {
                stop(e);
                dotNetRef.invokeMethodAsync("SetDragOver", false);

                const files = e.dataTransfer && e.dataTransfer.files;
                if (!files || files.length === 0) return;

                const file = files[0];
                const buf = await file.arrayBuffer();
                const b64 = arrayBufferToBase64(buf);

                await dotNetRef.invokeMethodAsync("OnFileDropped", file.name, b64);
            }, true);

            console.log("Global drop enabled (anywhere on page).");
        }
    };
})();

function arrayBufferToBase64(buffer) {
    // Buffer -> Uint8Array -> binary string -> base64
    const bytes = new Uint8Array(buffer);
    let binary = "";
    const chunkSize = 0x8000; // avoids call stack issues

    for (let i = 0; i < bytes.length; i += chunkSize) {
        const chunk = bytes.subarray(i, i + chunkSize);
        binary += String.fromCharCode.apply(null, chunk);
    }

    return btoa(binary);
}

document.addEventListener("mousedown", e => {
    if (!e.target.classList.contains("col-resizer")) return;

    const th = e.target.parentElement;
    const startX = e.pageX;
    const startWidth = th.offsetWidth;

    const onMouseMove = e => {
        th.style.width = (startWidth + e.pageX - startX) + "px";
    };

    const onMouseUp = () => {
        document.removeEventListener("mousemove", onMouseMove);
        document.removeEventListener("mouseup", onMouseUp);
    };

    document.addEventListener("mousemove", onMouseMove);
    document.addEventListener("mouseup", onMouseUp);
});
