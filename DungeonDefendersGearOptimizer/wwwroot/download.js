// wwwroot/download.js
window.download = window.download || {};

// Best for CSV/text (avoids base64 + handles large-ish text better)
window.download.fromText = function (fileName, contentType, text) {
    // Optional: UTF-8 BOM helps Excel open UTF-8 CSV correctly
    const BOM = "\uFEFF";
    const blob = new Blob([BOM, text], { type: contentType });

    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = fileName;
    a.style.display = "none";
    document.body.appendChild(a);
    a.click();
    a.remove();
    URL.revokeObjectURL(url);
};