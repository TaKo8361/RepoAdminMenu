using MenuLib.MonoBehaviors;
using MenuLib;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using System.Xml.Linq;
using RepoAdminMenu.Utils;

namespace RepoAdminMenu {
    internal class Menu {

        private static string selectedPlayerId;

        private static REPOPopupPage currentMenu;

        private static string currentMenuStr = "";

        private static Dictionary<string, Action> menus = new Dictionary<string, Action>();

        private static MethodInfo removeAllPagesMethod = AccessTools.Method(typeof(MenuManager), "PageCloseAll");

        private static Dictionary<string, Action<string, REPOPopupPage>> menuPreCallbacks = new Dictionary<string, Action<string, REPOPopupPage>>();
        private static Dictionary<string, Action<string, REPOPopupPage>> menuPostCallbacks = new Dictionary<string, Action<string, REPOPopupPage>>();

        public static void Init() {
            registerMenu("玩家列表", openPlayerListMenu);
            registerMenu("玩家資訊", openPlayerMenu);
            registerMenu("玩家升級", openPlayerUpgrades);
            registerMenu("生成選單", openSpawnMenu);
            registerMenu("生成道具", openSpawnItemsMenu);
            registerMenu("生成敵人", openSpawnEnemyMenu);
            registerMenu("生成貴重物品(全部)", openSpawnValuablesMenu);
            registerMenu("生成極小型貴重物品", openSpawnValuablesTinyMenu);
            registerMenu("生成小型貴重物品", openSpawnValuablesSmallMenu);
            registerMenu("生成中型貴重物品", openSpawnValuablesMediumMenu);
            registerMenu("生成大型貴重物品", openSpawnValuablesBigMenu);
            registerMenu("生成寬型貴重物品", openSpawnValuablesWideMenu);
            registerMenu("生成高型貴重物品", openSpawnValuablesTallMenu);
            registerMenu("生成特高型貴重物品", openSpawnValuablesVeryTallMenu);
            registerMenu("地圖", openMapMenu);
            registerMenu("關卡選擇器", openLevelSelectorMenu);
            registerMenu("設定", openSettingsMenu);
            registerMenu("製作人員名單", openCreditsMenu);
            registerMenu("主選單", openMainMenu);
        }

        public static void registerMenu(string name, Action openAction) {
            menus.Add(name, openAction);
        }

        public static void addMenuPreCallback(string mod_name, Action<string, REPOPopupPage> action) {
            menuPreCallbacks.Add(mod_name, action);
        }

        public static void removeMenuPreCallback(string mod_name) {
            menuPreCallbacks.Remove(mod_name);
        }

        public static void addMenuPostCallback(string mod_name, Action<string, REPOPopupPage> action) {
            menuPostCallbacks.Add(mod_name, action);
        }

        public static void removeMenuPostCallback(string mod_name) {
            menuPostCallbacks.Remove(mod_name);
        }

        public static void toggleMenu() {
            if (menus.Count < 1)
                Init();


            if (SemiFunc.MenuLevel()) {
                RepoAdminMenu.mls.LogInfo("只能在遊戲中開啟 Repo 管理選單！");
                return;
            }

            if (!SemiFunc.IsMasterClientOrSingleplayer()) {
                RepoAdminMenu.mls.LogInfo("請在單人模式或擔任主機時使用 Repo 管理選單。");
                return;
            }

            if (currentMenu != null && currentMenu.isActiveAndEnabled) {
                closePage(currentMenu);
            } else {
                RepoAdminMenu.mls.LogInfo("選單開啟中…");
                menus.GetValueOrDefault(currentMenuStr, openMainMenu).Invoke();
            }
        }

        public static void closePage(REPOPopupPage page) {
            RepoAdminMenu.mls.LogInfo("關閉中： " + page.menuPage.name);
            removeAllPagesMethod.Invoke(MenuManager.instance, new object[] { });
            page.ClosePage(true);
            MenuManager.instance.PageRemove(page.menuPage);
            if (currentMenu == page)
                currentMenu = null;
        }

        public static void navigate(REPOPopupPage page, string menu) {
            closePage(page);
            menus.GetValueOrDefault(menu, () => { RepoAdminMenu.mls.LogError("選單不存在： " + menu);  }).Invoke();
        }

        public static void openPage(REPOPopupPage page, string name) {
            MenuManager.instance.StartCoroutine(openPageInternal(page, name)); 
            
        }


        private static System.Collections.IEnumerator openPageInternal(REPOPopupPage page, string name) {
            yield return new WaitForSeconds(0.050f);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPostCallbacks) {
                RepoAdminMenu.mls.LogInfo("正在執行 post-callback，處理菜單 '" + entry.Key + "' 在菜單 '" + name + "'");
                entry.Value.Invoke(name, page);
            }
            RepoAdminMenu.mls.LogInfo("開啟頁面: " + page.menuPage.name);
            removeAllPagesMethod.Invoke(MenuManager.instance, new object[] { });
            page.OpenPage(false);
            currentMenu = page;
            currentMenuStr = name;
        }

        public static REPOPopupPage createMenu(string title, string currentMenu, string parentMenu) {
            var elem = MenuAPI.CreateREPOPopupPage(title, REPOPopupPage.PresetSide.Left, false, true);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPreCallbacks) {
                RepoAdminMenu.mls.LogInfo("正在執行 pre-callback，處理菜單 '" + entry.Key + "' 在菜單 '" + currentMenu + "'");
                entry.Value.Invoke(currentMenu, elem);
            }
            addBackButton(elem, parentMenu);
            return elem;
        }

        public static REPOPopupPage createMainMenu(string title) {
            var elem = MenuAPI.CreateREPOPopupPage(title, REPOPopupPage.PresetSide.Left, false, true);
            foreach (KeyValuePair<string, Action<string, REPOPopupPage>> entry in menuPreCallbacks) {
                RepoAdminMenu.mls.LogInfo("正在執行 pre-callback，處理菜單 '" + entry.Key + "' 在菜單 'mainmenu'");
                entry.Value.Invoke("mainmenu", elem);
            }
            addCloseButton(elem);
            return elem;
        }

        public static void addButton(REPOPopupPage parent, string text, Action action) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOButton(text, action, scrollView);
                return elem.rectTransform;
            });
        }

        public static void addBackButton(REPOPopupPage parent, string parentMenu) {
            parent.AddElement(transform => {
                MenuAPI.CreateREPOButton("← 返回", () => { closePage(parent); menus.GetValueOrDefault(parentMenu, () => { RepoAdminMenu.mls.LogError("Menu not found: " + parentMenu); }).Invoke(); }, transform, new Vector2(250, 20));
            });
        }

        public static void addCloseButton(REPOPopupPage parent) {
            parent.AddElement(transform => {
                MenuAPI.CreateREPOButton("取消", () => closePage(parent), transform, new Vector2(270, 20));
            });
        }


        public static void addToggle(REPOPopupPage parent, string text, System.Action<bool> action, string off, string on, bool defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOToggle(text, action, scrollView, Vector2.zero, off, on, defaultValue);
                return elem.rectTransform;
            });
        }
        public static void addLabel(REPOPopupPage parent, string text) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOLabel(text, scrollView);
                return elem.rectTransform;
            });
        }

        public static void addIntSlider(REPOPopupPage parent, string text, string description, System.Action<int> action, int min, int max, int defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, Vector2.zero, min, max, defaultValue, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }
        public static void addFloatSlider(REPOPopupPage parent, string text, string description, System.Action<float> action, float min, float max, int precision, float defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, Vector2.zero, min, max, precision, defaultValue, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }

        public static void addStringSlider(REPOPopupPage parent, string text, string description, System.Action<string> action, string[] options, string defaultValue) {
            parent.AddElementToScrollView(scrollView => {
                var elem = MenuAPI.CreateREPOSlider(text, description, action, scrollView, options, defaultValue, Vector2.zero, "", "", REPOSlider.BarBehavior.UpdateWithValue);
                return elem.rectTransform;
            });
        }

        public static void openMainMenu() {
            var mainMenu = createMainMenu("R.E.P.O. 管理員菜單");

            addButton(mainMenu, "玩家", () => { navigate(mainMenu, "playerList"); });
            addButton(mainMenu, "生成", () => { navigate(mainMenu, "spawn"); });
            addButton(mainMenu, "地圖", () => { navigate(mainMenu, "map"); });
            addButton(mainMenu, "設定", () => { navigate(mainMenu, "settings"); });
            addButton(mainMenu, "致謝", () => { navigate(mainMenu, "credits"); });
            addButton(mainMenu, "錯誤回報", () => { Application.OpenURL("https://github.com/proferabg/RepoAdminMenu/issues"); });

            openPage(mainMenu, "mainmenu");
        }

        private static void openPlayerListMenu() {
            var playersMenu = createMenu("R.A.M. - 玩家", "playerList", "mainmenu");

            foreach (PlayerAvatar player in SemiFunc.PlayerGetAll()) {
                addButton(playersMenu, SemiFunc.PlayerGetName(player), () => { selectedPlayerId = SemiFunc.PlayerGetSteamID(player); navigate(playersMenu, "player"); });
            }

            openPage(playersMenu, "playerList");
        }

        private static void openPlayerMenu() {
            if (selectedPlayerId == null) {
                RepoAdminMenu.mls.LogInfo("未選擇玩家");
                openPlayerListMenu();
                return;
            }

            PlayerAvatar avatar = SemiFunc.PlayerGetFromSteamID(selectedPlayerId);

            var playerMenu = createMenu("R.A.M. - " + SemiFunc.PlayerGetName(avatar), "player", "playerList");

            addToggle(playerMenu, "上帝模式", (b) => { PlayerUtil.toggleGodMode(b, avatar); }, "Off", "On", !PlayerUtil.isGod(avatar));
            addToggle(playerMenu, "無敵模式", (b) => { PlayerUtil.toggleNoDeath(b, avatar); }, "Off", "On", !PlayerUtil.isNoDeath(avatar));
            addToggle(playerMenu, "怪物看不見", (b) => { PlayerUtil.toggleNoTarget(b, avatar); }, "Off", "On", !PlayerUtil.isNoTarget(avatar));
            addToggle(playerMenu, "不會跌倒", (b) => { PlayerUtil.toggleNoTumble(b, avatar); }, "Off", "On", !PlayerUtil.isNoTumble(avatar));
            addToggle(playerMenu, "無限體力", (b) => { PlayerUtil.toggleInfiniteStamina(b, avatar); }, "Off", "On", !PlayerUtil.isInfiniteStamina(avatar));
            addToggle(playerMenu, "強制翻滾", (b) => { PlayerUtil.toggleForceTumble(b, avatar); }, "Off", "On", !PlayerUtil.isForceTumble(avatar));
            addButton(playerMenu, "升級", () => { navigate(playerMenu, "playerUpgrade"); });
            addButton(playerMenu, "治療", () => { PlayerUtil.healPlayer(avatar); });
            addButton(playerMenu, "自殺", () => { PlayerUtil.killPlayer(avatar); });
            addButton(playerMenu, "復活", () => { PlayerUtil.revivePlayer(avatar); });
            addButton(playerMenu, "傳送過去", () => { PlayerUtil.teleportTo(avatar); });
            addButton(playerMenu, "傳送過來", () => { PlayerUtil.summon(avatar); });
            addButton(playerMenu, "給予輸家王冠", () => { PlayerUtil.giveCrown(avatar); });

            openPage(playerMenu, "player");
        }

        private static void openPlayerUpgrades() {
            if (selectedPlayerId == null) {
                RepoAdminMenu.mls.LogInfo("未選擇玩家");
                openPlayerListMenu();
                return;
            }

            PlayerAvatar avatar = SemiFunc.PlayerGetFromSteamID(selectedPlayerId);

            var upgradesMenu = createMenu("R.A.M. - " + SemiFunc.PlayerGetName(avatar) + " - 升級", "playerUpgrade", "player");

            addIntSlider(upgradesMenu, "健康", "", (v) => { PlayerUtil.upgrade("health", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("health", avatar));
            addIntSlider(upgradesMenu, "跳躍", "", (v) => { PlayerUtil.upgrade("jump", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("jump", avatar));
            addIntSlider(upgradesMenu, "發射", "", (v) => { PlayerUtil.upgrade("launch", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("launch", avatar));
            addIntSlider(upgradesMenu, "地圖玩家數量", "", (v) => { PlayerUtil.upgrade("playercount", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("playercount", avatar));
            addIntSlider(upgradesMenu, "範圍", "", (v) => { PlayerUtil.upgrade("range", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("range", avatar));
            addIntSlider(upgradesMenu, "速度", "", (v) => { PlayerUtil.upgrade("speed", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("speed", avatar));
            addIntSlider(upgradesMenu, "耐力", "", (v) => { PlayerUtil.upgrade("stamina", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("stamina", avatar));
            addIntSlider(upgradesMenu, "力量", "", (v) => { PlayerUtil.upgrade("strength", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("strength", avatar));
            addIntSlider(upgradesMenu, "投擲", "", (v) => { PlayerUtil.upgrade("throw", avatar, v); }, 0, Configuration.MaxUpgradeLevel.Value, PlayerUtil.getUpgradeLevel("throw", avatar));

            openPage(upgradesMenu, "playerUpgrade");
        }

        private static void openSpawnMenu() {
            var spawnerMenu = createMenu("R.A.M. - 生成", "spawn", "mainmenu");

            addButton(spawnerMenu, "道具", () => { navigate(spawnerMenu, "spawnItem"); });
            addButton(spawnerMenu, "價值物品", () => { navigate(spawnerMenu, "spawnValuable"); });
            addButton(spawnerMenu, "敵人", () => { navigate(spawnerMenu, "spawnEnemy"); });

            openPage(spawnerMenu, "spawn");
        }

        private static void openSpawnItemsMenu() {
            var itemsMenu = createMenu("R.A.M. - 生成 - 道具", "spawnItem", "spawn");

            foreach (KeyValuePair<string, Item> entry in ItemUtil.getItems()) {
                addButton(itemsMenu, entry.Key, () => ItemUtil.spawnItem(entry.Value));
            }

            openPage(itemsMenu, "spawnItem");
        }

        private static void openSpawnValuablesMenu() {
            var valuablesMenu = createMenu("R.A.M. - 生成 - 價值物品", "spawnValuable", "spawn");


            addButton(valuablesMenu, "小型", () => { navigate(valuablesMenu, "spawnValuableTiny"); });
            addButton(valuablesMenu, "中型", () => { navigate(valuablesMenu, "spawnValuableSmall"); });
            addButton(valuablesMenu, "中等", () => { navigate(valuablesMenu, "spawnValuableMedium"); });
            addButton(valuablesMenu, "大型", () => { navigate(valuablesMenu, "spawnValuableBig"); });
            addButton(valuablesMenu, "寬型", () => { navigate(valuablesMenu, "spawnValuableWide"); });
            addButton(valuablesMenu, "高型", () => { navigate(valuablesMenu, "spawnValuableTall"); });
            addButton(valuablesMenu, "超高型", () => { navigate(valuablesMenu, "spawnValuableVeryTall"); });

            openPage(valuablesMenu, "spawnValuable");
        }

        private static void openSpawnValuablesTinyMenu() {
            var valuablesTinyMenu = createMenu("R.A.M. - 小型價值物品", "spawnValuableTiny", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getTinyValuables()) {
                addButton(valuablesTinyMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesTinyMenu, "spawnValuableTiny");
        }

        private static void openSpawnValuablesSmallMenu() {
            var valuablesSmallMenu = createMenu("R.A.M. - 中型價值物品", "spawnValuableSmall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getSmallValuables()) {
                addButton(valuablesSmallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesSmallMenu, "spawnValuableSmall");
        }

        private static void openSpawnValuablesMediumMenu() {
            var valuablesMediumMenu = createMenu("R.A.M. - 中等價值物品", "spawnValuableMedium", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getMediumValuables()) {
                addButton(valuablesMediumMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesMediumMenu, "spawnValuableMedium");
        }

        private static void openSpawnValuablesBigMenu() {
            var valuablesBigMenu = createMenu("R.A.M. - 大型價值物品", "spawnValuableBig", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getBigValuables()) {
                addButton(valuablesBigMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesBigMenu, "spawnValuableBig");
        }

        private static void openSpawnValuablesWideMenu() {
            var valuablesWideMenu = createMenu("R.A.M. - 寬型價值物品", "spawnValuableWide", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getWideValuables()) {
                addButton(valuablesWideMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesWideMenu, "spawnValuableWide");
        }

        private static void openSpawnValuablesTallMenu() {
            var valuablesTallMenu = createMenu("R.A.M. - 高型價值物品", "spawnValuableTall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getTallValuables()) {
                addButton(valuablesTallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesTallMenu, "spawnValuableTall");
        }

        private static void openSpawnValuablesVeryTallMenu() {
            var valuablesVeryTallMenu = createMenu("R.A.M. - 超高型價值物品", "spawnValuableVeryTall", "spawnValuable");

            foreach (KeyValuePair<string, GameObject> entry in ValuableUtil.getVeryTallValuables()) {
                addButton(valuablesVeryTallMenu, entry.Key, () => ValuableUtil.spawnValuable(entry.Value));
            }

            openPage(valuablesVeryTallMenu, "spawnValuableVeryTall");
        }

        private static void openSpawnEnemyMenu() {
            var spawnerMenu = createMenu("R.A.M. - 生成 - 敵人", "spawnEnemy", "spawn");

            foreach(string enemy in EnemyUtil.getEnemies().Keys) {
                addButton(spawnerMenu, enemy, () => EnemyUtil.spawnEnemy(enemy));
            }

            foreach (string enemy in EnemyUtil.getModEnemies().Keys) {
                addButton(spawnerMenu, "(Mod) " + enemy, () => EnemyUtil.spawnEnemy(enemy));
            }

            openPage(spawnerMenu, "spawnEnemy");
        }

        private static void openMapMenu() {
            var mapMenu = createMenu("R.A.M. - 地圖設定", "map", "mainmenu");

            foreach(KeyValuePair<string, Level> entry in MapUtil.getMaps()) {
                addToggle(mapMenu, entry.Key, (b) => { MapUtil.setMapEnabled(entry.Value, !b); }, "Off", "On", !MapUtil.isMapEnabled(entry.Value));
            }

            addButton(mapMenu, "關卡選擇器", () => { navigate(mapMenu, "levelSelector"); });
            addButton(mapMenu, "觸發撤離點", ExtractionPointUtil.discoverNext);
            addButton(mapMenu, "完成撤離點", ExtractionPointUtil.complete);

            openPage(mapMenu, "map");
        }

        private static void openLevelSelectorMenu() {
            var mapMenu = createMenu("R.A.M. - 關卡選擇器", "levelSelector", "map");

            foreach (KeyValuePair<string, Level> entry in MapUtil.getMaps()) {
                addButton(mapMenu, entry.Key, () => { MapUtil.changeLevel(entry.Value); });
            }
            addButton(mapMenu, "競技場", () => { MapUtil.changeLevel(RunManager.instance.levelArena); });
            addButton(mapMenu, "大廳", () => { MapUtil.changeLevel(RunManager.instance.levelLobby); });
            addButton(mapMenu, "商店", () => { MapUtil.changeLevel(RunManager.instance.levelShop); });

            openPage(mapMenu, "levelSelector");
        }

        private static void openSettingsMenu() {
            var settingsMenu = createMenu("R.A.M. - Settings", "settings", "mainmenu");

            addIntSlider(settingsMenu, "關卡調整", "", (v) => { RunManager.instance.levelsCompleted = v; }, 1, 9999, RunManager.instance.levelsCompleted);
            addToggle(settingsMenu, "無限金錢", (b) => { Settings.infiniteMoney = !b; }, "Off", "On", !Settings.infiniteMoney);
            addToggle(settingsMenu, "無破壞", (b) => { Settings.noBreak = !b; }, "Off", "On", !Settings.noBreak);
            addToggle(settingsMenu, "無電池/彈藥消耗", (b) => { Settings.noBatteryDrain = !b; }, "Off", "On", !Settings.noBatteryDrain);
            addToggle(settingsMenu, "無陷阱", (b) => { Settings.noTraps = !b; }, "Off", "On", !Settings.noTraps);
            addToggle(settingsMenu, "敵人弱化", (b) => { Settings.weakEnemies = !b; }, "Off", "On", !Settings.weakEnemies);
            addToggle(settingsMenu, "敵人聽不到", (b) => { Settings.deafEnemies = !b; }, "Off", "On", !Settings.deafEnemies);
            addToggle(settingsMenu, "敵人看不見", (b) => { Settings.blindEnemies = !b; }, "Off", "On", !Settings.blindEnemies);
            addToggle(settingsMenu, "爆破錘", (b) => { Settings.boomhammer = !b; }, "Off", "On", !Settings.boomhammer);
            addToggle(settingsMenu, "友善鴨鴨", (b) => { Settings.friendlyDuck = !b; }, "Off", "On", !Settings.friendlyDuck);
            addToggle(settingsMenu, "商店內升級", (b) => { Settings.useShopUpgrades = !b; }, "Off", "On", !Settings.useShopUpgrades);

            openPage(settingsMenu, "settings");
        }

        private static void openCreditsMenu() {
            var creditsMenu = createMenu("R.A.M. - 開發者資訊", "credits", "mainmenu");

            addLabel(creditsMenu, "Repo Admin Menu by");
            addLabel(creditsMenu, "  proferabg  ");
            addLabel(creditsMenu, "");
            addLabel(creditsMenu, "特別感謝!!:");
            addLabel(creditsMenu, " - REPOrium_Team");
            addLabel(creditsMenu, " - nickklmao");
            addLabel(creditsMenu, " - Godji");
            addLabel(creditsMenu, " - Zehs");
            addLabel(creditsMenu, "");
            addLabel(creditsMenu, "Repo Admin Menu © 2025");
            //addLabel(creditsMenu, "繁體中文翻譯 By- TaKo8361");
            //I’m not sure if I can add it :D  ↖ ↑ ↗
            
            openPage(creditsMenu, "credits");
        }

        public static string getSelectedPlayer() {
            return selectedPlayerId;
        }
    }
}
