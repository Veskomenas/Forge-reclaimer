# Server Configuration

All Forge Reclaimer settings are server-admin-only, synchronized to connected clients, and applied live. The server is authoritative: players see the same odds and material ranges that the server uses.

The configuration file is:

```text
BepInEx/config/helgi.forgereclaimer.cfg
```

Configuration Manager-style tools can edit the normal BepInEx entries. Changes made by an authorized server administrator are synchronized to clients.

## General

| Key | Default | Effect |
|---|---:|---|
| `Enabled` | `true` | Shows or hides the Recycle tab at crafting stations. |

## Outcome weights and ranges

| Key | Default | Effect |
|---|---:|---|
| `FailWeight` | `20` | Relative chance of returning 0% for an ingredient. |
| `PoorWeight` | `30` | Relative chance of the Poor range. |
| `NormalWeight` | `30` | Relative chance of the Normal range. |
| `GoodWeight` | `15` | Relative chance of the Good range. |
| `JackpotWeight` | `5` | Relative chance of returning 100%. |
| `PoorRange` | `0.25-0.5` | Poor return fraction, minimum to maximum. |
| `NormalRange` | `0.5-0.75` | Normal return fraction, minimum to maximum. |
| `GoodRange` | `0.75-0.99` | Good return fraction, minimum to maximum. |

Weights are relative, not percentages. With the default total weight of 100, they happen to read like percentages. Ranges accept values from 0 to 1 and are automatically ordered if entered backwards.

## Yield controls

| Key | Default | Effect |
|---|---:|---|
| `YieldMultiplier` | `1` | Scales every material maximum. `2` doubles it; `0.5` halves it. |
| `CapAtCraftingCost` | `true` | Prevents returning more of a material than the recipe cost. Recommended when the yield multiplier is above 1. |
| `DurabilityAffectsYield` | `true` | Applies the durability multiplier to non-jackpot returns. |
| `AllowedItemTypes` | weapons, armor, tools, utility, trinkets | Comma-, semicolon-, or space-separated Valheim item-type names eligible for recycling. |

## Blocking and material tuning

| Key | Default | Effect |
|---|---:|---|
| `BlockedItems` | empty | Comma-separated item prefab names that can never be recycled. |
| `BlockedMaterials` | empty | Comma-separated material prefab names that are never returned. The item itself can still be recycled. |
| `MaterialMultipliers` | empty | Per-material multiplier, on top of `YieldMultiplier`. |

Use prefab names—the same names used by Valheim’s `spawn` command. Example:

```text
BlockedItems = SwordCheat, AxeJotunBane
BlockedMaterials = DragonTear, Eitr
MaterialMultipliers = Iron:0.5; Silver:0.25
```

`MaterialMultipliers` pairs must be separated with semicolons or new lines. Both decimal styles are accepted, so `Iron:0.5` and `Iron:0,5` mean the same thing. Set a material multiplier to `0` to block that material.

## Skill and Epic Loot settings

See **Recycling Skill** for every skill setting and **Epic Loot & Compatibility** for magic-item and rarity settings.
