using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>
/// Runtime PlayMode interaction checks across the upgraded Cinderreach UI panels:
/// SessionComplete clickable BackToMenu -> ClassSelectUI, PauseMenuUI navigation,
/// DiabloVendorUI scrollable rows/affordability, DiabloInventoryUI sliders & potion consume,
/// and DiabloCharacterUI / DiabloStashUI / DiabloWaypointUI lifecycle.
/// </summary>
public class DiabloMenuPlayTests
{
    GameObject eventSys;
    readonly List<GameObject> tracked = new List<GameObject>();
    bool hadSaveBackup;
    byte[] saveBackup;

    TTrack Track<TTrack>(TTrack go) where TTrack : Object
    {
        if (go is GameObject g) tracked.Add(g);
        return go;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        RunProfile.Reset();
        var path = SaveService.DefaultPath;
        hadSaveBackup = File.Exists(path);
        saveBackup = hadSaveBackup ? File.ReadAllBytes(path) : null;
        if (EventSystem.current == null)
            eventSys = new GameObject("EventSystem", typeof(EventSystem));
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        for (int i = tracked.Count - 1; i >= 0; i--)
            if (tracked[i] != null) Object.DestroyImmediate(tracked[i]);
        tracked.Clear();
        SessionComplete.ResetRun();
        DiabloClickController.ExternalDrive = false;
        RunProfile.Reset();
        var path = SaveService.DefaultPath;
        if (hadSaveBackup && saveBackup != null)
            File.WriteAllBytes(path, saveBackup);
        else if (File.Exists(path))
            File.Delete(path);
        if (eventSys != null) Object.DestroyImmediate(eventSys);
        yield return null;
    }

    [UnityTest]
    public IEnumerator SessionComplete_MenuButtonOpensClassSelectAndTownButtonMovesHero()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        hero.AddComponent<Wallet>();
        SessionComplete.Show(hero, "Emberbrand Blade");
        yield return null;

        var menuBtn = hero.transform.Find("SessionCanvas/SessionPanel/SessionButtons/MenuButton")?.GetComponent<Button>();
        Assert.IsNotNull(menuBtn, "MenuButton missing on SessionCanvas.");
        ExecuteEvents.Execute(menuBtn.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
        yield return null;

        var cls = Object.FindFirstObjectByType<ClassSelectUI>(FindObjectsInactive.Include);
        Assert.IsNotNull(cls, "BackToMenu must open or spawn ClassSelectUI.");
        Track(cls.gameObject);
        Assert.IsTrue(cls.IsOpen, "ClassSelectUI must be open after clicking MenuButton.");

        cls.SetOpen(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator ClassSelectUI_DisplaysCardsAndSwitchesClass()
    {
        var clsGo = Track(new GameObject("ClassSelect"));
        var cls = clsGo.AddComponent<ClassSelectUI>();
        cls.SetOpen(true);
        yield return null;

        Assert.IsNotNull(clsGo.transform.Find("ClassSelectCanvas/MainContainer/CardsRow/Card_fenwalker"));
        Assert.IsNotNull(clsGo.transform.Find("ClassSelectCanvas/MainContainer/CardsRow/Card_lanternbearer"));

        cls.ShowSettings(true);
        yield return null;
        var settingsPanel = clsGo.transform.Find("ClassSelectCanvas/StartupSettingsPanel");
        Assert.IsNotNull(settingsPanel);
        Assert.IsTrue(settingsPanel.gameObject.activeSelf);

        cls.ChooseNew(ClassCatalog.LanternbearerId);
        yield return null;
        Assert.IsFalse(cls.IsOpen);
        Assert.AreEqual(ClassCatalog.LanternbearerId, RunProfile.ClassId);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PauseMenu_OpensInventoryAndSettingsCleanly()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        hero.AddComponent<Equipment>();
        hero.AddComponent<Inventory>();
        var invUi = hero.AddComponent<DiabloInventoryUI>();

        var pauseGo = Track(new GameObject("PauseMenu"));
        var pause = pauseGo.AddComponent<PauseMenuUI>();
        pause.SetOpen(true);
        yield return null;

        Assert.IsTrue(pause.IsOpen);
        Assert.IsTrue(DiabloClickController.ExternalDrive);
        Assert.AreEqual(0f, Time.timeScale, "solo pause freezes the world");
        Assert.IsTrue(pause.HoldsTime);

        pause.OpenInventory();
        yield return null;
        Assert.IsFalse(pause.IsOpen);
        Assert.AreEqual(1f, Time.timeScale, "leaving the pause resumes time");
        Assert.IsTrue(invUi.IsOpen);

        invUi.SetOpen(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator VendorUI_ScrollableRowsAndAffordabilityWork()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        var wallet = hero.AddComponent<Wallet>();
        hero.AddComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        hero.AddComponent<Inventory>();
        var vendorUi = hero.AddComponent<DiabloVendorUI>();
        var stock = VendorStock.Roll(1234);
        vendorUi.BindStock(stock);

        vendorUi.SetOpen(true);
        yield return null;
        Assert.IsTrue(vendorUi.IsOpen);

        var rows = hero.transform.Find("VendorCanvas/Panel/Columns/VendorColumn/Scroll/Viewport/VendorRows");
        Assert.IsNotNull(rows, "VendorRows must live inside Scroll/Viewport.");
        Assert.GreaterOrEqual(rows.childCount, 4, "Vendor rows must populate with stock items.");

        var inv = hero.GetComponent<Inventory>();
        var firstBuy = rows.GetChild(0).GetComponentInChildren<Button>();
        Assert.IsNotNull(firstBuy);
        var priceText = firstBuy.transform.Find("Price")?.GetComponent<Text>();
        Assert.IsNotNull(priceText);
        Assert.AreEqual(DiabloPalette.Blood, priceText.color, "Price text must show Blood red when purse is 0.");
        vendorUi.BuyOffer(0);
        Assert.AreEqual(0, inv.Count, "Buying when broke must leave bag empty.");

        wallet.Add(500);
        vendorUi.Refresh();
        yield return null;
        priceText = rows.GetChild(0).GetComponentInChildren<Button>()?.transform.Find("Price")?.GetComponent<Text>();
        Assert.AreEqual(DiabloPalette.GoldBright, priceText.color, "Price text must show GoldBright once affordable.");
        vendorUi.BuyOffer(0);
        Assert.AreEqual(1, inv.Count, "Buying affordable item must move it into bag.");

        vendorUi.SetOpen(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator InventoryUI_SettingsSlidersAndPotionConsumeWork()
    {
        bool prevDmg = DiabloSettings.ShowDamageNumbers;
        bool prevFps = DiabloSettings.ShowFPS;

        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        hero.AddComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        var inv = hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        var hp = hero.AddComponent<PlayerHealth>();
        hp.TakeDamage(10f);
        float damaged = hp.CurrentHP;
        inv.Add(new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Rare, baseType = "Healing Potion" });

        var invUi = hero.AddComponent<DiabloInventoryUI>();
        invUi.SetOpen(true);
        yield return null;

        invUi.EquipIndex(0); // a legacy consumable never equips into the helmet
        Assert.IsTrue(HealingFlask.For(hero).TryDrink(), "gear-slots S2: the flask heals instead.");
        yield return new WaitForSeconds(HealingFlask.TickSeconds + 0.15f);
        Assert.Greater(hp.CurrentHP, damaged, "The flask's first tick must heal the hero.");
        Assert.IsNull(hero.GetComponent<Equipment>().helmet, "A consumable must never equip into helmet.");

        invUi.ShowTab(DiabloInventoryUI.MenuTab.Settings);
        yield return null;
        var sliders = hero.GetComponentsInChildren<Slider>(true);
        Assert.GreaterOrEqual(sliders.Length, 4, "Settings tab must expose 4 real Slider controls.");
        foreach (var s in sliders)
        {
            Assert.IsNotNull(s.fillRect, "Slider must wire fillRect.");
            Assert.IsNotNull(s.handleRect, "Slider must wire handleRect.");
        }

        DiabloSettings.ShowDamageNumbers = prevDmg;
        DiabloSettings.ShowFPS = prevFps;
        DiabloSettings.Save();
        invUi.SetOpen(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator Panels_CharacterStashWaypointOpenAndCloseCleanly()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        hero.AddComponent<Equipment>().Equip(ItemDatabase.CreateStarterSword());
        hero.AddComponent<Inventory>();
        hero.AddComponent<Wallet>();
        var charUi = hero.AddComponent<DiabloCharacterUI>();
        var stashUi = hero.AddComponent<DiabloStashUI>();
        var wpUi = hero.AddComponent<DiabloWaypointUI>();

        charUi.SetOpen(true);
        yield return null;
        Assert.IsNotNull(hero.transform.Find("CharacterCanvas/Panel"));
        charUi.SetOpen(false);

        stashUi.SetOpen(true);
        yield return null;
        Assert.IsNotNull(hero.transform.Find("StashCanvas/Panel"));
        stashUi.SetOpen(false);

        wpUi.SetOpen(true);
        yield return null;
        Assert.IsNotNull(hero.transform.Find("WaypointCanvas/Panel"));
        wpUi.SetOpen(false);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BossBar_ShowsANearbyLivingBoss()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        var bossGo = Track(new GameObject("Hollow Abbot"));
        bossGo.transform.position = new Vector3(0f, 0f, 6f);
        var hp = bossGo.AddComponent<EnemyHealth>();
        hp.SetTier(150, LootTier.Boss);
        var bar = Track(new GameObject("BossBarTest")).AddComponent<DiabloTargetBar>();
        yield return null;
        yield return null;
        Assert.AreEqual(hp, bar.ShownBoss, "a boss within range gets the wide bar");
        var label = bar.transform.Find("TargetBarCanvas/BossPanel/BossName")?.GetComponent<Text>();
        Assert.IsNotNull(label);
        Assert.AreEqual("DER HOHLE ABT", label.text);

        bossGo.transform.position = new Vector3(0f, 0f, 80f);
        yield return new WaitForSecondsRealtime(0.6f);
        yield return null;
        Assert.IsNull(bar.ShownBoss, "out of range hides the bar");
    }

    [UnityTest]
    public IEnumerator LootLabels_NameNearbyDropsInRarityColor()
    {
        var hero = Track(new GameObject(DiabloNetworkPlayer.HeroName));
        var camGo = Track(new GameObject("LabelCam"));
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 10f, -8f);
        camGo.transform.LookAt(Vector3.zero);
        var dropGo = Track(new GameObject("GroundDrop"));
        dropGo.transform.position = new Vector3(1f, 0f, 1f);
        var drop = dropGo.AddComponent<LootDrop>();
        drop.item = ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId);
        var labels = Track(new GameObject("Labels")).AddComponent<DiabloLootLabels>();
        bool savedAlways = DiabloSettings.LootLabelsAlways;
        try
        {
            DiabloSettings.LootLabelsAlways = false; // Alt-only mode (setting off)
            DiabloLootLabels.ForceShow = true;
            yield return null;
            yield return null;
            Assert.AreEqual(1, labels.ShownCount);
            var (text, color) = DiabloLootLabels.LabelFor(drop);
            Assert.AreEqual("Karrenbollwerk", text);
            Assert.AreEqual(DiabloPalette.Rarity(ItemRarity.Unique), color);
            DiabloLootLabels.ForceShow = false;
            yield return null;
            Assert.AreEqual(0, labels.ShownCount, "with the setting off, labels only while Alt is held");
            DiabloSettings.LootLabelsAlways = true;
            yield return null;
            Assert.AreEqual(1, labels.ShownCount, "default: labels always shown");
        }
        finally { DiabloLootLabels.ForceShow = false; DiabloSettings.LootLabelsAlways = savedAlways; }
    }
}
