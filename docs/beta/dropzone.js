window.dropzone = (function () {
    let bound = false;
    let currentDotNetRef = null;

    // ===== base64 helper =====
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

    // ===== IndexedDB for persisting file handles (Chrome/Edge) =====
    const DB_NAME = "dropzone_db";
    const DB_VER = 1;
    const STORE = "kv";
    const LAST_HANDLE_KEY = "lastFileHandle";
    const LAST_DUN_BYTES_KEY = "lastDunFileBytes";
    const LAST_FILENAME_KEY = "lastFilename"

    function openDb() {
        return new Promise((resolve, reject) => {
            const req = indexedDB.open(DB_NAME, DB_VER);
            req.onupgradeneeded = () => {
                const db = req.result;
                if (!db.objectStoreNames.contains(STORE)) {
                    db.createObjectStore(STORE);
                }
            };
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    async function idbSet(key, value) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE, "readwrite");
            tx.objectStore(STORE).put(value, key);
            tx.oncomplete = () => resolve(true);
            tx.onerror = () => reject(tx.error);
        });
    }

    async function idbGet(key) {
        const db = await openDb();
        return new Promise((resolve, reject) => {
            const tx = db.transaction(STORE, "readonly");
            const req = tx.objectStore(STORE).get(key);
            req.onsuccess = () => resolve(req.result);
            req.onerror = () => reject(req.error);
        });
    }

    // ===== File System Access helpers =====
    function supportsFileSystemAccessFromDrop(e) {
        const items = e.dataTransfer && e.dataTransfer.items;
        return !!(items && items.length && items[0] && typeof items[0].getAsFileSystemHandle === "function");
    }

    async function tryGetDroppedFileHandle(e) {
        if (!supportsFileSystemAccessFromDrop(e)) return null;

        // Prefer the first item. (You can expand to support multiple later.)
        const item = e.dataTransfer.items[0];
        try {
            const handle = await item.getAsFileSystemHandle();
            if (handle && handle.kind === "file") return handle;
            return null;
        } catch {
            return null;
        }
    }

    async function ensureReadPermission(handle) {
        // Newer Chrome/Edge have queryPermission/requestPermission
        //console.log(`Query: [${new Date().toISOString()}]`)
        if (typeof handle.queryPermission === "function") {
            const q = await handle.queryPermission({ mode: "read" });
            if (q === "granted") return true;
        }
        //console.log(`Request [${new Date().toISOString()}]`)
        if (typeof handle.requestPermission === "function") {
            const r = await handle.requestPermission({ mode: "read" });
            return r === "granted";
        }
        //console.log(`Ensured Read Permission: [${new Date().toISOString()}]`);
        // If APIs not present, best effort
        return true;
    }

    async function readFileAndSend(dotNetRef, file) {
        const buf = await file.arrayBuffer();
        const bytes = new Uint8Array(buf);
        await idbSet(LAST_DUN_BYTES_KEY, buf);
        await idbSet(LAST_FILENAME_KEY, file.name);

        await dotNetRef.invokeMethodAsync("OnFileDropped", file.name, bytes);
    }

    async function tryLoadCachedDunBytes() {
        const buf = await idbGet(LAST_DUN_BYTES_KEY);
        if (!buf) return null;
        return Array.from(new Uint8Array(buf));
    }
   

    async function pickFile(dotNetRef) {
        currentDotNetRef = dotNetRef;
        try {
            // Prefer File System Access API when available (Chrome/Edge)
            if (supportsHandles() && window.showOpenFilePicker) {
                const [handle] = await window.showOpenFilePicker({
                    multiple: false,
                    excludeAcceptAllOption: false,
                    types: [
                        {
                            description: "Dungeon Defenders save file",
                            accept: { "application/octet-stream": [".dun"] }
                        }
                    ]
                });

                if (!handle) return false;

                await idbSet(LAST_HANDLE_KEY, handle);

                const ok = await ensureReadPermission(handle);
                if (!ok) return false;

                const file = await handle.getFile();
                await readFileAndSend(dotNetRef, file);
                return true;
            }

            // Fallback: classic file input (works everywhere)
            const file = await pickViaInput(".dun");
            if (!file) return false;

            await readFileAndSend(dotNetRef, file);
            return true;
        }
        catch (e) {
            // User cancel is not an error; return false
            console.warn("pickFile canceled/failed:", e);
            return false;
        }
    }

    // Creates an ephemeral <input type=file> and resolves with the chosen File
    function pickViaInput(accept) {
        return new Promise((resolve) => {
            const input = document.createElement("input");
            input.type = "file";
            input.accept = accept || "";
            input.style.position = "fixed";
            input.style.left = "-9999px";
            input.style.top = "-9999px";

            input.addEventListener("change", () => {
                const file = input.files && input.files.length ? input.files[0] : null;
                input.remove();
                resolve(file);
            }, { once: true });

            document.body.appendChild(input);
            input.click();
        });
    }
    function supportsHandles() {
        return typeof window.DataTransferItem !== "undefined"
            && typeof DataTransferItem.prototype.getAsFileSystemHandle === "function";
    }
    async function tryLoadLastHandleAndRead(dotNetRef) {
        try {
            const handle = await idbGet(LAST_HANDLE_KEY);
            if (!handle) return false;

            const ok = await ensureReadPermission(handle);
            if (!ok) return false;

            const file = await handle.getFile();
            await readFileAndSend(dotNetRef, file);
            return true;
        } catch {
            return false;
        }
    }

    async function tryLoadCachedFilename() {
        const fn = await idbGet(LAST_FILENAME_KEY);
        return (typeof fn === "string") ? fn : null;
    }
/*
    async function tryLoadCachedFilename(dotNetRef) {
        try {
            const handle = await idbGet(LAST_HANDLE_KEY);
            if (!handle) return false;

            const ok = await ensureReadPermission(handle);
            if (!ok) return false;

            const file = await handle.getFile();
            await readFileAndSend(dotNetRef, file);
            return true;
        } catch {
            return false;
        }
    }
    */
  
    return {
        init: function (_elementIdIgnored, dotNetRef) {
            currentDotNetRef = dotNetRef;
            if (bound) return;
            bound = true;

            const stop = (e) => {
                e.preventDefault();
                e.stopPropagation();
            };

            // Prevent default navigation for drops anywhere (capture phase)
            document.addEventListener("dragenter", (e) => {
                stop(e);
                currentDotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragover", (e) => {
                stop(e);
                if (e.dataTransfer) e.dataTransfer.dropEffect = "copy";
                currentDotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragleave", (e) => {
                if (!e.relatedTarget) {
                    stop(e);
                    currentDotNetRef.invokeMethodAsync("SetDragOver", false);
                }
            }, true);

            document.addEventListener("drop", async (e) => {
                e.preventDefault();

                const dt = e.dataTransfer;
                if (!dt) return;

                // 1. CAPTURE DATA IMMEDIATELY (Synchronously)
                // Get the file handle AND the fallback file right now
                const items = Array.from(dt.items || []);
                const fallbackFile = dt.files?.[0];

                currentDotNetRef.invokeMethodAsync("SetDragOver", false);

                // 2. Try File System Access handle
                const handle = await tryGetDroppedFileHandle(e);
                if (handle) {
                    try {
                        await idbSet(LAST_HANDLE_KEY, handle);
                        if (await ensureReadPermission(handle)) {
                            const file = await handle.getFile();
                            await readFileAndSend(dotNetRef, file);
                            return;
                        }
                    } catch (err) {
                        // fall through
                    }
                }

                // 3. Use the captured fallback
                // We use 'fallbackFile' which we saved before the first 'await'
                if (fallbackFile) {
                    await readFileAndSend(dotNetRef, fallbackFile);
                }
            }, true);

           // console.log("Global drop enabled (anywhere on page).");
        },

        // Call this from a "Load last file" button in your UI (Chrome/Edge only)
        loadLast: async function (dotNetRef) {
            currentDotNetRef = dotNetRef;

            try {
                const handle = await idbGet(LAST_HANDLE_KEY);
                if (!handle) return false;

                const ok = await ensureReadPermission(handle);
                if (!ok) return false;

                const file = await handle.getFile();
                await readFileAndSend(dotNetRef, file);
                return true;
            } catch {
                return false;
            }
        },


        supportsHandles,

        hasLast: async function () {
            try {
                const handle = await idbGet(LAST_HANDLE_KEY);
                return !!handle;
            } catch {
                return false;
            }
        },
        pickFile,
        tryLoadCachedDunBytes,
        tryLoadLastHandleAndRead,
        tryLoadCachedFilename,
    };
})();
