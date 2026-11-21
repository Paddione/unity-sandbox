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

        // 7) Lantern Hold town framing (S3): wide view over camp + vendor.
        hero.distance = 16f;
        yield return Wait(0.5f);
        yield return Shot("town_lantern_hold");

        // 8) Town & Sanctuary UI Gallery (Vendor, Stash, Waypoint, Inventory, Bag, Settings, Character, Pause).
        var bagComp = hero.GetComponent<Inventory>();
        var eqComp = hero.GetComponent<Equipment>();
        var walComp = hero.GetComponent<Wallet>();
        if (walComp != null && walComp.Gold < 250) walComp.Add(250 - walComp.Gold);
        if (bagComp != null && bagComp.Count < 4)
        {
            bagComp.Add(ItemDatabase.CreateUnique("Emberbrand Blade"));
            bagComp.Add(ItemDatabase.CreateUnique("Cryptwarden Plate"));
            bagComp.Add(ItemDatabase.CreateHealingPotion());
        }

        var vendorUI = hero.GetComponent<DiabloVendorUI>();
        if (vendorUI == null) vendorUI = hero.gameObject.AddComponent<DiabloVendorUI>();
        vendorUI.SetOpen(true);
        yield return Wait(0.4f);
        yield return Shot("vendor_open");
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

        var pauseGo = new GameObject("ShotCheckPause");
        var pauseUI = pauseGo.AddComponent<PauseMenuUI>();
        pauseUI.SetOpen(true);
        yield return Wait(0.35f);
        yield return Shot("pause_menu");
        pauseUI.SetOpen(false);
        Destroy(pauseGo);
        DiabloClickController.ExternalDrive = true;
        yield return Wait(0.2f);

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
        keepAlive = null;

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
                    rarity = ItemRarity.Magic,
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
                    rarity = ItemRarity.Magic,
                    baseType = "ClothTunic",
                    itemLevel = 3,
                    affixes = AffixLibrary.RollAffixes(ItemSlot.Armor, ItemRarity.Magic, rng, 1, 3),
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

        DiabloClickController.ExternalDrive = false;
        Log($"DONE {index} shots");
        Quit(0);
    }

    static void TeleportHero(DiabloClickController hero, Vector3 pos)
    {
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = pos;
        if (cc != null) cc.enabled = true;
        hero.SetDestination(pos);
    }

    static void Face(DiabloClickController hero, Vector3 dir)
    {
        hero.SetAimPoint(hero.transform.position + dir * 4f);
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
}
