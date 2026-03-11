window.initSplitters = function () {
    document.querySelectorAll('.splitter-h').forEach(el => {
        el.removeEventListener('mousedown', _onSplitterHDown);
        el.addEventListener('mousedown', _onSplitterHDown);
    });
    document.querySelectorAll('.splitter-v').forEach(el => {
        el.removeEventListener('mousedown', _onSplitterVDown);
        el.addEventListener('mousedown', _onSplitterVDown);
    });
};

function _onSplitterHDown(e) {
    e.preventDefault();
    const before = e.currentTarget.previousElementSibling;
    const after  = e.currentTarget.nextElementSibling;
    if (!before || !after) return;

    const startY      = e.clientY;
    const startBefore = before.getBoundingClientRect().height;
    const startAfter  = after.getBoundingClientRect().height;
    const total       = startBefore + startAfter;

    function onMove(e) {
        const dy        = e.clientY - startY;
        const newBefore = Math.max(60, Math.min(total - 60, startBefore + dy));
        before.style.flex = '0 0 ' + newBefore + 'px';
        after.style.flex  = '0 0 ' + (total - newBefore) + 'px';
    }
    function onUp() {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
    }
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    document.body.style.cursor = 'row-resize';
    document.body.style.userSelect = 'none';
}

function _onSplitterVDown(e) {
    e.preventDefault();
    const before = e.currentTarget.previousElementSibling;
    const after  = e.currentTarget.nextElementSibling;
    if (!before || !after) return;

    const startX      = e.clientX;
    const startBefore = before.getBoundingClientRect().width;
    const startAfter  = after.getBoundingClientRect().width;
    const total       = startBefore + startAfter;

    function onMove(e) {
        const dx        = e.clientX - startX;
        const newBefore = Math.max(100, Math.min(total - 100, startBefore + dx));
        before.style.flex = '0 0 ' + newBefore + 'px';
        after.style.flex  = '0 0 ' + (total - newBefore) + 'px';
    }
    function onUp() {
        document.removeEventListener('mousemove', onMove);
        document.removeEventListener('mouseup', onUp);
        document.body.style.cursor = '';
        document.body.style.userSelect = '';
    }
    document.addEventListener('mousemove', onMove);
    document.addEventListener('mouseup', onUp);
    document.body.style.cursor = 'col-resize';
    document.body.style.userSelect = 'none';
}
