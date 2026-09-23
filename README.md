# Helgi's Forge Reclaimer

Adds a **Recycle** tab to vanilla crafting stations. Take apart gear you carry and get part of its
materials back — every material rolls its own luck, so sometimes it's trash and sometimes a jackpot.

| | |
|---|---|
| GUID | `helgi.forgereclaimer` |
| Assembly | `Helgi.ForgeReclaimer.dll` |
| Creator | Helgi |
| Depends on | BepInEx, Jotunn (config sync + translations) |
| Install on | server **and** every client (`NetworkCompatibility: EveryoneMustHaveMod`) |
| Status | 1.0.1 — public Thunderstore release |
| Languages | all 36 Valheim languages (Abenaki = English fallback) |

## How it plays
1. Open a crafting station (workbench, forge, …) → third tab **Recycle** / **Perdirbti**.
2. The list shows items from your bag that *this* station makes (quality stars + durability bar as vanilla).
   Grey = station level too low or item equipped.
3. The detail panel shows every material as `0-max` and the current odds.
4. **Recycle** runs the normal crafting progress bar, then the item is removed and each material rolls:

| Outcome | Default weight | Returns |
|---|---|---|
| Crumbles | 20 | 0% |
| Poor | 30 | 25–50% |
| Normal | 30 | 50–75% |
| Good | 15 | 75–99% |
| Jackpot | 5 | 100% (ignores durability) |

- **max** = base recipe + every upgrade level up to the item's quality (a 3★ sword returns up to 3 levels of mats).
- **Durability**: yield × (50% + 50% × durability). A broken item gives at most half (except jackpot).
- **Recycling skill** (own skill, 0-100, in the vanilla Skills panel, Hammer icon): every recycle earns base XP from
  the selected progression preset, multiplied by item quality and (when applicable) Epic Loot rarity. With level the
  crumble weight shrinks (-50% at 100, moved to *good*) and the jackpot weight grows (x2 at 100). Death skill loss
  applies as for any skill. The server can disable it entirely; existing levels are retained safely, while recycling
  uses the base outcome weights and grants no Recycling XP.
- Fractions are rounded randomly (0.4 of a coin = 40% chance of 1), so 1-unit ingredients still roll fairly.
- Full bag → the material drops at your feet.
- Plays the station's done / fail effect. Crafting XP only if `CraftingSkillGain` > 0.

**Not recyclable:** stackable items (arrows, food, materials), items without a recipe, items whose recipe
belongs to another station, upgrader stations, hand crafting (no station), equipped items, and Epic Loot magic
items by default. An admin can opt magic items in with `AllowMagicItemsToRecycle`; they still return ordinary
recipe materials only.

## Config (`BepInEx/config/helgi.forgereclaimer.cfg`, admin-only, synced from server, applies live)
| Section | Key | Default | Meaning |
|---|---|---|---|
| General | Enabled | true | Show the Recycle tab |
| Outcome | FailWeight / PoorWeight / NormalWeight / GoodWeight / JackpotWeight | 20 / 30 / 30 / 15 / 5 | How often each outcome happens (relative weights) |
| Outcome | PoorRange / NormalRange / GoodRange | 0.25-0.5 / 0.5-0.75 / 0.75-0.99 | Fraction of max each outcome returns (jackpot = 1) |
| Skill | SkillFailReduction | 0.5 | At Recycling 100 the fail weight shrinks by this fraction (moved to good), linear with level |
| Skill | EnableRecyclingSkill | true | Enable Recycling XP and level-based outcome bonuses; off uses base odds and preserves existing levels |
| Skill | ProgressionPreset | Balanced | XP pace: Vanilla = 1, Slow = 5, Balanced = 10, Fast = 20, Custom = `SkillGain`; affects XP only |
| Skill | SkillJackpotBonus | 1 | At Recycling 100 the jackpot weight grows by this fraction (1 = doubled), linear with level |
| Skill | SkillGain | 10 | Custom-preset base Recycling XP per item, before quality and Epic Loot rarity multipliers |
| Skill | CraftingSkillGain | 0 | Optional Crafting XP per recycled item |
| EpicLoot | AllowMagicItemsToRecycle | false | Block Epic Loot magic items by default; when enabled they return only ordinary recipe materials, never magical materials |
| EpicLoot | Magic / Rare / Epic / Legendary / Mythic / Ancient ExperienceMultiplier | 1.25 / 1.5 / 2 / 3 / 4 / 5 | Recycling XP multiplier for an Epic Loot item of that rarity; ignored when Epic Loot is absent |
| Yield | YieldMultiplier | 1 | Scale all returns (2 = double, 0.5 = half) |
| Yield | CapAtCraftingCost | true | Never return more than the item cost to craft (anti-duplication with multiplier > 1) |
| Yield | DurabilityAffectsYield | true | Yield x (50% + 50% x durability) |
| Yield | AllowedItemTypes | weapons, bow, shield, armor, utility, tool, trinket | Item categories that can be recycled |
| Blocking | BlockedItems | *(empty)* | Item prefab names never recyclable, e.g. `SwordCheat, AxeJotunBane` |
| Blocking | BlockedMaterials | *(empty)* | Materials never returned, e.g. `DragonTear, Eitr` (item still recyclable) |
| Blocking | MaterialMultipliers | *(empty)* | Per material, on top of YieldMultiplier, separated by `;` or a new line, e.g. `Iron:0.5; Silver:0.25` (0 = blocked; `0,5` is accepted) |

Names are prefab names (what `spawn` uses; full list in `data/item-dumps/ItemDump.tsv`), case-insensitive.
Effective max per material = cost x YieldMultiplier x MaterialMultiplier, capped at cost if CapAtCraftingCost;
the UI shows exactly this as `0-max`. An item whose every material is blocked disappears from the list.

Epic Loot is detected automatically, but remains optional. Magic items are blocked by default so Epic Loot's Sacrifice
and Disenchant systems retain control of the magical-material economy. An admin can enable `AllowMagicItemsToRecycle`;
those items still return only normal recipe materials, while their Recycling XP uses the configured rarity multiplier.
Vanilla and other modded items use `preset base XP × quality`.

### Recycling XP presets
`ProgressionPreset` selects the base XP before the item-quality and Epic Loot rarity multipliers. It does **not** change
salvage amounts, odds, durability, or any other recycling setting:

| Preset | Base XP | Intended pace |
|---|---:|---|
| Vanilla | 1 | Very slow, long-term progression |
| Slow | 5 | Deliberate progression |
| **Balanced** *(default)* | **10** | Noticeable progress through ordinary play |
| Fast | 20 | Casual / accelerated progression |
| Custom | `SkillGain` | An admin-selected base XP value from 0 to 100 |

With the skill enabled, `final Recycling XP = preset base XP × item quality × Epic Loot rarity multiplier`.
For example, Balanced gives a normal 3★ item `10 × 3 = 30` XP. If an admin has enabled recycling of magic items,
an Epic-rarity 3★ item uses the default Epic Loot multiplier of 2 and gives `10 × 3 × 2 = 60` XP. Vanilla and non-magic
modded items have a rarity multiplier of 1. If `EnableRecyclingSkill` is false, Recycling XP is always 0 regardless of
the selected preset.

## Code map
| File | Responsibility |
|---|---|
| `src/RecyclePlugin.cs` | BepInEx entry point, Harmony, version |
| `src/RecycleConfig.cs` | Synced config + allowed item types |
| `src/RecycleSkill.cs` | Recycling skill (Jotunn SkillManager), icon borrowed from the vanilla Hammer |
| `src/RecycleLocalization.cs` | Loads the embedded `Translations/*.json` into Jotunn |
| `Translations/<Language>.json` | One file per Valheim language (all 36), keys `hfr_*`; **edit these to fix a translation** |
| `src/Recycler.cs` | Pure game logic: eligibility, yields, odds, the roll, giving items |
| `src/RecycleTab.cs` | Harmony patches on `InventoryGui` (tab, list, detail panel, craft button) |

### How the tab hooks into vanilla (`InventoryGui`, Valheim 1.0.15)
Vanilla has two tabs and decides Craft vs Upgrade by **which tab button is not interactable**
(`InCraftTab()`). Our tab is a clone of `m_tabUpgrade`; while it's active both vanilla tabs are interactable,
so every method that branches on the tab is intercepted:

| Patch | Why |
|---|---|
| `Awake` postfix | clone Upgrade tab → "TabRecycle", strip `Localize` / `UIGamePad` components |
| `UpdateCraftingPanel` prefix | show tab only at stations with a craft tab (not upgraders / hand crafting) |
| `UpdateRecipeList` prefix (skip) | list recyclable inventory items via vanilla `AddRecipeToList(player, recipe, item, can)` |
| `UpdateRecipe` postfix | overwrite texts, requirement slots (`0-max`), station level, button |
| `OnCraftPressed` prefix (skip) | start the vanilla craft timer with our item, set `_recycling` |
| `DoCrafting` prefix (skip) | if `_recycling`: `Recycler.Recycle` instead of crafting/upgrading |
| `OnTabCraftPressed` / `OnTabUpgradePressed` / `Hide` | deactivate our tab |

`_recycling` is separate from "tab active" on purpose: switching tab while the bar runs must not turn the
recycle into a vanilla **upgrade** of the same item.

## Build & test
```sh
dotnet build -c Release
```
Copy `Directory.Build.user.props.example` to `Directory.Build.user.props` and set your local Valheim and BepInEx paths first.
Set the optional `DeployDir` path when you want a build copied into a local BepInEx plugins folder.
Test in single player before using a new build on a server.
Useful: `devcommands` → `spawn SwordIron 1 3` (3★ iron sword), `raiseskill Crafting 100`,
`raiseskill Recycling 100`, or the reliable custom-skill identifier `raiseskill helgi.forgereclaimer.recycling 100`
(needed for languages such as Turkish where the localized skill name contains a space; `raiseskill all` skips custom skills).
Don't join the test server with it until it's added to the modpack — ValheimEnforcer rejects unlisted mods.

## Open questions / TODO
- [ ] Tab position/size with 3 tabs — check in game, adjust `anchoredPosition` offset if it overlaps.
- [ ] Gamepad: tab has no gamepad binding (UIGamePad stripped).
- [ ] EpicLoot: enchanted items could also return enchanting materials (EpicLoot API).
- [ ] Should items from other mods (no recipe at a vanilla station) be supported?
- [ ] Publishing: create a new Thunderstore package version after changing packaged content or metadata.

## Translations
- `Translations/<Language>.json`, file name = Valheim's language name exactly (list: header of the game's
  localization table; `tools/gen_forge_reclaimer_translations.py` has it). Embedded into the DLL at build time.
- 14 keys: `hfr_skill`, `hfr_skill_desc`, `hfr_skill_line` (`$1` = level), `hfr_tab`, `hfr_button` (shown upper-case), `hfr_crafttype`, `hfr_odds` (keep the `<color>` tags and
  `$1..$5`), `hfr_worn`, `hfr_equipped`, `hfr_result_*` (`$1` = item name).
- Everything except English/Lithuanian is machine-drafted — native speakers welcome to fix. Abenaki uses English
  (no reliable translation; the game itself falls back to English there too).
- Item / material names and vanilla messages come from Valheim's own localization automatically.
