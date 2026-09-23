# Getting Started

Helgi's Forge Reclaimer adds a **Recycle** tab to vanilla crafting stations. It lets you dismantle eligible gear from your inventory and reclaim some of the materials used to make it.

## Installation

Install Forge Reclaimer on the server **and every client** that joins it. The mod requires BepInEx and Jotunn; Thunderstore installs both automatically.

## Recycling an item

1. Open a vanilla crafting station, such as a Workbench or Forge.
2. Select the **Recycle** tab.
3. Choose an eligible item from your inventory.
4. Review the material ranges and the current outcome odds.
5. Press **Recycle** and wait for the station progress bar to finish.

Each ingredient rolls independently. One material can crumble while another gives a good return from the same item. If your inventory is full, returned materials drop at your feet.

## Eligible items

An item must have a recipe made at the station you are using. It must also be unequipped and belong to an allowed item category.

The recycler intentionally does not accept stackable items, food, raw materials, arrows, items with no recipe, hand-crafted items, upgrader-station items, or gear made at another station. Epic Loot magic items are also blocked by default; see the **Epic Loot & Compatibility** page.

An item shown in grey cannot be recycled at that moment. The usual reasons are that it is equipped or that the station level is too low for its recipe.

## Quality and durability

The maximum return includes the base recipe and every upgrade level up to the item’s quality. A 3-star item therefore has a higher material maximum than a 1-star version of the same item.

By default, durability matters: a broken item can return at most half of its normal amount, while a fully repaired item can return its full rolled amount. Jackpot results always return 100% of the configured maximum and ignore durability.
