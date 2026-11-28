using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>Shotcheck part: Character select: cards, creation preview per class/gender/hair (-charselect-shots).</summary>
public sealed partial class DiabloShotCheck
{
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
}
