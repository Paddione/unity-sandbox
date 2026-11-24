using UnityEngine;

/// <summary>
/// Lantern Hold stash chest (replayability S5). Click inside
/// <see cref="InteractRadius"/> opens the hero's <see cref="DiabloStashUI"/>,
/// same interaction as the <see cref="Vendor"/>. Storage is the
/// <see cref="RunProfile.Stash"/> (40 slots, persisted in save v2).
/// Item moves are pure statics so tests need no scene.
/// </summary>
public sealed class StashChest : MonoBehaviour, IUsable
{
    public const float InteractRadius = 3.5f;
    /// <summary>Scene object name (pinned by VerifyDiabloZone).</summary>
    public const string ObjectName = "StashChest";

    public static bool CanInteract(Vector3 heroPos, Vector3 chestPos) =>
        Vendor.CanInteract(heroPos, chestPos, InteractRadius);

    /// <summary>Move bag[bagIndex] into the stash. False when the index is empty or the stash is full.</summary>
    public static bool Store(Inventory bag, Stash stash, int bagIndex)
    {
        if (bag == null || stash == null) return false;
        var item = bag.Get(bagIndex);
        if (item == null || stash.FreeSlots <= 0) return false;
        if (!bag.RemoveAt(bagIndex)) return false;
        stash.Add(item);
        Debug.Log($"[Stash] Stored {LootDrop.ItemLabel(item)} ({stash.Count}/{Stash.Capacity}).");
        return true;
    }

    /// <summary>Move stash[stashIndex] into the bag. False when the index is empty or the bag is full.</summary>
    public static bool Take(Stash stash, Inventory bag, int stashIndex)
    {
        if (bag == null || stash == null) return false;
        var item = stash.Get(stashIndex);
        if (item == null || bag.FreeSlots <= 0) return false;
        if (!stash.RemoveAt(stashIndex)) return false;
        bag.Add(item);
        return true;
    }

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || Camera.main == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        var hero = GameObject.Find("Player");
        if (hero == null) return;
        if (DiabloUIFactory.IsWorldInputBlocked(hero)) return;
        var heroUI = hero.GetComponent<DiabloStashUI>();
        if (heroUI != null && heroUI.IsOpen) return;
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!CanInteract(hero.transform.position, transform.position)) return;
        Use(hero);
    }

    public float UseRadius => InteractRadius;
    public string UseLabel => Loc.T("use.stash");
    void OnEnable() => Usables.Register(this);
    void OnDisable() => Usables.Unregister(this);

    public void Use(GameObject hero)
    {
        if (hero == null) return;
        var heroUI = hero.GetComponent<DiabloStashUI>();
        if (heroUI == null) heroUI = hero.AddComponent<DiabloStashUI>();
        heroUI.SetOpen(true);
        Debug.Log("[Stash] Chest opened.");
    }
}
