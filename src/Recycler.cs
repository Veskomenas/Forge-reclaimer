using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Helgi.ForgeReclaimer
{
    /// <summary>Game-logic side: which items can be recycled, what they can yield, and the roll itself.</summary>
    internal static class Recycler
    {
        public enum Outcome { Nothing, Poor, Normal, Good, Jackpot }

        public struct Yield
        {
            public ItemDrop Resource;
            /// <summary>What went into the item (recipe + upgrades).</summary>
            public int Cost;
            /// <summary>Most that can come back after YieldMultiplier, MaterialMultipliers and the cost cap.</summary>
            public float MaxExact;
            /// <summary>Most that can actually be rolled; also what the UI shows as "0-Max".</summary>
            public int Max => Mathf.Max(1, Mathf.CeilToInt(MaxExact - 0.0001f));
        }

        /// <summary>
        /// Equipped, or put away by the game while standing at a crafting station (Humanoid.HideHandItems unequips the
        /// hand items and remembers them to re-equip later) — either way the player still considers it "in use".
        /// </summary>
        public static bool IsInUse(Player player, ItemDrop.ItemData item) =>
            item.m_equipped || item == player.m_hiddenLeftItem || item == player.m_hiddenRightItem;

        /// <summary>Recipe for the item if it can be taken apart at this station at all (null otherwise).</summary>
        public static Recipe GetRecipe(ItemDrop.ItemData item, CraftingStation station)
        {
            if (item == null || station == null || station.m_upgrader) return null;
            if (item.m_shared.m_maxStackSize > 1) return null;
            if (!RecycleConfig.IsAllowedType(item.m_shared.m_itemType)) return null;
            // Epic Loot's Sacrifice/Disenchant systems own the magic-item economy. An admin may explicitly opt in
            // to recycling magic gear, but Recycle still returns only the ordinary recipe materials.
            if (!RecycleConfig.AllowEpicLootMagicItemsToRecycle.Value && EpicLootIntegration.IsMagicItem(item)) return null;

            Recipe recipe = ObjectDB.instance.GetRecipe(item);
            if (recipe == null || !recipe.m_enabled || recipe.m_resources == null || recipe.m_resources.Length == 0) return null;
            // "Any one of these" recipes: we can't know which ingredient was used, returning all would duplicate.
            if (recipe.m_requireOnlyOneIngredient) return null;
            // Multi-output recipes (e.g. modded x5) would need dividing per unit; not recyclable for now.
            if (recipe.m_amount > 1) return null;

            CraftingStation required = recipe.GetRequiredStation(item.m_quality);
            if (required == null || required.m_name != station.m_name) return null;
            if (RecycleConfig.IsItemBlocked(recipe.m_item.gameObject.name)) return null;
            if (GetYields(recipe, item).Count == 0) return null; // every material blocked
            return recipe;
        }

        /// <summary>Station is high enough level for the item's current quality.</summary>
        public static bool StationLevelOk(Recipe recipe, ItemDrop.ItemData item, CraftingStation station) =>
            station.GetLevel() >= recipe.GetRequiredStationLevel(item.m_quality);

        /// <summary>Everything that went into the item: base recipe plus every upgrade up to its quality.</summary>
        public static List<Yield> GetYields(Recipe recipe, ItemDrop.ItemData item)
        {
            var yields = new List<Yield>();
            foreach (Piece.Requirement req in recipe.m_resources)
            {
                if (req.m_resItem == null || !req.m_recover || req.m_upgraderResource) continue;
                int cost = 0;
                for (int level = 1; level <= item.m_quality; level++)
                    cost += req.GetAmount(level);
                if (cost <= 0) continue;

                float mult = RecycleConfig.YieldMultiplier.Value * RecycleConfig.MaterialMultiplier(req.m_resItem.gameObject.name);
                if (mult <= 0f) continue; // blocked material
                float max = cost * mult;
                if (RecycleConfig.CapAtCraftingCost.Value) max = Mathf.Min(max, cost);
                yields.Add(new Yield { Resource = req.m_resItem, Cost = cost, MaxExact = max });
            }
            return yields;
        }

        public static float DurabilityFactor(ItemDrop.ItemData item)
        {
            if (!RecycleConfig.DurabilityAffectsYield.Value || !item.m_shared.m_useDurability) return 1f;
            float pct = item.GetDurabilityPercentage();
            if (float.IsNaN(pct) || float.IsInfinity(pct)) return 1f; // modded item with 0 max durability
            return 0.5f + 0.5f * Mathf.Clamp01(pct);
        }

        /// <summary>
        /// Outcome weights after the Recycling-skill bonus (skill factor 0-1 = level 0-100):
        /// fail weight shrinks (moved to good), jackpot weight grows.
        /// </summary>
        public static float[] GetWeights(Player player)
        {
            // Keep the registered skill and its saved progress intact when disabled, but do not let it
            // affect rolls. This makes the setting safe to toggle on a live server.
            float skill = RecycleConfig.EnableRecyclingSkill.Value && player != null
                ? player.GetSkillFactor(RecycleSkill.Type)
                : 0f;
            float fail = RecycleConfig.FailWeight.Value;
            float moved = fail * skill * RecycleConfig.SkillFailReduction.Value;
            float[] weights =
            {
                fail - moved,
                RecycleConfig.PoorWeight.Value,
                RecycleConfig.NormalWeight.Value,
                RecycleConfig.GoodWeight.Value + moved,
                RecycleConfig.JackpotWeight.Value * (1f + skill * RecycleConfig.SkillJackpotBonus.Value),
            };
            for (int i = 0; i < weights.Length; i++)
                if (!(weights[i] > 0f)) weights[i] = 0f; // also catches NaN
            return weights;
        }

        public static int[] GetPercentages(float[] weights)
        {
            float sum = Mathf.Max(0.0001f, weights.Sum());
            return weights.Select(w => Mathf.RoundToInt(100f * w / sum)).ToArray();
        }

        private static Outcome RollOutcome(float[] weights)
        {
            float sum = weights.Sum();
            if (sum <= 0f) return Outcome.Nothing; // admin set every weight to 0
            float r = Random.Range(0f, sum);
            for (int i = 0; i < weights.Length; i++)
            {
                if (r < weights[i]) return (Outcome)i;
                r -= weights[i];
            }
            return Outcome.Normal;
        }

        private static float RollFraction(Outcome outcome)
        {
            (float min, float max) r;
            switch (outcome)
            {
                case Outcome.Poor: r = RecycleConfig.Range(RecycleConfig.PoorRange); break;
                case Outcome.Normal: r = RecycleConfig.Range(RecycleConfig.NormalRange); break;
                case Outcome.Good: r = RecycleConfig.Range(RecycleConfig.GoodRange); break;
                case Outcome.Jackpot: return 1f;
                default: return 0f;
            }
            return Random.Range(r.min, r.max);
        }

        /// <summary>Round down, then round up with probability equal to the remainder (so 0.4 of a coin is a 40% chance of 1).</summary>
        private static int StochasticRound(float value)
        {
            int whole = Mathf.FloorToInt(value);
            return whole + (Random.value < value - whole ? 1 : 0);
        }

        /// <summary>Remove the item, roll every ingredient, give the materials back. Returns false if nothing happened.</summary>
        public static bool Recycle(Player player, CraftingStation station, ItemDrop.ItemData item)
        {
            Inventory inventory = player.GetInventory();
            Recipe recipe = GetRecipe(item, station);
            // Re-check everything here, not only via the button state: anything can call OnCraftPressed.
            if (recipe == null || !inventory.ContainsItem(item)) return false;
            if (!StationLevelOk(recipe, item, station) || !station.CheckUsable(player, true)) return false;
            if (IsInUse(player, item))
            {
                player.Message(MessageHud.MessageType.Center, "$hfr_equipped");
                return false;
            }

            List<Yield> yields = GetYields(recipe, item);
            float durability = DurabilityFactor(item);
            float[] weights = GetWeights(player);
            int quality = item.m_quality;
            string itemName = item.m_shared.m_name;

            inventory.RemoveItem(item);

            Outcome best = Outcome.Nothing;
            float gotFraction = 0f, maxFraction = 0f;
            foreach (Yield y in yields)
            {
                Outcome outcome = RollOutcome(weights);
                float fraction = RollFraction(outcome);
                float exact = outcome == Outcome.Jackpot ? y.MaxExact : y.MaxExact * fraction * durability;
                int amount = Mathf.Clamp(StochasticRound(exact), 0, y.Max);
                if (outcome > best) best = outcome;
                gotFraction += amount;
                maxFraction += y.MaxExact;
                if (amount > 0) Give(player, y.Resource, amount);
            }

            Outcome summary = maxFraction <= 0 || gotFraction <= 0 ? Outcome.Nothing
                : best == Outcome.Jackpot && gotFraction >= maxFraction ? Outcome.Jackpot
                : gotFraction / maxFraction >= 0.75f ? Outcome.Good
                : gotFraction / maxFraction >= 0.45f ? Outcome.Normal
                : Outcome.Poor;
            string key = summary switch
            {
                Outcome.Nothing => "$hfr_result_nothing",
                Outcome.Poor => "$hfr_result_poor",
                Outcome.Good => "$hfr_result_good",
                Outcome.Jackpot => "$hfr_result_jackpot",
                _ => "$hfr_result_normal",
            };
            player.Message(MessageHud.MessageType.Center, Localization.instance.Localize(key, Localization.instance.Localize(itemName)));

            (summary == Outcome.Nothing ? station.m_craftItemDoneFailEffects : station.m_craftItemDoneEffects)
                .Create(player.transform.position, Quaternion.identity);
            if (RecycleConfig.EnableRecyclingSkill.Value)
            {
                float rarityMultiplier = EpicLootIntegration.GetExperienceMultiplier(item);
                player.RaiseSkill(RecycleSkill.Type, RecycleConfig.RecyclingSkillGain() * quality * rarityMultiplier);
            }
            if (RecycleConfig.CraftingSkillGain.Value > 0f)
                player.RaiseSkill(station.m_craftingSkill, RecycleConfig.CraftingSkillGain.Value);
            return true;
        }

        /// <summary>
        /// Give <paramref name="amount"/> of a material in stacks of at most its max stack size (vanilla AddItem/SetStack
        /// silently cap a single call at one stack). Whatever doesn't fit in the bag is dropped at the player's feet.
        /// </summary>
        private static void Give(Player player, ItemDrop resource, int amount)
        {
            Inventory inventory = player.GetInventory();
            string name = resource.m_itemData.m_shared.m_name;
            Sprite icon = resource.m_itemData.GetIcon();
            int maxStack = Mathf.Max(1, resource.m_itemData.m_shared.m_maxStackSize);
            Transform t = player.transform;

            for (int remaining = amount; remaining > 0;)
            {
                int chunk = Mathf.Min(remaining, maxStack);
                remaining -= chunk;

                ItemDrop.ItemData data = resource.m_itemData.Clone();
                data.m_stack = chunk;
                data.m_worldLevel = Game.m_worldLevel; // so it stacks with items already in the bag
                data.m_dropPrefab = resource.gameObject;

                if (inventory.CanAddItem(data) && inventory.AddItem(data)) continue;
                ItemDrop.DropItem(data, chunk, t.position + t.forward + Vector3.up, t.rotation);
            }
            player.Message(MessageHud.MessageType.TopLeft, "$msg_added " + name, amount, icon);
        }
    }
}
