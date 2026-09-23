using System;
using System.Linq;
using System.Reflection;

namespace Helgi.ForgeReclaimer
{
    /// <summary>
    /// Optional integration with Epic Loot's supported API. Reflection deliberately keeps Epic Loot out of this
    /// mod's dependencies: vanilla servers and clients never load, reference, or need EpicLoot.dll.
    /// </summary>
    internal static class EpicLootIntegration
    {
        private static MethodInfo _tryGetRarity;
        private static MethodInfo _isMagicItem;
        private static bool _loggedAvailable;
        private static bool _loggedFailure;

        /// <summary>Returns the configured multiplier for a magic item's actual Epic Loot rarity, or 1 for all other items.</summary>
        public static float GetExperienceMultiplier(ItemDrop.ItemData item)
        {
            if (item == null || !EnsureApi()) return 1f;

            try
            {
                object[] args = { item, 0 }; // TryGetRarity(ItemData, ref int)
                if (!(_tryGetRarity.Invoke(null, args) is bool found) || !found) return 1f;

                switch ((int)args[1])
                {
                    case 0: return RecycleConfig.EpicLootMagicExperienceMultiplier.Value;
                    case 1: return RecycleConfig.EpicLootRareExperienceMultiplier.Value;
                    case 2: return RecycleConfig.EpicLootEpicExperienceMultiplier.Value;
                    case 3: return RecycleConfig.EpicLootLegendaryExperienceMultiplier.Value;
                    case 4: return RecycleConfig.EpicLootMythicExperienceMultiplier.Value;
                    case 5: return RecycleConfig.EpicLootAncientExperienceMultiplier.Value;
                    default:
                        RecyclePlugin.Log.LogWarning($"Epic Loot returned unknown item rarity {(int)args[1]}; using normal Recycling XP.");
                        return 1f;
                }
            }
            catch (Exception ex)
            {
                if (!_loggedFailure)
                {
                    _loggedFailure = true;
                    RecyclePlugin.Log.LogWarning($"Epic Loot rarity lookup failed; using normal Recycling XP. {ex.Message}");
                }
                return 1f;
            }
        }

        /// <summary>True only for an item carrying Epic Loot magic data; always false when Epic Loot is unavailable.</summary>
        public static bool IsMagicItem(ItemDrop.ItemData item)
        {
            if (item == null || !EnsureApi() || _isMagicItem == null) return false;
            try
            {
                return _isMagicItem.Invoke(null, new object[] { item }) is bool magic && magic;
            }
            catch (Exception ex)
            {
                if (!_loggedFailure)
                {
                    _loggedFailure = true;
                    RecyclePlugin.Log.LogWarning($"Epic Loot magic-item lookup failed; treating the item as non-magic. {ex.Message}");
                }
                return false;
            }
        }

        private static bool EnsureApi()
        {
            if (_tryGetRarity != null) return true;

            Assembly epicLoot = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "EpicLoot");
            Type api = epicLoot?.GetType("EpicLoot.API");
            _tryGetRarity = api?.GetMethod("TryGetRarity", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(ItemDrop.ItemData), typeof(int).MakeByRefType() }, null);
            _isMagicItem = api?.GetMethod("IsMagicItem", BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(ItemDrop.ItemData) }, null);
            if (_tryGetRarity == null) return false;

            if (!_loggedAvailable)
            {
                _loggedAvailable = true;
                RecyclePlugin.Log.LogInfo("Epic Loot detected: Recycling XP now uses its configured rarity multipliers.");
            }
            return true;
        }
    }
}
