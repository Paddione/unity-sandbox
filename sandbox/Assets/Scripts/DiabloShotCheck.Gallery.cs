using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Full gallery sequence: HUD, town/UI, Ossuary and Abbot, class kits, act bosses, itemization/death, reload.</summary>
public sealed partial class DiabloShotCheck
{
    /// <summary>
    /// cinderreach-hud-redesign: the new HUD in three states — hurt hero with a
    /// shield and gold, a hurt companion in the party frames, and an elite on
    /// the target bar with its tier plaque.
    /// </summary>
    IEnumerator HudRedesignShots(DiabloClickController hero)
    {
        var hp = hero.GetComponent<PlayerHealth>();
        var wallet = Wallet.EnsureWallet(hero.gameObject);
        int goldBefore = wallet != null ? wallet.Gold : 0;
        if (wallet != null) wallet.SetGold(1284);
        if (hp != null)
        {
            hp.TakeDamage(hp.MaxHP * 0.28f);
            hp.GrantShield(15f, 6f);
        }
        yield return Wait(0.5f);
        yield return Shot("hud_solo");

        foreach (var c in PartyCompanion.Live)
        {
            var ch = c != null ? c.GetComponent<PlayerHealth>() : null;
            if (ch != null) { ch.TakeDamage(ch.MaxHP * 0.55f); break; }
        }
        yield return Wait(0.4f);
        yield return Shot("hud_party");

        var elite = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        elite.name = "Ash Elite_0";
        elite.transform.position = hero.transform.position + hero.transform.right * 3f;
        var eh = elite.AddComponent<EnemyHealth>();
        eh.tier = LootTier.Elite;
        DiabloMelee.LastStruck = elite;
        DiabloMelee.LastStruckTime = Time.time;
        yield return Wait(0.3f);
        DiabloMelee.LastStruckTime = Time.time;
        yield return Shot("hud_target_elite");
        DiabloMelee.LastStruck = null;
        Destroy(elite);
        if (wallet != null) wallet.SetGold(goldBefore);
        if (hp != null) hp.Heal(hp.MaxHP);
        yield return Wait(0.2f);
    }

    IEnumerator TownAndUiGalleryShots(DiabloClickController hero)
    {
        // 7) Lantern Hold town framing (S3): wide view over camp + vendor.
        hero.distance = 16f;
        yield return Wait(0.5f);
        yield return Shot("town_lantern_hold");

        // cinderreach-town-npc-identity: hero among the Hold figures; each wears its own look and turns to the hero.
        var townBack = hero.transform.position;
        var townCc = hero.GetComponent<CharacterController>();
        if (townCc != null) townCc.enabled = false;
        hero.transform.position = new Vector3(1f, townBack.y, 11.5f);
        if (townCc != null) townCc.enabled = true;
        hero.distance = 11f;
        yield return Wait(1.2f);
        yield return Shot("town_npcs_facing");
        if (townCc != null) townCc.enabled = false;
        hero.transform.position = townBack;
        if (townCc != null) townCc.enabled = true;
        hero.distance = 16f;
        yield return Wait(0.3f);

        // 8) Town & Sanctuary UI Gallery (Vendor, Stash, Waypoint, Inventory, Bag, Settings, Character, Pause).
        var bagComp = hero.GetComponent<Inventory>();
        var walComp = hero.GetComponent<Wallet>();
        if (walComp != null && walComp.Gold < 250) walComp.Add(250 - walComp.Gold);
        if (bagComp != null && bagComp.Count < 4)
        {
            bagComp.Add(ItemDatabase.CreateUnique("Emberbrand Blade"));
            bagComp.Add(ItemDatabase.CreateUnique("Cryptwarden Plate"));
            bagComp.Add(ItemDatabase.CreateUnique(ItemDatabase.VialSashId));
        }

        var vendorUI = hero.GetComponent<DiabloVendorUI>();
        if (vendorUI == null) vendorUI = hero.gameObject.AddComponent<DiabloVendorUI>();
        vendorUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("vendor_open");
        yield return CompareShot(hero.gameObject, "VendorCanvas", VendorCompareItem(hero.gameObject), "vendor_compare");
        vendorUI.SetOpen(false);
        yield return Wait(0.2f);

        var stashUI = hero.GetComponent<DiabloStashUI>();
        if (stashUI == null) stashUI = hero.gameObject.AddComponent<DiabloStashUI>();
        stashUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("stash_open");
        stashUI.SetOpen(false);
        yield return Wait(0.2f);

        var waypointUI = hero.GetComponent<DiabloWaypointUI>();
        if (waypointUI == null) waypointUI = hero.gameObject.AddComponent<DiabloWaypointUI>();
        waypointUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("waypoint_open");
        waypointUI.SetOpen(false);
        yield return Wait(0.2f);

        var invUI = hero.GetComponent<DiabloInventoryUI>();
        if (invUI == null) invUI = hero.gameObject.AddComponent<DiabloInventoryUI>();
        invUI.SetOpen(true);
        yield return Wait(0.35f);
        yield return Shot("inventory_open");
        {
            // Shift-compare gallery: a worn weapon and a bag rival with gains and losses.
            var eq = hero.GetComponent<Equipment>();
            var bag = hero.GetComponent<Inventory>();
            var prevWeapon = eq != null ? eq.weapon : null;
            string baseType = prevWeapon != null ? prevWeapon.baseType : ItemDatabase.StarterSwordBase;
            var worn = GalleryItem(baseType, ItemRarity.Magic, (AffixLibrary.Damage, 4f), (AffixLibrary.Crit, 6f), (AffixLibrary.MaxHP, 10f));
            var rival = GalleryItem(baseType, ItemRarity.Rare, (AffixLibrary.Damage, 7f), (AffixLibrary.Crit, 2f), (AffixLibrary.MaxHP, 10f), (AffixLibrary.LifeSteal, 3f));
            if (eq != null && bag != null)
            {
                if (bag.Add(rival))
                {
                    eq.Equip(worn);
                    invUI.Refresh();
                    yield return Wait(0.5f);
                    yield return Shot("inventory_compare");
                    bag.RemoveAt(bag.Count - 1);
                    if (prevWeapon != null) eq.Equip(prevWeapon);
                    else if (eq.UnequipTo(ItemSlot.Weapon, bag)) bag.RemoveAt(bag.Count - 1);
                    invUI.Refresh();
                    yield return Wait(0.2f);
                }
                // Unique selection: the fixed tooltip column shows the unique effect.
                // Autoloot can fill the bag during the waits above: displace the last
                // item if full and restore it afterwards so the pose always runs.
                var unique = GalleryItem(baseType, ItemRarity.Unique, (AffixLibrary.Damage, 9f), (AffixLibrary.Crit, 5f));
                unique.uniqueId = ItemDatabase.LancerWardId;
                unique.itemLevel = 12;
                ItemInstance displaced = null;
                if (bag.Count >= bag.Slots) { displaced = bag.Get(bag.Count - 1); bag.RemoveAt(bag.Count - 1); }
                if (bag.Add(unique))
                {
                    invUI.Refresh();
                    yield return Wait(0.5f);
                    yield return Shot("inventory_unique_selected");
                    bag.RemoveAt(bag.Count - 1);
                    invUI.Refresh();
                    yield return Wait(0.2f);
                }
                if (displaced != null) bag.Add(displaced);
            }
        }
        invUI.ShowTab(DiabloInventoryUI.MenuTab.Bag);
        yield return Wait(0.35f);
        yield return Shot("bag_open");
        invUI.ShowTab(DiabloInventoryUI.MenuTab.Settings);
        yield return Wait(0.35f);
        yield return Shot("settings_open");
        invUI.SetOpen(false);
        yield return Wait(0.2f);

        // 9) Character sheet (S4 seam: DiabloCharacterUI, C panel).
        var charUI = hero.GetComponent<DiabloCharacterUI>();
        if (charUI == null) charUI = hero.gameObject.AddComponent<DiabloCharacterUI>();
        charUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("character_open");
        charUI.SetOpen(false);
        yield return Wait(0.2f);

        // 9b) Story dialog + expedition log (cinderreach-deutsch-story S7):
        // accept H1 first so the log shows a tracked task, then Ottilie's offer.
        StoryDirector.State.Refresh(RunProfile.Act);
        var storyPlan = StoryDialogs.Plan(StoryDirector.State, StoryIds.Ottilie);
        if (storyPlan.dialog != null)
        {
            var dlgUI = hero.GetComponent<StoryDialogUI>();
            if (dlgUI == null) dlgUI = hero.gameObject.AddComponent<StoryDialogUI>();
            dlgUI.Show(storyPlan, StoryIds.Ottilie);
            yield return Wait(0.4f);
            yield return Shot("story_dialog");
            dlgUI.Advance();
            dlgUI.Advance();
            yield return Wait(0.3f);
            yield return Shot("story_dialog_2");
            while (dlgUI.IsOpen) dlgUI.Advance();
            yield return Wait(0.2f);
        }
        var logUI = hero.GetComponent<StoryQuestUI>();
        if (logUI == null) logUI = hero.gameObject.AddComponent<StoryQuestUI>();
        logUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("story_questlog");
        logUI.SetOpen(false);
        yield return Wait(0.2f);

        var pauseGo = new GameObject("ShotCheckPause");
        var pauseUI = pauseGo.AddComponent<PauseMenuUI>();
        pauseUI.SetOpen(true);
        yield return Wait(0.35f);
        yield return Shot("pause_menu");
        pauseUI.SetOpen(false);
        Destroy(pauseGo);
        DiabloClickController.ExternalDrive = true;
        yield return Wait(0.2f);
    }

    IEnumerator OssuaryAndAbbotShots(DiabloClickController hero, Vector3 camFwd, System.Action<GameObject> onBoss)
    {
        // 10) Sunken Ossuary arrival (S5): teleport the hero through the
        // FenGate destination like ZoneGate.Teleport does, wide shot.
        var ossuaryGate = GameObject.Find(ZoneGate.FenGateName);
        if (ossuaryGate == null || ossuaryGate.GetComponent<ZoneGate>() == null)
        {
            Log("FAIL no FenGate with ZoneGate for the ossuary leg");
            Quit(2);
            yield break;
        }
        TeleportHero(hero, ossuaryGate.GetComponent<ZoneGate>().destination);
        hero.distance = 18f;
        yield return Wait(1.0f);
        yield return Shot("ossuary_arrival");

        // 10a) gear-slots S7: flask HUD mid-cooldown (outside the hold, so no refill),
        // then the new slots on the ground.
        {
            var flaskHp = hero.GetComponent<PlayerHealth>();
            if (flaskHp != null) flaskHp.TakeDamage(flaskHp.MaxHP * 0.5f);
            var flask = HealingFlask.For(hero.gameObject);
            bool drank = flask.TryDrink();
            hero.distance = 9f;
            yield return Wait(1.5f);
            Log($"FLASK drank={drank} charges={flask.Charges}/{flask.MaxCharges} cooldown={flask.CooldownRemaining:0.0}");
            yield return Shot("flask_cooldown_hud");
            var fwd2 = hero.transform.forward.sqrMagnitude > 0.01f ? hero.transform.forward.normalized : camFwd;
            var at = hero.transform.position + fwd2 * 2.5f;
            var drops = new[]
            {
                ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.IronboundGripsId), 5, 1),
                GalleryItem("LeatherBoots", ItemRarity.Rare, (AffixLibrary.MoveSpeed, 0.2f)),
                ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.QuickdrawBeltId), 5, 1),
            };
            for (int i = 0; i < drops.Length; i++)
            {
                drops[i].slot = i == 0 ? ItemSlot.Gloves : i == 1 ? ItemSlot.Boots : ItemSlot.Belt;
                LootDrop.SpawnItem(at + Vector3.right * (i - 1) * 1.2f, drops[i], false);
            }
            DiabloLootLabels.ForceShow = true;
            yield return Wait(0.8f);
            yield return Shot("gear_slots_ground");
            DiabloLootLabels.ForceShow = false;
            hero.distance = 18f;
        }

        // 10b) Edge wall (playtest 2026-11-23): hero near the far Ossuary corner,
        // the two rim walls in front of the camera, companions in formation.
        var ossuaryArrival = ossuaryGate.GetComponent<ZoneGate>().destination;
        TeleportHero(hero, KilnLayout.OssuaryOffset + camFwd * 15f + Vector3.up);
        Face(hero, camFwd);
        hero.distance = 14f;
        yield return Wait(1.2f);
        yield return Shot("ossuary_edge_wall");
        TeleportHero(hero, ossuaryArrival);
        yield return Wait(0.5f);

        // 11) Bone Warden at range (S5 RangedCaster, hold band 6-8 m).
        var warden = GameObject.Find("Bone Warden");
        if (warden == null)
        {
            Log("FAIL no Bone Warden found");
            Quit(2);
            yield break;
        }
        Face(hero, warden.transform.position - hero.transform.position);
        hero.distance = 8f;
        hero.pitch = 40f;
        yield return Wait(1.2f); // let the 0.6 s telegraph play
        yield return Shot("bone_warden");

        // 12) The Hollow Abbot phase 2 (S5 BossController: summons + fan).
        var boss = GameObject.Find(BossController.BossName);
        if (boss == null)
        {
            Log("FAIL no Hollow Abbot found");
            Quit(2);
            yield break;
        }
        TeleportHero(hero, boss.transform.position + new Vector3(0f, 0f, 5f));
        Face(hero, boss.transform.position - hero.transform.position);
        var bossCtl = boss.GetComponent<BossController>();
        if (bossCtl != null) bossCtl.EnterPhaseTwo();
        hero.distance = 10f;
        yield return Wait(1.0f);
        yield return Shot("abbot_phase2");
        onBoss?.Invoke(boss);
    }

    IEnumerator ClassKitGalleryShots(DiabloClickController hero, DiabloSkills skills, GameObject boss)
    {
        // 13a) Class select & Startup settings overlay (second-class S4).
        var selectGo = new GameObject("ClassSelect");
        var select = selectGo.AddComponent<ClassSelectUI>();
        select.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("class_select");
        select.ShowSettings(true);
        yield return Wait(0.3f);
        yield return Shot("start_settings");
        select.ShowSettings(false);
        select.ChooseNew(ClassCatalog.LanternbearerId);
        Destroy(selectGo);
        DiabloClickController.ExternalDrive = true;

        // 13b) Lanternbearer in the abbot fight: wisps out, then the searing halo.
        Face(hero, boss.transform.position - hero.transform.position);
        hero.distance = 9f;
        hero.pitch = 45f;
        skills.CastToward(DiabloSkills.Skill.Wisps, boss.transform.position - hero.transform.position);
        yield return Wait(0.8f);
        yield return Shot("lanternbearer_wisps");
        skills.CastToward(DiabloSkills.Skill.SearingHalo, boss.transform.position - hero.transform.position);
        yield return Wait(0.12f);
        yield return Shot("lanternbearer_halo");
        skills.CastToward(DiabloSkills.Skill.LanternBolt, boss.transform.position - hero.transform.position);
        yield return Wait(0.15f);
        yield return Shot("lanternbearer_bolt");
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        var look = hero.GetComponent<ClassAppearance>();
        if (look != null) look.Apply(ClassCatalog.Fenwalker);

        // 13c) Party roles (party-roles S6): tank slam ring, rogue quick shot
        // and flurry, healer renewing circle — one pose per role.
        Face(hero, boss.transform.position - hero.transform.position);
        hero.distance = 9f;
        hero.pitch = 45f;
        skills.CastToward(DiabloSkills.Skill.SeismicSlam, boss.transform.position - hero.transform.position);
        yield return Wait(0.12f);
        yield return Shot("tank_seismic_slam");
        RunProfile.ClassId = ClassCatalog.RogueId;
        if (look != null) look.Apply(ClassCatalog.Rogue);
        skills.CastToward(DiabloSkills.Skill.QuickShot, boss.transform.position - hero.transform.position);
        yield return Wait(0.15f);
        yield return Shot("rogue_quick_shot");
        skills.CastToward(DiabloSkills.Skill.BladeFlurry, boss.transform.position - hero.transform.position);
        yield return Wait(0.12f);
        yield return Shot("rogue_blade_flurry");
        RunProfile.ClassId = ClassCatalog.LanternbearerId;
        if (look != null) look.Apply(ClassCatalog.Lanternbearer);
        skills.CastToward(DiabloSkills.Skill.RenewingCircle, boss.transform.position - hero.transform.position);
        yield return Wait(0.12f);
        yield return Shot("healer_renewing_circle");
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        if (look != null) look.Apply(ClassCatalog.Fenwalker);

        // 13d) class-kits-parity: Fenwalker combo chain (mire ground -> slam -> shatter
        // cleave, both cleave variants) and Rogue combo chain (venom -> smoke -> keg,
        // both quick shot variants with a spark shot through the keg).
        var toBoss = boss.transform.position - hero.transform.position;
        Face(hero, toBoss);
        hero.distance = 10f;
        hero.pitch = 48f;
        KitCast(skills, DiabloSkills.Skill.MireGround, toBoss);
        yield return Wait(0.3f);
        yield return Shot("fenwalker_mire_ground");
        KitCast(skills, DiabloSkills.Skill.IronVow, toBoss);
        KitCast(skills, DiabloSkills.Skill.SeismicSlam, toBoss);
        KitCast(skills, DiabloSkills.Skill.MireHook, toBoss);
        yield return Wait(0.12f);
        yield return Shot("fenwalker_hook_slam");
        RunProfile.SkillVariants[ClassCatalog.FenwalkerId + ":" + DiabloSkills.Skill.Cleave] = SkillVariantCatalog.CleaveRoundId;
        KitCast(skills, DiabloSkills.Skill.Cleave, toBoss);
        yield return Wait(0.1f);
        yield return Shot("fenwalker_shatter_whirl");
        RunProfile.SkillVariants[ClassCatalog.FenwalkerId + ":" + DiabloSkills.Skill.Cleave] = SkillVariantCatalog.CleaveFrontalId;
        KitCast(skills, DiabloSkills.Skill.Cleave, toBoss);
        yield return Wait(0.1f);
        yield return Shot("fenwalker_shatter_frontal");
        RunProfile.SkillVariants.Remove(ClassCatalog.FenwalkerId + ":" + DiabloSkills.Skill.Cleave);
        Debug.Log($"[ShotCheck] fenwalker combos: {string.Join(",", ClassCombos.TriggeredLines())}");

        RunProfile.ClassId = ClassCatalog.RogueId;
        if (look != null) look.Apply(ClassCatalog.Rogue);
        KitCast(skills, DiabloSkills.Skill.VenomVial, toBoss);
        KitCast(skills, DiabloSkills.Skill.SmokePot, toBoss);
        yield return Wait(0.3f);
        yield return Shot("rogue_venom_smoke");
        KitCast(skills, DiabloSkills.Skill.PowderKeg, toBoss);
        yield return Wait(0.2f);
        yield return Shot("rogue_powder_keg");
        RunProfile.SkillVariants[ClassCatalog.RogueId + ":" + DiabloSkills.Skill.QuickShot] = SkillVariantCatalog.QuickFanId;
        KitCast(skills, DiabloSkills.Skill.QuickShot, toBoss);
        yield return Wait(0.15f);
        yield return Shot("rogue_fan_spark_shot");
        RunProfile.SkillVariants.Remove(ClassCatalog.RogueId + ":" + DiabloSkills.Skill.QuickShot);
        KitCast(skills, DiabloSkills.Skill.QuickShot, toBoss);
        yield return Wait(0.15f);
        yield return Shot("rogue_single_shot");
        KitCast(skills, DiabloSkills.Skill.ShadowRoll, -toBoss);
        yield return Wait(0.2f);
        yield return Shot("rogue_shadow_roll");
        Debug.Log($"[ShotCheck] kit combos: {string.Join(",", ClassCombos.TriggeredLines())}");
        RunProfile.ClassId = ClassCatalog.FenwalkerId;
        if (look != null) look.Apply(ClassCatalog.Fenwalker);
    }

    IEnumerator ActZoneBossShots(DiabloClickController hero)
    {
        // 14) act-one: Cinder Kiln entrance, a thrall pack, the Kilnmother in phase 2, the Act I screen.
        RunProfile.Act.Unlock(ActProgress.Kiln);
        keepAlive = hero.GetComponent<PlayerHealth>();
        TeleportHero(hero, KilnLayout.KilnArrival);
        hero.distance = 18f;
        hero.pitch = 50f;
        yield return Wait(1.0f);
        yield return Shot("kiln_entrance");
        var pack = GameObject.Find(KilnLayout.ThrallPrefix + "0");
        if (pack != null)
        {
            TeleportHero(hero, pack.transform.position + new Vector3(0f, 0f, 5f));
            Face(hero, pack.transform.position - hero.transform.position);
            hero.distance = 10f;
            yield return Wait(0.8f);
            yield return Shot("thrall_pack");
        }
        // cinderreach-act-boss-gating: the Kilnmother appears with her spawn cutscene once Act I's other quests are done.
        yield return SpawnCutsceneShot(1, "kilnmother_spawn_cutscene");
        var mother = GameObject.Find(KilnLayout.KilnmotherName);
        if (mother != null)
        {
            TeleportHero(hero, mother.transform.position + new Vector3(0f, 0f, 6f));
            Face(hero, mother.transform.position - hero.transform.position);
            var mc = mother.GetComponent<KilnmotherController>();
            if (mc != null) { mc.EnterPhaseTwo(); mc.FireWaveNow(Time.time); }
            BurningGround.Spawn(hero.transform.position + new Vector3(2f, 0f, -1f));
            hero.distance = 13f;
            yield return Wait(1.4f);
            yield return Shot("kilnmother_phase2");
        }
        SessionComplete.ShowActComplete(hero.gameObject);
        yield return Wait(0.5f);
        yield return Shot("act_one_complete");
        SessionComplete.ActOneMode = false;
        foreach (var sc in Object.FindObjectsByType<SessionComplete>(FindObjectsSortMode.None)) sc.PressOn();

        // 15) act-two-opening: Glass Barrens arrival, a Shardback pack with an Augur field, the Glasswright in phase 2.
        RunProfile.Act.CompleteAct();
        TeleportHero(hero, BarrensLayout.BarrensArrival);
        hero.distance = 18f;
        hero.pitch = 50f;
        yield return Wait(1.0f);
        yield return Shot("barrens_arrival");
        var shard = GameObject.Find(BarrensLayout.ShardbackPrefix + "1");
        if (shard != null)
        {
            TeleportHero(hero, shard.transform.position + new Vector3(0f, 0f, 5f));
            Face(hero, shard.transform.position - hero.transform.position);
            ShardField.Spawn(hero.transform.position + new Vector3(-2f, 0f, 1f));
            hero.distance = 10f;
            yield return Wait(1.3f);
            yield return Shot("shardback_pack");
        }
        yield return SpawnCutsceneShot(2, "glasswright_spawn_cutscene");
        var glass = GameObject.Find(GlasswrightController.BossName);
        if (glass != null)
        {
            TeleportHero(hero, glass.transform.position + new Vector3(0f, 0f, 7f));
            Face(hero, glass.transform.position - hero.transform.position);
            var gc = glass.GetComponent<GlasswrightController>();
            if (gc != null) { gc.EnterPhaseTwo(); gc.FireLancesNow(Time.time, hero.transform.position + new Vector3(2f, 0f, 0f)); }
            hero.distance = 14f;
            yield return Wait(0.6f);
            yield return Shot("glasswright_phase2");
        }
        SessionComplete.ShowZoneBossCleared(hero.gameObject);
        yield return Wait(0.5f);
        yield return Shot("barrens_cleared");
        SessionComplete.ZoneBossMode = false;
        foreach (var sc in Object.FindObjectsByType<SessionComplete>(FindObjectsSortMode.None)) sc.PressOn();
        // cinderreach-act-boss-gating: Thewes at the Moortor after H10/H11/N6/N7.
        yield return SpawnCutsceneShot(3, "thewes_spawn_cutscene");
        keepAlive = null;
    }

    IEnumerator ItemizationAndDeathShots(DiabloClickController hero, float baseDist, float basePitch, Vector3 camFwd)
    {
        // 13) SessionComplete overlay (S5 end screen, shown on boss death;
        // here opened directly as the visual proof — the gameplay proof is
        // the autoplay boss kill in docs/session-loop-run-log.md).
        SessionComplete.Show(hero.gameObject, BossController.LastBossDrop);
        yield return Wait(0.5f);
        yield return Shot("session_complete");

        // S5 itemization presentation: one ground drop per rarity with its own
        // sprite + glow, the open bag with the shared details card (rolls and
        // +/- deltas), and the open vendor with the sell column.
        {
            // Clear stage: session overlay off, hero back at Lantern Hold.
            foreach (Transform child in hero.transform)
                if (child.name == "SessionCanvas") child.gameObject.SetActive(false);
            TeleportHero(hero, new Vector3(-2.0f, 1f, 11f));
            yield return Wait(0.6f);
            var inv = hero.GetComponent<Inventory>();
            var eq = hero.GetComponent<Equipment>();
            var rng = new System.Random(1234);
            if (inv != null && eq != null)
            {
                // Clean stage: only the three S5 demo items in the bag.
                inv.SetContents(new System.Collections.Generic.List<ItemInstance>());
                var purse = hero.GetComponent<Wallet>();
                if (purse != null && purse.Gold < 200) purse.Add(200);
                var equipped = new ItemInstance
                {
                    slot = ItemSlot.Weapon,
                    rarity = ItemRarity.Rare,
                    baseType = "ShortSword",
                    itemLevel = 3,
                    affixes = new System.Collections.Generic.List<Affix>
                    {
                        new Affix(AffixLibrary.Damage, 3f),
                    },
                };
                equipped.EnsureIdentity();
                eq.Equip(equipped);
                var magic = new ItemInstance
                {
                    slot = ItemSlot.Armor,
                    rarity = ItemRarity.Rare,
                    baseType = "ClothTunic",
                    itemLevel = 3,
                    affixes = AffixLibrary.RollAffixes(ItemSlot.Armor, ItemRarity.Rare, rng, 1, 3),
                };
                var rare = new ItemInstance
                {
                    slot = ItemSlot.Weapon,
                    rarity = ItemRarity.Rare,
                    baseType = "ShortBow",
                    itemLevel = 6,
                    affixes = AffixLibrary.RollAffixes(ItemSlot.Weapon, ItemRarity.Rare, rng, 1, 6),
                };
                var unique = ItemDatabase.RollUnique(ItemDatabase.CreateUnique(ItemDatabase.BulwarkCarrierId), 6, 1);
                foreach (var it in new[] { magic, rare, unique }) { it.EnsureIdentity(); inv.Add(it); }
                Vector3 fwd = hero.transform.forward.sqrMagnitude > 0.01f ? hero.transform.forward.normalized : camFwd;
                Vector3 anchor = hero.transform.position + fwd * 2.5f;
                LootDrop.SpawnItem(anchor - Vector3.right * 1.2f, magic, false);
                LootDrop.SpawnItem(anchor, rare, false);
                LootDrop.SpawnItem(anchor + Vector3.right * 1.2f, unique, false);
                Face(hero, fwd);
                hero.distance = 6f;
                hero.pitch = 42f;
                yield return Wait(0.8f);
                yield return Shot("s5_ground_rarities");
                DiabloLootLabels.ForceShow = true; // as if Alt were held
                yield return Wait(0.2f);
                yield return Shot("loot_labels_alt");
                DiabloLootLabels.ForceShow = false;
                bool savedFilter = DiabloSettings.LootFilterUniqueOnly;
                DiabloSettings.LootFilterUniqueOnly = true; // cinderreach-loot-click-filter: Rare plates hidden
                yield return Wait(0.2f);
                yield return Shot("loot_filter_unique");
                DiabloSettings.LootFilterUniqueOnly = savedFilter;
                var invUi = hero.GetComponent<DiabloInventoryUI>();
                if (invUi != null)
                {
                    invUi.SetOpen(true);
                    invUi.ShowTab(DiabloInventoryUI.MenuTab.Bag);
                    invUi.Refresh();
                    yield return Wait(0.4f);
                    yield return Shot("s5_inventory_details");
                    DiabloInventoryUI.CursorScreenOverride = new Vector2(Screen.width * 0.52f, Screen.height * 0.45f);
                    invUi.ClickBag(0); // cinderreach-inventory-cursor-item: item rides the cursor
                    yield return Wait(0.2f);
                    yield return Shot("inventory_cursor_item");
                    invUi.ClickOutside();
                    DiabloInventoryUI.CursorScreenOverride = null;
                    // cinderreach-bag-quest-upgrades: the 10x10 grid at the 100-slot cap.
                    var shotBag = hero.GetComponent<Inventory>();
                    if (shotBag != null)
                    {
                        shotBag.RaiseSlots(Inventory.MaxSlots);
                        invUi.Refresh();
                        yield return Wait(0.3f);
                        yield return Shot("bag_slots_100");
                    }
                    invUi.SetOpen(false);
                }
                var vendorUi = hero.GetComponent<DiabloVendorUI>();
                if (vendorUi != null)
                {
                    vendorUi.SetOpen(true);
                    yield return Wait(0.4f);
                    yield return Shot("s5_vendor");
                    vendorUi.RequestSellAllRares(); // cinderreach-vendor-sell-all: confirm with count + gold
                    yield return Wait(0.2f);
                    yield return Shot("vendor_sell_all_confirm");
                    vendorUi.CancelSellAllRares();
                    vendorUi.SetOpen(false);
                }
            }
        }

        hero.distance = baseDist;
        hero.pitch = basePitch;

        // Death beat (presentation only: no respawn teleport here).
        PlayerHealth.LastDeathTime = Time.time;
        yield return Wait(0.3f);
        yield return Shot("death_veil");
        yield return Wait(1.7f);
        yield return Shot("death_veil_lifting");
        PlayerHealth.LastDeathTime = float.NegativeInfinity;
    }

    IEnumerator ReloadAndNewHeroShots(DiabloClickController hero)
    {
        // In-game "New run" reloads the scene; the saved hero must come back (not a fresh level 1).
        var prog0 = hero.GetComponent<DiabloProgression>();
        var wallet0 = hero.GetComponent<Wallet>();
        int levelBefore = prog0 != null ? prog0.Level : -1;
        int goldBefore = wallet0 != null ? wallet0.Gold : -1;
        var inv0 = hero.GetComponent<DiabloInventoryUI>();
        if (inv0 == null) inv0 = hero.gameObject.AddComponent<DiabloInventoryUI>();
        DiabloClickController.ExternalDrive = false;
        inv0.StartNewRun(RunProfile.Normal);
        float until = Time.realtimeSinceStartup + 15f;
        GameObject after = null;
        while (Time.realtimeSinceStartup < until)
        {
            yield return null;
            if (SaveService.ContinueAfterReload) continue;
            after = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (after != null) break;
        }
        yield return Wait(1.0f);
        after = GameObject.Find(DiabloNetworkPlayer.HeroName);
        int levelAfter = after != null && after.GetComponent<DiabloProgression>() != null ? after.GetComponent<DiabloProgression>().Level : -1;
        int goldAfter = after != null && after.GetComponent<Wallet>() != null ? after.GetComponent<Wallet>().Gold : -1;
        bool resumed = levelAfter == levelBefore && goldAfter == goldBefore;
        Log($"RELOAD_CONTINUE {(resumed ? "PASS" : "FAIL")} level {levelBefore}->{levelAfter} gold {goldBefore}->{goldAfter}");
        yield return Shot("after_new_run");

        // Start screen from in-game, pick a different class: confirm, then a fresh world and hero.
        var csGo = new GameObject("ShotCheckNewHero");
        var cs = csGo.AddComponent<ClassSelectUI>();
        cs.SetOpen(true);
        string newId = RunProfile.ClassId == ClassCatalog.RogueId ? ClassCatalog.FenwalkerId : ClassCatalog.RogueId;
        cs.RequestNew(newId);
        yield return Wait(0.3f);
        yield return Shot("new_hero_confirm");
        bool asked = cs.ConfirmVisible;
        cs.ConfirmNew();
        until = Time.realtimeSinceStartup + 15f;
        while (Time.realtimeSinceStartup < until && SaveService.NewHeroAfterReload != null) yield return null;
        yield return Wait(1.0f);
        var fresh = GameObject.Find(DiabloNetworkPlayer.HeroName);
        int freshLevel = fresh != null && fresh.GetComponent<DiabloProgression>() != null ? fresh.GetComponent<DiabloProgression>().Level : -1;
        bool freshOk = asked && RunProfile.ClassId == newId && freshLevel == 1 && RunProfile.Act.Waypoints.Count <= 1;
        Log($"NEW_HERO {(freshOk ? "PASS" : "FAIL")} asked={asked} class={RunProfile.ClassId} level={freshLevel} waypoints={RunProfile.Act.Waypoints.Count}");
        yield return Shot("after_new_hero");
    }
}
