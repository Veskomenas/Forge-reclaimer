using BepInEx;
using HarmonyLib;
using Jotunn.Utils;

namespace Helgi.ForgeReclaimer
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class RecyclePlugin : BaseUnityPlugin
    {
        public const string Guid = "helgi.forgereclaimer";
        public const string Name = "Helgi's Forge Reclaimer";
        public const string Version = "1.0.1";

        internal static BepInEx.Logging.ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            RecycleConfig.Bind(Config);
            RecycleSkill.Register();
            RecycleLocalization.Register();
            new Harmony(Guid).PatchAll(typeof(RecyclePlugin).Assembly);
            Log.LogInfo($"{Name} {Version} loaded");
        }
    }
}
