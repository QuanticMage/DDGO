#!/usr/bin/env python3
"""
update_event_prices.py

Download the DDRnG event price spreadsheet and update the Price values in
DungeonDefendersGearOptimizer/EventInfo.cs to match the guide.

The prices live in three grid tabs of the spreadsheet:
    "Weapons Prices", "Accessories Prices", "Pets Prices"
Each grid is laid out in vertical section blocks (columns 2/6/10/14/18), where
every block is  [ (blank) | Name | Price | DemandCount ]  and section headers
(e.g. GUARDIANS, DPS, HIGH END MASKS) appear in the Name column with an empty
Price column. The section name -> EventInfo category (e.g. "Pets > DPS").

What it does:
  * updates the price of every EventInfo entry whose name is found in the sheet
    (case/punctuation-insensitive match), rounding half-up to an int;
  * reports ADDED events   (in the sheet, missing from EventInfo.cs);
  * reports REMOVED events (in EventInfo.cs, no longer in the sheet);
  * with --add-new, appends the added events in alphabetical order, with an
    empty hash phrase and the category derived from the sheet section.

Usage:
    python tools/update_event_prices.py                 # update prices, print report
    python tools/update_event_prices.py --dry-run       # report only, write nothing
    python tools/update_event_prices.py --add-new       # also append new events
    python tools/update_event_prices.py --no-download   # reuse cached _sheet.xlsx

Requires:  openpyxl   (pip install openpyxl)
"""
import argparse, io, math, os, re, sys, urllib.request

SHEET_ID = "1XzYuWB4I9RoNe8x-DD2M1koWs72tW8sTgyQ_n-d7l-E"
GRID_TABS = ["Weapons Prices", "Accessories Prices", "Pets Prices"]
BLOCK_COLS = (2, 6, 10, 14, 18)  # first column of each vertical section block

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_CS = os.path.normpath(os.path.join(HERE, "..", "DungeonDefendersGearOptimizer", "EventInfo.cs"))
CACHE_XLSX = os.path.join(HERE, "_sheet.xlsx")

ACRONYMS = {"DPS"}


def norm(s: str) -> str:
    """Loose key for matching names/sections: lowercase alphanumerics only."""
    return re.sub(r"[^a-z0-9]+", "", s.lower().replace("&", "and"))


def smart_title(section: str) -> str:
    return " ".join(w if w.upper() in ACRONYMS else w.capitalize() for w in section.split())


def download_sheet(path: str) -> None:
    url = f"https://docs.google.com/spreadsheets/d/{SHEET_ID}/export?format=xlsx"
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    data = urllib.request.urlopen(req, timeout=90).read()
    if not data.startswith(b"PK"):
        raise SystemExit("Download did not return an xlsx (is the sheet still public?).")
    with open(path, "wb") as f:
        f.write(data)


def is_header(name, price) -> bool:
    if price is not None:
        return False
    s = str(name).strip()
    if not s or not any(c.isalpha() for c in s):
        return False
    up = s.upper()
    if up != s or len(s) > 30:
        return False
    return not any(bad in up for bad in ("REF", "N/A", "HTTP", "ACCESS", "IMAGE"))


def parse_prices(xlsx_path):
    """Return {norm_name: (display_name, price_float, category)} from the grid tabs."""
    import openpyxl
    wb = openpyxl.load_workbook(xlsx_path, data_only=True)
    result, conflicts = {}, []
    for tab in GRID_TABS:
        if tab not in wb.sheetnames:
            print(f"  WARNING: tab {tab!r} not found", file=sys.stderr)
            continue
        kind = tab.split()[0]  # "Weapons" / "Accessories" / "Pets"
        ws = wb[tab]
        for col in BLOCK_COLS:
            if col + 1 > ws.max_column:
                break
            section = None
            for r in range(1, ws.max_row + 1):
                name = ws.cell(r, col).value
                price = ws.cell(r, col + 1).value
                if isinstance(price, (int, float)) and isinstance(name, str) and name.strip():
                    nm = name.strip()
                    if nm.startswith("#"):
                        continue
                    cat = f"{kind} > {smart_title(section)}" if section else f"{kind} > Unknown"
                    key = norm(nm)
                    if key in result and abs(result[key][1] - float(price)) > 1e-6:
                        conflicts.append((nm, result[key][1], float(price)))
                    result.setdefault(key, (nm, float(price), cat))
                elif is_header(name, price):
                    section = name.strip()
    return result, conflicts


ENTRY_RE = re.compile(
    r'(new\s*\(\s*"((?:[^"\\]|\\.)*)"\s*,\s*"(?:[^"\\]|\\.)*"\s*,\s*"(?:[^"\\]|\\.)*"\s*,\s*)(\d+)(\s*\))'
)


def canonical_category(kind_sub, existing_cats):
    """Map a derived 'Kind > Sub' to the exact casing already used in EventInfo.cs."""
    kind, _, sub = kind_sub.partition(" > ")
    want = (kind.lower(), norm(sub))
    for cat in existing_cats:
        k, _, s = cat.partition(" > ")
        if (k.lower(), norm(s)) == want:
            return cat
    return kind_sub


def main():
    ap = argparse.ArgumentParser(description="Update EventInfo.cs prices from the price sheet.")
    ap.add_argument("--file", default=DEFAULT_CS, help="path to EventInfo.cs")
    ap.add_argument("--dry-run", action="store_true", help="report only; do not write")
    ap.add_argument("--add-new", action="store_true", help="append events found only in the sheet")
    ap.add_argument("--no-download", action="store_true", help="reuse cached _sheet.xlsx")
    ap.add_argument("--keep-xlsx", action="store_true", help="do not delete the downloaded xlsx")
    args = ap.parse_args()

    if not args.no_download:
        print("Downloading price sheet ...")
        download_sheet(CACHE_XLSX)
    elif not os.path.exists(CACHE_XLSX):
        raise SystemExit("--no-download given but no cached _sheet.xlsx present.")

    sheet, conflicts = parse_prices(CACHE_XLSX)
    print(f"Parsed {len(sheet)} priced events from the sheet.")
    if conflicts:
        print(f"  NOTE: {len(conflicts)} name(s) appeared twice with different prices (kept first):")
        for nm, a, b in conflicts:
            print(f"    {nm}: {a} vs {b}")

    src = open(args.file, encoding="utf-8").read()
    existing_cats = set(re.findall(r'"((?:Pets|Weapons|Accessories) > [^"]+)"', src))

    cs_names = {}       # norm -> display name (first seen)
    changes = []
    unmatched_in_cs = set()

    def repl(m):
        prefix, name, oldp, suffix = m.group(1), m.group(2), int(m.group(3)), m.group(4)
        key = norm(name)
        cs_names.setdefault(key, name)
        if key not in sheet:
            unmatched_in_cs.add(name)
            return m.group(0)
        newp = int(math.floor(sheet[key][1] + 0.5))
        if newp != oldp:
            changes.append((name, oldp, newp))
        return f"{prefix}{newp}{suffix}"

    new_src, n = ENTRY_RE.subn(repl, src)
    print(f"Scanned {n} EventInfo entries; {len(changes)} price change(s).")

    added = [sheet[k] for k in sheet if k not in cs_names]           # in sheet, not in cs
    removed = sorted(unmatched_in_cs)                                 # in cs, not in sheet
    added.sort(key=lambda t: t[0].lower())

    # ---- optionally append the new events, alphabetically ----
    appended = []
    if args.add_new and added:
        lines = new_src.split("\n")
        # entry lines look like: <indent>new ("Name", ...),
        idx = [(i, m.group(2)) for i, l in enumerate(lines)
               for m in [ENTRY_RE.search(l)] if m]
        indent = re.match(r"\s*", lines[idx[0][0]]).group(0) if idx else "\t\t\t"
        for disp, price, cat in added:
            cat = canonical_category(cat, existing_cats)
            newp = int(math.floor(price + 0.5))
            entry = f'{indent}new ("{disp}", "{cat}", "", {newp}),'
            # find insertion point: before first existing entry whose name sorts after disp
            pos = None
            for i, nm in idx:
                if nm.lower() > disp.lower():
                    pos = i
                    break
            if pos is None:  # after last entry
                pos = idx[-1][0] + 1
            lines.insert(pos, entry)
            idx = [(i, m.group(2)) for i, l in enumerate(lines)
                   for m in [ENTRY_RE.search(l)] if m]  # rebuild after insert
            appended.append((disp, cat, newp))
        new_src = "\n".join(lines)

    # ---- report ----
    print("\n=== PRICE CHANGES ===")
    for nm, o, p in sorted(changes, key=lambda t: t[0].lower()):
        print(f"  {nm:42s} {o:>6} -> {p:<6}")
    print(f"  ({len(changes)} changed)")

    print("\n=== ADDED (in sheet, not in EventInfo.cs) ===")
    for disp, price, cat in added:
        tag = "  [appended]" if any(a[0] == disp for a in appended) else ""
        print(f"  {disp:42s} {int(math.floor(price + 0.5)):>6}   {cat}{tag}")
    print(f"  ({len(added)} added)")

    print("\n=== REMOVED (in EventInfo.cs, not in sheet) ===")
    for nm in removed:
        print(f"  {nm}")
    print(f"  ({len(removed)} removed - not deleted automatically)")

    if args.dry_run:
        print("\n--dry-run: no file written.")
    else:
        with open(args.file, "w", encoding="utf-8", newline="") as f:
            f.write(new_src)
        print(f"\nWrote {args.file}")

    if not args.keep_xlsx and not args.no_download and os.path.exists(CACHE_XLSX):
        os.remove(CACHE_XLSX)


if __name__ == "__main__":
    main()
