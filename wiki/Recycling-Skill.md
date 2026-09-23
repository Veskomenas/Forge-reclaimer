# Recycling Skill

Forge Reclaimer adds an optional **Recycling** skill to Valheim’s normal Skills panel. It uses the Hammer icon and follows Valheim’s usual death skill-loss rules.

## What the skill changes

As Recycling level rises from 0 to 100:

- The crumble weight is reduced linearly.
- The removed crumble weight is moved to the Good outcome.
- The jackpot weight increases linearly.

With default settings at skill level 100, the crumble weight is reduced by 50% and the jackpot weight is doubled. Material yields themselves are not directly multiplied by skill level; the skill changes the outcome odds.

## XP presets

`ProgressionPreset` selects the base Recycling XP per item before item-quality and Epic Loot rarity multipliers.

| Preset | Base XP | Intended pace |
|---|---:|---|
| `Vanilla` | 1 | Very slow, long-term progression. |
| `Slow` | 5 | Deliberate progression. |
| `Balanced` | 10 | Default; noticeable progression through ordinary play. |
| `Fast` | 20 | Casual or accelerated progression. |
| `Custom` | `SkillGain` | Uses the admin-selected `SkillGain` value. |

The preset changes XP only. It does not change salvage amounts, outcome odds, or durability behavior.

## XP formula

When the Recycling skill is enabled:

```text
final Recycling XP = preset base XP × item quality × Epic Loot rarity multiplier
```

Vanilla and ordinary modded items use a rarity multiplier of 1. Example: a normal 3-star item on the default Balanced preset grants:

```text
10 × 3 = 30 XP
```

Epic Loot rarity multipliers apply only when Epic Loot is installed and the item is recognized as magic. See **Epic Loot & Compatibility**.

## Admin settings

| Key | Default | Effect |
|---|---:|---|
| `EnableRecyclingSkill` | `true` | Enables the skill, XP gain, and skill-based outcome bonuses. |
| `ProgressionPreset` | `Balanced` | Selects Vanilla, Slow, Balanced, Fast, or Custom XP pace. |
| `SkillGain` | `10` | Custom-preset base XP per item. |
| `SkillFailReduction` | `0.5` | At level 100, fraction of crumble weight moved to Good. |
| `SkillJackpotBonus` | `1` | At level 100, fraction by which jackpot weight increases; `1` means double. |
| `CraftingSkillGain` | `0` | Optional Crafting XP granted per recycled item. |

## Disabling the skill

Set `EnableRecyclingSkill` to `false` to keep recycling but remove Recycling XP and skill-based outcome bonuses. Existing player Recycling levels are preserved safely. If an administrator enables the feature later, players resume from their stored level.
