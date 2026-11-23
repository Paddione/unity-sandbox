using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Waypoint shrine (act-one S5): one per zone (Lantern Hold, Ashen Fen,
/// Sunken Ossuary, Cinder Kiln). Standing close discovers it (saved in
/// <see cref="ActProgress.Waypoints"/>); clicking it opens
/// <see cref="DiabloWaypointUI"/>, which lists every discovered shrine whose
/// zone is unlocked and travels through <see cref="ZoneTravel"/>.
/// </summary>
public sealed class WaypointShrine : MonoBehaviour
{
    public const float DiscoverRadius = 2.5f;
    public const float ArrivalOffset = 2f;

    public string shrineId = "Town";
    public string displayName = "Lantern Hold";
    public int zone = ActProgress.Town;

    public Vector3 Arrival => transform.position + Vector3.forward * ArrivalOffset + Vector3.up * 0.05f;

    /// <summary>All shrines in the loaded scenes (scene search: OnEnable does not run for EditMode AddComponent).</summary>
    public static WaypointShrine[] All() => Object.FindObjectsByType<WaypointShrine>(FindObjectsSortMode.None);

    void Update()
    {
        var heroT = HeroLocator.Solo();
        if (heroT == null) return;
        var hero = heroT.gameObject;
        TryDiscover(hero);
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || Camera.main == null || !mouse.leftButton.wasPressedThisFrame) return;
        if (DiabloUIFactory.IsWorldInputBlocked(hero)) return;
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f) || hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!Vendor.CanInteract(hero.transform.position, transform.position, DiscoverRadius + 1f)) return;
        var ui = hero.GetComponent<DiabloWaypointUI>();
        if (ui == null) ui = hero.AddComponent<DiabloWaypointUI>();
        ui.SetOpen(true);
    }

    /// <summary>Discover when the hero is close (headless-callable). True on first discovery.</summary>
    public bool TryDiscover(GameObject hero)
    {
        if (hero == null) return false;
        var d = hero.transform.position - transform.position;
        d.y = 0f;
        if (d.magnitude > DiscoverRadius) return false;
        if (!RunProfile.Act.Discover(shrineId)) return false;
        DiabloAudio.PlayShrine();
        Debug.Log($"[Waypoint] Discovered {displayName}.");
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero);
        return true;
    }

    public static WaypointShrine Find(string id)
    {
        foreach (var s in All())
            if (s != null && s.shrineId == id) return s;
        return null;
    }

    /// <summary>Travel targets: discovered shrines in unlocked zones.</summary>
    public static List<WaypointShrine> Destinations()
    {
        var list = new List<WaypointShrine>();
        foreach (var s in All())
            if (s != null && RunProfile.Act.Waypoints.Contains(s.shrineId) && RunProfile.Act.IsUnlocked(s.zone))
                list.Add(s);
        list.Sort((a, b) => a.zone.CompareTo(b.zone));
        return list;
    }

    /// <summary>Shrines that are not destinations yet (undiscovered or zone sealed), zone order.</summary>
    public static List<WaypointShrine> Locked()
    {
        var open = Destinations();
        var list = new List<WaypointShrine>();
        foreach (var s in All())
            if (s != null && !open.Contains(s)) list.Add(s);
        list.Sort((a, b) => a.zone.CompareTo(b.zone));
        return list;
    }

    /// <summary>Travel to a shrine (UI button / autoplay). False when not a destination.</summary>
    public static bool Travel(string id, GameObject hero)
    {
        var target = Find(id);
        if (target == null || hero == null || !Destinations().Contains(target)) return false;
        ZoneTravel.TeleportHero(hero, target.Arrival);
        try
        {
            if (target.shrineId == "Town") MusicDirector.Ensure().PlayTown();
            else MusicDirector.Ensure().PlayZone(target.zone);
        }
        catch { }
        if (target.zone == ActProgress.Kiln) KilnLayout.ResetPlaza(); // fair-fights: re-arm arrival safety
        if (target.zone == ActProgress.Barrens) BarrensLayout.ResetPlaza();
        Debug.Log($"[Waypoint] Travelled to {target.displayName}.");
        if (Diablo.IsLocalAuthority()) SaveService.SaveNow(hero, zoneReached: target.zone);
        return true;
    }
}
