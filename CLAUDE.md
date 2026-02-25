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

## Unreal Script Source Files (`.uc Files/`)

These 198 UnrealScript files are **read-only reference material** — game source code for Dungeon Defenders. They are not compiled by this project but are the authoritative source for understanding game mechanics, stat formulas, and item behavior. The `ItemAttributeMiner` and `DungeonDefendersOfflinePreprocessor` tools extract data from the compiled game packages that these scripts produce.

### Hero Equipment / Familiars

| Class | Key Mechanics |
|---|---|
| `HeroEquipment_Familiar_CoreHealer` | Heals crystal cores when health < 95%. Heal = `(Base + Stat) × Extra × Multiplier`. Mana cost = `(Multiplier × Stat / Base) ^ Exponent` (clamped). HealRangeBase = 245, HealInterval = 8.0s |
| `HeroEquipment_Familiar_MiniQueen` | Alternates melee and ranged web attacks. Cannot web already-webbed or poisoned enemies. AbsoluteDamageMultiplier = 0.8, ExtraNightmareMeleeDamageMultiplier = 20.0 |
| `HeroEquipment_Familiar_AoeBuffer` | AOE buff distributor. Buff range = `FClamp(WeaponReloadSpeedBonus, 1–50) × 40`. Applies buffs every 2s to nearby players. Upgrades every 10 levels |
| `HeroEquipment_Familiar_TowerDamageScaling` | Projectile familiar. Damage = `(DamageMultiplier × BaseDamage + Stat) × AbsoluteMultiplier × NightmareMultiplier`. Attack interval = `ProjectileShootInterval × (1 / ShootsStat ^ 1.5)`. ProjectileShootInterval default = 3.0s, TargetRange = 1200 |
| `HeroEquipment_Familiar_Melee_TowerScaling` | Extends TowerDamageScaling for melee. MeleeHitRadius = 110, MeleeDamageMomentum = 105000. Knockback = `1.0 + (KnockbackLinearScale × WeaponKnockback ^ KnockbackExpScale)`. Heal per hit = `Damage / BaseDamageToHealRatio × (HealMultiplier ^ DamageHealMultiplierExponent)` |

### Towers

| Class | Key Mechanics |
|---|---|
| `DunDefTower_CannonBall` | Projectile tower. Rotates skeletal controls to track targets. MaximumTargetYawDegrees = 360, TargetAimOffset = (20, 20, 0) |
| `DunDefTower_SAM` | Homing missile tower. Alternates left/right missile sockets. bLookAtLeadTarget = true. Damage = `HomingProjectileTemplate.ProjDamage × GetDamageMultiplier()` |

### Weapons

| Class | Key Mechanics |
|---|---|
| `DunDefWeapon_MagicStaff_Dot` | Charged projectile, StoredChargePercent 0–1.0. Different muzzle effects for uncharged vs fully charged. Force feedback scales with charge |
| `DunDefWeapon_MagicStaff_Channeling` | Sustained beam attack variant |
| `DunDefWeapon_MagicStaff_CustomRightClick` | Staff with custom right-click behavior |
| `DunDefWeapon_MagicStaff_WithOrbitingEffect` | Staff with orbiting particle effect |
| `DunDefWeapon_Minigun` | Rapid-fire weapon |
| `DunDefWeapon_NessieLauncher` | Nessie projectile launcher |
| `DunDefWeapon_HoloSword` | Holographic melee weapon |
| `DunDefWeapon_PortalGun` | Fires portal nodes (`DunDefPortalNode`) |

### Buffs & Status Effects

All buffs extend `DunDefBuff` and implement `ActivateBuff()` / `BuffEffect()` / `DeactivateBuff()` / `InitalizeActorStats()`.

| Class | Key Properties / Mechanics |
|---|---|
| `DunDefBuff_Shield` | Absorb shield. ShieldHealth = 1000 or ShieldHealthPercent = 0.5 of target max HP. Blocks all incoming damage until depleted |
| `DunDefBuff_Web` | Slow debuff. MovementSpeedMultiplier = 0.175, PlayerAttackRateMultiplier = 0.175, WebbedJumpZ = 800. Disables hero abilities. Cannot stack on already-webbed targets |
| `DunDefBuff_Contagion` | Spreading effect. Jumps to nearest valid target in range (max 20 AOE targets). Tracks previous hosts to prevent reinfection. Self-destroys if no target found |
| `DunDefBuff_OnHit` | Procs when owner deals damage. ExtraDamageAmount = 1, DealtDamageMultiplier = 1.0. Can spawn secondary buffs. Filterable by damage type, victim, and causer |
| `DunDefBuff_DamageAdjuster` | Modifies incoming/outgoing damage by multiplier |
| `DunDefBuff_ExtraLife` | Grants an extra life on death |
| `DunDefBuff_Harbinger` | Buff specific to Harbinger enemy encounters |
| `DunDefBuff_Boost` | Generic stat boost (tower and pawn). TowerBoostTypes / PawnBoostTypes arrays. Stackable with configurable max |

### Projectiles & Damage Types

| Class | Key Mechanics |
|---|---|
| `DunDefProjectile_Meteor` | AOE impact. Spawns 1–N fire clouds at impact point. MinFireSpreadRadius = 200, MaxFireSpreadRadius = 400. Optional homing mode |
| `DunDefProjectile_HarpoonDot` | Damage-over-time on hit |
| `DunDefProjectile_StaffDot` | DoT projectile for magic staff weapons |
| `DunDefProjectile_Meteor_HeroScaling` | Meteor variant that scales with hero stats |
| `DunDefDamageType_Hearts` | Heart pickup / healing damage type |
| `DunDefDamageType_ProtoBeam` | Beam weapon damage type |

### Bosses & Enemies

| Class | Key Mechanics |
|---|---|
| `DunDefGenieBoss` | Multi-phase boss. Two independent targeting eye beams (MaxLazerDistance = 2000). TowerDmgMultiplier = 0.5 (takes half damage from towers). Phase transitions via lamp disappear mechanic. States: EyeAttack, HeadHidden, Dying |
| `DunDefHarbinger` | Fire elemental. Charges then shoots from dual sockets. Immune to: darkness, coughing, shocking, ensnare |
| `DunDefDjinn` | Spell-caster enemy. AllyDmgMultiplier = 2.0, TowerDmgMultiplier = 0.8. Abilities: convert gold enemies, destroy/upgrade towers (channeled), beam attacks. SpawnImmuneTime = 2.0s. States: GoldCasting, TowerCasting, DjinnInLamp, Dying |
| `DunDefKraken` | Sea boss with tentacle attacks (`KrakenTentacle`) |
| `DunDefSharkMan` | Shark humanoid enemy |
| `DunDefGoblinCopter` | Flying goblin in helicopter |
| `DunDef_OldOne` | Ancient enemy type with breath attack (`DunDefOldOneBreath`) |

### Game Modes

| Class | Mode |
|---|---|
| `CTF_GameInfo` | Capture the Flag |
| `CTD_GameInfo` | Capture the Dune (defend) |
| `CTF_MultiFlag_GameInfo` | Multi-flag CTF variant |
| `GameInfo_RisingWater` | Rising water hazard map |
| `DunDefGRI_Delivery` | Escort/delivery objective |
| `DunDefGRI_KillEnemiesTimeLimit` | Timed kill objective |
| `DunDefGRI_GoldenTokens` | Golden mana token collection |
| `DunDefGRI_PortalDefense` | Portal-based defense |

### Key Game Mechanic Formulas

**Stat scaling (general pattern):**
```
Result = (Base + StatValue) × Multiplier ^ Exponent
```

**Familiar attack interval:**
```
Interval = BaseShootInterval × (1 / ShootsStat ^ ShotsPerSecondExponent)
```

**Familiar heal per hit:**
```
Heal = Damage / BaseDamageToHealRatio × (HealMultiplier ^ DamageHealMultiplierExponent)  [clamped]
```

**Familiar mana per hit:**
```
Mana = Damage / BaseDamageToManaRatio × (ManaMultiplier ^ DamageManaMultiplierExponent)  [clamped]
```

**Difficulty/Nightmare:** Standard nightmare multiplier is ~17–20× damage. Most familiar and tower damage formulas include a `NightmareMultiplier` factor.

**Web slow:** Movement and attack rate reduced to 17.5% of normal. Jump height = 800 (WebbedJumpZ).

**Djinn allegiance:** Allied Djinn deal 2× damage to enemies, 0.8× to towers. Enemy Djinn can convert gold enemies and channel-destroy towers.

### `.uc Files/UDKGame/` — Core Engine Layer (560 files)

This directory contains the complete UnrealScript source for the game's engine layer: hero, tower, enemy, equipment, buff, and UI systems. Key reference files are described below.

#### Core Enums & Constants (`_DataTypes.uc`, `_SpecialData.uc`)

**LevelUpValueType** — the 11 (actually 16) hero/tower stat indices used everywhere:
```
LU_HEALTH(1), LU_SPEED(2), LU_DAMAGE(3), LU_CASTINGRATE(4),
LU_HEROABILITYONE(5), LU_HEROABILITYTWO(6),
LU_DEFENSEHEALTH(7), LU_DEFENSEATTACKRATE(8), LU_DEFENSEBASEDAMAGE(9),
LU_DEFENSEAOE(10), LU_WEAPONBASEDAMAGE(11), LU_WEAPONALTDAMAGE(12),
LU_WEAPONELEMENTALDAMAGE(13), LU_DAMAGEVULNERABILITY(14),
LU_ATTACKSPEED(15), LU_TENACITY(16)
```

**EEquipmentStatType** — stat slots in equipment `StatModifiers[11]` array:
```
EQS_WEAPONBASEDAMAGE(0), EQS_WEAPONALTDAMAGE(1), EQS_WEAPONELEMENTALDAMAGE(2),
EQS_WEAPONSHOTSPERSECOND(3), EQS_CLIPAMMO(4), EQS_RELOADSPEED(5),
EQS_KNOCKBACK(6), EQS_CHARGESPEED(7), EQS_BLOCKING(8),
EQS_WEAPONNUMBEROFPROJECTILES(9), EQS_WEAPONPROJECTILESPEED(10),
EQS_DAMAGERESISTANCE(12), EQS_HEROSTAT(13)
```

**Equipment slot types (`EEquipmentType`):** `EQT_WEAPON(0)`, `EQT_ARMOR_TORSO(1)`, `EQT_ARMOR_PANTS(2)`, `EQT_ARMOR_BOOTS(3)`, `EQT_ARMOR_GLOVES(4)`, `EQT_FAMILIAR(5)`, `EQT_ACCESSORY1(7)`, `EQT_ACCESSORY2(8)`, `EQT_ACCESSORY3(9)`, `EQT_MASK(10)`

**Difficulty (`DGameDifficulty`):** `EGD_EASY(0)`, `EGD_MEDIUM(1)`, `EGD_HARD(2)`, `EGD_INSANE(3)`, `EGD_NIGHTMARE(4)`, `EGD_RUTHLESS(5)`

**Element types (`EElementTrait`):** `ET_NONE(0)`, `ET_EARTH(1)`, `ET_FIRE(2)`, `ET_WATER(3)`, `ET_LIGHTNING(4)`, `ET_ICE(5)`, `ET_OIL(6)`, `ET_POISON(7)`, `ET_MAGIC(8)`, `ET_DARK(9)`, `ET_HOLY(10)`

**Status effects (`EStatusEffect`):** `SE_FROZEN(1)`, `SE_STUNNED(2)`, `SE_OILED(3)`, `SE_FIRE(4)`, `SE_POISONED(5)`, `SE_SHOCKED(6)`, `SE_WEBBED(7)`

**Buff proc events (`EBuffSpawnEvent`):** `BSE_Spawned`, `BSE_ProjectileExploded`, `BSE_InstigatorAttacked`, `BSE_AbilityUsed`, `BSE_Death`, `BSE_TakeDamage`, `BSE_DealtDamage`, `BSE_AbilityCompleted`, `BSE_FireProjectile`, `BSE_AbilityProc`, `BSE_BuffEnd`, `BSE_Healed`, `BSE_KilledActor`, `BSE_Jumped`, `BSE_HealedTarget`, `BSE_Timer`

**Tower boost types (`ETowerBoostType`):** `ETB_RESISTANCE(0)`, `ETB_DAMAGE(1)`, `ETB_ATTACKRATE(2)`, `ETB_ATTACKRANGE(3)`

**Pawn boost types (`EPawnBoostType`):** `EPB_RESISTANCE(0)`, `EPB_DAMAGE(1)`, `EPB_ATTACKRATE(2)`, `EPB_ATTACKRANGE(3)`, `EPB_MOVEMENTSPEED(4)`, `EPB_ANIMSPEED(5)`, `EPB_ATK_ANIMSPEED(6)`, `EPB_GRAVITY(7)`

**System limits:** `MAX_BUFF_TIERS=6`, `MAX_BUFF_SLOTS=10`, `MAX_DAMAGEREDUCTIONS=4`, `MAX_LEVELUP_STATS=11`, `MAX_CORES=40`, `HERO_LEVEL_CAP=100`, `MAX_HERO_EXPERIENCE=3,000,000,000`

#### Hero System (`DunDefHero.uc`, `DunDefHeroStats.uc`)

**Hero types:** `EHT_APPRENTICE(0)`, `EHT_SQUIRE(1)`, `EHT_HUNTRESS(2)`, `EHT_MONK(3)`

**11 assignable hero stats** (correspond to `LevelUpValueType`): HeroHealth, HeroSpeed, HeroDamage, HeroCasting, HeroAbilityOne, HeroAbilityTwo, DefenseHealth, DefenseAttackRate, DefenseDamage, DefenseAOE, (WeaponDamage via equipment)

**Level-up XP formula constants:**
- Low levels: `EXP_PER_LEVEL_LINEAR_FACTOR=80.0`, exponent `2.5`
- High levels: `EXP_PER_LEVEL_LINEAR_FACTOR_HIGH=800000`, exponent `2.1`
- Double-high: `EXP_PER_LEVEL_LINEAR_FACTOR_DOUBLEHIGH=900000`, exponent `2.2`
- `MANAPOWER_PER_LEVEL_LINEAR_FACTOR=20.0`

**`HeroNetInfo` structure** (saved per hero): HeroName, HeroTemplate, all 10 stat modifiers, HeroLevel, HeroExperience, ManaPower, GUID1–4, color customization (C1/C2/C3 as LinearColor), CurrentCostumeIndex

**Stat scaling** uses `sActorStatModifier` and `sActorStatScalar` structs with fields: BaseValue, ScalingValue, ScalingExponent, BaseValueExponent, MaxValue, MinValue, bUseAdditiveScaling. Each `LevelUpValueType` has its own scaling curve with Initial/Full multipliers and exponents.

#### Equipment System (`HeroEquipment.uc`, `HeroEquipmentNative.uc`)

**`EquipmentNetInfo` structure** (the replicated item data):
- `StatModifiers[11]`: int array indexed by `LevelUpValueType`
- `DamageReductions[4]`: array of `{DamageType, PercentageReduction(0–100)}`
- `WeaponDamageBonus`, `WeaponAltDamageBonus`, `WeaponElementalDamageBonus`: int
- `WeaponShotsPerSecondBonus`, `WeaponClipAmmoBonus`, `WeaponReloadSpeedBonus`: byte/int
- `WeaponKnockbackBonus`, `WeaponChargeSpeedBonus`, `WeaponBlockingBonus`: byte
- `WeaponNumberOfProjectilesBonus`, `WeaponSpeedOfProjectilesBonus`: byte/int
- `WeaponAdditionalDamageType`, `WeaponAdditionalDamageAmount`: damage type + int
- `WeaponDrawScaleMultiplier`, `WeaponSwingSpeedMultiplier(default 1.0)`: float
- `Level(default 1)`, `MaxEquipmentLevel`, `StoredMana`: int
- `NameIndex_Base`, `NameIndex_QualityDescriptor`, `NameIndex_DamageReduction`: byte

**Quality tiers:** MaxHeroStatValue → TranscendentMaxHeroStatValue → SupremeMaxHeroStatValue → UltimateMaxHeroStatValue → UltimatePlusMaxHeroStatValue. Each tier has a `FullEquipmentSetStatMultiplier`.

**Upgrade cost formula constants:** `ManaCostPerLevelLinearFactor`, `ManaCostPerLevelExponentialFactor`, `HighLevelThreshold`, `HighLevelManaCostPerLevelExponentialFactorAdditional`

**Damage upgrade constants:** `DamageIncreasePerLevelMultiplier`, `MaxDamageIncreasePerLevel`, `ElementalDamageIncreasePerLevelMultiplier`, `UltimateDamageIncreasePerLevelMultiplier`

**Sell worth formula:** `SellWorthLinearFactor × (EquipmentRating ^ SellRatingExponent) × (Level ^ SellWorthMultiplierLevelBase)`, clamped to `[SellWorthMin, SellWorthMax]`

#### Tower System (`DunDefTower.uc` + 24 subtypes)

**Base tower properties:** `TowerUnitCost`, `ManaWorth`, `MaxUpgradeLevel`, `TowerUpgradeCosts[]`, `TowerUpgradeTimes[]`

**Stat multipliers (replicated):** `DamageMultiplier`, `AttackRateMultiplier`, `HealthMultiplier`, `AOEMultiplier`, `DamageMultiplierAdditional`, `ReistanceMultiplier`

**Scaling exponents:** `TowerDamageMultiplierExponent`, `AttackRateMultiplierExponent`, `AOEMultiplierExponent`, `HealthMultiplierExponent`

**Difficulty arrays (length 6):** `DifficultyHealthMultipliers[]`, `DifficultyDamageMultipliers[]`, `UpgradeLinearMultipliersHealth[]`, `UpgradeLinearMultipliersDamage[]`

**Nightmare/Competitive:** `NightmareHealthMultiplier`, `NightmareDamageMultiplier`, `CompetitiveDamageMultiplier`, `CompetitiveHealthMultiplier`

**Repair:** `CostOfTotalRepairWorthPercent`, `TimeOfTotalRepair`, `RepairTimeToHealthExponent`

**Boost system:** Each tower holds `TowerBoosters[]` and `TowerDeBoosters[]` arrays. Each boost entry: `{boostType: ETowerBoostType, boostExponent, boostBaseAmt, levelUpStat: LevelUpValueType, UpgradeLinearBoostMultipliers[]}`. Formula: `boostBaseAmt × (HeroStat ^ boostExponent)`.

**Tower damage formula:**
```
FinalDamage = BaseDamage
  × DifficultyDamageMultiplier[difficulty]
  × (1 + UpgradeLevel × UpgradeLinearMultipliersDamage)
  × NightmareDamageMultiplier  [if nightmare]
  × TowerBoostMultiplier       [from hero/familiar boosters]
```

**All 24 tower types:**
| Category | Classes |
|---|---|
| Blockades | `DunDefTower_Blockade`, `DunDefTower_SpikyBlockade`, `DunDefTower_BouncyBlockade` |
| Melee | `DunDefTower_DeadlyStriker`, `DunDefTower_SliceNDice` |
| Projectile | `DunDefTower_Fireball`, `DunDefTower_MagicMissile`, `DunDefTower_ChainLightning`, `DunDefTower_Harpoon`, `DunDefTower_BowlingBall`, `DunDefTower_MultiProjectile` |
| Traps | `DunDefTower_ProxMineTrap`, `DunDefTower_GasTrap`, `DunDefTower_OilTrap`, `DunDefTower_JackInTheBox` |
| Auras | `DunDefTower_Aura`, `DunDefTower_AuraHeal`, `DunDefTower_AuraStrengthDrain`, `DunDefTower_AuraEnrage`, `DunDefTower_AuraStickyGloop`, `DunDefTower_AuraDeathlyHallows` |
| Base types | `DunDefTower_DetonationType`, `DunDefTower_ProjectileType`, `DunDefTower_NonPhysical` |

#### Enemy System (`DunDefEnemy.uc`)

**Classification:** `EC_NORMAL(0)`, `EC_UNDEAD(1)`

**Scaling arrays (length 6, one per difficulty):** `DifficultyHealthMultipliers[]`, `DifficultyDamageMultipliers[]`, `DifficultySpeedMultipliers[]`, `DifficultyManaMultipliers[]`

**Ruthless mode defaults:** HealthMultiplier=9.0, SpeedMultiplier=0.35, DamageMultiplier=1.05, TowerDamageResistanceModifier=0.75

**Elemental system:** `ElementalEntries[]` — each entry has `{DamageType, UsageChance, ParticleEffect}`. `ElementalDamageModifiers[]` maps damage types to resistance multipliers.

**Drop system:** `DifficultyEquipmentQualityMultipliers[]`, `DifficultyEquipmentRarityWeightings[]`, `GlobalEquipmentDropQuality`, `MaxWaveEquipmentQualityMultiplier`

**Spawn system:** `SpawnClumpAbsoluteAmount`, `SpawnClumpMaximumAmount`, `SpawnClumpRelativePercent`, `MaxSimultaneousAllowedForPlayers[]`

**All enemy types:** `DunDefGoblin`, `DunDefOrc`, `DunDefKobold`, `DunDefSkeleton`, `DunDefDemon`, `DunDefOgre`, `DunDefWyvern`, `DunDefDarkElf`, `DunDefDarkElfWarrior`, `DunDefDarkElfMage`, `DunDefForestGolem`, `DunDefDragonBoss`, `DunDefPhoenixV3` (+ controllers for each)

#### Damage Type System (`DunDefDamageType.uc` + 40+ subtypes)

**Base properties:** `ElementalTrait: EElementTrait`, `IsElementalDamage`, `IgnoreResistances`, `bIsPassive`, `MinimumIntervalForDamageOfThisType`

**All physical/special types:** `Generic`, `Bleed`, `Blunt`, `Bound`, `Cutting`, `Debug`, `EnemySharp`, `EtherealSpike`, `ForceKill`, `ForceKnockBack`, `FullMomentum`, `IgnoreResistance`, `Invincible`, `MegaDamage`, `Passive`, `Silly`, `Special`, `Suction`

**Elemental types:** `Electric`, `ElectricCutting`, `Fire`, `Fire_Passive`, `Holy`, `Ice`, `Lightning`, `Lightning_FullMomentum`, `Lightning_Passive`, `Poison`, `Poison_Passive`

**Tower-specific types:** `InfernoTrap`, `LightingTower`, `LightningCloudTower`, `OwlNest`, `ProxMine`, `SamRocket`, `WispDen`

**Other:** `Bleed`, `Love`, `ProtonChargeBlast`

#### Buff System (`DunDefBuff.uc`, `DunDefBuffManager.uc`)

**Per-buff stat arrays:** `AdditiveStatModifier[LevelUpValueType]` and `MultiplicativeStatModifier[LevelUpValueType]` (17 entries each, replicated via `sBuffNetInfo.StatValueBuffsAdditive[]` and `StatValueBuffsMultiplicative[]`)

**Stacking:** `bCanStack`, `MaxStackCount`, `bStackingResetsTimer`, `LinearStatIncreasePerStack`

**Targeting filters:** `bOnlyAffectPlayers`, `bOnlyAffectEnemies`, `bOnlyAffectTowers`, `bOnlyAffectPawns`, `bCanBuffBoss`, `bApplyToInvincibleTargets`

**Limiting:** `bLimitNumSameBuffs` / `NumSameBuffsLimit` — removes oldest buff when cap hit. Per-owner limit: `bLimitNumSameBuffs_PerOwner` / `NumSameBuffs_PerOwner`

**`DunDefBuffManager`** tracks `tickingBuffs[]`, `LimitedBuffs[]`, `LimitedBuffsPerOwnerSpawnCounts[]`, and a global `CurrentID` counter.

#### Familiar Base Classes (`HeroEquipment_Familiar.uc` + variants)

| Class | Role |
|---|---|
| `HeroEquipment_Familiar` | Base: mesh, animation, size scaling, AI targeting |
| `HeroEquipment_Familiar_Melee` | Melee attacks |
| `HeroEquipment_Familiar_WithProjectileAI` | Ranged projectile attacks |
| `HeroEquipment_Familiar_PlayerHealer` | Heals the owner hero |
| `HeroEquipment_Familiar_TowerHealer` | Heals nearby towers |
| `HeroEquipment_Familiar_TowerBooster` | Applies `ETowerBoostType` boosts to towers (uses `Boostingtypes` array) |
| `HeroEquipment_Familiar_PawnBooster` | Applies `EPawnBoostType` boosts to nearby heroes |
| `HeroEquipment_Familiar_TADPS` | Tower-Adjacent DPS (damages enemies near towers) |
| `HeroEquipment_Familiar_Buff_Spawner` | Spawns `DunDefBuff` instances in area on `EBuffSpawnEvent` trigger |
| `HeroEquipment_Familiar_MoneyGiver` | Generates mana/currency |

Familiar size scales as: `DrawScale = BaseScale × (Level / SizeScalerMaximumLevel) ^ SizeScalerPower × MaximumLevelScaleMultiplier`

#### Player Abilities (`DunDefPlayerAbility.uc` + 16 subtypes)

**Ability status enum:** `EPA_INVISIBLE`, `EPA_NOTAPPLICABLE`, `EPA_UNDERLEVELREQUIREMENT`, `EPA_NOTENOUGHMANA`, `EPA_BADPHYSICSSTATE`, `EPA_COOLINGDOWN`, `EPA_CANACTIVATE`, `EPA_CASTING`

**Key properties:** `RequiredHeroLevel`, `AssociatedHeroStat: LevelUpValueType`, `ActivationInterval` (cooldown), `bPressAndHoldAbility`, `bInvincibleWhileCasting`

**All ability types:** `BuildTower`, `UpgradeTower`, `SellTower`, `RepairTower`, `TowerPlacement`, `Heal`, `BuildAura`, `GenericSpellTimer`, `GenericActorSpawner`, `AreaOfEffect`, `CharacterToggle`, `DetonateTraps`, `Squire_BloodRage`, `Squire_CircleSlice`, `Huntress_Invisibility`, `Initiate_Invisibility`, `Recruit_HeroBoost`, `Recruit_TowerBoost`, `Apprentice_Overcharge`, `PickUpItem`

#### Game Modes (UDKGame variants)

| Class | Mode | Notes |
|---|---|---|
| `GameInfo_MainSixPlayer` | Standard co-op | Up to 6 players, wave-based |
| `GameInfo_PureStrategy` | Tower-only | No direct player combat |
| `GameInfo_Competitive` | PvP | Score: +2 kill / -1 suicide; `GlobalPhysicalTowerHealthMultiplier=1.6`; `ManaToGiveOnRespawn=200` |
| `GameInfo_Special` | Event/seasonal | Custom rules |

---

## Key Implementation Details

- **Item Hashing:** `ItemHash.cs` — FNV-1a 30-bit hash maps item data to a 6-word phrase for stable, human-readable item IDs. Words are split 5 bits each across two adjective lists, one good-noun list, one verb list, one bad-noun list.
- **Virtualization:** `gridObservers.js` IntersectionObserver pattern; the grid only renders visible rows to handle 500+ items efficiently.
- **Offline-first:** No backend; all data in localStorage via `window.appState`. `UiState` is versioned (`CurrentUIOptionsVersion`) and migrated on load via `MigrateAndNormalize`.
- **Expression Parser:** Supports arithmetic `(TDmg+TRate)>=1000`, boolean `and`/`or`/`not`, string match `Location:ItemBox`, and negation `Best!:Hermit`. Field aliases like `THP`, `HDmg`, `Ab1`, etc., map to `ItemViewRow` properties.
- **`@ref` and render timing:** When a new `CharacterPanel` is added dynamically, its `@ref` is not populated until after the next render cycle. Code that needs to call methods on a newly added panel must retry in `OnAfterRenderAsync` until the ref is non-null.
