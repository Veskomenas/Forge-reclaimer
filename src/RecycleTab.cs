using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Helgi.ForgeReclaimer
{
    /// <summary>
    /// UI side: a third "Recycle" tab in the vanilla crafting panel, built by cloning the Upgrade tab.
    /// Vanilla decides Craft vs Upgrade from which tab button is NOT interactable; while our tab is active
    /// both vanilla tabs are interactable, so every vanilla method that branches on the tab is intercepted here.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGui))]
    internal static class RecycleTab
    {
        private static Button _tab;
        private static bool _active;
        /// <summary>A recycle was started; survives tab switches so the craft timer can't turn into an upgrade.</summary>
        private static bool _recycling;

        // ---------- tab creation / switching ----------

        [HarmonyPostfix, HarmonyPatch("Awake")]
        private static void CreateTab(InventoryGui __instance)
        {
            Button upgrade = __instance.m_tabUpgrade;
            GameObject go = Object.Instantiate(upgrade.gameObject, upgrade.transform.parent);
            go.name = "TabRecycle";
            _active = false;   // statics survive logout; a new InventoryGui starts clean
            _recycling = false;

            // Place it after Upgrade, using the same spacing as Craft -> Upgrade.
            var craftRt = (RectTransform)__instance.m_tabCraft.transform;
            var upRt = (RectTransform)upgrade.transform;
            ((RectTransform)go.transform).anchoredPosition = upRt.anchoredPosition + (upRt.anchoredPosition - craftRt.anchoredPosition);

            // Drop components that would re-localize the label or bind the Upgrade tab's gamepad button.
            foreach (Component c in go.GetComponentsInChildren<Component>(true))
            {
                string type = c.GetType().Name;
                if (type == "Localize" || type == "UIGamePad") Object.Destroy(c);
            }

            _tab = go.GetComponent<Button>();
            _tab.onClick = new Button.ButtonClickedEvent();
            _tab.onClick.AddListener(() => Activate(__instance));
            _tab.interactable = true;
            _styleSource = __instance.m_tabCraft.GetComponentInChildren<TMP_Text>(true);
            SetLabel();
        }

        private static TMP_Text _styleSource;

        /// <summary>Label styled exactly like the vanilla CRAFT tab (font, size, style) and upper-cased like it.</summary>
        private static void SetLabel()
        {
            if (_tab == null) return;
            // Turkish needs its own casing rules (i -> İ); every other Valheim language upper-cases fine invariantly.
            var culture = Localization.instance.GetSelectedLanguage() == "Turkish"
                ? new System.Globalization.CultureInfo("tr-TR")
                : System.Globalization.CultureInfo.InvariantCulture;
            string text = Localization.instance.Localize("$hfr_tab").ToUpper(culture);
            foreach (TMP_Text t in _tab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (_styleSource != null)
                {
                    t.font = _styleSource.font;
                    t.fontSharedMaterial = _styleSource.fontSharedMaterial;
                    t.fontSize = _styleSource.fontSize;
                    t.fontStyle = _styleSource.fontStyle;
                    t.enableAutoSizing = _styleSource.enableAutoSizing;
                    t.characterSpacing = _styleSource.characterSpacing;
                }
                t.text = text;
            }
        }

        private static void Activate(InventoryGui gui)
        {
            gui.SetActiveGroup(gui.m_uiGroups[3]);
            _active = true;
            gui.m_tabCraft.interactable = true;
            gui.m_tabUpgrade.interactable = true;
            _tab.interactable = false;
            SetLabel();
            gui.UpdateCraftingPanel();
        }

        private static void Deactivate()
        {
            _active = false;
            if (_tab != null) _tab.interactable = true;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.OnTabCraftPressed))]
        private static void OnCraftTab() => Deactivate();

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.OnTabUpgradePressed))]
        private static void OnUpgradeTab() => Deactivate();

        [HarmonyPostfix, HarmonyPatch(nameof(InventoryGui.Hide))]
        private static void OnHide(InventoryGui __instance)
        {
            if (_active)
            {
                // Leave the panel on the Craft tab, as vanilla would.
                __instance.m_tabCraft.interactable = false;
                __instance.m_tabUpgrade.interactable = true;
            }
            Deactivate();
            _recycling = false;
        }

        /// <summary>Only offer the tab at a real crafting station with a craft tab (not upgraders, not hand crafting).</summary>
        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.UpdateCraftingPanel))]
        private static void UpdateTabVisibility(InventoryGui __instance)
        {
            if (_tab == null) return;
            CraftingStation station = Player.m_localPlayer ? Player.m_localPlayer.GetCurrentCraftingStation() : null;
            bool show = RecycleConfig.Enabled.Value && station != null && station.m_hasCraftTab && !station.m_upgrader;
            _tab.gameObject.SetActive(show);
            if (!show && _active)
            {
                __instance.m_tabCraft.interactable = false;
                __instance.m_tabUpgrade.interactable = true;
                Deactivate();
            }
        }

        // ---------- recipe list ----------

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.UpdateRecipeList))]
        private static bool BuildRecycleList(InventoryGui __instance)
        {
            if (!_active) return true;

            foreach (InventoryGui.RecipeDataPair pair in __instance.m_availableRecipes)
                Object.Destroy(pair.InterfaceElement);
            __instance.m_availableRecipes.Clear();

            Player player = Player.m_localPlayer;
            CraftingStation station = player.GetCurrentCraftingStation();
            var entries = new List<(Recipe recipe, ItemDrop.ItemData item, bool can)>();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                Recipe recipe = Recycler.GetRecipe(item, station);
                if (recipe == null) continue;
                entries.Add((recipe, item, !Recycler.IsInUse(player, item) && Recycler.StationLevelOk(recipe, item, station)));
            }
            foreach (var e in entries.OrderByDescending(e => e.can)
                         .ThenBy(e => Localization.instance.Localize(e.item.m_shared.m_name))
                         .ThenBy(e => e.item.m_quality))
                __instance.AddRecipeToList(player, e.recipe, e.item, e.can);

            float height = Mathf.Max(__instance.m_recipeListBaseSize, __instance.m_availableRecipes.Count * __instance.m_recipeListSpace);
            __instance.m_recipeListRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            return false;
        }

        // ---------- detail panel ----------

        [HarmonyPostfix, HarmonyPatch(nameof(InventoryGui.UpdateRecipe))]
        private static void ShowRecycleDetails(InventoryGui __instance, Player player)
        {
            if (!_active) return;
            InventoryGui.RecipeDataPair sel = __instance.m_selectedRecipe;
            if (sel.Recipe == null || sel.ItemData == null)
            {
                __instance.m_craftButton.interactable = false;
                return;
            }

            CraftingStation station = player.GetCurrentCraftingStation();
            ItemDrop.ItemData item = sel.ItemData;
            Localization loc = Localization.instance;

            __instance.m_itemCraftType.gameObject.SetActive(true);
            __instance.m_itemCraftType.text = loc.Localize("$hfr_crafttype", loc.Localize(item.m_shared.m_name), item.m_quality.ToString());
            __instance.m_variantButton.gameObject.SetActive(false);

            int[] pct = Recycler.GetPercentages(Recycler.GetWeights(player));
            float durability = Recycler.DurabilityFactor(item);
            string odds = "\n\n";
            if (RecycleConfig.EnableRecyclingSkill.Value)
            {
                int skillLevel = Mathf.FloorToInt(player.GetSkillLevel(RecycleSkill.Type));
                odds += loc.Localize("$hfr_skill_line", skillLevel.ToString()) + "\n";
            }
            odds += loc.Localize("$hfr_odds", pct.Select(p => p.ToString()).ToArray());
            if (durability < 0.999f) odds += loc.Localize("$hfr_worn", Mathf.RoundToInt(durability * 100f).ToString());
            __instance.m_recipeDecription.text = loc.Localize(
                ItemDrop.ItemData.GetTooltip(item, item.m_quality, false, Game.m_worldLevel)) + odds;

            // Requirement slots show what can come back: 0 - max for each material.
            List<Recycler.Yield> yields = Recycler.GetYields(sel.Recipe, item);
            GameObject[] slots = __instance.m_recipeRequirementList;
            int pages = Mathf.Max(1, Mathf.CeilToInt((float)yields.Count / slots.Length));
            int firstYield = ((int)Time.fixedTime % pages) * slots.Length;
            for (int i = 0; i < slots.Length; i++)
            {
                Transform root = slots[i].transform;
                int yieldIndex = firstYield + i;
                if (yieldIndex >= yields.Count)
                {
                    InventoryGui.HideRequirement(root);
                    continue;
                }
                ItemDrop.ItemData res = yields[yieldIndex].Resource.m_itemData;
                Image icon = root.Find("res_icon").GetComponent<Image>();
                TMP_Text name = root.Find("res_name").GetComponent<TMP_Text>();
                TMP_Text amount = root.Find("res_amount").GetComponent<TMP_Text>();
                icon.gameObject.SetActive(true);
                name.gameObject.SetActive(true);
                amount.gameObject.SetActive(true);
                icon.sprite = res.GetIcon();
                icon.color = Color.white;
                name.text = loc.Localize(res.m_shared.m_name);
                amount.text = $"0-{yields[yieldIndex].Max}";
                amount.color = Color.white;
                root.GetComponent<UITooltip>().m_text = name.text;
            }

            int needLevel = sel.Recipe.GetRequiredStationLevel(item.m_quality);
            __instance.m_minStationLevelIcon.gameObject.SetActive(true);
            __instance.m_minStationLevelText.text = needLevel.ToString();
            __instance.m_minStationLevelText.color = station != null && station.GetLevel() >= needLevel
                ? __instance.m_minStationLevelBasecolor : Color.red;

            bool usable = station != null && station.CheckUsable(player, false);
            bool inUse = Recycler.IsInUse(player, item);
            bool can = usable && !inUse && Recycler.StationLevelOk(sel.Recipe, item, station)
                       && player.GetInventory().ContainsItem(item);
            __instance.m_craftButton.interactable = can;
            __instance.m_craftButton.GetComponentInChildren<TMP_Text>().text = loc.Localize("$hfr_button");
            __instance.m_craftButton.GetComponent<UITooltip>().m_text =
                inUse ? loc.Localize("$hfr_equipped") : !usable ? loc.Localize("$msg_missingstation") : "";
        }

        // ---------- the actual recycle ----------

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.OnCraftPressed))]
        private static bool StartRecycle(InventoryGui __instance)
        {
            if (!_active)
            {
                _recycling = false;
                return true;
            }
            InventoryGui.RecipeDataPair sel = __instance.m_selectedRecipe;
            if (sel.Recipe == null || sel.ItemData == null) return false;

            __instance.SetActiveGroup(__instance.m_uiGroups[3]);
            __instance.m_craftRecipe = sel.Recipe;
            __instance.m_craftUpgradeItem = sel.ItemData;
            __instance.m_multiCrafting = false;
            __instance.m_craftTimer = 0f;
            _recycling = true;

            CraftingStation station = Player.m_localPlayer.GetCurrentCraftingStation();
            if (station != null)
                station.m_craftItemEffects.Create(Player.m_localPlayer.transform.position, Quaternion.identity);
            return false;
        }

        [HarmonyPrefix, HarmonyPatch(nameof(InventoryGui.DoCrafting))]
        private static bool FinishRecycle(InventoryGui __instance, Player player)
        {
            if (!_recycling) return true;
            _recycling = false;

            CraftingStation station = player.GetCurrentCraftingStation();
            ItemDrop.ItemData item = __instance.m_craftUpgradeItem;
            __instance.m_craftRecipe = null;
            __instance.m_craftUpgradeItem = null;
            if (station != null && item != null)
                Recycler.Recycle(player, station, item);
            __instance.UpdateCraftingPanel();
            return false;
        }
    }
}
