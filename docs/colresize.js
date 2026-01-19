window.enableColumnResizing = function (tableLike) {
    const table =
        (tableLike instanceof Element) ? tableLike :
            (tableLike && tableLike.id) ? document.getElementById(tableLike.id) :
                null;

    if (!table) return;

    const cols = table.querySelectorAll("colgroup col");
    const ths = table.querySelectorAll("thead th");

    ths.forEach((th, i) => {
        const resizer = th.querySelector(".col-resizer");
        if (!resizer) return;

        // Map header cell index -> col index.
        // This assumes no colspans on the resizable header row.
        const col = cols[i];
        if (!col) return;

        let startX, startWidth;

        resizer.addEventListener("mousedown", e => {
            startX = e.pageX;
            startWidth = th.getBoundingClientRect().width;

            document.body.style.userSelect = "none";
            document.body.style.cursor = "col-resize";

            document.addEventListener("mousemove", onMove);
            document.addEventListener("mouseup", onUp);

            e.preventDefault();
            e.stopPropagation();
        });

        function onMove(e) {
            const dx = e.pageX - startX;
            const w = Math.max(30, startWidth + dx);
            col.style.width = `${w}px`;
        }

        function onUp() {
            document.removeEventListener("mousemove", onMove);
            document.removeEventListener("mouseup", onUp);
            document.body.style.userSelect = "";
            document.body.style.cursor = "";
        }
    });
};
