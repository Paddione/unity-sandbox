using UnityEngine;

/// <summary>
/// Lantern Hold vendor NPC (S3 town): KayKit Adventurer with an idle take.
/// Opens the hero's <see cref="DiabloVendorUI"/> on click while the hero is
/// inside <see cref="InteractRadius"/>. Potion drinking (key 1, wired from
/// the vendor UI) lives here as a static so the consumable never passes
/// through <see cref="Equipment"/> (S4-owned): potions are bag-only.
/// </summary>
public sealed class Vendor : MonoBehaviour
{
    /// <summary>Trade reach around the stall (click opens only inside).</summary>
    public const float InteractRadius = 3.5f;

    /// <summary>Scene object name (pinned by VerifyDiabloZone).</summary>
    public const string VendorObjectName = "Vendor";

    /// <summary>Stock seed for this zone visit (re-rolled per visit).</summary>
    public int zoneSeed = 7;

    public VendorStock stock;

    void Awake()
    {
        if (stock == null) stock = VendorStock.Roll(zoneSeed);
    }

    /// <summary>Re-roll the assortment for a new zone visit.</summary>
    public void Restock(int seed)
    {
        zoneSeed = seed;
        stock = VendorStock.Roll(seed);
    }

    /// <summary>
    /// Per-player stock seed (S4 co-op): the host (id 0) keeps the plain zone
    /// seed, every joined client derives its own assortment from it. Same
    /// input, same stock — the host recomputes this formula when it validates
    /// a purchase, so no stock ever replicates.
    /// </summary>
    public static int StockSeed(int zoneSeed, ulong clientId) =>
        zoneSeed ^ (int)(clientId & 0xffffffff);

    /// <summary>This visit's assortment for one player (host or client).</summary>
    public VendorStock StockFor(ulong clientId) => VendorStock.Roll(StockSeed(zoneSeed, clientId));

    /// <summary>Pure trade-reach rule (EditMode-pinnable, no scene).</summary>
    public static bool CanInteract(Vector3 heroPos, Vector3 vendorPos, float radius = InteractRadius)
    {
        var d = heroPos - vendorPos;
        d.y = 0f;
        return d.sqrMagnitude <= radius * radius;
    }

    void Update()
    {
        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (mouse == null || Camera.main == null) return;
        if (!mouse.leftButton.wasPressedThisFrame) return;
        var hero = GameObject.Find("Player");
        if (hero == null) return;
        if (DiabloUIFactory.IsWorldInputBlocked(hero)) return;
        var heroUI = hero.GetComponent<DiabloVendorUI>();
        if (heroUI != null && heroUI.IsOpen) return; // UI eats its own clicks
        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out var hit, 200f)) return;
        if (hit.collider == null || hit.collider.gameObject != gameObject) return;
        if (!CanInteract(hero.transform.position, transform.position)) return;
        if (StoryDirector.TryOpenTalk(hero, StoryIds.Ilsabe)) return; // story talk first, shop on the next click
        if (heroUI == null) heroUI = hero.AddComponent<DiabloVendorUI>();
        heroUI.BindStock(stock);
        heroUI.SetOpen(true);
        Debug.Log("[Vendor] Trade window opened.");
    }

    /// <summary>
    /// Drink the first healing potion in the hero's bag (key 1). False when
    /// the bag holds none (nothing happens, no sound).
    /// </summary>
    public static bool DrinkPotion(GameObject hero)
    {
        if (hero == null) return false;
        var inv = hero.GetComponent<Inventory>();
        if (inv == null) return false;
        for (int i = 0; i < inv.Count; i++)
        {
            var item = inv.Get(i);
            if (item != null && item.slot == ItemSlot.Consumable)
            {
                var relay = hero.GetComponent<DiabloNetworkPlayer>();
                if (relay != null && relay.IsSpawned)
                    return relay.RequestItemAction(DiabloNetworkPlayer.ItemAction.DrinkPotion, item);
                if (!inv.RemoveAt(i)) return false;
                var health = hero.GetComponent<PlayerHealth>();
                if (health != null) health.Heal(ItemDatabase.PotionHeal);
                DiabloAudio.PlayUIClick();
                Debug.Log($"[Vendor] Drank healing potion (+{ItemDatabase.PotionHeal} HP).");
                return true;
            }
        }
        return false;
    }
}
