using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Verifies all Cinderreach menu art sprites, 9-slice borders, layout groups,
/// potion safety, settings persistence/wiring, PauseMenuUI, and SessionComplete fixes.
/// </summary>
public class DiabloMenuPolishTests
{
    static readonly string[] AllBakedMenuSprites =
    {
        "ShortSword", "HeavyAxe", "Dagger",
        "ClothTunic", "LeatherVest", "Breastplate",
        "Hood", "Helm", "Circlet",
        "Lantern", "HoodedLantern", "EmberCenser",
        "HealingPotion", "GoldCoin",
        "TorchSconce", "LootChest", "BloodBanner",
        "PortraitFenwalker", "PortraitLanternbearer", "PortraitVendor",
        "SkillFireball", "SkillArrow", "SkillCleave",
        "SkillLanternBolt", "SkillWisps", "SkillSearingHalo", "SkillVeilStep",
        "CrestCinderreach", "WaypointSigil", "StashChestIcon", "SettingsCog",
        "StartupBackdrop",
    };

    [Test]
    public void MenuArt_All32SpritesLoadAsSpriteResources()
    {
        foreach (var name in AllBakedMenuSprites)
            Assert.IsNotNull(Res.Get<Sprite>("MenuArt/" + name), $"Missing Resources/MenuArt/{name} Sprite");
    }

    [Test]
    public void MenuArt_ChromeSpritesHaveNineSliceBorders()
    {
        Assert.Greater(DiabloMenuArt.PanelBackground().border.sqrMagnitude, 0f);
        Assert.Greater(DiabloMenuArt.HeaderBanner().border.sqrMagnitude, 0f);
        Assert.Greater(DiabloMenuArt.SlotBackground(ItemRarity.Rare).border.sqrMagnitude, 0f);
        Assert.Greater(DiabloMenuArt.TabBackground(true).border.sqrMagnitude, 0f);
        Assert.Greater(DiabloMenuArt.ButtonBackground(true).border.sqrMagnitude, 0f);
    }

    [Test]
    public void IconResolver_MapsLanternsPotionsGoldWithoutHoodCollision()
    {
        Assert.AreEqual("Lantern", DiabloMenuArt.ResolveIconName("Lantern"));
        Assert.AreEqual("Lantern", DiabloMenuArt.ResolveIconName("Worn Lantern"));
        Assert.AreEqual("HoodedLantern", DiabloMenuArt.ResolveIconName("HoodedLantern"));
        Assert.AreEqual("EmberCenser", DiabloMenuArt.ResolveIconName("EmberCenser"));
        Assert.AreEqual("HealingPotion", DiabloMenuArt.ResolveIconName("Healing Potion"));
        Assert.AreEqual("GoldCoin", DiabloMenuArt.ResolveIconName("GoldCoin"));
        Assert.AreEqual("Hood", DiabloMenuArt.ResolveIconName("Hood"));
    }

    [Test]
    public void SkillIcons_ResolveEveryClassSkill()
    {
        foreach (DiabloSkills.Skill sk in System.Enum.GetValues(typeof(DiabloSkills.Skill)))
            Assert.IsNotNull(DiabloMenuArt.SkillIcon(sk), $"Missing SkillIcon for {sk}");
    }

    [Test]
    public void Equipment_PotionCannotEquipToHelmetAndBagDrinksIt()
    {
        var go = new GameObject("HeroPotionTest");
        var eq = go.AddComponent<Equipment>();
        var inv = go.AddComponent<Inventory>();
        var hp = go.AddComponent<PlayerHealth>();
        hp.Refresh();
        hp.TakeDamage(10f);
        float before = hp.CurrentHP;

        var potion = new ItemInstance { slot = ItemSlot.Consumable, rarity = ItemRarity.Rare, baseType = "Healing Potion" };
        Assert.IsFalse(Equipment.CanEquip(potion, ClassCatalog.FenwalkerId));
        inv.Add(potion);

        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        ui.EquipIndex(0);

        Assert.IsNull(eq.helmet, "A legacy consumable must never equip into the Helmet slot.");
        Assert.AreEqual(1, inv.Count, "gear-slots S2: bag consumables no longer drink, the flask heals.");
        Assert.AreEqual(before, hp.CurrentHP, 0.001f, "Clicking a legacy consumable changes nothing.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void InventoryUI_HasNoLegacyBloodMoorCopyAndSlidersHaveFillAndHandle()
    {
        var go = new GameObject("HeroInvPolish");
        go.AddComponent<Equipment>();
        go.AddComponent<Inventory>();
        var ui = go.AddComponent<DiabloInventoryUI>();
        ui.SetOpen(true);
        foreach (var t in go.GetComponentsInChildren<Text>(true))
            StringAssert.DoesNotContain("blood-moor", t.text.ToLowerInvariant());
        var sliders = go.GetComponentsInChildren<Slider>(true);
        Assert.GreaterOrEqual(sliders.Length, 4);
        foreach (var s in sliders)
        {
            Assert.IsNotNull(s.fillRect, s.name + " missing fillRect");
            Assert.IsNotNull(s.handleRect, s.name + " missing handleRect");
        }
        Object.DestroyImmediate(go);
    }

    [Test]
    public void VendorUI_ColumnsAndRowHoldersHaveVerticalLayoutGroups()
    {
        var go = new GameObject("HeroVendorLayout");
        go.AddComponent<Wallet>();
        go.AddComponent<Inventory>();
        go.AddComponent<Equipment>();
        var ui = go.AddComponent<DiabloVendorUI>();
        ui.SetOpen(true);
        var vRows = go.transform.Find("VendorCanvas/Panel/Columns/VendorColumn/Scroll/Viewport/VendorRows");
        var bRows = go.transform.Find("VendorCanvas/Panel/Columns/BagColumn/Scroll/Viewport/BagRows");
        Assert.IsNotNull(vRows);
        Assert.IsNotNull(bRows);
        Assert.IsNotNull(vRows.GetComponent<VerticalLayoutGroup>());
        Assert.IsNotNull(bRows.GetComponent<VerticalLayoutGroup>());
        Object.DestroyImmediate(go);
    }

    [Test]
    public void TargetBar_FillHasSpriteSoFillAmountRenders()
    {
        var go = new GameObject("TargetBarSpriteTest");
        var bar = go.AddComponent<DiabloTargetBar>();
        var m = typeof(DiabloTargetBar).GetMethod("BuildUI", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        m?.Invoke(bar, null);
        var fillTransform = go.transform.Find("TargetBarCanvas/Panel/BarBg/BarFill");
        Assert.IsNotNull(fillTransform);
        var img = fillTransform.GetComponent<Image>();
        Assert.IsNotNull(img.sprite, "TargetBar fill Image requires a non-null Sprite for Filled mode.");
        Object.DestroyImmediate(go);
    }

    [Test]
    public void SessionComplete_HasGraphicRaycasterAndEnglishCopy()
    {
        var go = new GameObject("SessionCompleteTest");
        SessionComplete.Show(go, "Emberbrand Blade");
        var canvas = go.transform.Find("SessionCanvas");
        Assert.IsNotNull(canvas);
        Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>(), "SessionCanvas must have GraphicRaycaster so buttons are clickable.");
        StringAssert.DoesNotContain("Beute", SessionComplete.LastSummary);
        StringAssert.Contains("Erschlagen", SessionComplete.LastSummary);
        StringAssert.Contains("Gold", SessionComplete.LastSummary);
        SessionComplete.ResetRun();
        Object.DestroyImmediate(go);
    }

    [Test]
    public void Settings_NewOptionsResetAndCycleCleanly()
    {
        DiabloSettings.UIVol = 0.25f;
        DiabloSettings.MusicVol = 0.5f;
        DiabloSettings.MasterVol = 0.5f;
        Assert.AreEqual(0.25f, DiabloSettings.EffectiveMusic, 0.001f);
        Assert.AreEqual(0.125f, DiabloSettings.EffectiveUI, 0.001f);

        int prevIdx = DiabloSettings.ResolutionIndex;
        DiabloSettings.CycleResolution();
        Assert.AreNotEqual(prevIdx, DiabloSettings.ResolutionIndex);

        DiabloSettings.ResetDefaults();
        Assert.AreEqual(0.8f, DiabloSettings.MasterVol, 0.001f);
        Assert.AreEqual(0.8f, DiabloSettings.UIVol, 0.001f);
        Assert.AreEqual(2, DiabloSettings.QualityLevel);
        Assert.AreEqual(0, DiabloSettings.ResolutionIndex);
        Assert.IsTrue(DiabloSettings.VSync);
        Assert.IsTrue(DiabloSettings.ShowDamageNumbers);
    }

    [Test]
    public void PauseMenu_OpensTogglesSettingsAndUsesHdScaler()
    {
        var go = new GameObject("PauseMenuTest");
        var pause = go.AddComponent<PauseMenuUI>();
        pause.SetOpen(true);
        Assert.IsTrue(pause.IsOpen);
        var scaler = go.GetComponentInChildren<CanvasScaler>(true);
        Assert.IsNotNull(scaler);
        Assert.AreEqual(new Vector2(1920f, 1080f), scaler.referenceResolution);
        pause.ShowSettings();
        Assert.IsTrue(pause.SettingsOpen);
        pause.SetOpen(false);
        Assert.IsFalse(pause.IsOpen);
        DiabloClickController.ExternalDrive = false;
        Object.DestroyImmediate(go);
    }

    [Test]
    public void DeathVeil_SnapsDarkHoldsThenLifts()
    {
        Assert.AreEqual(0f, DiabloLifeGlobe.DeathVeilAlpha(-1f));
        Assert.Greater(DiabloLifeGlobe.DeathVeilAlpha(0f), 0.8f, "death must read at once");
        Assert.AreEqual(DiabloLifeGlobe.DeathVeilAlpha(0f), DiabloLifeGlobe.DeathVeilAlpha(1f), 1e-4f, "holds");
        Assert.Less(DiabloLifeGlobe.DeathVeilAlpha(2.5f), DiabloLifeGlobe.DeathVeilAlpha(1.5f), "lifts");
        Assert.AreEqual(0f, DiabloLifeGlobe.DeathVeilAlpha(DiabloLifeGlobe.DeathNoticeSeconds));
    }

    [Test]
    public void ClassCards_UseRoleAndRealSkillCount()
    {
        foreach (var cls in ClassCatalog.All)
        {
            StringAssert.Contains($"{cls.skills.Length} Fertigkeiten", ClassSelectUI.CardStatTag(cls));
            Assert.IsNotNull(DiabloMenuArt.ClassPortrait(cls.id), $"portrait for {cls.id}");
        }
        Assert.AreNotEqual(ClassSelectUI.CardSubtitle(ClassCatalog.Rogue), ClassSelectUI.CardSubtitle(ClassCatalog.Fenwalker));
        Assert.AreNotEqual(DiabloPalette.ClassAccent(ClassCatalog.RogueId), DiabloPalette.ClassAccent(ClassCatalog.FenwalkerId));
        Assert.AreNotSame(DiabloMenuArt.ClassPortrait(ClassCatalog.RogueId), DiabloMenuArt.ClassPortrait(ClassCatalog.FenwalkerId));
    }

    [Test]
    public void MainMenu_YieldsStartToClassSelectInGameScene()
    {
        var hero = new GameObject("ClassLookProbe").AddComponent<ClassAppearance>();
        try
        {
            Assert.IsTrue(MainMenu.ClassSelectOwnsStart(new string[0]));
            Assert.IsFalse(MainMenu.ClassSelectOwnsStart(new[] { "-shotcheck" }));
        }
        finally { Object.DestroyImmediate(hero.gameObject); }
    }

    [Test]
    public void ContinueLabel_NamesSavedHeroWithoutTouchingTheFile()
    {
        string path = System.IO.Path.Combine(Application.temporaryCachePath, "menu-peek-test.json");
        try
        {
            var save = new SaveGame { level = 7, gold = 321, classId = ClassCatalog.RogueId };
            SaveService.WriteSave(save, path);
            var before = System.IO.File.ReadAllText(path);
            string label = ClassSelectUI.ContinueLabel(path);
            StringAssert.Contains("SCHMUGGLER", label);
            StringAssert.Contains("STUFE 7", label);
            StringAssert.Contains("321 GOLD", label);
            Assert.AreEqual(before, System.IO.File.ReadAllText(path), "peek must not rewrite the save");
            Assert.AreEqual("GESPEICHERTE FUHRE FORTSETZEN", ClassSelectUI.ContinueLabel(path + ".missing"));
        }
        finally { if (System.IO.File.Exists(path)) System.IO.File.Delete(path); }
    }

    [Test]
    public void ItemLabels_ArePlayerReadable()
    {
        Assert.AreEqual("Karrenbollwerk", LootDrop.PrettyName(ItemDatabase.BulwarkCarrierId));
        Assert.AreEqual("Docht des Kranzes", LootDrop.PrettyName(ItemDatabase.WickOfHaloId));
        Assert.AreEqual("Leinenkittel", LootDrop.PrettyName("ClothTunic"));
        Assert.AreEqual("Abgewetztes Kurzschwert", LootDrop.PrettyName("Worn Short Sword"));
        Assert.AreEqual("Andenken des Schmugglers", LootDrop.PrettyName(ItemDatabase.RogueKeepsakeId));
        Assert.AreEqual("Karrenbollwerk", LootDrop.ItemLabel(ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId)));
    }

    [Test]
    public void Settings_BootLeavesAutomationAndScreenArgsAlone()
    {
        Assert.IsTrue(DiabloSettings.BootMayTouchDisplay(new[] { "cinderreach.exe" }));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(new[] { "x", "-autoplay" }));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(new[] { "x", "-shotcheck" }));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(new[] { "x", "-screen-width", "1280" }));
        Assert.IsFalse(DiabloSettings.BootMayTouchDisplay(new[] { "x", "-screen-fullscreen", "0" }));
    }

    [Test]
    public void Equipment_BlankSerializedSlotsAreEmpty()
    {
        var go = new GameObject("BlankSlotHero");
        try
        {
            var eq = go.AddComponent<Equipment>();
            var inv = go.AddComponent<Inventory>();
            eq.armor = new ItemInstance();   // what scene/Json serialization leaves in an empty slot
            eq.helmet = new ItemInstance();
            Assert.IsNull(eq.GetEquipped(ItemSlot.Armor), "blank placeholder reads as empty");
            Assert.IsFalse(eq.UnequipTo(ItemSlot.Helmet, inv), "nothing to unequip");
            Assert.AreEqual(0, inv.Count, "no phantom item lands in the bag");
        }
        finally { Object.DestroyImmediate(go); }
    }

    [Test]
    public void SpawnLoad_OnlyJoiningClientsBringTheirSave()
    {
        Assert.IsTrue(DiabloNetworkPlayer.ShouldLoadSaveOnSpawn(isServer: false), "a joining client brings its hero");
        Assert.IsFalse(DiabloNetworkPlayer.ShouldLoadSaveOnSpawn(isServer: true),
            "host/solo choose Continue or New on the start screen");
    }

    [Test]
    public void TargetBar_ShowsEnemyNamesNotObjectNames()
    {
        Assert.AreEqual("Aschenlanzer", DiabloTargetBar.DisplayName("Ash Lancer_0"));
        Assert.AreEqual("Schlackenknecht", DiabloTargetBar.DisplayName("Kiln Summon_1_2"));
        Assert.AreEqual("Schlackenknecht", DiabloTargetBar.DisplayName("Cinder Thrall_12"));
        Assert.AreEqual("Hülle", DiabloTargetBar.DisplayName("Husk_3"));
        Assert.AreEqual("Der Hohle Abt", DiabloTargetBar.DisplayName("Hollow Abbot"));
        Assert.AreEqual("Ältester vom Stillen Grund", DiabloTargetBar.DisplayName("Bone Warden Elite"));
        Assert.AreEqual(DiabloPalette.GoldBright, DiabloTargetBar.TierColor(LootTier.ActBoss));
    }

    [Test]
    public void BossBar_OnlyForBossTiers()
    {
        Assert.IsTrue(DiabloTargetBar.IsBossTier(LootTier.Boss));
        Assert.IsTrue(DiabloTargetBar.IsBossTier(LootTier.ActBoss));
        Assert.IsFalse(DiabloTargetBar.IsBossTier(LootTier.Elite));
        Assert.AreEqual("", DiabloTargetBar.PhaseLabel(null));
    }
}
