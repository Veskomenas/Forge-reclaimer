using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Jotunn.Managers;

namespace Helgi.ForgeReclaimer
{
    /// <summary>
    /// Loads Translations/&lt;Language&gt;.json (embedded in the DLL) into Jotunn — one file per language Valheim ships.
    /// File names must match Valheim's language names exactly (e.g. Portuguese_Brazilian, Chinese_Trad).
    /// Missing languages/keys fall back to English.
    /// </summary>
    internal static class RecycleLocalization
    {
        private const string Prefix = "Translations.";

        public static void Register()
        {
            var loc = LocalizationManager.Instance.GetLocalization();
            Assembly asm = typeof(RecycleLocalization).Assembly;
            int count = 0;
            foreach (string resource in asm.GetManifestResourceNames())
            {
                if (!resource.StartsWith(Prefix) || !resource.EndsWith(".json")) continue;
                string language = resource.Substring(Prefix.Length, resource.Length - Prefix.Length - ".json".Length);
                using (Stream stream = asm.GetManifestResourceStream(resource))
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    loc.AddJsonFile(language, json);
                    // Jotunn's Skills panel looks up its display name as skill_<numeric type>, not $hfr_skill.
                    // Add that language-specific key as well so switching language in-game updates the skill name.
                    Match skillName = Regex.Match(json, "\\\"hfr_skill\\\"\\s*:\\s*\\\"(?<value>(?:\\\\.|[^\\\"])*)\\\"");
                    if (skillName.Success)
                        loc.AddTranslation(language, $"skill_{(int)RecycleSkill.Type}", Regex.Unescape(skillName.Groups["value"].Value));
                }
                count++;
            }
            RecyclePlugin.Log.LogInfo($"Loaded {count} translations");
        }
    }
}
