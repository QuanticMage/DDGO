# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Dungeon Defenders Gear Optimizer (DDGO) - a Blazor WebAssembly tool for optimizing in-game equipment for the tower defense game "Dungeon Defenders." Runs entirely in the browser with no backend.

## Build & Deploy Commands

```bash
# Build the main web application
dotnet publish DungeonDefendersGearOptimizer\DDUP.csproj -c Release

# Deploy to GitHub Pages (PowerShell)
.\publish.ps1       # Production deployment
.\publishbeta.ps1   # Beta deployment
```

The publish script builds, copies output to `docs/`, and ensures `.nojekyll` exists for GitHub Pages.

## Solution Structure

- **DungeonDefendersGearOptimizer/** - Main Blazor WebAssembly app (.NET 8.0)
- **DungeonDefendersOfflinePreprocessor/** - WPF desktop tool for parsing Unreal Engine packages and game save files (.NET 9.0)
- **ItemAttributeMiner/** - Console tool for extracting item attributes from game files (.NET 9.0)
- **docs/** - GitHub Pages deployment folder (generated, do not edit directly)

## Architecture

### Main Application (DDUP)

**Core Data / Logic:**
- `Pages/Index.razor` - The entire app lives here: file upload, `UiState` management, equipment grid, multi-panel coordination, filtering, search, and item selection logic
- `DDDatabase.cs` - `DDStat` and `DDRes` enums (tower/hero stats, resistances), `DDLinearColor` struct for item color data
- `Ratings.cs` - `RatingModeInfo` records and the full `RatingModes` list; each mode defines stat priority weights for scoring items (Builder Stats, Hero Stats, etc.)
- `ExpressionParser.cs` - Full expression language for the custom filter/search field (arithmetic, boolean, comparisons, `:`/`!:` string matching)
- `View.cs` - `ItemViewRow` **class** (not struct) holding all display state for a grid row; `Filters` flags enum for type/slot filtering
- `ItemDatabase.cs` - Equipment type names, weapon types, equipment sets
- `EventInfo.cs` - Event-specific items and pricing data (900+ entries)
- `GeneratedItemTable.cs` - Pre-generated item data (~1.8 MB generated file, do not hand-edit)
- `ItemHash.cs` - 30-bit FNV-1a hash → 6-word mnemonic phrase (Adj Adj GoodNoun Verb Adj BadNoun) for human-readable item IDs
- `DamageCalculator.cs` - WIP/incomplete DPS calculator; not yet used in the UI

**UI Components:**
- `CharacterPanel.razor` - Absolute-positioned overlay on game-art background images; displays hero stats, equipment slots, and hero/rating-mode selectors
- `MultiHeroOptimizer.razor` - Collapsible panel for distributing gear across up to 9 heroes by priority
- `FilterChips.razor` - Multi-select chip filter with presets and a flyout menu
- `SearchBar.razor` - Text input wired to `ExpressionParser` for live filtering
- `RadioSelect.razor` - Custom dropdown with icon support, used for hero and rating-mode selection in `CharacterPanel`
- `ColorSwatches.razor` - Renders the colored stat-shard squares in name cells
- `HeroResultSheet.razor` - Summary stat sheet for equipped items on a hero

**State Management (`UiState` pattern):**

All persistent UI settings live in `UiState`, a `sealed record` with `init` properties inside `Index.razor`. Mutations always use:
```csharp
SetUi(u => u with { SomeProperty = newValue });
```
`SetUi` computes a diff between old and new state, triggers only the necessary recalculations (rating, filters, DPS, cache), calls `QueueSaveUiState()`, and calls `StateHasChanged()`. Never mutate `_ui` fields directly.

**Multi-panel architecture:**

Multiple `CharacterPanel` instances are tracked via `List<CharacterPanelSlot> _panelSlots`. Each slot holds a `@ref` to its panel, an `Id`, and an `IsLocked` flag. `ActivePanel` is the currently selected panel. When equipping an item that is already on another panel: if that panel's slot is locked, the equip is rejected; otherwise the item is moved.

**JavaScript Interop (`wwwroot/`):**
- `dropzone.js` - Drag & drop and file-picker interop
- `gridObservers.js` - IntersectionObserver-based virtualization for large item grids
- `atlasIconsCached.js` - Sprite atlas icon lookup cache
- `download.js` - File download trigger
- `wwwroot/index.html` (inline `<script>`) - `setupGlobalTooltips`, `setTheme`, `appState` (localStorage wrapper), `copyToClipboard`, `scrollRowIntoView`

**Tooltip system:**

`TooltipService` (registered as scoped DI) holds a `DotNetObjectReference` and calls `setupGlobalTooltips` in `index.html` via JSInterop. JS fires `ShowGlobalTooltip` / `HideGlobalTooltip` .NET methods on mouse events. Data attributes: `data-tooltip` for normal tooltips, `h-tooltip` for help-mode tooltips shown while holding `H`.

**Dark mode CSS architecture:**

Blazor scoped CSS appends a `[b-xxxxxxxx]` attribute to all rules in `.razor.css` files, giving them specificity that global `app.css` cannot override by class name alone. The pattern used throughout is:
```css
/* In app.css — wins via html element + class = specificity (0,0,2,1) */
html[data-theme="dark"] .classname {
    ...
}
```
This beats Blazor scoped `.classname[b-xxxxxxxx]` (specificity 0,0,1,1) when loaded after it. All dark mode overrides for scoped components belong in `app.css`, not in `.razor.css` files.

### Supporting Tools

**DungeonDefendersOfflinePreprocessor:**
- Uses `Eliot.UELib` to parse Unreal Engine 3 packages
- Extracts icon references and equipment data from game files
- Reads `DunDefHeroes.dun` save files

**ItemAttributeMiner:**
- Command-line tool for extracting equipment attributes
- Data pipeline: Extract → Preprocess → Generate `GeneratedItemTable.cs`

## Key Implementation Details

- **Item Hashing:** `ItemHash.cs` — FNV-1a 30-bit hash maps item data to a 6-word phrase for stable, human-readable item IDs. Words are split 5 bits each across two adjective lists, one good-noun list, one verb list, one bad-noun list.
- **Virtualization:** `gridObservers.js` IntersectionObserver pattern; the grid only renders visible rows to handle 500+ items efficiently.
- **Offline-first:** No backend; all data in localStorage via `window.appState`. `UiState` is versioned (`CurrentUIOptionsVersion`) and migrated on load via `MigrateAndNormalize`.
- **Expression Parser:** Supports arithmetic `(TDmg+TRate)>=1000`, boolean `and`/`or`/`not`, string match `Location:ItemBox`, and negation `Best!:Hermit`. Field aliases like `THP`, `HDmg`, `Ab1`, etc., map to `ItemViewRow` properties.
- **`@ref` and render timing:** When a new `CharacterPanel` is added dynamically, its `@ref` is not populated until after the next render cycle. Code that needs to call methods on a newly added panel must retry in `OnAfterRenderAsync` until the ref is non-null.
