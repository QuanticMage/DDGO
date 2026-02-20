# Dungeon Defenders Gear Optimizer (DDGO)

A browser-based tool for optimizing equipment in Dungeon Defenders. Runs entirely in your browser — no data is sent to any server.

## Getting Started

1. Open DDGO in your browser
2. In Dungeon Defenders, log into Ranked and click **Export to Open**
3. Find `DunDefHeroes.dun` in your game folder:
   - Steam Library → Dungeon Defenders → right-click → Manage → Browse Local Files → `Binaries\Win32` (or `Win64`)
4. Drag the `.dun` file into the drop zone, or click **Browse for file**

Chrome and Edge users can click **Reload** to re-read the same file from disk without dragging again.

## Hero Panels

After loading a file, your heroes appear in panels at the top. Each panel is a "paperdoll" showing equipped gear and stats.

- **Select a hero** from the dropdown to see their in-game equipment
- **Select a rating mode** to control how items are scored (Builder Stats, Hero Stats, DPS builds, etc.)
- **Equip from File** — wear whatever the hero has equipped in-game
- **Equip Best** — automatically find and equip the highest-rated armor set and accessories from the filtered item list
- **Clear** — remove all equipped items from the panel

### Multiple Heroes

Click **+ Add Hero** to add more panels side-by-side. Panels wrap to a new row if they exceed the window width.

- Click a panel to make it the **active** panel (highlighted border). Row clicks in the item grid equip to the active panel.
- **Lock** checkbox — prevents this panel's equipped items from being used by Equip Best on other panels. Useful for locking in your builder's gear before optimizing a DPS hero.
- Click **x** to remove a panel.

## Item Grid

The main grid shows all items from your save file. Click a row to equip/unequip it on the active hero panel.

- **Rating** — sum of important stats based on the current rating mode, accounting for upgrades and set bonuses. The small number is "sides" (secondary stats).
- **Stars** — how good the item is at what it does best, compared to the rest of your gear.
- **CV** — estimated trade value.
- Click column headers to sort.

## Filters

Use the filter chips above the grid to narrow down what's shown:

| Filter | What it does |
|--------|-------------|
| **Type** | Equipment slot — Helmet, Torso, Gauntlet, Boots, Weapons, Pets, Accessories (Brooch, Mask, Bracers, Shield). Presets for "Armor" and "Accessories". |
| **Set** | Armor set (Pristine, Plate, Chain, Mail, Leather, Zamira) or weapon class (Squire, Apprentice, Monk, Huntress). |
| **Other** | Special filters — Only Events, Exclude Events, Exclude Other Characters Equipment, Exclude Missing Resists. |

### Custom Search

The search bar supports an expression language for advanced filtering:

**Tags you can use:**
`name`, `location`, `quality`, `rating`, `lvl`, `maxlvl`, `set`, `type`, `best`, `sides`, `value`,
`thp`, `tdmg`, `trate`, `trange` (tower stats),
`hhp`, `hdmg`, `hrate`, `hspd`, `ab1`, `ab2` (hero stats),
`ressum`, `resavg`, `resg`, `resp`, `resf`, `resl` (resistances)

**Operators:** `>`, `<`, `>=`, `<=`, `=`, `!=`, `:` (contains), `!:` (doesn't contain), `and`, `or`, `not`

**Examples:**
- `rating > 500` — items rated above 500
- `quality : ult` — items with "ult" in the quality name
- `tdmg > 200 and trate > 100` — tower damage and rate both high
- `name : coal` — find items with "coal" in the name

## Settings

The toolbar above the filters has toggle options:

- **Censor Ult++** — hides Ult++ and special items for sharing screenshots
- **Show as upgraded** — displays inventory items as if fully upgraded according to current priority
- **Show with set bonus** — displays inventory items as if they have a set bonus
- **Compact Display** — shrinks the UI for smaller windows
- **Best includes sides** — makes Equip Best factor in sides stats at half value

## Dark Mode

Click the crescent moon icon in the top-right corner to toggle between light and dark themes. Your preference is saved and persists across sessions.

## Exporting

Click **Export to .csv** to download the current item list as a CSV file for use in spreadsheets.
