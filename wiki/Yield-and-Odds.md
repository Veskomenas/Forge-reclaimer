# Yield & Odds

Forge Reclaimer rolls every recipe ingredient separately. Recycling an item does not choose one result for the entire item: its wood, metal, leather, and other materials can each receive different outcomes.

## Default outcomes

| Outcome | Default weight | Return |
|---|---:|---|
| Crumbles | 20 | 0% |
| Poor | 30 | 25–50% |
| Normal | 30 | 50–75% |
| Good | 15 | 75–99% |
| Jackpot | 5 | 100% |

The default weights total 100, so the default chance matches each number. If an administrator changes the weights, each chance becomes:

```text
outcome chance = outcome weight / all outcome weights combined
```

## Material maximum

Before an outcome is rolled, Forge Reclaimer calculates each ingredient’s maximum possible return:

```text
recipe cost at the item’s quality
× YieldMultiplier
× MaterialMultiplier
```

If `CapAtCraftingCost` is enabled, the final maximum cannot exceed that material’s original recipe cost. The game UI displays this configured maximum as `0-max`.

The recipe cost at an item’s quality includes its base crafting cost plus the material cost of every upgrade level up to that quality. Higher-quality gear therefore has a higher reclaimable maximum.

## Durability

When `DurabilityAffectsYield` is enabled, non-jackpot returns are multiplied by:

```text
50% + (50% × current durability fraction)
```

A fully repaired item uses a multiplier of 1. A broken item uses 0.5, so it can return at most half of its rolled amount. Jackpot ignores durability and always returns 100% of the configured maximum.

## Fractional materials

Valheim materials are whole items, but calculated returns can be fractional. Forge Reclaimer rounds fairly: for example, `0.4` has a 40% chance to become 1 and a 60% chance to become 0. This keeps low-cost ingredients from being unfairly lost or always rounded up.

## Safe balancing starting point

The default values are deliberately conservative: an ingredient has a 20% chance to crumble and only a 5% jackpot chance. For a more generous server, increase `GoodWeight` or the yield multiplier first. Keep `CapAtCraftingCost` enabled unless you intentionally want recycling to create more materials than crafting consumes.
