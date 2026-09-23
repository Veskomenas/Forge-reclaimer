using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Configs;

namespace Helgi.ForgeReclaimer
{
    /// <summary>All settings are admin-only and synced from the server by Jotunn.</summary>
    internal static class RecycleConfig
    {
        public static ConfigEntry<bool> Enabled;

        // Outcome weights, rolled separately for every ingredient.
        public static ConfigEntry<float> FailWeight;
        public static ConfigEntry<float> PoorWeight;
        public static ConfigEntry<float> NormalWeight;
        public static ConfigEntry<float> GoodWeight;
        public static ConfigEntry<float> JackpotWeight;

        // Outcome ranges: fraction of the (scaled) max that each outcome returns.
        public static ConfigEntry<string> PoorRange;
        public static ConfigEntry<string> NormalRange;
        public static ConfigEntry<string> GoodRange;

        public static ConfigEntry<float> SkillFailReduction;
        public static ConfigEntry<bool> EnableRecyclingSkill;
        public static ConfigEntry<string> SkillProgressionPreset;
        public static ConfigEntry<bool> DurabilityAffectsYield;
        public static ConfigEntry<float> SkillGain;
        public static ConfigEntry<float> SkillJackpotBonus;
        public static ConfigEntry<float> CraftingSkillGain;
        public static ConfigEntry<float> EpicLootMagicExperienceMultiplier;
        public static ConfigEntry<bool> AllowEpicLootMagicItemsToRecycle;
        public static ConfigEntry<float> EpicLootRareExperienceMultiplier;
        public static ConfigEntry<float> EpicLootEpicExperienceMultiplier;
        public static ConfigEntry<float> EpicLootLegendaryExperienceMultiplier;
        public static ConfigEntry<float> EpicLootMythicExperienceMultiplier;
        public static ConfigEntry<float> EpicLootAncientExperienceMultiplier;
        public static ConfigEntry<string> AllowedItemTypes;

        public static ConfigEntry<float> YieldMultiplier;
        public static ConfigEntry<bool> CapAtCraftingCost;
        public static ConfigEntry<string> BlockedItems;
        public static ConfigEntry<string> BlockedMaterials;
        public static ConfigEntry<string> MaterialMultipliers;

        private static HashSet<ItemDrop.ItemData.ItemType> _allowedTypes;
        private static HashSet<string> _blockedItems, _blockedMaterials;
        private static Dictionary<string, float> _materialMultipliers;
        private static string _lastUnknownProgressionPreset;
        private static readonly Dictionary<ConfigEntry<string>, (float min, float max)> _ranges =
            new Dictionary<ConfigEntry<string>, (float, float)>();

        public static void Bind(ConfigFile cfg)
        {
            var admin = new ConfigurationManagerAttributes { IsAdminOnly = true };

            Enabled = cfg.Bind("General", "Enabled", true,
                new ConfigDescription("Show the Recycle tab at crafting stations.", null, admin));

            var weightRange = new AcceptableValueRange<float>(0f, 1000f);
            FailWeight = cfg.Bind("Outcome", "FailWeight", 20f,
                new ConfigDescription("Weight of 'crumbled' (0% of the ingredient back).", weightRange, admin));
            PoorWeight = cfg.Bind("Outcome", "PoorWeight", 30f,
                new ConfigDescription("Weight of 'poor' (25-50% back).", weightRange, admin));
            NormalWeight = cfg.Bind("Outcome", "NormalWeight", 30f,
                new ConfigDescription("Weight of 'normal' (50-75% back).", weightRange, admin));
            GoodWeight = cfg.Bind("Outcome", "GoodWeight", 15f,
                new ConfigDescription("Weight of 'good' (75-99% back).", weightRange, admin));
            JackpotWeight = cfg.Bind("Outcome", "JackpotWeight", 5f,
                new ConfigDescription("Weight of 'jackpot' (100% back, ignores durability).", weightRange, admin));

            EnableRecyclingSkill = cfg.Bind("Skill", "EnableRecyclingSkill", true,
                new ConfigDescription("Enable the Recycling skill, its experience gain, and its outcome bonuses. Disabling it preserves existing skill levels but makes recycling use the base outcome weights.", null, admin));
            SkillProgressionPreset = cfg.Bind("Skill", "ProgressionPreset", "Balanced",
                new ConfigDescription("Recycling XP pace: Vanilla = 1, Slow = 5, Balanced = 10, Fast = 20, Custom = use SkillGain. This changes XP only; material returns and outcome odds are configured separately.",
                    new AcceptableValueList<string>("Vanilla", "Slow", "Balanced", "Fast", "Custom"), admin));
            SkillFailReduction = cfg.Bind("Skill", "SkillFailReduction", 0.5f,
                new ConfigDescription("At Recycling skill 100 the fail weight is reduced by this fraction (scales linearly with level); the removed weight goes to 'good'.",
                    new AcceptableValueRange<float>(0f, 1f), admin));
            SkillJackpotBonus = cfg.Bind("Skill", "SkillJackpotBonus", 1f,
                new ConfigDescription("At Recycling skill 100 the jackpot weight is increased by this fraction (1 = doubled; scales linearly with level).",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            SkillGain = cfg.Bind("Skill", "SkillGain", 10f,
                new ConfigDescription("Base Recycling XP per item when ProgressionPreset is Custom. Final XP is this value x item quality x Epic Loot rarity multiplier (if applicable).",
                    new AcceptableValueRange<float>(0f, 100f), admin));
            CraftingSkillGain = cfg.Bind("Skill", "CraftingSkillGain", 0f,
                new ConfigDescription("Optional Crafting skill XP per recycled item (0 = recycling only trains Recycling).", new AcceptableValueRange<float>(0f, 100f), admin));

            AllowEpicLootMagicItemsToRecycle = cfg.Bind("EpicLoot", "AllowMagicItemsToRecycle", false,
                new ConfigDescription("Allow Epic Loot magic items in Recycle. They return only their normal recipe materials; use Epic Loot's Sacrifice/Disenchant systems for magical materials. Has no effect unless Epic Loot is installed.", null, admin));
            EpicLootMagicExperienceMultiplier = cfg.Bind("EpicLoot", "MagicExperienceMultiplier", 1.25f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Magic items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            EpicLootRareExperienceMultiplier = cfg.Bind("EpicLoot", "RareExperienceMultiplier", 1.5f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Rare items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            EpicLootEpicExperienceMultiplier = cfg.Bind("EpicLoot", "EpicExperienceMultiplier", 2f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Epic items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            EpicLootLegendaryExperienceMultiplier = cfg.Bind("EpicLoot", "LegendaryExperienceMultiplier", 3f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Legendary items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            EpicLootMythicExperienceMultiplier = cfg.Bind("EpicLoot", "MythicExperienceMultiplier", 4f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Mythic items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            EpicLootAncientExperienceMultiplier = cfg.Bind("EpicLoot", "AncientExperienceMultiplier", 5f,
                new ConfigDescription("Recycling XP multiplier for Epic Loot Ancient items. Has no effect unless Epic Loot is installed.",
                    new AcceptableValueRange<float>(0f, 10f), admin));

            DurabilityAffectsYield = cfg.Bind("Yield", "DurabilityAffectsYield", true,
                new ConfigDescription("Worn items give less: yield is scaled by 50% + 50% x durability.", null, admin));
            AllowedItemTypes = cfg.Bind("Yield", "AllowedItemTypes",
                "OneHandedWeapon,TwoHandedWeapon,TwoHandedWeaponLeft,Bow,Shield,Helmet,Chest,Legs,Shoulder,Utility,Tool,Trinket",
                new ConfigDescription("Item types that can be recycled (ItemDrop.ItemData.ItemType names). Stackable items are never recyclable.",
                    null, admin));
            AllowedItemTypes.SettingChanged += (_, __) => _allowedTypes = null;

            PoorRange = cfg.Bind("Outcome", "PoorRange", "0.25-0.5",
                new ConfigDescription("Fraction of the max returned on a 'poor' roll (min-max, 0-1).", null, admin));
            NormalRange = cfg.Bind("Outcome", "NormalRange", "0.5-0.75",
                new ConfigDescription("Fraction of the max returned on a 'normal' roll (min-max, 0-1).", null, admin));
            GoodRange = cfg.Bind("Outcome", "GoodRange", "0.75-0.99",
                new ConfigDescription("Fraction of the max returned on a 'good' roll (min-max, 0-1). Jackpot is always 1.", null, admin));
            foreach (var e in new[] { PoorRange, NormalRange, GoodRange })
                e.SettingChanged += (_, __) => _ranges.Clear();

            YieldMultiplier = cfg.Bind("Yield", "YieldMultiplier", 1f,
                new ConfigDescription("Scales every amount recycling can return (2 = double, 0.5 = half).",
                    new AcceptableValueRange<float>(0f, 10f), admin));
            CapAtCraftingCost = cfg.Bind("Yield", "CapAtCraftingCost", true,
                new ConfigDescription("Never return more of a material than the item cost to make (stops craft -> recycle duplication when YieldMultiplier > 1).",
                    null, admin));
            BlockedItems = cfg.Bind("Blocking", "BlockedItems", "",
                new ConfigDescription("Item prefab names that can never be recycled, comma separated (e.g. SwordCheat, AxeJotunBane).", null, admin));
            BlockedMaterials = cfg.Bind("Blocking", "BlockedMaterials", "",
                new ConfigDescription("Material prefab names never given back, comma separated (e.g. DragonTear, Eitr). The item can still be recycled.", null, admin));
            MaterialMultipliers = cfg.Bind("Blocking", "MaterialMultipliers", "",
                new ConfigDescription("Per-material multiplier on top of YieldMultiplier, separated by semicolons or new lines, e.g. Iron:0.5; Silver:0.25 (0 = blocked; comma decimal marks are accepted).", null, admin));
            BlockedItems.SettingChanged += (_, __) => _blockedItems = null;
            BlockedMaterials.SettingChanged += (_, __) => _blockedMaterials = null;
            MaterialMultipliers.SettingChanged += (_, __) => _materialMultipliers = null;
        }

        private static IEnumerable<string> SplitList(string value) =>
            value.Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);

        private static IEnumerable<string> SplitPairs(string value) =>
            value.Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);

        private static bool TryParseDecimal(string text, out float value)
        {
            // Config is commonly edited with a Lithuanian/European decimal comma. Separators for pair lists are
            // therefore semicolons/newlines, never commas.
            return float.TryParse((text ?? "").Trim().Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value)
                && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsItemBlocked(string prefabName)
        {
            if (_blockedItems == null)
                _blockedItems = new HashSet<string>(SplitList(BlockedItems.Value), StringComparer.OrdinalIgnoreCase);
            return prefabName != null && _blockedItems.Contains(prefabName);
        }

        /// <summary>Base Recycling XP selected by the friendly progression preset, before quality and rarity bonuses.</summary>
        public static float RecyclingSkillGain()
        {
            switch ((SkillProgressionPreset.Value ?? "").Trim().ToLowerInvariant())
            {
                case "vanilla": return 1f;
                case "slow": return 5f;
                case "balanced": return 10f;
                case "fast": return 20f;
                case "custom": return SkillGain.Value;
                default:
                    string invalid = SkillProgressionPreset.Value ?? "";
                    if (_lastUnknownProgressionPreset != invalid)
                    {
                        _lastUnknownProgressionPreset = invalid;
                        RecyclePlugin.Log.LogWarning($"ProgressionPreset '{invalid}' is unknown; using Custom SkillGain ({SkillGain.Value}).");
                    }
                    return SkillGain.Value;
            }
        }

        /// <summary>Multiplier for one material: 0 if blocked, else its MaterialMultipliers entry (default 1).</summary>
        public static float MaterialMultiplier(string prefabName)
        {
            if (_blockedMaterials == null)
                _blockedMaterials = new HashSet<string>(SplitList(BlockedMaterials.Value), StringComparer.OrdinalIgnoreCase);
            if (_blockedMaterials.Contains(prefabName)) return 0f;

            if (_materialMultipliers == null)
            {
                _materialMultipliers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
                foreach (string pair in SplitPairs(MaterialMultipliers.Value))
                {
                    string[] kv = pair.Split(':');
                    if (kv.Length == 2 && TryParseDecimal(kv[1], out float m))
                        _materialMultipliers[kv[0].Trim()] = Math.Max(0f, m);
                    else
                        RecyclePlugin.Log.LogWarning($"MaterialMultipliers: can't parse '{pair}' (expected Name:0.5; Name:0,5 is also accepted)");
                }
            }
            return _materialMultipliers.TryGetValue(prefabName, out float mult) ? mult : 1f;
        }

        /// <summary>Parsed "min-max" range of an outcome, clamped to 0-1; falls back to the default on a typo.</summary>
        public static (float min, float max) Range(ConfigEntry<string> entry)
        {
            if (_ranges.TryGetValue(entry, out var r)) return r;
            string[] parts = entry.Value.Split('-');
            if (parts.Length == 2 && TryParseDecimal(parts[0], out float a) && TryParseDecimal(parts[1], out float b))
            {
                a = UnityEngine.Mathf.Clamp01(a);
                b = UnityEngine.Mathf.Clamp01(b);
                r = (Math.Min(a, b), Math.Max(a, b));
            }
            else
            {
                RecyclePlugin.Log.LogWarning($"{entry.Definition.Key}: can't parse '{entry.Value}', using default {entry.DefaultValue}");
                string[] d = ((string)entry.DefaultValue).Split('-');
                TryParseDecimal(d[0], out float defaultA);
                TryParseDecimal(d[1], out float defaultB);
                r = (UnityEngine.Mathf.Clamp01(Math.Min(defaultA, defaultB)), UnityEngine.Mathf.Clamp01(Math.Max(defaultA, defaultB)));
            }
            _ranges[entry] = r;
            return r;
        }

        public static bool IsAllowedType(ItemDrop.ItemData.ItemType type)
        {
            if (_allowedTypes == null)
            {
                _allowedTypes = new HashSet<ItemDrop.ItemData.ItemType>();
                foreach (string raw in AllowedItemTypes.Value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (Enum.TryParse(raw.Trim(), true, out ItemDrop.ItemData.ItemType parsed)
                        && Enum.IsDefined(typeof(ItemDrop.ItemData.ItemType), parsed))
                        _allowedTypes.Add(parsed);
                    else
                        RecyclePlugin.Log.LogWarning($"AllowedItemTypes: '{raw.Trim()}' is not a defined ItemType and was ignored.");
                }
            }
            return _allowedTypes.Contains(type);
        }
    }
}
