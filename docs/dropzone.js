window.dropzone = {
    init: function (elementId, dotNetRef) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.error("dropzone.init: element not found:", elementId);
            return;
        }

        // Important: prevent browser navigating to the file on drop
        const prevent = (e) => {
            e.preventDefault();
            e.stopPropagation();
        };

        ["dragenter", "dragover", "dragleave", "drop"].forEach(evt =>
            el.addEventListener(evt, prevent)
        );

        el.addEventListener("dragenter", () => dotNetRef.invokeMethodAsync("SetDragOver", true));
        el.addEventListener("dragover", () => dotNetRef.invokeMethodAsync("SetDragOver", true));
        el.addEventListener("dragleave", () => dotNetRef.invokeMethodAsync("SetDragOver", false));

        el.addEventListener("drop", async (e) => {
            await dotNetRef.invokeMethodAsync("SetDragOver", false);

            const files = e.dataTransfer && e.dataTransfer.files;
            if (!files || files.length === 0) return;

            const file = files[0];

            const buf = await file.arrayBuffer();
            const b64 = arrayBufferToBase64(buf);

            await dotNetRef.invokeMethodAsync("OnFileDropped", file.name, b64);
        });
    }
};

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