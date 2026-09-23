using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Managers;

namespace Helgi.ForgeReclaimer
{
    /// <summary>
    /// The "Recycling" skill (0-100), registered through Jotunn so it lives in the vanilla Skills panel,
    /// levels with the normal skill curve and takes the normal death penalty.
    /// </summary>
    [HarmonyPatch]
    internal static class RecycleSkill
    {
        public const string Identifier = "helgi.forgereclaimer.recycling";
        public static Skills.SkillType Type { get; private set; }
        private static SkillConfig _config;

        public static void Register()
        {
            _config = new SkillConfig
            {
                Identifier = Identifier,
                Name = "$hfr_skill",
                Description = "$hfr_skill_desc",
                IncreaseStep = 1f,
            };
            Type = SkillManager.Instance.AddSkill(_config);
        }

        /// <summary>
        /// No custom art: borrow the vanilla Hammer icon. Jotunn builds the SkillDef when a player's Skills
        /// component wakes up, so the icon only has to be set before the character spawns.
        /// </summary>
        [HarmonyPostfix, HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
        private static void SetIcon(ObjectDB __instance)
        {
            if (_config == null || _config.Icon != null) return;
            var hammer = __instance.GetItemPrefab("Hammer");
            if (hammer != null)
                _config.Icon = hammer.GetComponent<ItemDrop>().m_itemData.GetIcon();
        }
    }
}
