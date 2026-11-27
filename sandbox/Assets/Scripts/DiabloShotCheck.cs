using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Visual self-check for the Windows player: launched with <c>-shotcheck</c>,
/// it takes over the local hero, plays the skill poses (bow draw/release, aim
/// hold, fireball flight, blast) from fixed camera angles, saves screenshots
/// and quits. Output: <c>-shotdir &lt;path&gt;</c>, default
/// <c>&lt;game folder&gt;/shotcheck</c>. Inert without the flag.
/// </summary>
public sealed class DiabloShotCheck : MonoBehaviour
{
    public const string Flag = "-shotcheck";
    const string EquipmentFlag = "-equipment-shots";
    /// <summary>Only the split-body locomotion poses (run, strafe, backpedal, stop) with bone logs.</summary>
    const string LocomotionFlag = "-locomotion-shots";
    const string MapFlag = "-map-shots";
    const string StatBreakdownFlag = "-stat-breakdown-shots";
    /// <summary>Character select only: cards, creation preview per class/gender/hair, new hero via reload.</summary>
    const string CharSelectFlag = "-charselect-shots";

    string dir;
    int index;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (!HasArg(Flag)) return;
        var go = new GameObject("DiabloShotCheck");
        DontDestroyOnLoad(go);
        go.AddComponent<DiabloShotCheck>();
    }

    IEnumerator Start()
    {
        dir = ArgValue("-shotdir") ?? Path.Combine(Path.GetDirectoryName(Application.dataPath), "shotcheck");
        Directory.CreateDirectory(dir);
        foreach (var old in Directory.GetFiles(dir, "*.png")) File.Delete(old);
        Log($"start, output {dir}");

        // Wait for the owned hero (network host spawn) with its skills.
        DiabloClickController hero = null;
        float deadline = Time.realtimeSinceStartup + 30f;
        while (hero == null && Time.realtimeSinceStartup < deadline)
        {
            var go = GameObject.Find(DiabloNetworkPlayer.HeroName);
            if (go != null)
            {
                var c = go.GetComponent<DiabloClickController>();
                if (c != null && c.isActiveAndEnabled && go.GetComponent<DiabloSkills>() != null) hero = c;
            }
            yield return null;
        }
        if (hero == null)
        {
            Log("FAIL no controllable hero within 30 s");
            Quit(2);
            yield break;
        }
        DiabloClickController.ExternalDrive = true;
        HitStop.Enabled = false;
        var skills = hero.GetComponent<DiabloSkills>();
        yield return Wait(1.5f); // let the scene settle (fade-ins, first frames)

        float baseDist = hero.distance, basePitch = hero.pitch, baseYaw = hero.yaw;
        var camFwd = Quaternion.Euler(0f, baseYaw, 0f) * Vector3.forward;
        var camRight = Quaternion.Euler(0f, baseYaw, 0f) * Vector3.right;

        if (HasArg(EquipmentFlag))
        {
            yield return EquipmentShots(hero, -camFwd);
            DiabloClickController.ExternalDrive = false;
            Log($"DONE {index} equipment shots");
            Quit(0);
            yield break;
        }

        if (HasArg(LocomotionFlag))
        {
            yield return LocomotionShots(hero, camFwd, camRight);
            hero.SetMoveInput(Vector3.zero);
            DiabloClickController.ExternalDrive = false;
            Log($"DONE {index} locomotion shots");
            Quit(0);
            yield break;
        }

        if (HasArg(CharSelectFlag))
        {
            yield return CharSelectShots();
            DiabloClickController.ExternalDrive = false;
            Log($"DONE {index} charselect shots");
            Quit(0);
            yield break;
        }

        if (HasArg(MapFlag))
        {
            yield return MapShots(hero);
            DiabloClickController.ExternalDrive = false;
            Log($"DONE {index} map shots");
            Quit(0);
            yield break;
        }

        if (HasArg(StatBreakdownFlag))
        {
            yield return StatBreakdownShots(hero);
            DiabloClickController.ExternalDrive = false;
            Debug.Log($"[ShotCheck] DONE {index} stat-breakdown shots");
            Quit(0);
            yield break;
        }

        yield return CoreCombatShots(hero, skills, baseDist, basePitch, camFwd, camRight);
        yield return TownAndUiGalleryShots(hero);
        GameObject boss = null;
        yield return OssuaryAndAbbotShots(hero, camFwd, b => boss = b);
        if (boss == null) yield break;
        yield return ClassKitGalleryShots(hero, skills, boss);
        yield return ActZoneBossShots(hero);
        yield return ItemizationAndDeathShots(hero, baseDist, basePitch, camFwd);
        yield return ReloadAndNewHeroShots(hero);

        DiabloClickController.ExternalDrive = false;
        Log($"DONE {index} shots");
        Quit(0);
    }

    IEnumerator CoreCombatShots(DiabloClickController hero, DiabloSkills skills,
        float baseDist, float basePitch, Vector3 camFwd, Vector3 camRight)
    {
        // 1) Gameplay framing, idle with sword.
        yield return Shot("gameplay_idle");

        // 1b) Wide look over the moor: props, fires, fog.
        hero.distance = 24f;
        yield return Wait(0.5f);
        yield return Shot("world_overview");
        hero.distance = baseDist;
        yield return Wait(0.3f);

        // 2) Close side view (left side, bow hand, toward the camera).
        hero.distance = 5.5f;
        hero.pitch = 30f;
        Face(hero, -camRight);
        yield return Wait(0.6f);
        yield return Shot("close_idle_sword");

        skills.CastToward(DiabloSkills.Skill.Arrow, -camRight);
        yield return Wait(0.08f);
        yield return Shot("bow_release_early");
        yield return Wait(0.15f);
        yield return Shot("bow_release_mid");
        yield return Wait(0.8f);
        yield return Shot("bow_aim_hold");

        // 3) Three-quarter front view of the aim.
        Face(hero, (-camFwd - camRight).normalized);
        skills.CastToward(DiabloSkills.Skill.Arrow, (-camFwd - camRight).normalized);
        yield return Wait(0.2f);
        yield return Shot("bow_release_front");
        yield return Wait(0.9f);
        yield return Shot("bow_aim_front");
        yield return Wait(DiabloSkills.BowStanceSeconds + 0.3f);
        yield return Shot("bow_put_away");

        // 4) Fireball in flight, then a blast in frame.
        hero.distance = 8f;
        hero.pitch = 45f;
        Face(hero, -camRight);
        yield return Wait(0.4f);
        skills.CastToward(DiabloSkills.Skill.Fireball, -camRight);
        yield return Wait(0.18f);
        yield return Shot("fireball_flight");
        var blastAt = hero.transform.position - camRight * 3f + Vector3.up * 0.5f;
        DiabloProjectile.SpawnBlast(blastAt, skills.fireballBlastRadius);
        yield return Wait(0.06f);
        yield return Shot("fireball_blast");

        // 5) Combat juice at gameplay framing: numbers, crit, blood, embers.
        hero.distance = baseDist;
        hero.pitch = basePitch;
        yield return Wait(1.2f);
        var hitAt = hero.transform.position - camRight * 2.5f;
        DiabloJuice.OnEnemyHit(hitAt, 2, false);
        yield return Wait(0.12f);
        DiabloJuice.OnEnemyHit(hitAt + camFwd * 0.6f, 6, true);
        yield return Wait(0.1f);
        yield return Shot("damage_numbers_crit");
        DiabloJuice.OnEnemyDeath(hitAt);
        DiabloProgression.GrantLocal(DiabloProgression.XpNormal, hitAt);
        yield return Wait(0.15f);
        yield return Shot("death_burst_xp");

        // 5b) Melee readability (playtest 2026-11-23): the swing's real damage
        // cone with impact rings on two struck spots, then a pale thrust whiff.
        var heroMelee = hero.GetComponent<DiabloMelee>();
        float swingRange = heroMelee != null ? heroMelee.attackRange : 2.2f;
        Face(hero, -camRight);
        yield return Wait(0.3f);
        var heroAt = hero.transform.position;
        MeleeConeVfx.ShowCone(heroAt, -camRight, swingRange, DiabloMelee.ArcHalfAngle, true);
        MeleeConeVfx.ShowHitRing(heroAt - camRight * 1.5f + camFwd * 0.6f);
        MeleeConeVfx.ShowHitRing(heroAt - camRight * 1.6f - camFwd * 0.5f);
        yield return Wait(0.06f);
        yield return Shot("melee_cone_hit");
        yield return Wait(0.4f);
        Face(hero, camRight);
        yield return Wait(0.2f);
        MeleeConeVfx.ShowCone(hero.transform.position, camRight,
            DiabloMelee.OutreachRange, DiabloMelee.OutreachHalfAngle, false);
        yield return Wait(0.06f);
        yield return Shot("melee_thrust_whiff");
        yield return Wait(0.4f);

        // 6) Level-up moment (banner at its overshoot, pillar rising).
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null)
        {
            prog.AddXp(DiabloProgression.XpToNext(prog.Level) - prog.Xp);
            yield return Wait(0.3f);
            yield return Shot("level_up");
            yield return Wait(1.0f);
            yield return Shot("level_up_settled");
        }
    }

    IEnumerator TownAndUiGalleryShots(DiabloClickController hero)
    {
        // 7) Lantern Hold town framing (S3): wide view over camp + vendor.
        hero.distance = 16f;
        yield return Wait(0.5f);
        yield return Shot("town_lantern_hold");

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
            if (eq != null && bag != null && bag.Add(rival))
            {
                eq.Equip(worn);
                invUI.Refresh();
                yield return Wait(0.5f);
                yield return CompareShot(hero.gameObject, "InventoryCanvas", rival, "inventory_compare", $"BagButton{bag.Count - 1}");
                bag.RemoveAt(bag.Count - 1);
                if (prevWeapon != null) eq.Equip(prevWeapon);
                else if (eq.UnequipTo(ItemSlot.Weapon, bag)) bag.RemoveAt(bag.Count - 1);
                invUI.Refresh();
                yield return Wait(0.2f);
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
                var invUi = hero.GetComponent<DiabloInventoryUI>();
                if (invUi != null)
                {
                    invUi.SetOpen(true);
                    invUi.ShowTab(DiabloInventoryUI.MenuTab.Bag);
                    invUi.Refresh();
                    yield return Wait(0.4f);
                    yield return Shot("s5_inventory_details");
                    invUi.SetOpen(false);
                }
                var vendorUi = hero.GetComponent<DiabloVendorUI>();
                if (vendorUi != null)
                {
                    vendorUi.SetOpen(true);
                    yield return Wait(0.4f);
                    yield return Shot("s5_vendor");
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

    /// <summary>cinderreach-group-quest-map: tracker, minimap, zone map, shared-dialog listener, log.</summary>
    /// <summary>
    /// Character select visibility: full-body card figures, the creation preview
    /// for every class with gender and hair variants, then a new hero through the
    /// real scene reload (controller, camera and pause menu must come back).
    /// </summary>
    IEnumerator CharSelectShots()
    {
        var go = new GameObject("ShotCheckCharSelect");
        var ui = go.AddComponent<ClassSelectUI>();
        ui.SetOpen(true);
        yield return Wait(0.8f);
        yield return Shot("charselect_cards");
        foreach (var cls in ClassCatalog.All)
        {
            ui.OpenCustomize(cls.id);
            yield return Wait(0.6f);
            yield return Shot($"create_{cls.id}_{GenderTag(ui)}_{ui.DraftProfile.hairStyle}_{ui.DraftProfile.hairColor}");
            for (int step = 0; step < 2; step++)
            {
                ui.CycleGender();
                ui.CycleHair();
                ui.CycleHairColor();
                ui.CycleHairColor();
                ui.CycleHairColor();
                yield return Wait(0.5f);
                yield return Shot($"create_{cls.id}_{GenderTag(ui)}_{ui.DraftProfile.hairStyle}_{ui.DraftProfile.hairColor}");
            }
            ui.CancelCustomize();
            yield return Wait(0.2f);
        }

        // New hero mid-session: the solo world reloads (the reported grey screen).
        // A failed -join (server unreachable) falls back to solo first, as in the report.
        var nm = Unity.Netcode.NetworkManager.Singleton;
        float joinDeadline = Time.realtimeSinceStartup + 150f;
        while (nm != null && nm.IsListening && !nm.IsServer && !nm.IsConnectedClient && Time.realtimeSinceStartup < joinDeadline)
            yield return null;
        Log($"solo before new hero: {Diablo.IsSolo()}");
        ClassSelectUI.HeroInWorld = true;
        ui.OpenCustomize(ClassCatalog.RogueId);
        ui.SetDraftName("Reloadprobe");
        yield return Wait(0.3f);
        ui.StartCustomizedHero();
        float until = Time.realtimeSinceStartup + 30f;
        while (Time.realtimeSinceStartup < until && (SceneReload.InProgress || SaveService.NewHeroAfterReload != null)) yield return null;
        DiabloClickController ctrl = null;
        while (Time.realtimeSinceStartup < until && ctrl == null)
        {
            var h = GameObject.Find(DiabloNetworkPlayer.HeroName);
            var c = h != null ? h.GetComponent<DiabloClickController>() : null;
            if (c != null && c.isActiveAndEnabled) ctrl = c;
            else yield return null;
        }
        DiabloClickController.ExternalDrive = false;
        yield return Wait(1.5f);
        var cam = Camera.main;
        float camDist = ctrl != null && cam != null ? Vector3.Distance(cam.transform.position, ctrl.transform.position) : -1f;
        bool pause = Object.FindFirstObjectByType<PauseMenuUI>(FindObjectsInactive.Include) != null;
        bool ok = ctrl != null && camDist > 0f && camDist < 40f && pause && RunProfile.ClassId == ClassCatalog.RogueId;
        Log($"NEW_HERO_RELOAD {(ok ? "PASS" : "FAIL")} controller={(ctrl != null)} camDist={camDist:0.0} pause={pause} class={RunProfile.ClassId} name={RunProfile.Hero.heroName}");
        yield return Shot("after_new_hero_reload");
    }

    static string GenderTag(ClassSelectUI ui) => ((HeroGender)ui.DraftProfile.gender).ToString().ToLowerInvariant();

    IEnumerator MapShots(DiabloClickController hero)
    {
        var story = RunProfile.Story;
        story.Refresh(RunProfile.Act);
        story.Accept("h1");
        story.Refresh(RunProfile.Act);
        story.Accept("n1");
        yield return Wait(1.2f);
        int marks = hero.GetComponent<StoryDirector>().Marks.Count;
        Log($"MAP marks={marks} minimapIcons={hero.GetComponent<MinimapHud>().IconsShown} renders={ZoneMap.Renders}");
        yield return Shot("map_minimap_tracker");

        var map = hero.GetComponent<WorldMapUI>();
        map.SetOpen(true);
        yield return Wait(0.6f);
        Log($"MAP zone={map.ViewZone} icons={map.IconsShown}");
        yield return Shot("map_zone_fen");
        map.Cycle(1);
        yield return Wait(0.6f);
        yield return Shot("map_zone_ossuary");
        map.SetOpen(false);
        yield return Wait(0.3f);

        StoryDialogShare.Receive(99, "Alrik", SharedDialogOp.Open, "h1.offer", StoryIds.Ottilie, "0,1,2,3", 1);
        yield return Wait(0.5f);
        yield return Shot("dialog_shared_listener");
        StoryDialogShare.Receive(99, "Alrik", SharedDialogOp.Close, "h1.offer", StoryIds.Ottilie, "", 1);
        yield return Wait(0.3f);

        var log = hero.GetComponent<StoryQuestUI>();
        log.SetOpen(true);
        yield return Wait(0.5f);
        yield return Shot("quest_log_tracking");
        log.SetOpen(false);
    }

    IEnumerator EquipmentShots(DiabloClickController hero, Vector3 towardCamera)
    {
        var equipment = hero.GetComponent<Equipment>();
        var appearance = hero.GetComponent<ClassAppearance>();
        var inventory = hero.GetComponent<Inventory>();
        float distance = hero.distance;
        float pitch = hero.pitch;
        Face(hero, towardCamera);
        var heroLook = appearance.AppliedProfile;
        foreach (var cls in ClassCatalog.All)
            foreach (HeroGender gender in System.Enum.GetValues(typeof(HeroGender)))
            {
                var look = heroLook;
                look.gender = (int)gender;
                appearance.Apply(cls, look);
                yield return Wait(.35f);
                yield return Shot($"base_{cls.id}_{gender.ToString().ToLowerInvariant()}");
            }
        appearance.Apply(ClassCatalog.Fenwalker, heroLook);

        hero.distance = 5f;
        hero.pitch = 33f;
        foreach (var id in ItemDatabase.UniqueIds)
        {
            var item = ItemDatabase.CreateUnique(id);
            appearance.Apply(ClassCatalog.Get(ItemDatabase.ClassRestriction(item.baseType)));
            var facing = item.slot == ItemSlot.Weapon
                ? Quaternion.Euler(0, HeroGearModels.IsBowUnique(id) ? 45f : -45f, 0) * towardCamera
                : id == ItemDatabase.ShardbackAegisId ? -towardCamera : towardCamera;
            Face(hero, facing);
            equipment.Equip(item);
            yield return Wait(.35f);
            yield return Shot(id.Replace(' ', '-').Replace("'", ""));
            equipment.SetSlot(item.slot, null);
        }

        // gear-slots S7: every class in a full six-slot role set, front and back, then the paperdoll.
        foreach (var cls in ClassCatalog.All)
        {
            appearance.Apply(cls, heroLook);
            foreach (var slot in Equipment.GearSlots)
            {
                string uid = slot == ItemSlot.Weapon ? ItemDatabase.ZoneWeaponFor(cls.id)
                    : HeroGearModels.IsLimbSlot(slot) ? ItemDatabase.RoleUniquesFor(slot)[(int)cls.role]
                    : ItemDatabase.UniqueForMatrix(cls.role, slot);
                equipment.Equip(ItemDatabase.CreateUnique(uid));
            }
            Face(hero, towardCamera);
            yield return Wait(.4f);
            yield return Shot($"gearset_{cls.id}_front");
            Face(hero, -towardCamera);
            yield return Wait(.3f);
            yield return Shot($"gearset_{cls.id}_back");
            Face(hero, towardCamera);
            var ui = hero.GetComponent<DiabloInventoryUI>() ?? hero.gameObject.AddComponent<DiabloInventoryUI>();
            ui.SetOpen(true);
            ui.Refresh();
            yield return Wait(.5f);
            yield return Shot($"gearset_{cls.id}_paperdoll");
            ui.SetOpen(false);
            foreach (var slot in Equipment.GearSlots) equipment.SetSlot(slot, null);
        }
        appearance.Apply(ClassCatalog.Fenwalker, heroLook);
        hero.distance = distance;
        hero.pitch = pitch;
    }

    static void TeleportHero(DiabloClickController hero, Vector3 pos)
    {
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = pos;
        if (cc != null) cc.enabled = true;
        hero.SetDestination(pos);
        PartyRoster.PortWithLeader(hero.gameObject, pos); // companions travel along, like ZoneTravel
    }

    /// <summary>
    /// Split-body locomotion check: the hero aims to screen right (profile)
    /// and runs forward, toward the camera (strafe), backward and stops.
    /// Each pose logs Animator state and foot heights above the hero root.
    /// </summary>
    IEnumerator LocomotionShots(DiabloClickController hero, Vector3 camFwd, Vector3 camRight)
    {
        hero.distance = 6f;
        hero.pitch = 25f;
        var poses = new (string name, Vector3 move)[]
        {
            ("loco_run_forward", camRight),
            ("loco_strafe", -camFwd),
            ("loco_backpedal", -camRight),
            ("loco_stop", Vector3.zero),
        };
        foreach (var (name, move) in poses)
        {
            Face(hero, camRight);
            hero.SetMoveInput(move);
            for (int i = 0; i < 4; i++)
            {
                Face(hero, camRight);
                yield return Wait(i == 0 ? 0.6f : 0.12f);
                LogBones(hero, $"{name}_{i}");
                yield return Shot($"{name}_{i}");
            }
        }
        yield return Wait(0.8f);
        LogBones(hero, "loco_stop_settled");
        yield return Shot("loco_stop_settled");
        hero.distance = 11f;
        hero.pitch = 55f;
    }

    static void LogBones(DiabloClickController hero, string tag)
    {
        var anim = hero.GetComponent<Animator>();
        var twist = hero.GetComponent<LocomotionTwist>();
        string state = "?";
        float nt = 0f;
        if (anim != null && anim.runtimeAnimatorController != null)
        {
            var info = anim.GetCurrentAnimatorStateInfo(0);
            foreach (var s in new[] { "Idle", "Run", "Strike", "BowAim", "BowShoot" })
                if (info.IsName(s)) state = s;
            nt = info.normalizedTime;
            if (anim.IsInTransition(0)) state += "*";
        }
        float baseY = hero.transform.position.y;
        string H(string bone)
        {
            var b = FindBone(hero.transform, bone);
            return b == null ? "-" : (b.position.y - baseY).ToString("F2");
        }
        Log($"bones {tag}: state={state} t={nt:F2} speed={(anim != null ? anim.GetFloat(LocomotionTwist.MoveSpeedParam) : 0f):F1} twist={(twist != null ? twist.Twist : 0f):F0} back={(twist != null && twist.Backpedaling)} vel={(twist != null ? twist.Velocity.magnitude : 0f):F1} hips={H("hips")} footL={H("foot.l")} footR={H("foot.r")} lowL={H("lowerleg.l")} lowR={H("lowerleg.r")}");
    }

    static Transform FindBone(Transform t, string name)
    {
        if (t.name == name) return t;
        foreach (Transform c in t)
        {
            var hit = FindBone(c, name);
            if (hit != null) return hit;
        }
        return null;
    }

    static void Face(DiabloClickController hero, Vector3 dir)
    {
        hero.SetAimPoint(hero.transform.position + dir * 4f);
    }

    /// <summary>Shotcheck cast: clear the cooldown, then cast (class-kits-parity poses).</summary>
    static void KitCast(DiabloSkills skills, DiabloSkills.Skill s, Vector3 dir)
    {
        skills.ReduceCooldown(s, 999f);
        skills.CastToward(s, dir);
    }

    IEnumerator Shot(string name)
    {
        yield return new WaitForEndOfFrame();
        index++;
        string path = Path.Combine(dir, $"{index:00}_{name}.png");
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Destroy(tex);
        Log($"shot {path}");
    }

    /// <summary>Hero kept at full life while set (act-one Kiln shots: the zone mobs on arrival).</summary>
    static PlayerHealth keepAlive;

    /// <summary>
    /// cinderreach-act-boss-gating: marks the act's other quests done (no
    /// toasts), lets the gate wake the boss and shoots its spawn cutscene
    /// right after the first ability showcase, then skips back to the hero.
    /// </summary>
    IEnumerator SpawnCutsceneShot(int act, string shot)
    {
        var parts = new System.Collections.Generic.List<string>();
        foreach (var id in ActBossGate.RequiredQuests(act)) parts.Add(id + ":" + StoryStatus.Done + ":");
        RunProfile.Story.Merge(string.Join(";", parts));
        RunProfile.Story.TakeChanges();
        ActBossGate.Tick(Time.unscaledTime);
        if (!CutsceneDirector.Active)
        {
            Debug.LogWarning($"[ShotCheck] {shot}: no spawn cutscene (act {act}).");
            yield break;
        }
        string id0 = CutsceneDirector.ActiveId;
        float until = Time.unscaledTime + 6f;
        while (CutsceneDirector.CurrentAbilityKey.Length == 0 && Time.unscaledTime < until) yield return null;
        yield return Wait(0.6f); // let the first cast's telegraph bloom
        Debug.Log($"[ShotCheck] {shot}: {id0} cast={CutsceneDirector.CurrentAbilityKey} banner={CutsceneDirector.BannerText}");
        yield return Shot(shot);
        CutsceneDirector.Skip();
        yield return Wait(0.3f);
    }

    static IEnumerator Wait(float seconds)
    {
        // Unscaled: the pause shot freezes Time.timeScale (solo pause).
        float end = Time.unscaledTime + seconds;
        while (Time.unscaledTime < end)
        {
            if (keepAlive != null) keepAlive.Heal(keepAlive.MaxHP);
            yield return null;
        }
    }

    static void Log(string msg) => Debug.Log($"[ShotCheck] {msg}");

    static void Quit(int code)
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit(code);
#endif
    }

    static bool HasArg(string flag)
    {
        foreach (var a in System.Environment.GetCommandLineArgs())
            if (a == flag) return true;
        return false;
    }

    static string ArgValue(string flag)
    {
        var args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == flag) return args[i + 1];
        return null;
    }

    static ItemInstance GalleryItem(string baseType, ItemRarity rarity, params (string key, float value)[] affixes)
    {
        var item = new ItemInstance { slot = ItemSlot.Weapon, rarity = rarity, baseType = baseType, itemLevel = 6 };
        foreach (var (k, v) in affixes) item.affixes.Add(new Affix(k, v));
        return item;
    }

    static ItemInstance VendorCompareItem(GameObject hero)
    {
        var vendor = hero.GetComponent<DiabloVendorUI>();
        var stock = vendor != null ? vendor.Stock : null;
        if (stock != null)
            foreach (var offer in stock.offers)
                if (offer.item != null && offer.item.slot != ItemSlot.Consumable) return offer.item;
        return null;
    }

    /// <summary>Hold Shift over an item: the shared tooltip with both cards and arrows.</summary>
    IEnumerator CompareShot(GameObject hero, string canvasName, ItemInstance item, string shotName, string anchorName = null)
    {
        if (item == null) yield break;
        var tip = ItemTooltip.For(hero);
        Vector2 pin = new Vector2(Screen.width * 0.30f, Screen.height * 0.72f);
        var canvasT = hero.transform.Find(canvasName);
        if (canvasT != null && anchorName != null)
            foreach (var t in canvasT.GetComponentsInChildren<RectTransform>())
                if (t.name == anchorName) { pin = t.position; break; }
        ItemTooltip.ShiftOverride = true;
        tip.PinnedScreen = pin;
        tip.Show(item, hero.GetComponent<Equipment>());
        yield return Wait(0.35f);
        yield return Shot(shotName);
        tip.Hide();
        tip.PinnedScreen = null;
        ItemTooltip.ShiftOverride = null;
    }

    IEnumerator StatBreakdownShots(DiabloClickController hero)
    {
        var eq = hero.GetComponent<Equipment>();
        var bag = hero.GetComponent<Inventory>();
        var prog = hero.GetComponent<DiabloProgression>();
        if (prog != null && prog.Sheet != null)
        {
            for (int i = 0; i < 4; i++) prog.Sheet.AwardLevel();
            prog.Sheet.SpendAttribute(CharacterSheet.Attribute.Strength);
            prog.Sheet.SpendAttribute(CharacterSheet.Attribute.Strength);
            prog.Sheet.SpendAttribute(CharacterSheet.Attribute.Vitality);
            prog.Sheet.SpendAttribute(CharacterSheet.Attribute.Focus);
            prog.Sheet.SpendSkill(RunProfile.Class.skills[0]);
            prog.Sheet.SpendSkill(RunProfile.Class.skills[2]);
        }

        var prevWeapon = eq != null ? eq.weapon : null;
        var prevArmor = eq != null ? eq.armor : null;
        var prevBelt = eq != null ? eq.belt : null;

        var weapon = GalleryItem(ItemDatabase.StarterSwordBase, ItemRarity.Rare,
            (AffixLibrary.Damage, 5f), (AffixLibrary.FireDamage, 3f), (AffixLibrary.Crit, 8f));
        var armor = new ItemInstance { slot = ItemSlot.Armor, rarity = ItemRarity.Rare, baseType = "Breastplate", itemLevel = 10 };
        armor.affixes.Add(new Affix(AffixLibrary.MaxHP, 24f));
        armor.affixes.Add(new Affix(AffixLibrary.Armor, 4f));
        var belt = new ItemInstance { slot = ItemSlot.Belt, rarity = ItemRarity.Rare, baseType = "WarBelt", itemLevel = 10 };
        belt.affixes.Add(new Affix(AffixLibrary.CooldownReduction, 10f));
        belt.affixes.Add(new Affix(AffixLibrary.FlaskHealing, 18f));
        var rival = GalleryItem(ItemDatabase.StarterSwordBase, ItemRarity.Rare,
            (AffixLibrary.Damage, 7f), (AffixLibrary.Crit, 4f), (AffixLibrary.LifeSteal, 5f));

        if (eq != null)
        {
            eq.Equip(weapon);
            eq.Equip(armor);
            eq.Equip(belt);
        }
        bool addedRival = bag != null && bag.Add(rival);

        var invUi = hero.GetComponent<DiabloInventoryUI>() ?? hero.gameObject.AddComponent<DiabloInventoryUI>();
        invUi.SetOpen(true);
        invUi.Refresh();
        yield return Wait(0.45f);
        yield return Shot("stat_breakdown_inventory");

        // Parameter tooltip pinned next to ParametersColumn.
        var infoTip = InfoTooltip.For(hero.gameObject);
        var snap = CharacterStatBreakdown.BuildFor(hero.gameObject);
        var cdrEntry = snap.Find("cooldown_reduction") ?? snap.Find("max_hp");
        var paramRow = hero.transform.Find("InventoryCanvas/Panel/InventoryPage/ParametersColumn/ParametersCard/ParametersScroll/Viewport/ParametersContent/ParamRow_" + cdrEntry.id) as RectTransform;
        infoTip.PinnedScreen = paramRow != null ? (Vector2)paramRow.position : new Vector2(Screen.width * 0.50f, Screen.height * 0.55f);
        infoTip.Show(cdrEntry.name, cdrEntry.formula, cdrEntry.RenderTooltipBody(), paramRow != null ? paramRow.gameObject : null, cdrEntry.capNote);
        yield return Wait(0.35f);
        yield return Shot("stat_breakdown_param_tip");
        infoTip.Hide();
        infoTip.PinnedScreen = null;

        // Extended ItemTooltip with roll spans + Shift-compare.
        if (addedRival)
        {
            yield return CompareShot(hero.gameObject, "InventoryCanvas", rival, "stat_breakdown_item_tip", $"BagButton{bag.Count - 1}");
            bag.RemoveAt(bag.Count - 1);
        }
        invUi.SetOpen(false);
        yield return Wait(0.2f);

        // Character sheet skill tooltip pinned.
        var charUi = hero.GetComponent<DiabloCharacterUI>() ?? hero.gameObject.AddComponent<DiabloCharacterUI>();
        charUi.SetOpen(true);
        charUi.Refresh();
        yield return Wait(0.35f);
        var skillData = SkillBreakdown.BuildFor(hero.gameObject, RunProfile.Class.skills[2]);
        var skillRow = hero.transform.Find("CharacterCanvas/Panel/SkillRowsContainer/Row" + RunProfile.Class.keys[2]) as RectTransform;
        infoTip.PinnedScreen = skillRow != null ? (Vector2)skillRow.position - new Vector2(390f, -60f) : new Vector2(Screen.width * 0.45f, Screen.height * 0.55f);
        infoTip.Show(skillData.title, skillData.meta, skillData.body, skillRow != null ? skillRow.gameObject : null);
        yield return Wait(0.35f);
        yield return Shot("stat_breakdown_skill_tip");
        infoTip.Hide();
        infoTip.PinnedScreen = null;
        charUi.SetOpen(false);

        if (eq != null)
        {
            eq.SetSlot(ItemSlot.Weapon, prevWeapon);
            eq.SetSlot(ItemSlot.Armor, prevArmor);
            eq.SetSlot(ItemSlot.Belt, prevBelt);
        }
        invUi.Refresh();
    }
}
