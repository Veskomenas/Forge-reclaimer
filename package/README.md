# Helgi's Forge Reclaimer

Recycle crafted gear at vanilla crafting stations and reclaim configurable amounts of its recipe materials.

## Features

- Adds a **Recycle** tab to supported vanilla crafting stations.
- Every material is rolled independently, with crumbles through jackpot outcomes.
- Supports durability-aware returns, yield multipliers, blocked items/materials, and per-material multipliers.
- Optional Recycling skill with server-controlled progression presets and rarity-aware Epic Loot XP.
- Detects Epic Loot automatically. Magic items are blocked by default; an admin may opt them in.
- Server settings are admin-only, synchronized to clients, and apply live.
- Includes all 36 Valheim language files (English fallback for Abenaki).

## Installation

Install with Thunderstore Mod Manager / r2modman. It installs the required BepInEx and Jotunn dependencies automatically.

This is a gameplay mod: install it on the server and on every client that joins it. Its network compatibility is set to require the same mod on all peers.

## Using the recycler

1. Open a vanilla crafting station.
2. Select the **Recycle** tab.
3. Choose an eligible, unequipped item from your inventory.
4. Review the displayed material ranges and outcome odds, then recycle it.

Stackable items, recipe-less items, hand-crafted items, equipped items, and items belonging to another station cannot be recycled. By default, Epic Loot magic items cannot be recycled.

## Server configuration

The config file is `BepInEx/config/helgi.forgereclaimer.cfg`. All settings are server-admin-only and synchronized to connected clients.

The server can tune return odds, material yields, durability behavior, eligibility, Epic Loot behavior, and Recycling skill XP. Set `EnableRecyclingSkill` to `false` to remove XP and skill-based odds bonuses without deleting existing player skill levels.

`ProgressionPreset` controls XP only: `Vanilla = 1`, `Slow = 5`, `Balanced = 10` (default), `Fast = 20`, and `Custom = SkillGain` base XP per recycled item before quality and Epic Loot rarity multipliers.

## Compatibility

- Requires BepInEx and Jotunn (installed automatically as dependencies).
- Epic Loot is optional and detected at runtime; it is not a required dependency.
- Compatible with Configuration Manager-style tools that edit normal BepInEx config entries. Server-synced settings remain authoritative.

## Source, issues, and support

Creator: Helgi

Source code and issue reports: [GitHub — Veskomenas/Forge-reclaimer](https://github.com/Veskomenas/Forge-reclaimer)

Please include the game version, mod version, BepInEx log, and relevant config values when reporting an issue.
