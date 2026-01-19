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
        
        await dotNetRef.invokeMethodAsync("OnFileDropped", file.name, bytes);
    }

    
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
              //  currentDotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragover", (e) => {
                stop(e);
                if (e.dataTransfer) e.dataTransfer.dropEffect = "copy";
                currentDotNetRef.invokeMethodAsync("SetDragOver", true);
            }, true);

            document.addEventListener("dragleave", (e) => {
                if (!e.relatedTarget) {
                    stop(e);
            //       currentDotNetRef.invokeMethodAsync("SetDragOver", false);
                }
            }, true);

            document.addEventListener("drop", async (e) => {
                //console.log(`Dropped handler called: [${new Date().toISOString()}]`);

                stop(e);
                //console.log(`Finished stop: [${new Date().toISOString()}]`);
                currentDotNetRef.invokeMethodAsync("SetDragOver", false);

                // Add this: Clear the effect to tell the browser we are done with this specific operation
                if (e.dataTransfer) {
                    e.dataTransfer.dropEffect = "none";
                }

                //console.log(`Trying to get dropped handle: [${new Date().toISOString()}]`);
                // 1) Try to capture a persistent handle (Chrome/Edge)
                const handle = await tryGetDroppedFileHandle(e);
                if (handle) {
                    try {
                        //console.log(`Saving Handle: [${new Date().toISOString()}]`);
                        // Save handle for "Load last file"
                        await idbSet(LAST_HANDLE_KEY, handle);
                        //console.log(`Calling ensure: [${new Date().toISOString()}]`);
                        // Read now (requires permission; drop usually implies user intent)
                        const ok = await ensureReadPermission(handle);
                        if (ok) {
                            const file = await handle.getFile();
                            //console.log(`ReadAndSend: [${new Date().toISOString()}]`);

                            await readFileAndSend(dotNetRef, file);
                            //console.log(`Done: [${new Date().toISOString()}]`);

                            return;
                        }
                        // If permission denied, fall through to normal file
                    } catch {
                        // fall through
                    }
                }

                // 2) Fallback: standard dropped File (no path, no persistence)
                const files = e.dataTransfer && e.dataTransfer.files;
                if (!files || files.length === 0) return;
                //console.log(`ReadAndSend: [${new Date().toISOString()}]`);

                await readFileAndSend(dotNetRef, files[0]);
                //console.log(`Done: [${new Date().toISOString()}]`);

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


        supportsHandles: function () {
            return typeof window.DataTransferItem !== "undefined"
                && typeof DataTransferItem.prototype.getAsFileSystemHandle === "function";
        },

        hasLast: async function () {
            try {
                const handle = await idbGet(LAST_HANDLE_KEY);
                return !!handle;
            } catch {
                return false;
            }
        }

    };
})();
