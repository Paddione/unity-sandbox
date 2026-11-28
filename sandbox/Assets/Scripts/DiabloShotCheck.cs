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
public sealed partial class DiabloShotCheck : MonoBehaviour
{
    public const string Flag = "-shotcheck";
    const string EquipmentFlag = "-equipment-shots";
    /// <summary>Only the split-body locomotion poses (run, strafe, backpedal, stop) with bone logs.</summary>
    const string LocomotionFlag = "-locomotion-shots";
    const string MapFlag = "-map-shots";
    const string StatBreakdownFlag = "-stat-breakdown-shots";
    /// <summary>Character select only: cards, creation preview per class/gender/hair, new hero via reload.</summary>
    const string CharSelectFlag = "-charselect-shots";
    const string AbilityVfxFlag = "-ability-vfx-shots";

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

        if (HasArg(AbilityVfxFlag))
        {
            yield return AbilityVfxShots(hero, skills, camFwd, camRight);
            DiabloClickController.ExternalDrive = false;
            Debug.Log($"[ShotCheck] DONE {index} ability-vfx shots");
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
        yield return HudRedesignShots(hero);

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

    static void TeleportHero(DiabloClickController hero, Vector3 pos)
    {
        var cc = hero.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
        hero.transform.position = pos;
        if (cc != null) cc.enabled = true;
        hero.SetDestination(pos);
        PartyRoster.PortWithLeader(hero.gameObject, pos); // companions travel along, like ZoneTravel
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

}
