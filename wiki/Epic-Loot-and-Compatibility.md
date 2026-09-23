# Epic Loot & Compatibility

## Epic Loot support

Forge Reclaimer detects Epic Loot automatically at runtime. Epic Loot is optional: the recycler works normally without it and does not require it as a Thunderstore dependency.

By default, Epic Loot magic items are **not recyclable**. This protects Epic Loot’s own Sacrifice and Disenchant systems and prevents Forge Reclaimer from becoming another source of magical shards, dust, or similar enchanting materials.

## Allowing magic items

An administrator can set:

```text
AllowMagicItemsToRecycle = true
```

This permits Epic Loot magic gear in the Recycle tab. It does **not** return magical materials. A magic item always returns only its ordinary recipe materials, using the same yield and odds rules as any other item.

## Rarity-aware Recycling XP

When the Recycling skill is enabled, Epic Loot magic items use a configurable XP multiplier based on rarity:

| Setting | Default multiplier |
|---|---:|
| `MagicExperienceMultiplier` | 1.25 |
| `RareExperienceMultiplier` | 1.5 |
| `EpicExperienceMultiplier` | 2 |
| `LegendaryExperienceMultiplier` | 3 |
| `MythicExperienceMultiplier` | 4 |
| `AncientExperienceMultiplier` | 5 |

These multipliers affect Recycling XP only. They do not increase material returns or jackpot odds.

Example: with the Balanced preset, a 3-star Epic-rarity item gives:

```text
10 base XP × 3 quality × 2 Epic multiplier = 60 Recycling XP
```

## Configuration and admin mods

Forge Reclaimer uses normal BepInEx configuration entries. It is compatible with Configuration Manager-style tools and other admin tools that edit those entries. Its settings are server-admin-only and synchronized to clients, so the server remains authoritative.

Changing a player’s skill level through another admin mod does not conflict with Forge Reclaimer: the recycler reads the player’s current Recycling skill level when calculating its outcome bonuses.

## Multiplayer requirements

Forge Reclaimer uses `EveryoneMustHaveMod` network compatibility. Install the same mod on the server and on every client that joins it. Server settings synchronize automatically; clients should not attempt to override server-owned recycling rules.
