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

The publish script builds, copies output to `docs/`, ensures `.nojekyll` exists, and updates the base href to `/DDGO/`.

## Solution Structure

- **DungeonDefendersGearOptimizer/** - Main Blazor WebAssembly app (.NET 8.0)
- **DungeonDefendersOfflinePreprocessor/** - WPF desktop tool for parsing Unreal Engine packages and game save files (.NET 9.0)
- **ItemAttributeMiner/** - Console tool for extracting item attributes from game files (.NET 9.0)
- **docs/** - GitHub Pages deployment folder (generated, do not edit directly)

## Architecture

### Main Application (DDUP)

**Core Files:**
- `Pages/Index.razor` - Main page with file upload, character selection, equipment grid
- `DDDatabase.cs` - Game stat enums and color management
- `Ratings.cs` - Equipment rating modes for different character classes (Builder Stats, Hero Stats, etc.)
- `ExpressionParser.cs` - Filter expression parser supporting arithmetic, boolean, comparison operators
- `View.cs` - `ItemViewRow` struct for display state (100+ properties)
- `ItemDatabase.cs` - Equipment types, weapon types, equipment sets
- `EventInfo.cs` - Event-specific items and pricing data (900+ entries)
- `GeneratedItemTable.cs` - Pre-generated item data (large generated file, ~1.8MB)

**UI Components:**
- `CharacterPanel.razor` - Character display and equipment selection
- `FilterChips.razor` - Dynamic filter UI
- `SearchBar.razor` - Advanced search with expression support

**JavaScript Interop (`wwwroot/`):**
- `dropzone.js` - Drag & drop file handling
- `GridObserver.js` - Virtualization for large item grids
- `tooltip.js` - Tooltip system
- `download.js` - File download functionality

### Supporting Tools

**DungeonDefendersOfflinePreprocessor:**
- Uses `Eliot.UELib` to parse Unreal Engine 3 packages
- Extracts icon references and equipment data from game files
- Reads `DunDefHeroes.dun` save files

**ItemAttributeMiner:**
- Command-line tool for extracting equipment attributes
- Data pipeline: Extract → Preprocess → Generate `GeneratedItemTable.cs`

## Key Implementation Details

- **Item Hashing:** 30-bit hash for unique item identification using procedural word generation
- **Virtualization:** Grid observer pattern for efficient rendering of 500+ items
- **Offline-first:** No backend required; runs entirely in browser with localStorage persistence
- **Expression Parser:** Full expression language for search filters (arithmetic, boolean, comparisons, string matching with `:` and `!:`)

## Tech Stack

- Blazor WebAssembly (.NET 8.0)
- C# with Razor components
- Scoped CSS styling
- JavaScript for browser interop
- UELib for Unreal Engine package parsing (desktop tools)
